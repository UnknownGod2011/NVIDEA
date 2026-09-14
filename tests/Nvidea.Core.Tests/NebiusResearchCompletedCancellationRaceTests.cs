using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchCompletedCancellationRaceTests
{
    [Fact]
    public async Task ReconcileCancellationAsync_AppliesVerifiedCompletionWhenCancellationLosesRace()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new MemoryResultTransport();
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            const string remoteJobId = "job-123";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                original.JobId,
                original.Checkpoint!.Step,
                opaqueId,
                now,
                now.AddMinutes(20)));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
                original.JobId,
                original.Checkpoint.Step,
                opaqueId,
                remoteJobId,
                now));

            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new CompletedServerlessClient(CompletedResponse(expectedName));
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);
            await reconciler.RequestCancellationAsync(original.JobId);

            var remote = new RemoteResearchStageResult(
                original.JobId,
                original.Checkpoint.Step,
                opaqueId,
                remoteJobId,
                new JobStepResult(
                    Completed: false,
                    CheckpointStep: ResearchJobHandler.EvidenceStep,
                    CheckpointPayload: "{\"evidence\":\"completed-before-cancel\"}"),
                now.AddSeconds(10),
                now.AddMinutes(20));
            await results.PutAsync(ResearchResultProtector.Protect(
                remote,
                rsa.ExportSubjectPublicKeyInfoPem()));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ingestor.IngestAsync(original.JobId, now.AddSeconds(20)));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ingestor.IngestCompletedAfterCancellationRequestedAsync(
                    original.JobId,
                    "job-substituted",
                    now.AddSeconds(20)));

            var applied = await reconciler.ReconcileCancellationAsync(original.JobId);

            Assert.Equal(AgentJobState.Pending, applied.State);
            Assert.Equal(JobExecutionLocation.Local, applied.ExecutionLocation);
            Assert.Equal(ResearchJobHandler.EvidenceStep, applied.Checkpoint!.Step);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, applied.RemoteResearch!.State);
            Assert.NotNull(applied.RemoteResearch.ResultAppliedAt);
            Assert.Equal(1, client.CancelCalls);
            Assert.Equal(1, results.DeleteCalls);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_result_applied_after_cancel_request");
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_cancelled");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                reconciler.ReconcileCancellationAsync(original.JobId));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileCancellationAsync_WaitsForProtectedResultWhileAuthenticatedLifetimeRemains()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new MemoryResultTransport();
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            const string opaqueId = "nR8GhQmBetQy0yM5c9hV2dLrO4tX7wSu";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                original.JobId,
                original.Checkpoint!.Step,
                opaqueId,
                now,
                now.AddMinutes(20)));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
                original.JobId,
                original.Checkpoint.Step,
                opaqueId,
                "job-123",
                now));

            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new CompletedServerlessClient(CompletedResponse(expectedName));
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);
            await reconciler.RequestCancellationAsync(original.JobId);

            var waiting = await reconciler.ReconcileCancellationAsync(original.JobId);

            Assert.Equal(AgentJobState.Running, waiting.State);
            Assert.Equal(JobExecutionLocation.NebiusServerless, waiting.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, waiting.RemoteResearch!.State);
            Assert.Equal(1, client.CancelCalls);
            Assert.Equal(0, results.DeleteCalls);
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_result_expired_after_cancel_request");
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_cancelled");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileCancellationAsync_ExpiresMissingResultOnlyAfterDurableLifetimeElapsed()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new MemoryResultTransport();
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var reservedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
            var original = CreatePendingJob(reservedAt);
            await store.SaveAsync(original);

            const string opaqueId = "pT9JiRnCfuRz1zN6d0iW3eMsP5uY8xTv";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                original.JobId,
                original.Checkpoint!.Step,
                opaqueId,
                reservedAt,
                reservedAt.AddMinutes(10)));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
                original.JobId,
                original.Checkpoint.Step,
                opaqueId,
                "job-123",
                reservedAt.AddSeconds(1)));

            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new CompletedServerlessClient(CompletedResponse(expectedName));
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);
            await reconciler.RequestCancellationAsync(original.JobId);

            var expired = await reconciler.ReconcileCancellationAsync(original.JobId);

            Assert.Equal(AgentJobState.Failed, expired.State);
            Assert.Equal(JobExecutionLocation.Local, expired.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Expired, expired.RemoteResearch!.State);
            Assert.NotNull(expired.RemoteResearch.TerminalAt);
            Assert.Contains("before cancellation was confirmed", expired.LastError, StringComparison.Ordinal);
            Assert.Equal(1, client.CancelCalls);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_result_expired_after_cancel_request");
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_cancelled");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusServerlessResponse CompletedResponse(string expectedName) =>
        new(HttpStatusCode.OK,
            $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"COMPLETED"}}""");

    private static AgentJobRecord CreatePendingJob(DateTimeOffset now) => new(
        Guid.NewGuid(),
        new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true,
            MaxAttempts: 3),
        AgentJobState.Pending,
        JobExecutionLocation.Local,
        Attempt: 0,
        new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now),
        ApprovalScope: null,
        LastError: null,
        CreatedAt: now,
        UpdatedAt: now);

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "nvidea-nebius-completed-cancel-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CompletedServerlessClient : INebiusServerlessJobClient
    {
        private readonly NebiusServerlessResponse _getResponse;

        public CompletedServerlessClient(NebiusServerlessResponse getResponse)
        {
            _getResponse = getResponse;
        }

        public int CancelCalls { get; private set; }

        public Task<NebiusServerlessResponse> CreateAsync(
            NebiusServerlessJobSpec spec,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> GetAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_getResponse);

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{\"items\":[]}"));

        public Task<NebiusServerlessResponse> ListAsync(
            string? pageToken,
            CancellationToken cancellationToken = default) =>
            ListAsync(cancellationToken);

        public Task<NebiusServerlessResponse> CancelAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            return Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{}"));
        }
    }

    private sealed class MemoryResultTransport : IProtectedResearchResultTransport
    {
        private readonly Dictionary<string, ProtectedResearchResultEnvelope> _items = new(StringComparer.Ordinal);

        public int DeleteCalls { get; private set; }

        public Task PutAsync(
            ProtectedResearchResultEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            _items[envelope.OpaqueWorkItemId] = envelope;
            return Task.CompletedTask;
        }

        public Task<ProtectedResearchResultEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(opaqueWorkItemId, out var value);
            return Task.FromResult(value);
        }

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            _items.Remove(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
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

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
