namespace Nvidea.Core.Jobs;

/// <summary>
/// Owns the durable marker used to prove that protected remote-research payload cleanup is still
/// owed. The marker is intentionally staged into the same job CAS as the result/terminal state that
/// makes deletion safe. Cleanup execution itself is idempotent and the marker is cleared only after
/// every required delete has completed successfully.
/// </summary>
internal sealed class DurableProtectedPayloadCleanupIntent
{
    private const int MaxOpaqueWorkItemIdLength = 512;
    private readonly JsonAgentJobStore _store;

    internal DurableProtectedPayloadCleanupIntent(JsonAgentJobStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    internal AgentJobRecord Stage(AgentJobRecord record, string opaqueWorkItemId, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateOpaqueWorkItemId(opaqueWorkItemId);

        if (record.PendingProtectedPayloadCleanup is not null)
        {
            ValidatePending(record, opaqueWorkItemId);
            return record;
        }

        var provenance = record.RemoteResearch
            ?? throw new InvalidOperationException("Protected payload cleanup requires remote research provenance.");
        if (!string.Equals(provenance.OpaqueWorkItemId, opaqueWorkItemId, StringComparison.Ordinal))
            throw new InvalidOperationException("Protected payload cleanup target does not match durable remote provenance.");

        return record with
        {
            PendingProtectedPayloadCleanup = new PendingProtectedPayloadCleanup(
                Guid.NewGuid(),
                opaqueWorkItemId,
                now ?? DateTimeOffset.UtcNow)
        };
    }

    internal PendingProtectedPayloadCleanup ValidatePending(AgentJobRecord record, string expectedOpaqueWorkItemId)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateOpaqueWorkItemId(expectedOpaqueWorkItemId);

        var pending = record.PendingProtectedPayloadCleanup
            ?? throw new InvalidOperationException("Job has no pending protected payload cleanup intent.");
        if (pending.CleanupId == Guid.Empty)
            throw new InvalidOperationException("Pending protected payload cleanup has an invalid cleanup id.");
        ValidateOpaqueWorkItemId(pending.OpaqueWorkItemId);
        if (!string.Equals(pending.OpaqueWorkItemId, expectedOpaqueWorkItemId, StringComparison.Ordinal))
            throw new InvalidOperationException("Pending protected payload cleanup targets substituted remote provenance.");
        if (pending.CreatedAt == default)
            throw new InvalidOperationException("Pending protected payload cleanup has an invalid creation timestamp.");

        var provenance = record.RemoteResearch
            ?? throw new InvalidOperationException("Pending protected payload cleanup requires remote research provenance.");
        if (!string.Equals(provenance.OpaqueWorkItemId, pending.OpaqueWorkItemId, StringComparison.Ordinal))
            throw new InvalidOperationException("Pending protected payload cleanup no longer matches durable remote provenance.");

        return pending;
    }

    internal async Task<AgentJobRecord> ClearAsync(
        AgentJobRecord current,
        Guid cleanupId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (cleanupId == Guid.Empty)
            throw new ArgumentException("Cleanup id is required.", nameof(cleanupId));
        if (current.PendingAuditEvent is not null)
            throw new InvalidOperationException("Protected payload cleanup cannot be marked complete before its required audit is durable.");

        var pending = current.PendingProtectedPayloadCleanup
            ?? throw new InvalidOperationException("Job has no pending protected payload cleanup intent.");
        if (pending.CleanupId != cleanupId)
            throw new InvalidOperationException("Pending protected payload cleanup changed before completion could be recorded.");
        ValidatePending(current, pending.OpaqueWorkItemId);

        var replacement = current with
        {
            PendingProtectedPayloadCleanup = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        if (!await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Job changed while protected payload cleanup completion was being recorded.");

        return replacement;
    }

    private static void ValidateOpaqueWorkItemId(string opaqueWorkItemId)
    {
        if (string.IsNullOrWhiteSpace(opaqueWorkItemId)
            || opaqueWorkItemId.Length > MaxOpaqueWorkItemIdLength
            || opaqueWorkItemId.Any(char.IsControl))
        {
            throw new InvalidOperationException("Protected payload cleanup requires a bounded opaque work-item id.");
        }
    }
}
