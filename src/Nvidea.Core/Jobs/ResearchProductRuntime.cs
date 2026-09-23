using Nvidea.Core.Research;

namespace Nvidea.Core.Jobs;

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

public interface IResearchCloudExecutionCoordinator
{
    Task<ResearchJobStatus> DispatchCurrentStageAsync(Guid jobId, ResearchCloudAuthorization authorization, TimeSpan? workItemLifetime = null, CancellationToken cancellationToken = default);
    Task<ResearchJobStatus> ReconcileAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<ResearchJobStatus> RequestCancellationAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public sealed class ResearchProductRuntime
{
    private readonly ILocalResearchRuntime? _local;
    private readonly IResearchCloudExecutionCoordinator? _cloud;
    private readonly JsonAgentJobStore _store;

    public ResearchProductRuntime(string stateDirectory, ILocalResearchRuntime? local, IResearchCloudExecutionCoordinator? cloud = null, bool remoteDispatchEnabled = false)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory)) throw new ArgumentException("Research state directory is required.", nameof(stateDirectory));
        _local = local;
        _cloud = cloud;
        RemoteDispatchEnabled = remoteDispatchEnabled;
        if (RemoteDispatchEnabled && _cloud is null) throw new ArgumentException("Remote dispatch cannot be enabled without a cloud execution coordinator.", nameof(cloud));
        if (RemoteDispatchEnabled && _local is null) throw new ArgumentException("Remote dispatch cannot be enabled without the local research runtime that creates and validates eligible checkpoints.", nameof(local));
        var root = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(root);
        _store = new JsonAgentJobStore(Path.Combine(root, "research-jobs.json"));
    }

    public bool LocalExecutionAvailable => _local is not null;
    public bool RemoteDispatchEnabled { get; }
    public bool RemoteLifecycleAvailable => _cloud is not null;

    public Task<ResearchJobStatus> CreateAsync(string question, CancellationToken cancellationToken = default) => RequireLocal().CreateAsync(question, cancellationToken);

    public async Task<IReadOnlyList<ResearchJobStatus>> ListAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        return jobs.Where(static job => string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal)).OrderByDescending(static job => job.UpdatedAt).Select(ResearchJobStatus.FromRecord).ToArray();
    }

    public async Task<ResearchJobStatus> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default) => ResearchJobStatus.FromRecord(await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false));

    public async Task<DurableResearchReceipt> ReadCompletedReceiptAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobHandler.ReadCompletedReceipt(job);
    }

    /// <summary>
    /// Reads only canonical, payload-free citation authority from the durable completed checkpoint.
    /// This works for locally completed and remotely ingested results and deliberately does not
    /// require a configured local Tavily/Nemotron execution runtime.
    /// </summary>
    public async Task<ResearchJudgeEvidence> ReadCompletedJudgeEvidenceAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobHandler.ReadCompletedJudgeEvidence(job);
    }

    public async Task<ResearchJobStatus> RunNextLocalStepAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (ResearchJobStatus.HasUnfinishedRemoteProvenance(current)) throw new InvalidOperationException("Remote or ambiguous research must be reconciled before any local stage can run.");
        if (current.ExecutionLocation != JobExecutionLocation.Local) throw new InvalidOperationException("Only local research can run through the local product action.");
        return await RequireLocal().RunNextStepAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> RecoverInterruptedAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (ResearchJobStatus.HasUnfinishedRemoteProvenance(current)) throw new InvalidOperationException("Remote or ambiguous research must be reconciled instead of locally recovered.");
        return await RequireLocal().RecoverInterruptedAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> DispatchCurrentStageAsync(Guid jobId, ResearchCloudAuthorization authorization, TimeSpan? workItemLifetime = null, CancellationToken cancellationToken = default)
    {
        if (!RemoteDispatchEnabled) throw new InvalidOperationException("Nebius Serverless research dispatch is disabled until the live deployment contract and local research runtime are explicitly enabled.");
        return await RequireCloud().DispatchCurrentStageAsync(jobId, authorization, workItemLifetime, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> ReconcileRemoteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (!ResearchJobStatus.HasUnfinishedRemoteProvenance(current)) throw new InvalidOperationException("Research job has no unfinished remote lifecycle to reconcile.");
        return await RequireCloud().ReconcileAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResearchJobStatus> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var remoteState = current.RemoteResearch?.State;
        if (remoteState == RemoteResearchProvenanceState.DispatchReserved) throw new InvalidOperationException("Nebius dispatch outcome is ambiguous; reconcile the reservation before attempting cancellation.");
        if (remoteState == RemoteResearchProvenanceState.Dispatched) return await RequireCloud().RequestCancellationAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (remoteState == RemoteResearchProvenanceState.CancelRequested) throw new InvalidOperationException("Nebius cancellation is already requested; reconcile provider state instead.");
        if (current.ExecutionLocation != JobExecutionLocation.Local) throw new InvalidOperationException("Non-local research without a supported remote lifecycle cannot be cancelled locally.");
        return await RequireLocal().CancelAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public Task<ResearchReport> ReadCompletedReportAsync(Guid jobId, CancellationToken cancellationToken = default) => RequireLocal().ReadCompletedReportAsync(jobId, cancellationToken);

    private ILocalResearchRuntime RequireLocal() => _local ?? throw new InvalidOperationException("Local research execution is unavailable; configure TAVILY_API_KEY. Existing Nebius lifecycle reconciliation/cancellation remains available when composed.");
    private IResearchCloudExecutionCoordinator RequireCloud() => _cloud ?? throw new InvalidOperationException("Nebius research lifecycle support is unavailable in this product composition; local replay is intentionally blocked.");

    private async Task<AgentJobRecord> GetRequiredResearchAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("Research job id is required.", nameof(jobId));
        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal)) throw new InvalidOperationException($"Job '{job.JobId}' is not a research job.");
        return job;
    }
}
