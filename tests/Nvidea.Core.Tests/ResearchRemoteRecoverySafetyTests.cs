using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchRemoteRecoverySafetyTests
{
    [Fact]
    public async Task Stale_dispatch_reservation_is_never_advertised_or_rearmed_as_local_crash_recovery()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-remote-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var runtime = new ResearchJobRuntime(
                directory,
                new ResearchEngine(new NeverInferenceClient(), new NeverResearchProvider()),
                new MemoryAuditTrail());
            var created = await runtime.CreateAsync("Do not replay an ambiguous Nebius dispatch");

            var store = new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json"));
            var current = await store.GetAsync(created.JobId)
                ?? throw new InvalidOperationException("Expected persisted research job.");
            var checkpoint = current.Checkpoint
                ?? throw new InvalidOperationException("Expected persisted research checkpoint.");
            var reservedAt = DateTimeOffset.UtcNow.Subtract(ResearchJobStatus.InterruptedRecoveryDelay).AddMinutes(-1);
            var reserved = current with
            {
                State = AgentJobState.Running,
                ExecutionLocation = JobExecutionLocation.Local,
                RemoteResearch = new RemoteResearchProvenance(
                    ResearchWorkItemProtector.ProtocolVersion,
                    "opaque-reservation-abcdefghijklmnop",
                    RemoteJobId: null,
                    checkpoint.Step,
                    checkpoint.SavedAt,
                    reservedAt,
                    RemoteResearchProvenanceState.DispatchReserved,
                    WorkItemExpiresAt: reservedAt.AddHours(1)),
                UpdatedAt = reservedAt
            };
            await store.SaveAsync(reserved);

            var status = await runtime.GetStatusAsync(created.JobId);
            Assert.False(status.CanRecoverInterrupted);
            Assert.False(status.CanRunNextStep);
            Assert.NotEqual(ResearchJobStage.Interrupted, status.Stage);
            Assert.Contains("reconcile", status.DisplayText, StringComparison.OrdinalIgnoreCase);

            await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.RecoverInterruptedAsync(created.JobId));

            var after = await store.GetAsync(created.JobId)
                ?? throw new InvalidOperationException("Expected reserved job after rejected recovery.");
            Assert.Equal(AgentJobState.Running, after.State);
            Assert.Equal(JobExecutionLocation.Local, after.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, after.RemoteResearch?.State);
            Assert.Equal(checkpoint.Step, after.Checkpoint?.Step);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class NeverInferenceClient : IAgentInferenceClient
    {
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Local inference must not execute during recovery-safety validation.");
    }

    private sealed class NeverResearchProvider : IResearchProvider
    {
        public Task<ResearchBatch> SearchAsync(
            IReadOnlyList<ResearchQuery> queries,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Local research provider must not execute during recovery-safety validation.");
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }
}
