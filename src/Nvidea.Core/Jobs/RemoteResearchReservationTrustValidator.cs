namespace Nvidea.Core.Jobs;

/// <summary>
/// Pure fail-closed validation for the durable trust root shared by remote-research reservation
/// recovery and provider-create authority. It deliberately does not infer whether provider Create
/// is safe from a marker-cleared record; callers must preserve that ambiguity rule explicitly.
/// </summary>
public static class RemoteResearchReservationTrustValidator
{
    public const string ReservationAuditEventType = "research.remote_dispatch_reserved";

    public static RemoteResearchReservationTrust ValidateReserved(
        AgentJobRecord record,
        DateTimeOffset? now = null,
        bool requireUnexpired = false)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!string.Equals(record.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal)
            || record.State != AgentJobState.Running
            || record.ExecutionLocation != JobExecutionLocation.Local
            || record.ApprovalScope is not null)
        {
            throw new InvalidOperationException(
                "Remote dispatch authority requires an approval-free, local Running research reservation.");
        }

        var provenance = record.RemoteResearch
            ?? throw new InvalidOperationException("Remote dispatch reservation provenance is missing.");
        if (provenance.State != RemoteResearchProvenanceState.DispatchReserved
            || !string.IsNullOrWhiteSpace(provenance.RemoteJobId)
            || !string.Equals(provenance.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(provenance.OpaqueWorkItemId)
            || provenance.WorkItemExpiresAt is null)
        {
            throw new InvalidOperationException("Durable remote dispatch reservation provenance is incomplete or invalid.");
        }

        var checkpoint = record.Checkpoint
            ?? throw new InvalidOperationException("Remote dispatch reservation is missing its durable input checkpoint.");
        if (!string.Equals(checkpoint.Step, provenance.InputCheckpointStep, StringComparison.Ordinal)
            || checkpoint.SavedAt != provenance.InputCheckpointSavedAt)
        {
            throw new InvalidDataException("Remote dispatch reservation no longer matches its exact durable input checkpoint.");
        }

        var expiresAt = provenance.WorkItemExpiresAt.Value;
        if (expiresAt <= provenance.DispatchedAt
            || expiresAt - provenance.DispatchedAt > ResearchWorkItemProtector.MaxLifetime)
        {
            throw new InvalidDataException("Remote dispatch reservation has an invalid protected work-item lifetime.");
        }
        if (requireUnexpired && expiresAt <= (now ?? DateTimeOffset.UtcNow))
            throw new InvalidOperationException("Protected work item expired before provider creation could be authorized.");

        var commitment = record.RemoteWorkItemEnvelopeSha256
            ?? throw new InvalidDataException("Atomic remote dispatch reservation is missing its protected envelope commitment.");
        commitment = ResearchWorkItemEnvelopeCommitment.ValidateCanonicalSha256(
            commitment,
            nameof(record.RemoteWorkItemEnvelopeSha256));

        return new RemoteResearchReservationTrust(provenance, checkpoint, commitment, expiresAt);
    }

    public static void RequirePendingReservationAudit(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var pending = record.PendingAuditEvent;
        if (pending is null)
            throw new RemoteResearchDispatchReservationRecoveryNotRequiredException();
        if (!string.Equals(pending.EventType, ReservationAuditEventType, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Dispatch reservation recovery found a different pending audit event and will not treat it as provider-dispatch authority.");
        }
    }

    public static void RequireAuditSettled(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.PendingAuditEvent is not null)
            throw new InvalidOperationException("Nebius provider creation requires the reservation audit to be durably settled.");
    }
}

public sealed record RemoteResearchReservationTrust(
    RemoteResearchProvenance Provenance,
    AgentJobCheckpoint Checkpoint,
    string EnvelopeSha256,
    DateTimeOffset WorkItemExpiresAt);
