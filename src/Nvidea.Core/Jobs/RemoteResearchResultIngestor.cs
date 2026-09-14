using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

public enum RemoteResearchProvenanceState
{
    DispatchReserved,
    Dispatched,
    ResultApplied,
    CancelRequested,
    Cancelled,
    RemoteFailed,
    Expired
}

/// <summary>
/// Durable client-side provenance for exactly one remotely executed research stage.
/// RemoteJobId is intentionally nullable only while State == DispatchReserved. WorkItemExpiresAt
/// records the cryptographically authenticated encrypted-work-item lifetime so terminal lifecycle
/// reconciliation never has to infer TTL from wall-clock conventions. ProviderFailureCode is an
/// optional bounded provider classification captured only for a verified terminal RemoteFailed state;
/// it is untrusted data and carries no retry, resource-selection, or other execution authority.
/// </summary>
public sealed record RemoteResearchProvenance(
    string ProtocolVersion,
    string OpaqueWorkItemId,
    string? RemoteJobId,
    string InputCheckpointStep,
    DateTimeOffset InputCheckpointSavedAt,
    DateTimeOffset DispatchedAt,
    RemoteResearchProvenanceState State,
    DateTimeOffset? ResultAppliedAt = null,
    DateTimeOffset? WorkItemExpiresAt = null,
    DateTimeOffset? TerminalAt = null,
    string? ProviderFailureCode = null);

public sealed record RemoteResearchDispatchReservation(
    Guid LocalJobId,
    string CheckpointStep,
    string OpaqueWorkItemId,
    DateTimeOffset ReservedAt,
    DateTimeOffset? WorkItemExpiresAt = null);

public sealed class RemoteResearchResultNotAvailableException : InvalidOperationException
{
    public RemoteResearchResultNotAvailableException()
        : base("Remote research result is not available yet.")
    {
    }
}

/// <summary>
/// Applies one protected Nebius research result to the local durable job using a compare-and-swap
/// boundary. Remote dispatch is two-phase: ReserveDispatchAsync first freezes the exact local
/// checkpoint, opaque work-item id, and encrypted object expiry durably, then AttachDispatchAsync
/// records the Nebius job id. A crash after reservation therefore cannot silently replay the local
/// stage. A reservation with no remote job id remains explicitly ambiguous and must not be auto-retried.
/// </summary>
public sealed class RemoteResearchResultIngestor
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(1);

    private readonly JsonAgentJobStore _store;
    private readonly IProtectedResearchResultTransport _results;
    private readonly IProtectedResearchWorkItemTransport? _workItems;
    private readonly IAuditTrail _auditTrail;
    private readonly string _clientPrivateKeyPem;

    public RemoteResearchResultIngestor(
        JsonAgentJobStore store,
        IProtectedResearchResultTransport results,
        string clientPrivateKeyPem,
        IAuditTrail auditTrail,
        IProtectedResearchWorkItemTransport? workItems = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _auditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
        _workItems = workItems;
        _clientPrivateKeyPem = string.IsNullOrWhiteSpace(clientPrivateKeyPem)
            ? throw new ArgumentException("Client private key is required.", nameof(clientPrivateKeyPem))
            : clientPrivateKeyPem;
    }

    /// <summary>
    /// Performs the deterministic, no-mutation portion of remote dispatch reservation validation.
    /// This is intentionally safe to call before encrypted transport upload. ReserveDispatchAsync
    /// repeats the same eligibility and audit validation against freshly loaded state immediately
    /// before its CAS so this preflight cannot weaken concurrency safety.
    /// </summary>
    public async Task PreflightDispatchReservationAsync(
        Guid localJobId,
        string checkpointStep,
        CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(localJobId, cancellationToken).ConfigureAwait(false);
        ValidateDispatchReservationTarget(current, checkpointStep);
        _ = PrepareDispatchReservationAudit(current);
    }

    public async Task<AgentJobRecord> ReserveDispatchAsync(
        RemoteResearchDispatchReservation reservation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        if (string.IsNullOrWhiteSpace(reservation.OpaqueWorkItemId))
            throw new InvalidOperationException("Dispatch reservation is missing the opaque work-item id.");

        var expiresAt = reservation.WorkItemExpiresAt ?? reservation.ReservedAt + ResearchWorkItemProtector.MaxLifetime;
        if (expiresAt <= reservation.ReservedAt || expiresAt - reservation.ReservedAt > ResearchWorkItemProtector.MaxLifetime)
            throw new InvalidOperationException("Dispatch reservation contains an invalid encrypted work-item lifetime.");

        var current = await GetRequiredResearchAsync(reservation.LocalJobId, cancellationToken).ConfigureAwait(false);
        ValidateDispatchReservationTarget(current, reservation.CheckpointStep);

        var checkpoint = current.Checkpoint!;
        var provenance = new RemoteResearchProvenance(
            ResearchWorkItemProtector.ProtocolVersion,
            reservation.OpaqueWorkItemId,
            RemoteJobId: null,
            checkpoint.Step,
            checkpoint.SavedAt,
            reservation.ReservedAt,
            RemoteResearchProvenanceState.DispatchReserved,
            WorkItemExpiresAt: expiresAt);
        var replacement = current with
        {
            State = AgentJobState.Running,
            ExecutionLocation = JobExecutionLocation.Local,
            LastError = null,
            NextAttemptAt = null,
            RemoteResearch = provenance,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var reservationAudit = PrepareAudit(
            replacement,
            "research.remote_dispatch_reserved",
            "Encrypted research stage reserved before Nebius job creation.");

        var applied = await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false);
        if (!applied)
            throw new InvalidOperationException("Research state changed while remote dispatch was being reserved.");

        await AppendAuditAsync(reservationAudit, cancellationToken).ConfigureAwait(false);
        return replacement;
    }

    public async Task<AgentJobRecord> AttachDispatchAsync(
        NebiusResearchDispatchReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var current = await GetRequiredResearchAsync(receipt.LocalJobId, cancellationToken).ConfigureAwait(false);
        var reserved = current.RemoteResearch
            ?? throw new InvalidOperationException("Remote dispatch must be durably reserved before Nebius creation.");
        if (current.State != AgentJobState.Running
            || current.ExecutionLocation != JobExecutionLocation.Local
            || reserved.State != RemoteResearchProvenanceState.DispatchReserved)
            throw new InvalidOperationException("Remote dispatch can only attach to an exact durable DispatchReserved state.");
        if (current.ApprovalScope is not null)
            throw new InvalidOperationException("Approval-bearing research cannot be dispatched remotely.");
        if (string.IsNullOrWhiteSpace(receipt.OpaqueWorkItemId) || string.IsNullOrWhiteSpace(receipt.RemoteJobId))
            throw new InvalidOperationException("Dispatch receipt is missing remote provenance identifiers.");

        var checkpoint = current.Checkpoint
            ?? throw new InvalidOperationException("Research dispatch requires a durable input checkpoint.");
        if (!string.Equals(checkpoint.Step, receipt.CheckpointStep, StringComparison.Ordinal)
            || !string.Equals(reserved.OpaqueWorkItemId, receipt.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(reserved.InputCheckpointStep, checkpoint.Step, StringComparison.Ordinal)
            || reserved.InputCheckpointSavedAt != checkpoint.SavedAt)
            throw new InvalidOperationException("Dispatch receipt does not match the exact reserved research checkpoint.");

        var provenance = reserved with
        {
            RemoteJobId = receipt.RemoteJobId,
            DispatchedAt = receipt.DispatchedAt,
            State = RemoteResearchProvenanceState.Dispatched
        };
        var replacement = current with
        {
            ExecutionLocation = JobExecutionLocation.NebiusServerless,
            Attempt = current.Attempt + 1,
            LastError = null,
            NextAttemptAt = null,
            RemoteResearch = provenance,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var dispatchAudit = PrepareAudit(
            replacement,
            "research.remote_dispatched",
            "Reserved encrypted research stage attached to Nebius Serverless provenance.");

        if (!await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while remote dispatch provenance was being attached.");

        await AppendAuditAsync(dispatchAudit, cancellationToken).ConfigureAwait(false);
        return replacement;
    }

    public Task<AgentJobRecord> IngestAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default) =>
        IngestCoreAsync(
            jobId,
            RemoteResearchProvenanceState.Dispatched,
            expectedRemoteJobId: null,
            "research.remote_result_applied",
            "Protected remote research result applied exactly once.",
            now,
            cancellationToken);

    /// <summary>
    /// Narrow recovery entry point for the cancellation-vs-completion race. The lifecycle reconciler
    /// may call this only after it has freshly verified that the exact durable Nebius job is Completed.
    /// Keeping this path separate prevents ordinary ingestion from accepting CancelRequested stages.
    /// </summary>
    internal Task<AgentJobRecord> IngestCompletedAfterCancellationRequestedAsync(
        Guid jobId,
        string verifiedRemoteJobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(verifiedRemoteJobId))
            throw new ArgumentException("Verified Nebius job id is required.", nameof(verifiedRemoteJobId));

        return IngestCoreAsync(
            jobId,
            RemoteResearchProvenanceState.CancelRequested,
            verifiedRemoteJobId,
            "research.remote_result_applied_after_cancel_request",
            "Verified Nebius completion won the cancellation race; protected remote research result applied exactly once.",
            now,
            cancellationToken);
    }

    private async Task<AgentJobRecord> IngestCoreAsync(
        Guid jobId,
        RemoteResearchProvenanceState requiredProvenanceState,
        string? expectedRemoteJobId,
        string auditEventType,
        string auditSummary,
        DateTimeOffset? now,
        CancellationToken cancellationToken)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var provenance = current.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote execution provenance.");
        if (current.ExecutionLocation != JobExecutionLocation.NebiusServerless || current.State != AgentJobState.Running)
            throw new InvalidOperationException("Only an in-flight Nebius research stage can ingest a remote result.");
        if (provenance.State != requiredProvenanceState || provenance.ResultAppliedAt is not null)
            throw new InvalidOperationException("Remote research result has already been applied or is not eligible for this ingestion path.");
        if (string.IsNullOrWhiteSpace(provenance.RemoteJobId))
            throw new InvalidOperationException("Dispatched research provenance is missing the Nebius job id.");
        if (expectedRemoteJobId is not null
            && !string.Equals(provenance.RemoteJobId, expectedRemoteJobId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verified remote completion does not match durable Nebius job provenance.");
        }
        if (!string.Equals(provenance.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Remote research provenance protocol is unsupported.");

        var checkpoint = current.Checkpoint
            ?? throw new InvalidOperationException("Remote research job is missing its input checkpoint.");
        if (!string.Equals(checkpoint.Step, provenance.InputCheckpointStep, StringComparison.Ordinal)
            || checkpoint.SavedAt != provenance.InputCheckpointSavedAt)
            throw new InvalidOperationException("Local research checkpoint no longer matches the dispatched remote stage.");

        var envelope = await _results.GetAsync(provenance.OpaqueWorkItemId, cancellationToken).ConfigureAwait(false)
            ?? throw new RemoteResearchResultNotAvailableException();
        if (!string.Equals(envelope.OpaqueWorkItemId, provenance.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(envelope.RemoteJobId, provenance.RemoteJobId, StringComparison.Ordinal))
            throw new InvalidOperationException("Remote result transport returned substituted provenance.");

        var currentTime = now ?? DateTimeOffset.UtcNow;
        var result = ResearchResultProtector.Unprotect(envelope, _clientPrivateKeyPem, currentTime);
        if (result.CompletedAt > currentTime + AllowedClockSkew)
            throw new InvalidOperationException("Remote research result completion timestamp is implausibly in the future.");
        if (result.LocalJobId != current.JobId
            || !string.Equals(result.InputCheckpointStep, provenance.InputCheckpointStep, StringComparison.Ordinal)
            || !string.Equals(result.OpaqueWorkItemId, provenance.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(result.RemoteJobId, provenance.RemoteJobId, StringComparison.Ordinal))
            throw new InvalidOperationException("Protected remote research result does not match the expected local stage provenance.");

        var step = result.StepResult;
        if (step.RequiresApproval || !string.IsNullOrWhiteSpace(step.ApprovalScope))
            throw new InvalidOperationException("Remote research result cannot carry approval authority.");
        if (string.IsNullOrWhiteSpace(step.CheckpointStep))
            throw new InvalidOperationException("Remote research result did not produce a durable checkpoint.");

        var outputCheckpoint = new AgentJobCheckpoint(step.CheckpointStep, step.CheckpointPayload, result.CompletedAt);
        var appliedProvenance = provenance with
        {
            State = RemoteResearchProvenanceState.ResultApplied,
            ResultAppliedAt = currentTime
        };
        var replacement = current with
        {
            State = step.Completed ? AgentJobState.Completed : AgentJobState.Pending,
            ExecutionLocation = JobExecutionLocation.Local,
            Checkpoint = outputCheckpoint,
            ApprovalScope = null,
            LastError = null,
            NextAttemptAt = null,
            RemoteResearch = appliedProvenance,
            UpdatedAt = currentTime
        };
        var resultAudit = PrepareAudit(replacement, auditEventType, auditSummary);

        if (!await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while protected remote result was being applied.");

        await AppendAuditAsync(resultAudit, cancellationToken).ConfigureAwait(false);
        await CleanupProtectedPayloadsAsync(provenance.OpaqueWorkItemId).ConfigureAwait(false);
        return replacement;
    }

    public async Task CleanupProtectedPayloadsAsync(string opaqueWorkItemId)
    {
        await TryDeleteAsync(_results, opaqueWorkItemId).ConfigureAwait(false);
        if (_workItems is not null)
            await TryDeleteAsync(_workItems, opaqueWorkItemId).ConfigureAwait(false);
    }

    private async Task<AgentJobRecord> GetRequiredResearchAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));
        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Remote result ingestion only accepts research jobs.");
        return job;
    }

    private static void ValidateDispatchReservationTarget(AgentJobRecord current, string checkpointStep)
    {
        if (string.IsNullOrWhiteSpace(checkpointStep))
            throw new InvalidOperationException("Dispatch reservation is missing the checkpoint step.");
        if (current.State != AgentJobState.Pending || current.ExecutionLocation != JobExecutionLocation.Local)
            throw new InvalidOperationException("Only a pending local research stage can be reserved for remote dispatch.");
        if (current.RemoteResearch is { State: not RemoteResearchProvenanceState.ResultApplied })
            throw new InvalidOperationException("Research job already carries unfinished remote execution provenance.");
        if (current.ApprovalScope is not null)
            throw new InvalidOperationException("Approval-bearing research cannot be dispatched remotely.");

        var checkpoint = current.Checkpoint
            ?? throw new InvalidOperationException("Research dispatch requires a durable input checkpoint.");
        if (!string.Equals(checkpoint.Step, checkpointStep, StringComparison.Ordinal))
            throw new InvalidOperationException("Dispatch reservation does not match the current research checkpoint.");
    }

    private static AuditEvent PrepareDispatchReservationAudit(AgentJobRecord current)
    {
        var projected = current with
        {
            State = AgentJobState.Running,
            ExecutionLocation = JobExecutionLocation.Local,
            LastError = null,
            NextAttemptAt = null
        };
        return PrepareAudit(
            projected,
            "research.remote_dispatch_reserved",
            "Encrypted research stage reserved before Nebius job creation.");
    }

    private static AuditEvent PrepareAudit(AgentJobRecord job, string eventType, string summary)
    {
        var auditEvent = new AuditEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, job.Definition.CapabilityId, job.JobId.ToString("N"), eventType,
            job.Definition.Risk, true, false, string.Empty, summary,
            new Dictionary<string, string>
            {
                ["jobType"] = job.Definition.JobType,
                ["state"] = job.State.ToString(),
                ["executionLocation"] = job.ExecutionLocation.ToString(),
                ["attempt"] = job.Attempt.ToString()
            });
        AuditEventTrust.ValidateForPersistence(auditEvent, nameof(job));
        return auditEvent;
    }

    private Task AppendAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        _auditTrail.AppendAsync(auditEvent, cancellationToken);

    private static async Task TryDeleteAsync(IProtectedResearchResultTransport transport, string opaqueWorkItemId)
    {
        try
        {
            await transport.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Protected artifacts are bounded by protocol expiry; cleanup is best effort.
        }
    }

    private static async Task TryDeleteAsync(IProtectedResearchWorkItemTransport transport, string opaqueWorkItemId)
    {
        try
        {
            await transport.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Protected artifacts are bounded by protocol expiry; cleanup is best effort.
        }
    }
}
