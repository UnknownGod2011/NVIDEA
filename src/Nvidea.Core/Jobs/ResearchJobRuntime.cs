using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Research;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Trusted local host for durable research jobs. This runtime intentionally executes job steps
/// in-process today; it does not claim Nebius Serverless execution until a real remote dispatcher
/// owns that boundary. Durable checkpoints are stored by <see cref="JsonAgentJobStore"/> and are
/// protected by Windows DPAPI when running on Windows.
///
/// The runtime owns an OS-backed lease for the research state directory for its entire lifetime.
/// This prevents two NVIDEA processes from concurrently mutating the same durable research jobs,
/// including interrupted-stage recovery. Dispose the runtime before another process/runtime is
/// allowed to take ownership of the same research state.
/// </summary>
public sealed class ResearchJobRuntime : IDisposable
{
    public const string CapabilityId = "research.deep";

    private static readonly IReadOnlySet<DataPermission> Permissions = new HashSet<DataPermission>
    {
        DataPermission.NetworkAccess
    };

    private readonly IAgentJobStore _store;
    private readonly IAuditTrail _auditTrail;
    private readonly ResumableJobOrchestrator _orchestrator;
    private readonly SemaphoreSlim _recoveryGate = new(1, 1);
    private readonly StateDirectoryLease _stateLease;
    private bool _disposed;

    public ResearchJobRuntime(
        string stateDirectory,
        ResearchEngine engine,
        IAuditTrail? auditTrail = null)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("Research state directory is required.", nameof(stateDirectory));
        ArgumentNullException.ThrowIfNull(engine);

        var root = Path.GetFullPath(stateDirectory);
        var lease = StateDirectoryLease.Acquire(root);
        try
        {
            _store = new JsonAgentJobStore(Path.Combine(root, "research-jobs.json"));
            _auditTrail = auditTrail ?? new JsonLinesAuditTrail(Path.Combine(root, "research-audit.jsonl"));
            _orchestrator = new ResumableJobOrchestrator(
                _store,
                new LocalResearchExecutionPolicy(),
                _auditTrail,
                new IAgentJobHandler[] { new ResearchJobHandler(engine) });
            _stateLease = lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public async Task<ResearchJobStatus> CreateAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
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
        ThrowIfDisposed();
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
        ThrowIfDisposed();
        var job = await _orchestrator.RunNextStepAsync(jobId, cancellationToken).ConfigureAwait(false);
        EnsureResearch(job);
        return ResearchJobStatus.FromRecord(job);
    }

    /// <summary>
    /// Explicitly re-arms a stale local research job left in Running by a process crash.
    /// This transition never executes provider work itself. It is deliberately research-only,
    /// requires a known checkpoint and a grace period, and preserves the current checkpoint so
    /// the next user-initiated RunNextStepAsync retries exactly the interrupted stage.
    /// </summary>
    public async Task<ResearchJobStatus> RecoverInterruptedAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _recoveryGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            var job = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
            var now = DateTimeOffset.UtcNow;
            if (job.State != AgentJobState.Running)
                throw new InvalidOperationException("Only an interrupted Running research job can be recovered.");
            if (!ResearchJobStatus.IsRecoverableCheckpoint(job.Checkpoint?.Step))
                throw new InvalidOperationException("Interrupted research checkpoint is not safe to retry.");
            if (job.ExecutionLocation != JobExecutionLocation.Local)
                throw new InvalidOperationException("Only local research execution can be recovered by this runtime.");
            if (job.ApprovalScope is not null)
                throw new InvalidOperationException("Research recovery cannot carry an approval scope.");
            if (job.Attempt >= job.Definition.MaxAttempts)
                throw new InvalidOperationException("Research retry limit has been reached.");
            if (job.UpdatedAt > now - ResearchJobStatus.InterruptedRecoveryDelay)
                throw new InvalidOperationException("Research is not stale enough to recover yet.");

            var pending = job with
            {
                State = AgentJobState.Pending,
                LastError = null,
                NextAttemptAt = null,
                UpdatedAt = now
            };
            await _store.SaveAsync(pending, cancellationToken).ConfigureAwait(false);
            await _auditTrail.AppendAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    now,
                    pending.Definition.CapabilityId,
                    pending.JobId.ToString("N"),
                    "research.interrupted_rearmed",
                    pending.Definition.Risk,
                    true,
                    false,
                    string.Empty,
                    "User explicitly re-armed an interrupted research stage; retry may repeat provider work and cost.",
                    new Dictionary<string, string>
                    {
                        ["jobType"] = pending.Definition.JobType,
                        ["state"] = pending.State.ToString(),
                        ["executionLocation"] = pending.ExecutionLocation.ToString(),
                        ["attempt"] = pending.Attempt.ToString()
                    }),
                cancellationToken).ConfigureAwait(false);
            return ResearchJobStatus.FromRecord(pending);
        }
        finally
        {
            _recoveryGate.Release();
        }
    }

    public async Task<ResearchJobStatus> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var existing = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var job = await _orchestrator.CancelAsync(existing.JobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobStatus.FromRecord(job);
    }

    public async Task<ResearchReport> ReadCompletedReportAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var job = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobHandler.ReadCompletedReport(job);
    }

    public async Task<ResearchJobStatus> GetStatusAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var job = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        return ResearchJobStatus.FromRecord(job);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _stateLease.Dispose();
        _recoveryGate.Dispose();
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

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

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
