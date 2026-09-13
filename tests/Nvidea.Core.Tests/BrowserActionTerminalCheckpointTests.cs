using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class BrowserActionTerminalCheckpointTests
{
    private const string SensitivePayload = "{\"value\":\"super-secret-password\",\"uploadPath\":\"C:\\\\private.txt\"}";

    [Fact]
    public async Task Exhausted_browser_failure_scrubs_executable_checkpoint_before_persist_and_return()
    {
        var store = new InMemoryStore();
        var orchestrator = Create(store, new ThrowingBrowserHandler());
        var initial = new AgentJobCheckpoint("browser.action.pending", SensitivePayload, DateTimeOffset.UtcNow);
        var created = await orchestrator.CreateAsync(Definition(maxAttempts: 1), initial);

        var failed = await orchestrator.RunNextStepAsync(created.JobId);
        var durable = await store.GetAsync(created.JobId);

        Assert.Equal(AgentJobState.Failed, failed.State);
        Assert.Equal("browser.action.terminal.scrubbed", failed.Checkpoint?.Step);
        Assert.Equal("{\"actionPayloadRemoved\":true}", failed.Checkpoint?.Payload);
        Assert.DoesNotContain("super-secret-password", failed.Checkpoint?.Payload ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("private.txt", failed.Checkpoint?.Payload ?? string.Empty, StringComparison.Ordinal);
        Assert.Null(failed.ApprovalScope);
        Assert.NotNull(durable);
        Assert.Equal(failed.Checkpoint, durable!.Checkpoint);
        Assert.Null(durable.ApprovalScope);
    }

    [Fact]
    public async Task Retryable_browser_failure_keeps_executable_checkpoint_for_the_retry()
    {
        var store = new InMemoryStore();
        var orchestrator = Create(store, new ThrowingBrowserHandler());
        var initial = new AgentJobCheckpoint("browser.action.pending", SensitivePayload, DateTimeOffset.UtcNow);
        var created = await orchestrator.CreateAsync(Definition(maxAttempts: 2), initial);

        var retry = await orchestrator.RunNextStepAsync(created.JobId);

        Assert.Equal(AgentJobState.RetryScheduled, retry.State);
        Assert.Equal("browser.action.pending", retry.Checkpoint?.Step);
        Assert.Equal(SensitivePayload, retry.Checkpoint?.Payload);
        Assert.NotNull(retry.NextAttemptAt);
    }

    [Fact]
    public async Task Cancelling_approval_paused_browser_job_scrubs_action_and_scope()
    {
        var store = new InMemoryStore();
        var orchestrator = Create(store, new ApprovalBrowserHandler());
        var initial = new AgentJobCheckpoint("browser.action.pending", SensitivePayload, DateTimeOffset.UtcNow);
        var created = await orchestrator.CreateAsync(Definition(maxAttempts: 2), initial);

        var paused = await orchestrator.RunNextStepAsync(created.JobId);
        Assert.Equal(AgentJobState.WaitingForApproval, paused.State);
        Assert.Equal("exact-scope-with-sensitive-target", paused.ApprovalScope);

        var cancelled = await orchestrator.CancelAsync(created.JobId);
        var durable = await store.GetAsync(created.JobId);

        Assert.Equal(AgentJobState.Cancelled, cancelled.State);
        Assert.Equal("browser.action.terminal.scrubbed", cancelled.Checkpoint?.Step);
        Assert.Equal("{\"actionPayloadRemoved\":true}", cancelled.Checkpoint?.Payload);
        Assert.Null(cancelled.ApprovalScope);
        Assert.NotNull(durable);
        Assert.Equal(cancelled.Checkpoint, durable!.Checkpoint);
        Assert.Null(durable.ApprovalScope);
    }

    [Fact]
    public void Non_browser_terminal_record_is_not_rewritten()
    {
        var now = DateTimeOffset.UtcNow;
        var checkpoint = new AgentJobCheckpoint("research.pending", SensitivePayload, now);
        var record = new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                "research",
                "research.web",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Medium,
                false,
                false,
                1),
            AgentJobState.Failed,
            JobExecutionLocation.Local,
            1,
            checkpoint,
            "scope",
            "failed",
            now,
            now);

        var projected = BrowserActionTerminalCheckpoint.ScrubIfTerminal(record);

        Assert.Same(record, projected);
        Assert.Same(checkpoint, projected.Checkpoint);
        Assert.Equal("scope", projected.ApprovalScope);
    }

    private static AgentJobDefinition Definition(int maxAttempts) =>
        new(
            "browser.action",
            "browser.task",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Medium,
            true,
            false,
            maxAttempts);

    private static ResumableJobOrchestrator Create(IAgentJobStore store, IAgentJobHandler handler) =>
        new(store, new ConservativeJobExecutionPolicy(), new NoOpAudit(), new[] { handler });

    private sealed class ThrowingBrowserHandler : IAgentJobHandler
    {
        public string JobType => "browser.action";

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated browser failure");
    }

    private sealed class ApprovalBrowserHandler : IAgentJobHandler
    {
        public string JobType => "browser.action";

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default) =>
            Task.FromResult(new JobStepResult(
                Completed: false,
                RequiresApproval: true,
                ApprovalScope: "exact-scope-with-sensitive-target",
                CheckpointStep: "browser.action.pending",
                CheckpointPayload: SensitivePayload));
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

    private sealed class NoOpAudit : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
    }
}
