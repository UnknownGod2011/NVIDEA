using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchRemoteRecoverySafetyTests
{
    [Fact]
    public async Task Stale_dispatch_reservation_is_never_advertised_rearmed_run_or_cancelled_as_local_work()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-remote-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var inference = new NeverInferenceClient();
            var provider = new NeverResearchProvider();
            var runtime = new ResearchJobRuntime(
                directory,
                new ResearchEngine(inference, provider),
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
            Assert.True(status.RequiresRemoteReconciliation);
            Assert.False(status.CanRecoverInterrupted);
            Assert.False(status.CanRunNextStep);
            Assert.False(status.CanCancel);
            Assert.NotEqual(ResearchJobStage.Interrupted, status.Stage);
            Assert.Contains("reconcile", status.DisplayText, StringComparison.OrdinalIgnoreCase);

            await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.RunNextStepAsync(created.JobId));
            await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.RecoverInterruptedAsync(created.JobId));
            await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.CancelAsync(created.JobId));
            Assert.Equal(0, inference.Calls);
            Assert.Equal(0, provider.Calls);

            var after = await store.GetAsync(created.JobId)
                ?? throw new InvalidOperationException("Expected reserved job after rejected local mutations.");
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
        public int Calls { get; private set; }

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("Local inference must not execute during recovery-safety validation.");
        }
    }

    private sealed class NeverResearchProvider : IResearchProvider
    {
        public int Calls { get; private set; }

        public Task<ResearchBatch> SearchAsync(
            IReadOnlyList<ResearchQuery> queries,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("Local research provider must not execute during recovery-safety validation.");
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }
}