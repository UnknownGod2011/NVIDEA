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

    public Task<AgentJobRecord> CreateAsync(
        AgentJobDefinition definition,
        CancellationToken cancellationToken = default) =>
        CreateAsync(Guid.NewGuid(), definition, initialCheckpoint: null, cancellationToken);

    public Task<AgentJobRecord> CreateAsync(
        AgentJobDefinition definition,
        AgentJobCheckpoint? initialCheckpoint,
        CancellationToken cancellationToken = default) =>
        CreateAsync(Guid.NewGuid(), definition, initialCheckpoint, cancellationToken);

    /// <summary>
    /// Creates a job using a caller-supplied durable identity. This is intentionally idempotent
    /// for an already-existing equivalent job so a parent workflow can persist the child id before
    /// child creation/execution and safely reconcile after process failure.
    /// </summary>
    public async Task<AgentJobRecord> CreateAsync(
        Guid jobId,
        AgentJobDefinition definition,
        AgentJobCheckpoint? initialCheckpoint = null,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.JobType) || string.IsNullOrWhiteSpace(definition.CapabilityId))
            throw new ArgumentException("Job type and capability id are required.", nameof(definition));
        if (definition.MaxAttempts is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(definition), "MaxAttempts must be between 1 and 10.");
        if (!_handlers.ContainsKey(definition.JobType))
            throw new InvalidOperationException($"No handler is registered for job type '{definition.JobType}'.");
        if (initialCheckpoint is not null && string.IsNullOrWhiteSpace(initialCheckpoint.Step))
            throw new ArgumentException("Initial checkpoints require a non-empty step.", nameof(initialCheckpoint));

        var existing = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (!DefinitionEquivalent(existing.Definition, definition)
                || !CheckpointEquivalent(existing.Checkpoint, initialCheckpoint))
            {
                throw new InvalidOperationException($"Job '{jobId}' already exists with a different definition or checkpoint.");
            }

            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var record = new AgentJobRecord(jobId, definition, AgentJobState.Pending,
            _executionPolicy.Choose(definition), 0, initialCheckpoint, null, null, now, now);
        var createdAudit = PrepareAudit(record, "job.created", true, false, "Job created.");
        await _store.SaveAsync(record, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync(createdAudit, cancellationToken).ConfigureAwait(false);
        return record;
    }

    public async Task<AgentJobRecord> RunNextStepAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State is AgentJobState.Completed or AgentJobState.Failed or AgentJobState.Cancelled) return job;
        if (job.State == AgentJobState.WaitingForApproval) return job;
        // A durable Running record may be the residue of a process crash after a side effect began.
        // Replaying it automatically could duplicate a consequential action, so recovery is fail-closed.
        if (job.State == AgentJobState.Running) return job;
        if (job.State == AgentJobState.RetryScheduled && job.NextAttemptAt is { } next && next > DateTimeOffset.UtcNow) return job;

        ValidateJobAuditContract(job);
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
                var awaitingApprovalAudit = PrepareAudit(updated, "job.awaiting_approval", true, false, "Job paused for explicit approval.");
                await AppendAuditAsync(awaitingApprovalAudit, cancellationToken).ConfigureAwait(false);
            }
            else if (result.Completed)
            {
                updated = running with { State = AgentJobState.Completed, Checkpoint = checkpoint, ApprovalScope = null, UpdatedAt = DateTimeOffset.UtcNow };
                var completedAudit = PrepareAudit(updated, "job.completed", true, true, "Job completed.");
                await AppendAuditAsync(completedAudit, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                updated = running with { State = AgentJobState.Pending, Checkpoint = checkpoint, ApprovalScope = null, UpdatedAt = DateTimeOffset.UtcNow };
            }
            await _store.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            return updated;
        }
        catch (JobAuditContractException)
        {
            _ephemeralApprovals.Revoke(jobId);
            // The handler may already have started a consequential operation. Leaving the durable
            // record Running blocks automatic replay until the producer contract is corrected or a
            // trusted recovery path verifies the outcome.
            throw;
        }
        catch (AmbiguousJobExecutionException)
        {
            _ephemeralApprovals.Revoke(jobId);
            var ambiguous = running with
            {
                LastError = "Execution outcome is ambiguous; fresh verification is required before replay.",
                NextAttemptAt = null,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var ambiguousAudit = PrepareAudit(
                ambiguous,
                "job.execution_ambiguous",
                false,
                false,
                "Job may have produced a side effect but verification was inconclusive; automatic retry was blocked.");
            await _store.SaveAsync(ambiguous, cancellationToken).ConfigureAwait(false);
            await AppendAuditAsync(ambiguousAudit, cancellationToken).ConfigureAwait(false);
            return ambiguous;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _ephemeralApprovals.Revoke(jobId);
            if (string.Equals(running.Definition.JobType, BrowserActionTerminalCheckpoint.JobType, StringComparison.OrdinalIgnoreCase))
            {
                var ambiguousCancellation = running with
                {
                    LastError = "Browser execution was cancelled while in flight; fresh verification is required before replay.",
                    NextAttemptAt = null,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                var cancellationAudit = PrepareAudit(
                    ambiguousCancellation,
                    "job.cancellation_ambiguous",
                    false,
                    false,
                    "Browser execution was cancelled while in flight; automatic replay was blocked pending fresh verification.");
                await _store.SaveAsync(ambiguousCancellation, CancellationToken.None).ConfigureAwait(false);
                await AppendAuditAsync(cancellationAudit, CancellationToken.None).ConfigureAwait(false);
                return ambiguousCancellation;
            }

            var cancelled = running with { State = AgentJobState.Cancelled, LastError = "Cancelled", UpdatedAt = DateTimeOffset.UtcNow };
            var cancelledAudit = PrepareAudit(cancelled, "job.cancelled", false, false, "Job cancelled.");
            await _store.SaveAsync(cancelled, CancellationToken.None).ConfigureAwait(false);
            await AppendAuditAsync(cancelledAudit, CancellationToken.None).ConfigureAwait(false);
            return cancelled;
        }
        catch (Exception ex)
        {
            _ephemeralApprovals.Revoke(jobId);
            var exhausted = running.Attempt >= running.Definition.MaxAttempts;
            var failureDiagnostic = JobFailureDiagnostic.FromException(ex);
            var failed = running with
            {
                State = exhausted ? AgentJobState.Failed : AgentJobState.RetryScheduled,
                LastError = failureDiagnostic,
                NextAttemptAt = exhausted ? null : DateTimeOffset.UtcNow + RetryDelay(running.Attempt),
                UpdatedAt = DateTimeOffset.UtcNow
            };
            failed = BrowserActionTerminalCheckpoint.ScrubIfTerminal(failed);
            var failureAudit = PrepareAudit(
                failed,
                exhausted ? "job.failed" : "job.retry_scheduled",
                false,
                false,
                exhausted
                    ? "Job execution failed after exhausting retries; untrusted handler/provider diagnostic text was not copied into audit."
                    : "Job execution failed and retry was scheduled; untrusted handler/provider diagnostic text was not copied into audit.");
            await _store.SaveAsync(failed, cancellationToken).ConfigureAwait(false);
            await AppendAuditAsync(failureAudit, cancellationToken).ConfigureAwait(false);
            return failed;
        }
    }

    public async Task<AgentJobRecord> ResumeAfterApprovalAsync(Guid jobId, string approvalScope, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State != AgentJobState.WaitingForApproval) throw new InvalidOperationException("Job is not waiting for approval.");
        if (!string.Equals(job.ApprovalScope, approvalScope, StringComparison.Ordinal)) throw new UnauthorizedAccessException("Approval scope does not match the paused action.");

        var resumed = job with { State = AgentJobState.Pending, ApprovalScope = null, UpdatedAt = DateTimeOffset.UtcNow };
        var auditableApproval = resumed with { ApprovalScope = approvalScope };
        var approvedAudit = PrepareAudit(auditableApproval, "job.approved", true, true, "Exact paused action approved with ephemeral single-use execution grant.");
        await _store.SaveAsync(resumed, cancellationToken).ConfigureAwait(false);
        var grant = _approvalAuthorizer.GrantExactScope(approvalScope, TimeSpan.FromMinutes(2));
        _ephemeralApprovals.Put(jobId, grant);
        await AppendAuditAsync(approvedAudit, cancellationToken).ConfigureAwait(false);
        return resumed;
    }

    /// <summary>
    /// Restores a non-authorizing approval wait after a restart lost an ephemeral grant before
    /// execution began. This may only re-arm a Pending job; Running is intentionally ambiguous.
    /// </summary>
    public async Task<AgentJobRecord> RearmApprovalAsync(
        Guid jobId,
        string approvalScope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(approvalScope))
            throw new ArgumentException("Approval scope is required.", nameof(approvalScope));

        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State == AgentJobState.WaitingForApproval)
        {
            if (!string.Equals(job.ApprovalScope, approvalScope, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("Persisted approval scope does not match the paused job.");
            return job;
        }
        if (job.State != AgentJobState.Pending)
            throw new InvalidOperationException("Only a pending job can safely be re-armed for approval after restart.");

        _ephemeralApprovals.Revoke(jobId);
        var waiting = job with
        {
            State = AgentJobState.WaitingForApproval,
            ApprovalScope = approvalScope,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var rearmedAudit = PrepareAudit(
            waiting,
            "job.approval_rearmed",
            true,
            false,
            "Approval wait restored after restart; no execution grant was created.");
        await _store.SaveAsync(waiting, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync(rearmedAudit, cancellationToken).ConfigureAwait(false);
        return waiting;
    }

    /// <summary>
    /// Completes a durable Running job only after a trusted host has independently proven the
    /// intended post-action state from fresh evidence. This transition never invokes a handler,
    /// never restores an approval grant, and exists solely to avoid replaying ambiguous side effects.
    /// </summary>
    internal async Task<AgentJobRecord> CompleteAmbiguousRunningAsync(
        Guid jobId,
        AgentJobCheckpoint verifiedCheckpoint,
        string evidenceSummary,
        CancellationToken cancellationToken = default)
    {
        if (verifiedCheckpoint is null)
            throw new ArgumentNullException(nameof(verifiedCheckpoint));
        if (string.IsNullOrWhiteSpace(verifiedCheckpoint.Step))
            throw new ArgumentException("A verified checkpoint step is required.", nameof(verifiedCheckpoint));
        if (string.IsNullOrWhiteSpace(evidenceSummary))
            throw new ArgumentException("An evidence summary is required.", nameof(evidenceSummary));

        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State != AgentJobState.Running)
            throw new InvalidOperationException("Only a durable Running job may be completed through ambiguous-side-effect reconciliation.");

        var completed = job with
        {
            State = AgentJobState.Completed,
            Checkpoint = verifiedCheckpoint,
            ApprovalScope = null,
            LastError = null,
            NextAttemptAt = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var reconciliationAudit = PrepareAudit(
            completed,
            "job.reconciled_completed",
            true,
            false,
            $"Ambiguous in-flight job was marked complete from fresh post-crash evidence without replay. {evidenceSummary}");
        _ephemeralApprovals.Revoke(jobId);
        await _store.SaveAsync(completed, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync(reconciliationAudit, cancellationToken).ConfigureAwait(false);
        return completed;
    }

    public async Task<AgentJobRecord> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job.State is AgentJobState.Completed or AgentJobState.Failed or AgentJobState.Cancelled) return job;
        var cancelledRaw = job with { State = AgentJobState.Cancelled, LastError = "Cancelled by user", UpdatedAt = DateTimeOffset.UtcNow };
        var cancelled = job.State == AgentJobState.Running
            && string.Equals(job.Definition.JobType, BrowserActionTerminalCheckpoint.JobType, StringComparison.OrdinalIgnoreCase)
            ? cancelledRaw
            : BrowserActionTerminalCheckpoint.ScrubIfTerminal(cancelledRaw);
        var cancellationAudit = PrepareAudit(
            cancelled,
            "job.cancelled",
            false,
            false,
            job.State == AgentJobState.Running
                && string.Equals(job.Definition.JobType, BrowserActionTerminalCheckpoint.JobType, StringComparison.OrdinalIgnoreCase)
                ? "In-flight browser job cancelled; executable checkpoint retained only for ambiguous-side-effect verification."
                : "Job cancelled by user.");
        _ephemeralApprovals.Revoke(jobId);
        await _store.SaveAsync(cancelled, cancellationToken).ConfigureAwait(false);
        await AppendAuditAsync(cancellationAudit, cancellationToken).ConfigureAwait(false);
        return cancelled;
    }

    private async Task<AgentJobRecord> GetRequiredAsync(Guid jobId, CancellationToken cancellationToken) =>
        await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException($"Job '{jobId}' was not found.");

    private static AuditEvent PrepareAudit(AgentJobRecord job, string eventType, bool allowed, bool approved, string summary)
    {
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            job.Definition.CapabilityId,
            job.JobId.ToString("N"),
            eventType,
            job.Definition.Risk,
            allowed,
            approved,
            job.ApprovalScope ?? string.Empty,
            summary,
            new Dictionary<string, string>
            {
                ["jobType"] = job.Definition.JobType,
                ["state"] = job.State.ToString(),
                ["executionLocation"] = job.ExecutionLocation.ToString(),
                ["attempt"] = job.Attempt.ToString()
            });

        try
        {
            AuditEventTrust.ValidateForPersistence(auditEvent, nameof(auditEvent));
        }
        catch (ArgumentException ex)
        {
            throw new JobAuditContractException("Job audit data violates the durable audit trust contract.", ex);
        }

        return auditEvent;
    }

    private static void ValidateJobAuditContract(AgentJobRecord job) =>
        _ = PrepareAudit(job with { ApprovalScope = null }, "job.contract_check", false, false, "Job audit contract validated before execution state mutation.");

    private Task AppendAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        _auditTrail.AppendAsync(auditEvent, cancellationToken);

    private static bool DefinitionEquivalent(AgentJobDefinition left, AgentJobDefinition right) =>
        string.Equals(left.JobType, right.JobType, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.CapabilityId, right.CapabilityId, StringComparison.Ordinal)
        && left.Risk == right.Risk
        && left.ContainsPrivateOsData == right.ContainsPrivateOsData
        && left.BenefitsFromBackgroundExecution == right.BenefitsFromBackgroundExecution
        && left.MaxAttempts == right.MaxAttempts
        && left.RequiredPermissions.SetEquals(right.RequiredPermissions);

    private static bool CheckpointEquivalent(AgentJobCheckpoint? left, AgentJobCheckpoint? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return string.Equals(left.Step, right.Step, StringComparison.Ordinal)
            && string.Equals(left.Payload, right.Payload, StringComparison.Ordinal);
    }

    private static TimeSpan RetryDelay(int attempt) => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, Math.Clamp(attempt, 1, 6))));

    private sealed class JobAuditContractException : InvalidOperationException
    {
        public JobAuditContractException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
