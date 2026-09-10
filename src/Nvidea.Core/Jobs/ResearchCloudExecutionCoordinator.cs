namespace Nvidea.Core.Jobs;

/// <summary>
/// Narrow product-facing contract for remote research execution. The implementation may be backed by
/// Nebius Serverless, but callers do not receive provider clients, secret material, transport handles,
/// or mutable deployment configuration.
/// </summary>
public interface IRemoteResearchClientRuntime
{
    Task<AgentJobRecord> DispatchAsync(
        RemoteResearchWorkItem workItem,
        ResearchCloudAuthorization authorization,
        CancellationToken cancellationToken = default);

    Task<AgentJobRecord> ReconcileReservedAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<AgentJobRecord> ReconcileDispatchedAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default);

    Task<AgentJobRecord> RequestCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<AgentJobRecord> ReconcileCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Production integration boundary between the durable desktop research store and the already-built
/// remote research protocol. Remote execution is always an explicit per-stage action: the caller must
/// supply a disclosure-versioned approval scoped to the exact current job/checkpoint. Private OS data,
/// completed stages, approval-bearing stages, and non-local input records fail closed.
///
/// The state-directory lease spans dispatch/reconciliation/cancellation so local RunNextStep/Cancel
/// operations in another NVIDEA process cannot race the remote lifecycle CAS transitions.
/// </summary>
public sealed class ResearchCloudExecutionCoordinator : IResearchCloudExecutionCoordinator
{
    public static readonly TimeSpan DefaultWorkItemLifetime = TimeSpan.FromHours(1);

    private readonly string _stateDirectory;
    private readonly JsonAgentJobStore _store;
    private readonly IRemoteResearchClientRuntime _remote;
    private readonly SemaphoreSlim _mutationGate = new(1, 1);

    public ResearchCloudExecutionCoordinator(
        string stateDirectory,
        IRemoteResearchClientRuntime remote)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("Research state directory is required.", nameof(stateDirectory));

        _remote = remote ?? throw new ArgumentNullException(nameof(remote));
        _stateDirectory = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(_stateDirectory);
        _store = new JsonAgentJobStore(Path.Combine(_stateDirectory, "research-jobs.json"));
    }

    public Task<ResearchJobStatus> DispatchCurrentStageAsync(
        Guid jobId,
        ResearchCloudAuthorization authorization,
        TimeSpan? workItemLifetime = null,
        CancellationToken cancellationToken = default) =>
        WithMutationLeaseAsync(async ct =>
        {
            ArgumentNullException.ThrowIfNull(authorization);
            var current = await GetRequiredResearchAsync(jobId, ct).ConfigureAwait(false);
            ValidateDispatchCandidate(current);

            var checkpoint = current.Checkpoint
                ?? throw new InvalidOperationException("Research dispatch requires a durable input checkpoint.");
            var lifetime = workItemLifetime ?? DefaultWorkItemLifetime;
            if (lifetime <= TimeSpan.Zero || lifetime > ResearchWorkItemProtector.MaxLifetime)
                throw new ArgumentOutOfRangeException(nameof(workItemLifetime), "Remote research lifetime must be positive and no more than 24 hours.");

            var now = DateTimeOffset.UtcNow;
            var workItem = new RemoteResearchWorkItem(
                current.JobId,
                checkpoint.Step,
                checkpoint.Payload,
                current.Definition.ContainsPrivateOsData,
                now,
                now + lifetime);

            // Validate before any remote transport/provider work. DispatchAsync validates again at the
            // cryptographic boundary; the duplicate check is intentional defense in depth.
            ResearchWorkItemProtector.ValidateAuthorization(authorization, workItem, now);
            var dispatched = await _remote.DispatchAsync(workItem, authorization, ct).ConfigureAwait(false);
            return ResearchJobStatus.FromRecord(dispatched);
        }, cancellationToken);

    /// <summary>
    /// Advances only an already-remote lifecycle. A DispatchReserved record is reconciled without
    /// assuming whether Nebius accepted the create call; a Dispatched record is checked for provider
    /// completion/result ingestion; a cancellation request is reconciled independently.
    /// </summary>
    public Task<ResearchJobStatus> ReconcileAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        WithMutationLeaseAsync(async ct =>
        {
            var current = await GetRequiredResearchAsync(jobId, ct).ConfigureAwait(false);
            var provenance = current.RemoteResearch
                ?? throw new InvalidOperationException("Research job has no remote execution provenance to reconcile.");

            AgentJobRecord reconciled = provenance.State switch
            {
                RemoteResearchProvenanceState.DispatchReserved =>
                    await _remote.ReconcileReservedAsync(jobId, ct).ConfigureAwait(false),
                RemoteResearchProvenanceState.Dispatched =>
                    await _remote.ReconcileDispatchedAsync(jobId, cancellationToken: ct).ConfigureAwait(false),
                RemoteResearchProvenanceState.CancelRequested =>
                    await _remote.ReconcileCancellationAsync(jobId, ct).ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Remote research state '{provenance.State}' is not reconcilable.")
            };

            return ResearchJobStatus.FromRecord(reconciled);
        }, cancellationToken);

    public Task<ResearchJobStatus> RequestCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        WithMutationLeaseAsync(async ct =>
        {
            var current = await GetRequiredResearchAsync(jobId, ct).ConfigureAwait(false);
            var provenance = current.RemoteResearch
                ?? throw new InvalidOperationException("Research job has no remote execution provenance to cancel.");
            if (current.ExecutionLocation != JobExecutionLocation.NebiusServerless
                || current.State != AgentJobState.Running
                || provenance.State != RemoteResearchProvenanceState.Dispatched)
            {
                throw new InvalidOperationException("Only an actively dispatched Nebius research stage can request remote cancellation.");
            }

            var updated = await _remote.RequestCancellationAsync(jobId, ct).ConfigureAwait(false);
            return ResearchJobStatus.FromRecord(updated);
        }, cancellationToken);

    private async Task<AgentJobRecord> GetRequiredResearchAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));

        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Cloud research execution only accepts research jobs.");
        return job;
    }

    private static void ValidateDispatchCandidate(AgentJobRecord job)
    {
        if (job.State != AgentJobState.Pending || job.ExecutionLocation != JobExecutionLocation.Local)
            throw new InvalidOperationException("Only a pending local research stage can be dispatched to Nebius.");
        if (job.Definition.ContainsPrivateOsData)
            throw new InvalidOperationException("Research containing private OS-local data must remain on-device.");
        if (job.ApprovalScope is not null)
            throw new InvalidOperationException("Approval-bearing research cannot be dispatched remotely.");
        if (job.RemoteResearch is { State: not RemoteResearchProvenanceState.ResultApplied })
            throw new InvalidOperationException("Research job already carries unfinished remote execution provenance.");
        if (!ResearchJobStatus.IsRecoverableCheckpoint(job.Checkpoint?.Step))
            throw new InvalidOperationException("Current research checkpoint is not eligible for remote execution.");
    }

    private async Task<T> WithMutationLeaseAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var lease = StateDirectoryLease.Acquire(_stateDirectory);
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutationGate.Release();
        }
    }
}
