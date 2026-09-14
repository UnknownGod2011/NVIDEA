using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchTerminalPayloadCleanupRecoveryTests
{
    [Fact]
    public async Task Terminal_cleanup_failure_is_durable_and_restart_recovers_without_provider_replay()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new InMemoryAuditTrail();
            var transport = new FailOnceDeleteResultTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                transport,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.Parse("2026-09-14T13:00:00Z");
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
                GetResponse = StateResponse(expectedName, "FAILED")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);
            transport.FailNextDelete = true;

            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(1)));
            Assert.Contains("cleanup remains pending", failure.Message, StringComparison.OrdinalIgnoreCase);

            var stranded = await store.GetAsync(original.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(AgentJobState.Failed, stranded!.State);
            Assert.Equal(JobExecutionLocation.Local, stranded.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.RemoteFailed, stranded.RemoteResearch!.State);
            Assert.Null(stranded.PendingAuditEvent);
            Assert.NotNull(stranded.PendingProtectedPayloadCleanup);
            Assert.Equal(opaqueId, stranded.PendingProtectedPayloadCleanup!.OpaqueWorkItemId);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(1, transport.DeleteCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_failed"));

            client.ThrowOnGet = true;
            var recovered = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(2));

            Assert.Equal(AgentJobState.Failed, recovered.State);
            Assert.Equal(RemoteResearchProvenanceState.RemoteFailed, recovered.RemoteResearch!.State);
            Assert.Null(recovered.PendingAuditEvent);
            Assert.Null(recovered.PendingProtectedPayloadCleanup);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(2, transport.DeleteCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_failed"));

            var repeated = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(3));
            Assert.Equal(AgentJobState.Failed, repeated.State);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(2, transport.DeleteCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_failed"));
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
            "nvidea-nebius-terminal-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CountingServerlessClient : INebiusServerlessJobClient
    {
        public int GetCalls { get; private set; }
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
                throw new InvalidOperationException("Provider GET must not be replayed during cleanup recovery.");
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{}"));
    }

    private sealed class FailOnceDeleteResultTransport : IProtectedResearchResultTransport
    {
        public bool FailNextDelete { get; set; }
        public int DeleteCalls { get; private set; }

        public Task PutAsync(
            ProtectedResearchResultEnvelope envelope,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ProtectedResearchResultEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            if (FailNextDelete)
            {
                FailNextDelete = false;
                throw new IOException("Injected protected payload deletion failure.");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (Events.Any(existing => existing.EventId == auditEvent.EventId))
                throw new InvalidOperationException("Duplicate audit event id.");
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
