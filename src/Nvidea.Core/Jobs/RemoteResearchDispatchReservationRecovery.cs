using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Raised when a DispatchReserved stage has no pending reservation audit. In that state a crash may
/// have happened before, during, or after the Nebius Create request, so creating another provider job
/// is not safe; callers must fall back to conservative provider reconciliation instead.
/// </summary>
public sealed class RemoteResearchDispatchReservationRecoveryNotRequiredException : InvalidOperationException
{
    public RemoteResearchDispatchReservationRecoveryNotRequiredException()
        : base("Remote dispatch reservation has no pending reservation audit; provider creation cannot be safely replayed and requires reconciliation.")
    {
    }
}

/// <summary>
/// Recovers an atomic remote-research DispatchReserved trust root after a reservation-audit delivery
/// failure. A still-pending reservation audit is the durable proof that Atomic ReserveAsync never
/// returned to its caller and therefore Nebius Create was never reached. Recovery consumes only the
/// protected durable job record and that exact pending audit; it never re-reads, re-uploads, or
/// re-hashes the mutable shared work-item transport.
/// </summary>
public sealed class RemoteResearchDispatchReservationRecovery
{
    private readonly JsonAgentJobStore _store;
    private readonly DurableJobAuditOutbox _auditOutbox;

    public RemoteResearchDispatchReservationRecovery(JsonAgentJobStore store, IAuditTrail auditTrail)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _auditOutbox = new DurableJobAuditOutbox(
            _store,
            auditTrail ?? throw new ArgumentNullException(nameof(auditTrail)));
    }

    public async Task<AgentJobRecord> RecoverAuditAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));

        var current = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        var expected = RemoteResearchReservationTrustValidator.ValidateReserved(current);
        RemoteResearchReservationTrustValidator.RequirePendingReservationAudit(current);

        var settled = await _auditOutbox.FlushAsync(current, cancellationToken).ConfigureAwait(false);
        var actual = RemoteResearchReservationTrustValidator.ValidateReserved(settled);
        if (!ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(expected.EnvelopeSha256, actual.EnvelopeSha256))
        {
            throw new InvalidDataException(
                "Protected work-item envelope commitment changed while dispatch reservation audit recovery was settling.");
        }
        if (!string.Equals(expected.Provenance.OpaqueWorkItemId, actual.Provenance.OpaqueWorkItemId, StringComparison.Ordinal)
            || expected.Checkpoint.SavedAt != actual.Checkpoint.SavedAt
            || !string.Equals(expected.Checkpoint.Step, actual.Checkpoint.Step, StringComparison.Ordinal)
            || expected.WorkItemExpiresAt != actual.WorkItemExpiresAt)
        {
            throw new InvalidDataException("Remote dispatch reservation trust root changed while its audit was settling.");
        }

        RemoteResearchReservationTrustValidator.RequireAuditSettled(settled);
        return settled;
    }
}
