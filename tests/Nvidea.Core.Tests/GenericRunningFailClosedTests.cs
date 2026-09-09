using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class GenericRunningFailClosedTests
{
    [Fact]
    public async Task Generic_orchestrator_never_replays_running_non_research_job()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-5);
        var jobId = Guid.NewGuid();
        var definition = new AgentJobDefinition(
            "browser.action",
            "browser.task",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.High,
            ContainsPrivateOsData: true,
            BenefitsFromBackgroundExecution: false,
            MaxAttempts: 3);
        var running = new AgentJobRecord(
            jobId,
            definition,
            AgentJobState.Running,
            JobExecutionLocation.Local,
            1,
            new AgentJobCheckpoint("browser.started.v1", "{\"opaque\":true}", now),
            null,
            null,
            now,
            now);

        var store = new MemoryJobStore(running);
        var handler = new CountingHandler(definition.JobType);
        var orchestrator = new ResumableJobOrchestrator(
            store,
            new ConservativeJobExecutionPolicy(),
            new MemoryAuditTrail(),
            new IAgentJobHandler[] { handler });

        var result = await orchestrator.RunNextStepAsync(jobId);

        Assert.Equal(AgentJobState.Running, result.State);
        Assert.Equal(0, handler.ExecuteCalls);
        Assert.Equal(AgentJobState.Running, (await store.GetAsync(jobId))!.State);
    }

    private sealed class CountingHandler(string jobType) : IAgentJobHandler
    {
        public string JobType { get; } = jobType;
        public int ExecuteCalls { get; private set; }

        public Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default)
        {
            ExecuteCalls++;
            return Task.FromResult(new JobStepResult(Completed: true));
        }
    }

    private sealed class MemoryJobStore(AgentJobRecord initial) : IAgentJobStore
    {
        private AgentJobRecord _record = initial;

        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AgentJobRecord?>(_record.JobId == jobId ? _record : null);

        public Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AgentJobRecord>>([_record]);

        public Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
        {
            _record = record;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }
}
