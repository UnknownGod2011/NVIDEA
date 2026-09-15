using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableProtectedPayloadCleanupContentionTests
{
    private const string OpaqueId = "cleanup-contention-opaque-71";
    private const string RemoteJobId = "remote-cleanup-contention-71";
    private const string EnvelopeSha256 = "7171717171717171717171717171717171717171717171717171717171717171";

    [Fact]
    public async Task ClearAsync_ConcurrentLegitimateCompletion_PreservesNewerStateAndIndependentBindingObligation()
    {
        var root = CreateTempDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var cleanup = new PendingProtectedPayloadCleanup(Guid.NewGuid(), OpaqueId, now);
            var binding = new PendingResearchDispatchBinding(
                Guid.NewGuid(), OpaqueId, RemoteJobId, EnvelopeSha256, now.AddHours(1), now);
            var definition = new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true,
                MaxAttempts: 3);
            var provenance = new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                OpaqueId,
                RemoteJobId,
                ResearchJobHandler.PlannedStep,
                now.AddMinutes(-5),
                now.AddMinutes(-4),
                RemoteResearchProvenanceState.ResultApplied,
                ResultAppliedAt: now.AddMinutes(-1),
                WorkItemExpiresAt: now.AddHours(1));
            var initial = new AgentJobRecord(
                Guid.NewGuid(),
                definition,
                AgentJobState.Pending,
                JobExecutionLocation.Local,
                0,
                new AgentJobCheckpoint(ResearchJobHandler.EvidenceStep, "{\"evidence\":true}", now.AddMinutes(-1)),
                null,
                null,
                now.AddMinutes(-10),
                now,
                RemoteResearch: provenance,
                PendingProtectedPayloadCleanup: cleanup,
                RemoteWorkItemEnvelopeSha256: EnvelopeSha256,
                PendingResearchDispatchBinding: binding);
            await store.SaveAsync(initial);

            var observer = new CompleteResearchOnFirstAttemptObserver(store, now.AddSeconds(1));
            var intent = new DurableProtectedPayloadCleanupIntent(store, observer);

            var cleared = await intent.ClearAsync(initial, cleanup.CleanupId);

            Assert.Equal(2, observer.Calls);
            Assert.Equal(AgentJobState.Completed, cleared.State);
            Assert.Equal(JobExecutionLocation.Local, cleared.ExecutionLocation);
            Assert.Equal("research.final", cleared.Checkpoint!.Step);
            Assert.Equal("{\"final\":true}", cleared.Checkpoint.Payload);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, cleared.RemoteResearch!.State);
            Assert.Null(cleared.PendingProtectedPayloadCleanup);
            Assert.Null(cleared.PendingAuditEvent);
            Assert.Equal(binding, cleared.PendingResearchDispatchBinding);
            Assert.Equal(EnvelopeSha256, cleared.RemoteWorkItemEnvelopeSha256);

            var durable = await store.GetAsync(initial.JobId);
            Assert.NotNull(durable);
            Assert.Equal(cleared, durable);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ClearAsync_RepeatedContention_IsBoundedAndLeavesCleanupDebtDurable()
    {
        var root = CreateTempDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var cleanup = new PendingProtectedPayloadCleanup(Guid.NewGuid(), OpaqueId, now);
            var definition = new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true);
            var provenance = new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                OpaqueId,
                RemoteJobId,
                ResearchJobHandler.PlannedStep,
                now.AddMinutes(-5),
                now.AddMinutes(-4),
                RemoteResearchProvenanceState.ResultApplied,
                ResultAppliedAt: now.AddMinutes(-1),
                WorkItemExpiresAt: now.AddHours(1));
            var initial = new AgentJobRecord(
                Guid.NewGuid(), definition, AgentJobState.Pending, JobExecutionLocation.Local, 0,
                new AgentJobCheckpoint(ResearchJobHandler.EvidenceStep, "{}", now.AddMinutes(-1)),
                null, null, now.AddMinutes(-10), now,
                RemoteResearch: provenance,
                PendingProtectedPayloadCleanup: cleanup);
            await store.SaveAsync(initial);

            var observer = new AlwaysContendObserver(store);
            var intent = new DurableProtectedPayloadCleanupIntent(store, observer);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => intent.ClearAsync(initial, cleanup.CleanupId));

            Assert.Contains("kept changing", error.Message, StringComparison.Ordinal);
            Assert.Equal(4, observer.Calls);
            var durable = await store.GetAsync(initial.JobId);
            Assert.NotNull(durable);
            Assert.Equal(cleanup.CleanupId, durable!.PendingProtectedPayloadCleanup!.CleanupId);
            Assert.Equal(OpaqueId, durable.PendingProtectedPayloadCleanup.OpaqueWorkItemId);
            Assert.Null(durable.PendingAuditEvent);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class CompleteResearchOnFirstAttemptObserver : IProtectedPayloadCleanupCompletionObserver
    {
        private readonly JsonAgentJobStore _store;
        private readonly DateTimeOffset _completedAt;
        public int Calls { get; private set; }

        public CompleteResearchOnFirstAttemptObserver(JsonAgentJobStore store, DateTimeOffset completedAt)
        {
            _store = store;
            _completedAt = completedAt;
        }

        public async Task BeforeCompletionCompareExchangeAsync(
            Guid jobId,
            PendingProtectedPayloadCleanup cleanup,
            int attempt,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (attempt != 0)
                return;

            var current = await _store.GetAsync(jobId, cancellationToken) ?? throw new InvalidOperationException();
            var replacement = current with
            {
                State = AgentJobState.Completed,
                Checkpoint = new AgentJobCheckpoint("research.final", "{\"final\":true}", _completedAt),
                UpdatedAt = _completedAt
            };
            Assert.True(await _store.CompareExchangeAsync(current, replacement, cancellationToken));
        }
    }

    private sealed class AlwaysContendObserver : IProtectedPayloadCleanupCompletionObserver
    {
        private readonly JsonAgentJobStore _store;
        public int Calls { get; private set; }

        public AlwaysContendObserver(JsonAgentJobStore store) => _store = store;

        public async Task BeforeCompletionCompareExchangeAsync(
            Guid jobId,
            PendingProtectedPayloadCleanup cleanup,
            int attempt,
            CancellationToken cancellationToken)
        {
            Calls++;
            var current = await _store.GetAsync(jobId, cancellationToken) ?? throw new InvalidOperationException();
            var replacement = current with { UpdatedAt = current.UpdatedAt.AddTicks(1) };
            Assert.True(await _store.CompareExchangeAsync(current, replacement, cancellationToken));
        }
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-cleanup-contention-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
