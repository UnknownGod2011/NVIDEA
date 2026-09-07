using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class InitialCheckpointTests
{
    [Fact]
    public async Task Create_persists_initial_checkpoint_before_any_handler_step_runs()
    {
        var store = new InMemoryStore();
        var handler = new CountingHandler();
        var orchestrator = new ResumableJobOrchestrator(
            store,
            new ConservativeJobExecutionPolicy(),
            new InMemoryAudit(),
            new[] { handler });
        var checkpoint = new AgentJobCheckpoint("browser.action.pending", "{\"kind\":\"Navigate\"}", DateTimeOffset.UtcNow);
        var definition = new AgentJobDefinition(
            "browser.action",
            "browser.agent",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.Medium,
            ContainsPrivateOsData: true,
            BenefitsFromBackgroundExecution: false);

        var created = await orchestrator.CreateAsync(definition, checkpoint);
        var stored = await store.GetAsync(created.JobId);

        Assert.NotNull(stored);
        Assert.Equal("browser.action.pending", stored!.Checkpoint?.Step);
        Assert.Equal(checkpoint.Payload, stored.Checkpoint?.Payload);
        Assert.Equal(0, handler.Executions);
    }

    [Fact]
    public async Task Create_rejects_an_initial_checkpoint_without_a_step()
    {
        var orchestrator = new ResumableJobOrchestrator(
            new InMemoryStore(),
            new ConservativeJobExecutionPolicy(),
            new InMemoryAudit(),
            new[] { new CountingHandler() });
        var definition = new AgentJobDefinition(
            "browser.action",
            "browser.agent",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.Medium,
            ContainsPrivateOsData: true,
            BenefitsFromBackgroundExecution: false);

        await Assert.ThrowsAsync<ArgumentException>(() => orchestrator.CreateAsync(
            definition,
            new AgentJobCheckpoint("", "payload", DateTimeOffset.UtcNow)));
    }

    private sealed class CountingHandler : IAgentJobHandler
    {
        public int Executions { get; private set; }
        public string JobType => "browser.action";

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default)
        {
            Executions++;
            return Task.FromResult(new JobStepResult(Completed: true));
        }
    }

    private sealed class InMemoryStore : IAgentJobStore
    {
        private readonly Dictionary<Guid, AgentJobRecord> _records = new();

        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_records.TryGetValue(jobId, out var record) ? record : null);

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
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
    }
}
