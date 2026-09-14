using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchTerminalAuditOutboxTests
{
    [Theory]
    [InlineData("FAILED", AgentJobState.Failed, RemoteResearchProvenanceState.RemoteFailed, "research.remote_failed")]
    [InlineData("CANCELLED", AgentJobState.Cancelled, RemoteResearchProvenanceState.Cancelled, "research.remote_cancelled")]
    public async Task ReconcileDispatchedAsync_TerminalAuditFailureRecoversWithoutProviderReplay(
        string providerState,
        AgentJobState expectedJobState,
        RemoteResearchProvenanceState expectedProvenanceState,
        string expectedEventType)
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FaultInjectingAuditTrail();
            var transport = new CountingResultTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                transport,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
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
            var client = new CountingServerlessClient
            {
                GetResponse = StateResponse(expectedName, providerState)
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            audit.RemainingAppendFailures = 1;
            await Assert.ThrowsAsync<IOException>(() =>
                reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(1)));

            var stranded = await store.GetAsync(original.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(expectedJobState, stranded!.State);
            Assert.Equal(JobExecutionLocation.Local, stranded.ExecutionLocation);
            Assert.Equal(expectedProvenanceState, stranded.RemoteResearch!.State);
            Assert.NotNull(stranded.RemoteResearch.TerminalAt);
            Assert.NotNull(stranded.PendingAuditEvent);
            Assert.Equal(expectedEventType, stranded.PendingAuditEvent!.EventType);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(0, transport.DeleteCalls);
            Assert.DoesNotContain(audit.Events, e => e.EventType == expectedEventType);

            client.ThrowOnGet = true;
            var recovered = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(2));

            Assert.Equal(expectedJobState, recovered.State);
            Assert.Equal(expectedProvenanceState, recovered.RemoteResearch!.State);
            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(1, transport.DeleteCalls);
            Assert.Equal(1, audit.Events.Count(e => e.EventType == expectedEventType));

            var repeated = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(3));
            Assert.Equal(expectedJobState, repeated.State);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(1, transport.DeleteCalls);
            Assert.Equal(1, audit.Events.Count(e => e.EventType == expectedEventType));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileDispatchedAsync_ExpiredResultAuditFailureBlocksCleanupUntilRecovery()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FaultInjectingAuditTrail();
            var transport = new CountingResultTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                transport,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(5);
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                original.JobId,
                original.Checkpoint!.Step,
                opaqueId,
                now,
                expiresAt));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
                original.JobId,
                original.Checkpoint.Step,
                opaqueId,
                "job-123",
                now));

            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new CountingServerlessClient
            {
                GetResponse = StateResponse(expectedName, "COMPLETED")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            audit.RemainingAppendFailures = 1;
            await Assert.ThrowsAsync<IOException>(() =>
                reconciler.ReconcileDispatchedAsync(original.JobId, expiresAt.AddSeconds(1)));

            var stranded = await store.GetAsync(original.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(AgentJobState.Failed, stranded!.State);
            Assert.Equal(RemoteResearchProvenanceState.Expired, stranded.RemoteResearch!.State);
            Assert.NotNull(stranded.PendingAuditEvent);
            Assert.Equal("research.remote_result_expired", stranded.PendingAuditEvent!.EventType);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(1, transport.GetCalls);
            Assert.Equal(0, transport.DeleteCalls);

            client.ThrowOnGet = true;
            var recovered = await reconciler.ReconcileDispatchedAsync(original.JobId, expiresAt.AddMinutes(1));

            Assert.Equal(AgentJobState.Failed, recovered.State);
            Assert.Equal(RemoteResearchProvenanceState.Expired, recovered.RemoteResearch!.State);
            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(1, transport.GetCalls);
            Assert.Equal(1, transport.DeleteCalls);
            Assert.Equal(1, audit.Events.Count(e => e.EventType == "research.remote_result_expired"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileCancellationAsync_TerminalAuditFailureRecoversWithoutProviderReplay()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FaultInjectingAuditTrail();
            var transport = new CountingResultTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                transport,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
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
            var client = new CountingServerlessClient
            {
                GetResponse = StateResponse(expectedName, "CANCELLED")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            await reconciler.RequestCancellationAsync(original.JobId);
            Assert.Equal(1, client.CancelCalls);

            audit.RemainingAppendFailures = 1;
            await Assert.ThrowsAsync<IOException>(() =>
                reconciler.ReconcileCancellationAsync(original.JobId));

            var stranded = await store.GetAsync(original.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(AgentJobState.Cancelled, stranded!.State);
            Assert.Equal(RemoteResearchProvenanceState.Cancelled, stranded.RemoteResearch!.State);
            Assert.NotNull(stranded.PendingAuditEvent);
            Assert.Equal("research.remote_cancelled", stranded.PendingAuditEvent!.EventType);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(0, transport.DeleteCalls);

            client.ThrowOnGet = true;
            var recovered = await reconciler.ReconcileCancellationAsync(original.JobId);

            Assert.Equal(AgentJobState.Cancelled, recovered.State);
            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(1, client.CancelCalls);
            Assert.Equal(1, transport.DeleteCalls);
            Assert.Equal(1, audit.Events.Count(e => e.EventType == "research.remote_cancelled"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusServerlessResponse StateResponse(string expectedName, string state) =>
        new(HttpStatusCode.OK,
            $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"{{state}}"}}""");

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
            "nvidea-nebius-terminal-audit-outbox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CountingServerlessClient : INebiusServerlessJobClient
    {
        public int GetCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public bool ThrowOnGet { get; set; }
        public NebiusServerlessResponse GetResponse { get; set; } = new(HttpStatusCode.OK, "{}");

        public Task<NebiusServerlessResponse> CreateAsync(
            NebiusServerlessJobSpec spec,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> GetAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            if (ThrowOnGet)
                throw new InvalidOperationException("Provider GET must not be replayed during terminal audit recovery.");
            return Task.FromResult(GetResponse);
        }

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

    private sealed class CountingResultTransport : IProtectedResearchResultTransport
    {
        public int GetCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task PutAsync(
            ProtectedResearchResultEnvelope envelope,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ProtectedResearchResultEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult<ProtectedResearchResultEnvelope?>(null);
        }

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FaultInjectingAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();
        public int RemainingAppendFailures { get; set; }

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (RemainingAppendFailures > 0)
            {
                RemainingAppendFailures--;
                throw new IOException("simulated audit persistence failure");
            }

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
