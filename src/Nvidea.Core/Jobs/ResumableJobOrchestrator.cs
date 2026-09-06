using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

public sealed class ResumableJobOrchestrator
{
    private readonly IAgentJobStore _store;
    private readonly IJobExecutionPolicy _executionPolicy;
    private readonly IAuditTrail _auditTrail;
    private readonly IReadOnlyDictionary<string, IAgentJobHandler> _handlers;
    private readonly ScopedApprovalAuthorizer _approvalAuthorizer;
    private readonly EphemeralJobApprovalStore _ephemeralApprovals;

    public ResumableJobOrchestrator(
        IAgentJobStore store,
        IJobExecutionPolicy executionPolicy,
        IAuditTrail auditTrail,
        IEnumerable<IAgentJobHandler> handlers,
        ScopedApprovalAuthorizer? approvalAuthorizer = null,
        EphemeralJobApprovalStore? ephemeralApprovals = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _executionPolicy = executionPolicy ?? throw new ArgumentNullException(nameof(executionPolicy));
        _auditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
        _handlers = (handlers ?? throw new ArgumentNullException(nameof(handlers)))
            .ToDictionary(x => x.JobType, StringComparer.OrdinalIgnoreCase);
        _approvalAuthorizer = approvalAuthorizer ?? new ScopedApprovalAuthorizer();
        _ephemeralApprovals = ephemeralApprovals ?? new EphemeralJobApprovalStore();
    }

    public async Task<AgentJobRecord> CreateAsync(AgentJobDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.JobType) || string.IsNullOrWhiteSpace(definition.CapabilityId))
            throw new ArgumentException("Job type and capability id are required.", nameof(definition));
        if (definition.MaxAttempts is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(definition), "MaxAttempts must be between 1 and 10.");
        if (!_handlers.ContainsKey(definition.JobType))
            throw new InvalidOperationException($"No handler is registered for job type '{definition.JobType}'.");

        var now = DateTimeOffset.UtcNow;
        var record = new AgentJobRecord(Guid.NewGuid(), definition, AgentJobState.Pending,
            _executionPolicy.Choose(definition), 0, null, null, null, now, now);
        await _store.SaveAsync(record, cancellationToken).ConfigureAwait(false);
        await AuditAsync(record, "job.created", true, false, "Job created.", cancellationToken).ConfigureAwait(false);
        return record;
    }

    public async Task<AgentJobRecord> RunNextStepAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State is AgentJobState.Completed or AgentJobState.Failed or AgentJobState.Cancelled) return job;
        if (job.State == AgentJobState.WaitingForApproval) return job;
        if (job.State == AgentJobState.RetryScheduled && job.NextAttemptAt is { } next && next > DateTimeOffset.UtcNow) return job;

        var running = job with { State = AgentJobState.Running, Attempt = job.Attempt + 1, LastError = null, NextAttemptAt = null, UpdatedAt = DateTimeOffset.UtcNow };
        await _store.SaveAsync(running, cancellationToken).ConfigureAwait(false);
        var executionContext = new JobExecutionContext(jobId, _ephemeralApprovals.Take(jobId));

        try
        {
            var result = await _handlers[job.Definition.JobType].ExecuteStepAsync(running, executionContext, cancellationToken).ConfigureAwait(false);
            var checkpoint = string.IsNullOrWhiteSpace(result.CheckpointStep) ? running.Checkpoint : new AgentJobCheckpoint(result.CheckpointStep!, result.CheckpointPayload, DateTimeOffset.UtcNow);
            AgentJobRecord updated;
            if (result.RequiresApproval)
            {
                if (string.IsNullOrWhiteSpace(result.ApprovalScope)) throw new InvalidOperationException("Approval-paused jobs require an exact approval scope.");
                updated = running with { State = AgentJobState.WaitingForApproval, Checkpoint = checkpoint, ApprovalScope = result.ApprovalScope, UpdatedAt = DateTimeOffset.UtcNow };
                await AuditAsync(updated, "job.awaiting_approval", true, false, "Job paused for explicit approval.", cancellationToken).ConfigureAwait(false);
            }
            else if (result.Completed)
            {
                updated = running with { State = AgentJobState.Completed, Checkpoint = checkpoint, ApprovalScope = null, UpdatedAt = DateTimeOffset.UtcNow };
                await AuditAsync(updated, "job.completed", true, true, "Job completed.", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                updated = running with { State = AgentJobState.Pending, Checkpoint = checkpoint, ApprovalScope = null, UpdatedAt = DateTimeOffset.UtcNow };
            }
            await _store.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            return updated;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ephemeralApprovals.Revoke(jobId);
            var cancelled = running with { State = AgentJobState.Cancelled, LastError = "Cancelled", UpdatedAt = DateTimeOffset.UtcNow };
            await _store.SaveAsync(cancelled, CancellationToken.None).ConfigureAwait(false);
            await AuditAsync(cancelled, "job.cancelled", false, false, "Job cancelled.", CancellationToken.None).ConfigureAwait(false);
            return cancelled;
        }
        catch (Exception ex)
        {
            _ephemeralApprovals.Revoke(jobId);
            var exhausted = running.Attempt >= running.Definition.MaxAttempts;
            var failed = running with { State = exhausted ? AgentJobState.Failed : AgentJobState.RetryScheduled, LastError = ex.Message, NextAttemptAt = exhausted ? null : DateTimeOffset.UtcNow + RetryDelay(running.Attempt), UpdatedAt = DateTimeOffset.UtcNow };
            await _store.SaveAsync(failed, cancellationToken).ConfigureAwait(false);
            await AuditAsync(failed, exhausted ? "job.failed" : "job.retry_scheduled", false, false, ex.Message, cancellationToken).ConfigureAwait(false);
            return failed;
        }
    }

    public async Task<AgentJobRecord> ResumeAfterApprovalAsync(Guid jobId, string approvalScope, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State != AgentJobState.WaitingForApproval) throw new InvalidOperationException("Job is not waiting for approval.");
        if (!string.Equals(job.ApprovalScope, approvalScope, StringComparison.Ordinal)) throw new UnauthorizedAccessException("Approval scope does not match the paused action.");

        var resumed = job with { State = AgentJobState.Pending, ApprovalScope = null, UpdatedAt = DateTimeOffset.UtcNow };
        await _store.SaveAsync(resumed, cancellationToken).ConfigureAwait(false);
        var grant = _approvalAuthorizer.GrantExactScope(approvalScope, TimeSpan.FromMinutes(2));
        _ephemeralApprovals.Put(jobId, grant);
        var auditableApproval = resumed with { ApprovalScope = approvalScope };
        await AuditAsync(auditableApproval, "job.approved", true, true, "Exact paused action approved with ephemeral single-use execution grant.", cancellationToken).ConfigureAwait(false);
        return resumed;
    }

    public async Task<AgentJobRecord> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State is AgentJobState.Completed or AgentJobState.Failed or AgentJobState.Cancelled) return job;
        _ephemeralApprovals.Revoke(jobId);
        var cancelled = job with { State = AgentJobState.Cancelled, LastError = "Cancelled by user", UpdatedAt = DateTimeOffset.UtcNow };
        await _store.SaveAsync(cancelled, cancellationToken).ConfigureAwait(false);
        await AuditAsync(cancelled, "job.cancelled", false, false, "Job cancelled by user.", cancellationToken).ConfigureAwait(false);
        return cancelled;
    }

    private async Task<AgentJobRecord> GetRequiredAsync(Guid jobId, CancellationToken cancellationToken) =>
        await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException($"Job '{jobId}' was not found.");

    private Task AuditAsync(AgentJobRecord job, string eventType, bool allowed, bool approved, string summary, CancellationToken cancellationToken) =>
        _auditTrail.AppendAsync(new AuditEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, job.Definition.CapabilityId, job.JobId.ToString("N"), eventType, job.Definition.Risk, allowed, approved, job.ApprovalScope ?? string.Empty, summary,
            new Dictionary<string, string> { ["jobType"] = job.Definition.JobType, ["state"] = job.State.ToString(), ["executionLocation"] = job.ExecutionLocation.ToString(), ["attempt"] = job.Attempt.ToString() }), cancellationToken);

    private static TimeSpan RetryDelay(int attempt) => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, Math.Clamp(attempt, 1, 6))));
}
