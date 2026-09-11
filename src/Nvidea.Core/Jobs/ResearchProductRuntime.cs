using Nvidea.Core.Research;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Narrow local-research surface used by product composition. Keeping this contract separate from
/// provider-aware lifecycle control makes it possible to prove that remote/ambiguous records are
/// never accidentally routed back through the in-process research handler.
/// </summary>
public interface ILocalResearchRuntime
{
    Task<ResearchJobStatus> CreateAsync(string question, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResearchJobStatus>> ListAsync(CancellationToken cancellationToken = default);
    Task<ResearchJobStatus> RunNextStepAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<ResearchJobStatus> RecoverInterruptedAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<ResearchJobStatus> CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<ResearchReport> ReadCompletedReportAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<ResearchJobStatus> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Narrow provider-aware lifecycle surface used by product composition. Implementations may use
/// Nebius Serverless, but the product layer receives no provider clients, credentials or transport
/// handles.
/// </summary>
public interface IResearchCloudExecutionCoordinator
{
    Task<ResearchJobStatus> DispatchCurrentStageAsync(
        Guid jobId,
        ResearchCloudAuthorization authorization,
        TimeSpan? workItemLifetime = null,
        CancellationToken cancellationToken = default);

    Task<ResearchJobStatus> ReconcileAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<ResearchJobStatus> RequestCancellationAsync(Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lifecycle-aware product facade for durable research. Reads are projected directly from the shared
/// durable store so local, remote and ambiguous records have one truthful status surface. Mutations are
/// routed by durable provenance and delegated to runtimes that revalidate state under the shared
/// state-directory lease.
///
/// Local research and remote lifecycle authority are deliberately independent. A desktop can recover,
/// reconcile, or cancel already-dispatched Nebius work even when the local Tavily-backed executor is
/// temporarily unavailable; in that mode every local mutation fails closed instead of replaying work.
///
/// Remote dispatch is independently feature-gated. A product can safely compose this facade before
/// the first credential-backed Serverless PASS while still gaining correct reconciliation/cancellation
/// semantics for records created by contract probes or future enabled builds.
/// </summary>
public sealed class ResearchProductRuntime
{
    private readonly ILocalResearchRuntime? _local;
    private readonly IResearchCloudExecutionCoordinator? _cloud;
    private readonly JsonAgentJobStore _store;

    public ResearchProductRuntime(
        string stateDirectory,
        ILocalResearchRuntime? local,
        IResearchCloudExecutionCoordinator? cloud = null,
        bool remoteDispatchEnabled = false)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("Research state directory is required.", nameof(stateDirectory));

        _local = local;
        _cloud = cloud;
        RemoteDispatchEnabled = remoteDispatchEnabled;
        if (RemoteDispatchEnabled && _cloud is null)
            throw new ArgumentException("Remote dispatch cannot be enabled without a cloud execution coordinator.", nameof(cloud));
        if (RemoteDispatchEnabled && _local is null)
            throw new ArgumentException("Remote dispatch cannot be enabled without the local research runtime that creates and validates eligible checkpoints.", nameof(local));

        var root = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(root);
        _store = new JsonAgentJobStore(Path.Combine(root, "research-jobs.json"));
    }

    /// <summary>
    /// True when the Tavily-backed in-process research executor is composed. This is intentionally
    /// separate from <see cref="RemoteLifecycleAvailable"/> so recovery of existing cloud work does
    /// not depend on local research credentials.
    /// </summary>
    public bool LocalExecutionAvailable => _local is not null;

    public bool RemoteDispatchEnabled { get; }

    /// <summary>
    /// Indicates whether this product composition has a provider-aware lifecycle coordinator.
    /// This is intentionally independent from <see cref="RemoteDispatchEnabled"/>: an application
    /// may need to reconcile/cancel already-remote durable records while keeping all new paid cloud
    /// dispatch disabled.
    /// </summary>
    public bool RemoteLifecycleAvailable => _cloud is not null;

    public Task<ResearchJobStatus> CreateAsync(
        string question,
        CancellationToken cancellationToken = default) =>
        RequireLocal().CreateAsync(question, cancellationToken);

    public async Task<IReadOnlyList<ResearchJobStatus>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var jobs = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        return jobs
            .Where(static job => string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            .OrderByDescending(static job => job.UpdatedAt)
            .Select(ResearchJobStatus.FromRecord)
            .ToArray();
    }

    public async Task<ResearchJobStatus> GetStatusAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        ResearchJobStatus.FromRecord(await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false));

    public async Task<ResearchJobStatus> RunNextLocalStepAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (ResearchJobStatus.HasUnfinishedRemoteProvenance(current))
            throw new InvalidOperationException("Remote or ambiguous research must be reconciled before any local stage can run.");
        if (current.ExecutionLocation != JobExecutionLocation.Local)
            throw new InvalidOperationException("Only local research can run through the local product action.");

        return await RequireLocal().RunNextStepAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> RecoverInterruptedAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (ResearchJobStatus.HasUnfinishedRemoteProvenance(current))
            throw new InvalidOperationException("Remote or ambiguous research must be reconciled instead of locally recovered.");
        return await RequireLocal().RecoverInterruptedAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> DispatchCurrentStageAsync(
        Guid jobId,
        ResearchCloudAuthorization authorization,
        TimeSpan? workItemLifetime = null,
        CancellationToken cancellationToken = default)
    {
        if (!RemoteDispatchEnabled)
            throw new InvalidOperationException("Nebius Serverless research dispatch is disabled until the live deployment contract and local research runtime are explicitly enabled.");

        var cloud = RequireCloud();
        return await cloud.DispatchCurrentStageAsync(
            jobId,
            authorization,
            workItemLifetime,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> ReconcileRemoteAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (!ResearchJobStatus.HasUnfinishedRemoteProvenance(current))
            throw new InvalidOperationException("Research job has no unfinished remote lifecycle to reconcile.");

        return await RequireCloud().ReconcileAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var remoteState = current.RemoteResearch?.State;

        if (remoteState == RemoteResearchProvenanceState.DispatchReserved)
        {
            throw new InvalidOperationException(
                "Nebius dispatch outcome is ambiguous; reconcile the reservation before attempting cancellation.");
        }

        if (remoteState == RemoteResearchProvenanceState.Dispatched)
            return await RequireCloud().RequestCancellationAsync(jobId, cancellationToken).ConfigureAwait(false);

        if (remoteState == RemoteResearchProvenanceState.CancelRequested)
            throw new InvalidOperationException("Nebius cancellation is already requested; reconcile provider state instead.");

        if (current.ExecutionLocation != JobExecutionLocation.Local)
            throw new InvalidOperationException("Non-local research without a supported remote lifecycle cannot be cancelled locally.");

        return await RequireLocal().CancelAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public Task<ResearchReport> ReadCompletedReportAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        RequireLocal().ReadCompletedReportAsync(jobId, cancellationToken);

    private ILocalResearchRuntime RequireLocal() =>
        _local ?? throw new InvalidOperationException(
            "Local research execution is unavailable; configure TAVILY_API_KEY. Existing Nebius lifecycle reconciliation/cancellation remains available when composed.");

    private IResearchCloudExecutionCoordinator RequireCloud() =>
        _cloud ?? throw new InvalidOperationException(
            "Nebius research lifecycle support is unavailable in this product composition; local replay is intentionally blocked.");

    private async Task<AgentJobRecord> GetRequiredResearchAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));

        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException($"Job '{job.JobId}' is not a research job.");
        return job;
    }
}
