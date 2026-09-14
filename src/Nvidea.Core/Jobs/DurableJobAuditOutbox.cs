using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Couples a job-state transition to its audit intent by storing the exact validated event inside
/// the same durable job record before the transition is considered fully settled. Recovery first
/// proves the event is present in the append-only audit trail, then CAS-clears the pending marker.
/// This closes the crash window where job CAS succeeds but audit append does not.
/// </summary>
internal sealed class DurableJobAuditOutbox
{
    private const int MaxClearAttempts = 4;
    private readonly JsonAgentJobStore _store;
    private readonly IAuditTrail _auditTrail;

    public DurableJobAuditOutbox(JsonAgentJobStore store, IAuditTrail auditTrail)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _auditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
    }

    public AgentJobRecord Stage(AgentJobRecord replacement, AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(auditEvent);
        if (replacement.PendingAuditEvent is not null)
            throw new InvalidOperationException("A job cannot stage a second audit event while one is pending recovery.");

        AuditEventTrust.ValidateForPersistence(auditEvent, nameof(auditEvent));
        return replacement with { PendingAuditEvent = auditEvent };
    }

    public async Task<AgentJobRecord> FlushAsync(
        AgentJobRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var current = record;

        for (var attempt = 0; attempt < MaxClearAttempts; attempt++)
        {
            var pending = current.PendingAuditEvent;
            if (pending is null)
                return current;

            ValidateExternalActionAuditBinding(current, pending);
            AuditEventTrust.ValidateForPersistence(pending, nameof(record));
            await EnsureAuditPresentAsync(pending, cancellationToken).ConfigureAwait(false);

            var cleared = current with { PendingAuditEvent = null };
            if (await _store.CompareExchangeAsync(current, cleared, cancellationToken).ConfigureAwait(false))
                return cleared;

            current = await _store.GetAsync(current.JobId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Job disappeared while pending audit recovery was clearing its durable marker.");

            if (current.PendingAuditEvent is null)
                return current;
            if (!JsonAgentJobStore.AuditEventEquivalent(current.PendingAuditEvent, pending))
                throw new InvalidOperationException("Pending job audit changed during recovery; refusing to clear a different event.");
        }

        throw new InvalidOperationException("Pending job audit could not be cleared after bounded concurrent recovery attempts.");
    }

    private async Task EnsureAuditPresentAsync(AuditEvent pending, CancellationToken cancellationToken)
    {
        var existing = FindById(await _auditTrail.ReadAllAsync(cancellationToken).ConfigureAwait(false), pending.EventId);
        if (existing is not null)
        {
            EnsureEquivalent(existing, pending);
            return;
        }

        try
        {
            await _auditTrail.AppendAsync(pending, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (InvalidOperationException)
        {
            // Another recovery actor may have appended this exact id after our read. Re-read and
            // accept only byte-for-byte-equivalent audit semantics; otherwise fail closed.
            existing = FindById(await _auditTrail.ReadAllAsync(cancellationToken).ConfigureAwait(false), pending.EventId);
            if (existing is null)
                throw;
            EnsureEquivalent(existing, pending);
        }
    }

    private static void ValidateExternalActionAuditBinding(AgentJobRecord record, AuditEvent pending)
    {
        var action = record.PendingExternalAction;
        if (action is null)
            return;
        if (action.AuditEventId == Guid.Empty || action.AuditEventId != pending.EventId)
            throw new InvalidDataException("Pending external action is not bound to the pending audit event; audit recovery was aborted.");
    }

    private static AuditEvent? FindById(IReadOnlyList<AuditEvent> events, Guid eventId) =>
        events.FirstOrDefault(candidate => candidate.EventId == eventId);

    private static void EnsureEquivalent(AuditEvent existing, AuditEvent pending)
    {
        if (!JsonAgentJobStore.AuditEventEquivalent(existing, pending))
            throw new InvalidDataException("Audit trail contains the pending event id with conflicting content; recovery was aborted.");
    }
}
