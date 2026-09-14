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
    private const string ReservationAuditEventType = "research.remote_dispatch_reserved";

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
        var expectedCommitment = ValidateDurableReservation(current);

        var pending = current.PendingAuditEvent;
        if (pending is null)
            throw new RemoteResearchDispatchReservationRecoveryNotRequiredException();
        if (!string.Equals(pending.EventType, ReservationAuditEventType, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Dispatch reservation recovery found a different pending audit event and will not settle it as provider-dispatch authority.");
        }

        var settled = await _auditOutbox.FlushAsync(current, cancellationToken).ConfigureAwait(false);
        var settledCommitment = ValidateDurableReservation(settled);
        if (!ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(expectedCommitment, settledCommitment))
        {
            throw new InvalidDataException(
                "Protected work-item envelope commitment changed while dispatch reservation audit recovery was settling.");
        }

        if (settled.PendingAuditEvent is not null)
            throw new InvalidOperationException("Dispatch reservation audit recovery did not settle the pending audit marker.");

        return settled;
    }

    private static string ValidateDurableReservation(AgentJobRecord record)
    {
        if (!string.Equals(record.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal)
            || record.State != AgentJobState.Running
            || record.ExecutionLocation != JobExecutionLocation.Local
            || record.ApprovalScope is not null)
        {
            throw new InvalidOperationException(
                "Only an approval-free, local Running research reservation can be recovered for remote dispatch.");
        }

        var provenance = record.RemoteResearch
            ?? throw new InvalidOperationException("Research dispatch reservation provenance is missing.");
        if (provenance.State != RemoteResearchProvenanceState.DispatchReserved
            || !string.IsNullOrWhiteSpace(provenance.RemoteJobId)
            || !string.Equals(provenance.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(provenance.OpaqueWorkItemId)
            || provenance.WorkItemExpiresAt is null)
        {
            throw new InvalidOperationException("Durable remote dispatch reservation provenance is incomplete or not recoverable.");
        }

        var checkpoint = record.Checkpoint
            ?? throw new InvalidOperationException("Remote dispatch reservation is missing its durable input checkpoint.");
        if (!string.Equals(checkpoint.Step, provenance.InputCheckpointStep, StringComparison.Ordinal)
            || checkpoint.SavedAt != provenance.InputCheckpointSavedAt)
        {
            throw new InvalidDataException("Remote dispatch reservation no longer matches its exact durable input checkpoint.");
        }

        var commitment = record.RemoteWorkItemEnvelopeSha256
            ?? throw new InvalidDataException("Atomic remote dispatch reservation is missing its protected envelope commitment.");
        return ResearchWorkItemEnvelopeCommitment.ValidateCanonicalSha256(
            commitment,
            nameof(record.RemoteWorkItemEnvelopeSha256));
    }
}
