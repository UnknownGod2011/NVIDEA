using Nvidea.Core.Capabilities;
using Nvidea.Core.Research;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Trusted local host for durable research jobs. This runtime intentionally executes job steps
/// in-process today; it does not claim Nebius Serverless execution until a real remote dispatcher
/// owns that boundary. Durable checkpoints are stored by <see cref="JsonAgentJobStore"/> and are
/// protected by Windows DPAPI when running on Windows.
/// </summary>
public sealed class ResearchJobRuntime
{
    public const string CapabilityId = "research.deep";

    private static readonly IReadOnlySet<DataPermission> Permissions = new HashSet<DataPermission>
    {
        DataPermission.NetworkAccess
    };

    private readonly IAgentJobStore _store;
    private readonly ResumableJobOrchestrator _orchestrator;

    public ResearchJobRuntime(
        string stateDirectory,
        ResearchEngine engine,
        IAuditTrail? auditTrail = null)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("Research state directory is required.", nameof(stateDirectory));
        ArgumentNullException.ThrowIfNull(engine);

        var root = Path.GetFullPath(stateDirectory);
        Directory.CreateDirectory(root);
        _store = new JsonAgentJobStore(Path.Combine(root, "research-jobs.json"));
        auditTrail ??= new JsonLinesAuditTrail(Path.Combine(root, "research-audit.jsonl"));
        _orchestrator = new ResumableJobOrchestrator(
            _store,
            new LocalResearchExecutionPolicy(),
            auditTrail,
            new IAgentJobHandler[] { new ResearchJobHandler(engine) });
    }

    public async Task<ResearchJobStatus> CreateAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var definition = new AgentJobDefinition(
            ResearchJobHandler.Type,
            CapabilityId,
            Permissions,
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: false,
            MaxAttempts: 3);

        var job = await _orchestrator.CreateAsync(
            definition,
            ResearchJobHandler.CreateInitialCheckpoint(question),
            cancellationToken).ConfigureAwait(false);
        return ResearchJobStatus.FromRecord(job);
    }

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

    public async Task<ResearchJobStatus> RunNextStepAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _orchestrator.RunNextStepAsync(jobId, cancellationToken).ConfigureAwait(false);
        EnsureResearch(job);
        return ResearchJobStatus.FromRecord(job);
    }

    public async Task<ResearchJobStatus> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var job = await _orchestrator.CancelAsync(existing.JobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobStatus.FromRecord(job);
    }

    public async Task<ResearchReport> ReadCompletedReportAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobHandler.ReadCompletedReport(job);
    }

    public async Task<ResearchJobStatus> GetStatusAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobStatus.FromRecord(job);
    }

    private async Task<AgentJobRecord> GetRequiredResearchAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));

        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        EnsureResearch(job);
        return job;
    }

    private static void EnsureResearch(AgentJobRecord job)
    {
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException($"Job '{job.JobId}' is not a research job.");
        if (job.ExecutionLocation != JobExecutionLocation.Local)
            throw new InvalidOperationException("Local research runtime encountered a non-local execution record.");
    }

    private sealed class LocalResearchExecutionPolicy : IJobExecutionPolicy
    {
        public JobExecutionLocation Choose(AgentJobDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);
            if (!string.Equals(definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
                throw new InvalidOperationException("Local research policy only accepts research jobs.");
            return JobExecutionLocation.Local;
        }
    }
}
