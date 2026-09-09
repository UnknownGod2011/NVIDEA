using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

public enum RemoteResearchProvenanceState
{
    Dispatched,
    ResultApplied,
    CancelRequested,
    Cancelled
}

/// <summary>
/// Durable client-side provenance for exactly one remotely executed research stage.
/// It deliberately stores opaque identifiers and checkpoint identity only; user research
/// content remains inside the protected checkpoint and encrypted transport envelopes.
/// </summary>
public sealed record RemoteResearchProvenance(
    string ProtocolVersion,
    string OpaqueWorkItemId,
    string RemoteJobId,
    string InputCheckpointStep,
    DateTimeOffset InputCheckpointSavedAt,
    DateTimeOffset DispatchedAt,
    RemoteResearchProvenanceState State,
    DateTimeOffset? ResultAppliedAt = null);

/// <summary>
/// Applies one protected Nebius research result to the local durable job using a compare-and-swap
/// boundary. Exact local job id, remote job id, opaque work-item id, input checkpoint step and
/// input checkpoint timestamp must still match. Duplicate, stale, substituted and approval-bearing
/// results fail closed instead of overwriting newer local state.
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

    public async Task<AgentJobRecord> AttachDispatchAsync(
        NebiusResearchDispatchReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var current = await GetRequiredResearchAsync(receipt.LocalJobId, cancellationToken).ConfigureAwait(false);
        if (current.State != AgentJobState.Pending)
            throw new InvalidOperationException("Only a pending research stage can be attached to a remote dispatch.");
        if (current.ExecutionLocation != JobExecutionLocation.Local)
            throw new InvalidOperationException("Research dispatch can only attach from trusted local state.");
        if (current.RemoteResearch is { State: not RemoteResearchProvenanceState.ResultApplied })
            throw new InvalidOperationException("Research job already carries unfinished remote execution provenance.");
        if (current.ApprovalScope is not null)
            throw new InvalidOperationException("Approval-bearing research cannot be dispatched remotely.");
        if (string.IsNullOrWhiteSpace(receipt.OpaqueWorkItemId)
            || string.IsNullOrWhiteSpace(receipt.RemoteJobId))
        {
            throw new InvalidOperationException("Dispatch receipt is missing remote provenance identifiers.");
        }

        var checkpoint = current.Checkpoint
            ?? throw new InvalidOperationException("Research dispatch requires a durable input checkpoint.");
        if (!string.Equals(checkpoint.Step, receipt.CheckpointStep, StringComparison.Ordinal))
            throw new InvalidOperationException("Dispatch receipt does not match the current research checkpoint.");

        var provenance = new RemoteResearchProvenance(
            ResearchWorkItemProtector.ProtocolVersion,
            receipt.OpaqueWorkItemId,
            receipt.RemoteJobId,
            checkpoint.Step,
            checkpoint.SavedAt,
            receipt.DispatchedAt,
            RemoteResearchProvenanceState.Dispatched);
        var replacement = current with
        {
            State = AgentJobState.Running,
            ExecutionLocation = JobExecutionLocation.NebiusServerless,
            Attempt = current.Attempt + 1,
            LastError = null,
            NextAttemptAt = null,
            RemoteResearch = provenance,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var applied = await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false);
        if (!applied)
            throw new InvalidOperationException("Research state changed while remote dispatch provenance was being attached.");

        await AppendAuditAsync(replacement, "research.remote_dispatched", "Encrypted research stage attached to Nebius Serverless provenance.", cancellationToken).ConfigureAwait(false);
        return replacement;
    }

    public async Task<AgentJobRecord> IngestAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var provenance = current.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote execution provenance.");
        if (current.ExecutionLocation != JobExecutionLocation.NebiusServerless || current.State != AgentJobState.Running)
            throw new InvalidOperationException("Only an in-flight Nebius research stage can ingest a remote result.");
        if (provenance.State != RemoteResearchProvenanceState.Dispatched || provenance.ResultAppliedAt is not null)
            throw new InvalidOperationException("Remote research result has already been applied or is no longer ingestible.");
        if (!string.Equals(provenance.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Remote research provenance protocol is unsupported.");

        var checkpoint = current.Checkpoint
            ?? throw new InvalidOperationException("Remote research job is missing its input checkpoint.");
        if (!string.Equals(checkpoint.Step, provenance.InputCheckpointStep, StringComparison.Ordinal)
            || checkpoint.SavedAt != provenance.InputCheckpointSavedAt)
        {
            throw new InvalidOperationException("Local research checkpoint no longer matches the dispatched remote stage.");
        }

        var envelope = await _results.GetAsync(provenance.OpaqueWorkItemId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remote research result is not available yet.");
        if (!string.Equals(envelope.OpaqueWorkItemId, provenance.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(envelope.RemoteJobId, provenance.RemoteJobId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Remote result transport returned substituted provenance.");
        }

        var currentTime = now ?? DateTimeOffset.UtcNow;
        var result = ResearchResultProtector.Unprotect(envelope, _clientPrivateKeyPem, currentTime);
        if (result.CompletedAt > currentTime + AllowedClockSkew)
            throw new InvalidOperationException("Remote research result completion timestamp is implausibly in the future.");
        if (result.LocalJobId != current.JobId
            || !string.Equals(result.InputCheckpointStep, provenance.InputCheckpointStep, StringComparison.Ordinal)
            || !string.Equals(result.OpaqueWorkItemId, provenance.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(result.RemoteJobId, provenance.RemoteJobId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Protected remote research result does not match the expected local stage provenance.");
        }

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

        var exchanged = await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false);
        if (!exchanged)
            throw new InvalidOperationException("Research state changed while the remote result was being ingested; result was not applied.");

        await AppendAuditAsync(replacement, "research.remote_result_applied", "Verified encrypted Nebius research result was applied exactly once.", cancellationToken).ConfigureAwait(false);
        await BestEffortDeleteAsync(provenance.OpaqueWorkItemId).ConfigureAwait(false);
        return replacement;
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

    private Task AppendAuditAsync(AgentJobRecord job, string eventType, string summary, CancellationToken cancellationToken) =>
        _auditTrail.AppendAsync(
            new AuditEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                job.Definition.CapabilityId,
                job.JobId.ToString("N"),
                eventType,
                job.Definition.Risk,
                true,
                false,
                string.Empty,
                summary,
                new Dictionary<string, string>
                {
                    ["jobType"] = job.Definition.JobType,
                    ["state"] = job.State.ToString(),
                    ["executionLocation"] = job.ExecutionLocation.ToString(),
                    ["attempt"] = job.Attempt.ToString()
                }),
            cancellationToken);

    private async Task BestEffortDeleteAsync(string opaqueWorkItemId)
    {
        try
        {
            await _results.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // The encrypted result is TTL-bounded. Cleanup failure must not roll back a committed CAS.
        }

        if (_workItems is null) return;
        try
        {
            await _workItems.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Same bounded-retention rule as the encrypted result transport.
        }
    }
}
