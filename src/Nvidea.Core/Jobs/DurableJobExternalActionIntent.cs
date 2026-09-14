using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Coordinates durable intent for an external side effect whose delivery can become ambiguous
/// across process failure. The intent is persisted alongside its exact audit authority, the audit
/// is made durable before provider delivery, and the intent is cleared only after a caller has
/// independently established that delivery no longer requires reconciliation.
///
/// This class intentionally never interprets a persisted intent as provider success.
/// </summary>
internal sealed class DurableJobExternalActionIntent
{
    private const int MaxClearAttempts = 4;
    private const int MaxTargetIdLength = 512;

    private readonly JsonAgentJobStore _store;
    private readonly DurableJobAuditOutbox _auditOutbox;

    public DurableJobExternalActionIntent(JsonAgentJobStore store, IAuditTrail auditTrail)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _auditOutbox = new DurableJobAuditOutbox(store, auditTrail ?? throw new ArgumentNullException(nameof(auditTrail)));
    }

    public AgentJobRecord Stage(
        AgentJobRecord replacement,
        AuditEvent auditEvent,
        DurableExternalActionKind kind,
        string targetId,
        DateTimeOffset? createdAt = null)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(auditEvent);
        if (replacement.PendingExternalAction is not null)
            throw new InvalidOperationException("A job cannot stage a second external action while one is pending reconciliation.");

        ValidateTargetId(targetId);
        var withAudit = _auditOutbox.Stage(replacement, auditEvent);
        return withAudit with
        {
            PendingExternalAction = new PendingExternalAction(
                Guid.NewGuid(),
                kind,
                targetId,
                auditEvent.EventId,
                createdAt ?? DateTimeOffset.UtcNow)
        };
    }

    /// <summary>
    /// Persists a new external-action intent and its exact audit authority, or reuses an already
    /// durable equivalent intent after restart. Existing intent always wins over a newly prepared
    /// audit event: callers must not manufacture another delivery identity merely because a prior
    /// provider call failed or its result became ambiguous.
    /// </summary>
    public async Task<AgentJobRecord> StageOrReuseAndFlushAsync(
        AgentJobRecord current,
        AuditEvent newAuditEvent,
        DurableExternalActionKind kind,
        string targetId,
        DateTimeOffset? createdAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(newAuditEvent);
        ValidateTargetId(targetId);

        if (current.PendingExternalAction is not null)
        {
            ValidatePending(current, kind, targetId);
            return current.PendingAuditEvent is null
                ? current
                : await FlushAuditAsync(current, cancellationToken).ConfigureAwait(false);
        }

        var staged = Stage(current, newAuditEvent, kind, targetId, createdAt);
        if (!await _store.CompareExchangeAsync(current, staged, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Job state changed while the external action intent was being staged.");

        return await FlushAuditAsync(staged, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the exact pending action only when both its semantic kind and provider target match
    /// the freshly authenticated lifecycle context. This is a fail-closed substitution boundary:
    /// a stale/corrupt action can never redirect a later provider call or be silently cleared.
    /// </summary>
    public PendingExternalAction ValidatePending(
        AgentJobRecord record,
        DurableExternalActionKind expectedKind,
        string expectedTargetId)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateTargetId(expectedTargetId);

        var pending = record.PendingExternalAction
            ?? throw new InvalidOperationException("No durable external action is pending.");
        if (pending.Kind != expectedKind)
            throw new InvalidDataException("Pending external action kind does not match the expected provider operation.");
        if (!string.Equals(pending.TargetId, expectedTargetId, StringComparison.Ordinal))
            throw new InvalidDataException("Pending external action target does not match authenticated remote provenance.");
        if (pending.ActionId == Guid.Empty || pending.AuditEventId == Guid.Empty)
            throw new InvalidDataException("Pending external action identity is invalid.");
        if (pending.CreatedAt == default)
            throw new InvalidDataException("Pending external action creation time is invalid.");
        if (record.PendingAuditEvent is not null && record.PendingAuditEvent.EventId != pending.AuditEventId)
            throw new InvalidDataException("Pending external action is bound to a different audit event.");

        return pending;
    }

    /// <summary>
    /// Makes the exact audit bound to the pending external action durable. The action intent remains
    /// persisted after this returns and must not be cleared merely because an attempted provider call
    /// returned or threw; callers clear only after provider state makes replay unnecessary.
    /// </summary>
    public async Task<AgentJobRecord> FlushAuditAsync(
        AgentJobRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var action = record.PendingExternalAction
            ?? throw new InvalidOperationException("No durable external action is pending.");
        var audit = record.PendingAuditEvent;
        if (audit is not null && audit.EventId != action.AuditEventId)
            throw new InvalidDataException("Pending external action is bound to a different audit event.");

        return await _auditOutbox.FlushAsync(record, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears one exact durable intent after fresh provider reconciliation proves that replay is no
    /// longer needed. Concurrent state changes are re-read and accepted only when the same action was
    /// already cleared; a different pending action always fails closed.
    /// </summary>
    public async Task<AgentJobRecord> ClearAsync(
        AgentJobRecord record,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (actionId == Guid.Empty)
            throw new ArgumentException("External action id is required.", nameof(actionId));

        var current = record;
        for (var attempt = 0; attempt < MaxClearAttempts; attempt++)
        {
            var pending = current.PendingExternalAction;
            if (pending is null)
                return current;
            if (pending.ActionId != actionId)
                throw new InvalidOperationException("Pending external action changed during reconciliation; refusing to clear a different action.");
            if (current.PendingAuditEvent is not null)
                throw new InvalidOperationException("External action intent cannot be cleared before its bound audit is durable.");

            var cleared = current with { PendingExternalAction = null };
            if (await _store.CompareExchangeAsync(current, cleared, cancellationToken).ConfigureAwait(false))
                return cleared;

            current = await _store.GetAsync(current.JobId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Job disappeared while external action intent was being cleared.");

            if (current.PendingExternalAction is null)
                return current;
        }

        throw new InvalidOperationException("Pending external action could not be cleared after bounded concurrent reconciliation attempts.");
    }

    private static void ValidateTargetId(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            throw new ArgumentException("External action target id is required.", nameof(targetId));
        if (targetId.Length > MaxTargetIdLength || targetId.Any(char.IsControl))
            throw new ArgumentException("External action target id is invalid.", nameof(targetId));
    }
}
