using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchCancellationAuditOutboxTests
{
    [Fact]
    public async Task RequestCancellationAsync_AuditFailurePersistsIntentAndBlocksProviderUntilRecovery()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FaultInjectingAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new NullResultTransport(),
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
                GetResponse = RunningResponse(expectedName)
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            audit.RemainingAppendFailures = 1;
            await Assert.ThrowsAsync<IOException>(() =>
                reconciler.RequestCancellationAsync(original.JobId));

            var stranded = await store.GetAsync(original.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, stranded!.RemoteResearch!.State);
            Assert.NotNull(stranded.PendingAuditEvent);
            Assert.Equal("research.remote_cancel_requested", stranded.PendingAuditEvent!.EventType);
            Assert.Equal(0, client.CancelCalls);
            Assert.Equal(0, client.GetCalls);
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_cancel_requested");

            var recovered = await reconciler.ReconcileCancellationAsync(original.JobId);

            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, recovered.RemoteResearch!.State);
            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(1, client.GetCalls);
            Assert.Equal(1, client.CancelCalls);
            Assert.Equal(1, audit.Events.Count(e => e.EventType == "research.remote_cancel_requested"));
            Assert.Equal(1, audit.Events.Count(e => e.EventType == "research.remote_cancel_redriven"));
            Assert.True(
                audit.Events.FindIndex(e => e.EventType == "research.remote_cancel_requested")
                < audit.Events.FindIndex(e => e.EventType == "research.remote_cancel_redriven"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileCancellationAsync_PendingAuditFailureOccursBeforeAnyProviderObservationOrCancel()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FaultInjectingAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new NullResultTransport(),
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
                GetResponse = RunningResponse(expectedName)
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            audit.RemainingAppendFailures = 1;
            await Assert.ThrowsAsync<IOException>(() =>
                reconciler.RequestCancellationAsync(original.JobId));

            audit.RemainingAppendFailures = 1;
            await Assert.ThrowsAsync<IOException>(() =>
                reconciler.ReconcileCancellationAsync(original.JobId));

            var durable = await store.GetAsync(original.JobId);
            Assert.NotNull(durable);
            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, durable!.RemoteResearch!.State);
            Assert.NotNull(durable.PendingAuditEvent);
            Assert.Equal("research.remote_cancel_requested", durable.PendingAuditEvent!.EventType);
            Assert.Equal(0, client.GetCalls);
            Assert.Equal(0, client.CancelCalls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusServerlessResponse RunningResponse(string expectedName) =>
        new(HttpStatusCode.OK,
            $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}""");

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
            "nvidea-nebius-cancel-audit-outbox-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CountingServerlessClient : INebiusServerlessJobClient
    {
        public int GetCalls { get; private set; }
        public int CancelCalls { get; private set; }
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

    private sealed class NullResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(
            ProtectedResearchResultEnvelope envelope,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ProtectedResearchResultEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
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
