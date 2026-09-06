using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResumableJobOrchestratorTests
{
    [Fact]
    public void Execution_policy_keeps_private_os_data_local()
    {
        var policy = new ConservativeJobExecutionPolicy();
        var definition = Definition(privateOsData: true, background: true);

        Assert.Equal(JobExecutionLocation.Local, policy.Choose(definition));
    }

    [Fact]
    public void Execution_policy_routes_non_private_background_work_to_nebius()
    {
        var policy = new ConservativeJobExecutionPolicy();
        var definition = Definition(privateOsData: false, background: true);

        Assert.Equal(JobExecutionLocation.NebiusServerless, policy.Choose(definition));
    }

    [Fact]
    public async Task Job_persists_checkpoint_and_completes_across_steps()
    {
        var store = new InMemoryStore();
        var audit = new InMemoryAudit();
        var handler = new SequenceHandler(
            new JobStepResult(false, CheckpointStep: "searched", CheckpointPayload: "q=nemotron"),
            new JobStepResult(true, CheckpointStep: "synthesized", CheckpointPayload: "done"));
        var orchestrator = Create(store, audit, handler);

        var job = await orchestrator.CreateAsync(Definition());
        var first = await orchestrator.RunNextStepAsync(job.JobId);
        var second = await orchestrator.RunNextStepAsync(job.JobId);

        Assert.Equal(AgentJobState.Pending, first.State);
        Assert.Equal("searched", first.Checkpoint?.Step);
        Assert.Equal(AgentJobState.Completed, second.State);
        Assert.Equal("synthesized", second.Checkpoint?.Step);
        Assert.Contains(audit.Events, x => x.EventType == "job.completed");
    }

    [Fact]
    public async Task Approval_pause_requires_exact_scope()
    {
        var store = new InMemoryStore();
        var audit = new InMemoryAudit();
        var handler = new SequenceHandler(new JobStepResult(false, true, "browser.submit:abc", "form-ready"));
        var orchestrator = Create(store, audit, handler);

        var job = await orchestrator.CreateAsync(Definition());
        var paused = await orchestrator.RunNextStepAsync(job.JobId);

        Assert.Equal(AgentJobState.WaitingForApproval, paused.State);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => orchestrator.ResumeAfterApprovalAsync(job.JobId, "browser.submit:other"));
        var resumed = await orchestrator.ResumeAfterApprovalAsync(job.JobId, "browser.submit:abc");
        Assert.Equal(AgentJobState.Pending, resumed.State);
    }

    [Fact]
    public async Task Failures_schedule_retry_then_exhaust()
    {
        var store = new InMemoryStore();
        var audit = new InMemoryAudit();
        var handler = new ThrowingHandler();
        var orchestrator = Create(store, audit, handler);
        var job = await orchestrator.CreateAsync(Definition(maxAttempts: 2));

        var first = await orchestrator.RunNextStepAsync(job.JobId);
        Assert.Equal(AgentJobState.RetryScheduled, first.State);
        Assert.NotNull(first.NextAttemptAt);

        var forced = first with { NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(-1) };
        await store.SaveAsync(forced);
        var second = await orchestrator.RunNextStepAsync(job.JobId);
        Assert.Equal(AgentJobState.Failed, second.State);
    }

    [Fact]
    public async Task Json_store_round_trips_checkpoint_atomically()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nvidea-jobs-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "jobs.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var now = DateTimeOffset.UtcNow;
            var record = new AgentJobRecord(Guid.NewGuid(), Definition(), AgentJobState.Pending, JobExecutionLocation.Local, 1,
                new AgentJobCheckpoint("step-1", "payload", now), null, null, now, now);

            await store.SaveAsync(record);
            var loaded = await store.GetAsync(record.JobId);

            Assert.NotNull(loaded);
            Assert.Equal("step-1", loaded!.Checkpoint?.Step);
            Assert.Equal("payload", loaded.Checkpoint?.Payload);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    private static AgentJobDefinition Definition(bool privateOsData = false, bool background = false, int maxAttempts = 3) =>
        new("research", "research.web", new HashSet<DataPermission> { DataPermission.NetworkAccess }, CapabilityRiskLevel.Medium,
            privateOsData, background, maxAttempts);

    private static ResumableJobOrchestrator Create(IAgentJobStore store, IAuditTrail audit, IAgentJobHandler handler) =>
        new(store, new ConservativeJobExecutionPolicy(), audit, new[] { handler });

    private sealed class SequenceHandler : IAgentJobHandler
    {
        private readonly Queue<JobStepResult> _results;
        public SequenceHandler(params JobStepResult[] results) => _results = new Queue<JobStepResult>(results);
        public string JobType => "research";
        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default) =>
            Task.FromResult(_results.Dequeue());
    }

    private sealed class ThrowingHandler : IAgentJobHandler
    {
        public string JobType => "research";
        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("transient");
    }

    private sealed class InMemoryStore : IAgentJobStore
    {
        private readonly Dictionary<Guid, AgentJobRecord> _records = new();
        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records.TryGetValue(jobId, out var value) ? value : null);
        public Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentJobRecord>>(_records.Values.ToArray());
        public Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
        {
            _records[record.JobId] = record;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAudit : IAuditTrail
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
