using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class JobAuditContractOrderingTests
{
    [Fact]
    public async Task Create_rejects_malformed_capability_before_store_mutation()
    {
        var store = new TrackingStore();
        var audit = new TrackingAudit();
        var orchestrator = Create(store, audit, new FixedHandler("research", new JobStepResult(true)));
        var definition = Definition(capabilityId: "research.web\nforged");

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => orchestrator.CreateAsync(definition));

        Assert.Equal(0, store.SaveCount);
        Assert.Empty(audit.Events);
        Assert.Empty(await store.ListAsync());
    }

    [Fact]
    public async Task Loaded_job_with_malformed_audit_metadata_is_rejected_before_running_save()
    {
        const string malformedJobType = "research\nforged";
        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();
        var record = new AgentJobRecord(
            jobId,
            Definition(jobType: malformedJobType),
            AgentJobState.Pending,
            JobExecutionLocation.Local,
            0,
            null,
            null,
            null,
            now,
            now);
        var store = new TrackingStore(record);
        var audit = new TrackingAudit();
        var handler = new FixedHandler(malformedJobType, new JobStepResult(true));
        var orchestrator = Create(store, audit, handler);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => orchestrator.RunNextStepAsync(jobId));

        Assert.Equal(0, store.SaveCount);
        Assert.Empty(audit.Events);
        Assert.Equal(AgentJobState.Pending, (await store.GetAsync(jobId))!.State);
        Assert.Equal(0, handler.ExecutionCount);
    }

    [Fact]
    public async Task Malformed_handler_approval_scope_leaves_job_running_and_is_not_replayed()
    {
        var store = new TrackingStore();
        var audit = new TrackingAudit();
        var handler = new FixedHandler(
            "research",
            new JobStepResult(false, RequiresApproval: true, ApprovalScope: "browser.agent|abc|BrowserWrite\nforged"));
        var orchestrator = Create(store, audit, handler);
        var created = await orchestrator.CreateAsync(Definition());
        var savesAfterCreate = store.SaveCount;

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => orchestrator.RunNextStepAsync(created.JobId));

        var durable = await store.GetAsync(created.JobId);
        Assert.NotNull(durable);
        Assert.Equal(AgentJobState.Running, durable!.State);
        Assert.Null(durable.ApprovalScope);
        Assert.Equal(savesAfterCreate + 1, store.SaveCount);
        Assert.Equal(1, handler.ExecutionCount);
        Assert.DoesNotContain(audit.Events, x => x.EventType == "job.awaiting_approval");

        var replayAttempt = await orchestrator.RunNextStepAsync(created.JobId);
        Assert.Equal(AgentJobState.Running, replayAttempt.State);
        Assert.Equal(1, handler.ExecutionCount);
    }

    [Fact]
    public async Task Resume_rejects_corrupt_scope_before_state_mutation_or_approval_audit()
    {
        const string malformedScope = "browser.agent|abc|BrowserWrite\nforged";
        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();
        var waiting = new AgentJobRecord(
            jobId,
            Definition(),
            AgentJobState.WaitingForApproval,
            JobExecutionLocation.Local,
            1,
            null,
            malformedScope,
            null,
            now,
            now);
        var store = new TrackingStore(waiting);
        var audit = new TrackingAudit();
        var orchestrator = Create(store, audit, new FixedHandler("research", new JobStepResult(true)));

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => orchestrator.ResumeAfterApprovalAsync(jobId, malformedScope));

        Assert.Equal(0, store.SaveCount);
        Assert.Empty(audit.Events);
        Assert.Equal(AgentJobState.WaitingForApproval, (await store.GetAsync(jobId))!.State);
        Assert.Equal(malformedScope, (await store.GetAsync(jobId))!.ApprovalScope);
    }

    [Fact]
    public async Task Reconciliation_rejects_multiline_evidence_before_marking_running_job_complete()
    {
        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();
        var running = new AgentJobRecord(
            jobId,
            Definition(),
            AgentJobState.Running,
            JobExecutionLocation.Local,
            1,
            new AgentJobCheckpoint("executing", "opaque", now),
            null,
            "Execution outcome is ambiguous; fresh verification is required before replay.",
            now,
            now);
        var store = new TrackingStore(running);
        var audit = new TrackingAudit();
        var orchestrator = Create(store, audit, new FixedHandler("research", new JobStepResult(true)));
        var verified = new AgentJobCheckpoint("verified", "done", now.AddSeconds(1));

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            orchestrator.CompleteAmbiguousRunningAsync(jobId, verified, "verified\nforged audit line"));

        Assert.Equal(0, store.SaveCount);
        Assert.Empty(audit.Events);
        Assert.Equal(AgentJobState.Running, (await store.GetAsync(jobId))!.State);
        Assert.Equal("executing", (await store.GetAsync(jobId))!.Checkpoint?.Step);
    }

    private static AgentJobDefinition Definition(
        string jobType = "research",
        string capabilityId = "research.web") =>
        new(
            jobType,
            capabilityId,
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Medium,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: false,
            MaxAttempts: 3);

    private static ResumableJobOrchestrator Create(
        IAgentJobStore store,
        IAuditTrail audit,
        IAgentJobHandler handler) =>
        new(store, new ConservativeJobExecutionPolicy(), audit, new[] { handler });

    private sealed class FixedHandler : IAgentJobHandler
    {
        private readonly JobStepResult _result;

        public FixedHandler(string jobType, JobStepResult result)
        {
            JobType = jobType;
            _result = result;
        }

        public string JobType { get; }
        public int ExecutionCount { get; private set; }

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.FromResult(_result);
        }
    }

    private sealed class TrackingStore : IAgentJobStore
    {
        private readonly Dictionary<Guid, AgentJobRecord> _records = new();

        public TrackingStore(params AgentJobRecord[] records)
        {
            foreach (var record in records)
                _records[record.JobId] = record;
        }

        public int SaveCount { get; private set; }

        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records.TryGetValue(jobId, out var record) ? record : null);

        public Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentJobRecord>>(_records.Values.ToArray());

        public Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            _records[record.JobId] = record;
            return Task.CompletedTask;
        }
    }

    private sealed class TrackingAudit : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Events.ToArray());
    }
}
