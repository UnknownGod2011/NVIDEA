using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchLifecycleFailureProvenanceTests
{
    [Theory]
    [InlineData("Quota")]
    [InlineData("FutureProviderCode")]
    public async Task ReconcileDispatchedAsync_CapturesBoundedFailureCodeBeforeCasReturns(string providerCode)
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new NullResultTransport(),
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(
                new RemoteResearchDispatchReservation(
                    original.JobId,
                    original.Checkpoint!.Step,
                    opaqueId,
                    now,
                    now.AddMinutes(20)));
            await ingestor.AttachDispatchAsync(
                new NebiusResearchDispatchReceipt(
                    original.JobId,
                    original.Checkpoint.Step,
                    opaqueId,
                    "job-123",
                    now));

            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var client = new FakeServerlessClient
            {
                GetResponse = new NebiusServerlessResponse(
                    HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"FAILED","stateDetails":{"code":"{{providerCode}}","message":"Provider-controlled diagnostic text must stay non-authoritative."}}}""")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            var returned = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(1));
            var durable = await store.GetAsync(original.JobId);

            Assert.Equal(AgentJobState.Failed, returned.State);
            Assert.Equal(RemoteResearchProvenanceState.RemoteFailed, returned.RemoteResearch!.State);
            Assert.Equal(providerCode, returned.RemoteResearch.ProviderFailureCode);
            Assert.NotNull(returned.RemoteResearch.TerminalAt);

            Assert.NotNull(durable);
            Assert.Equal(providerCode, durable!.RemoteResearch!.ProviderFailureCode);
            Assert.Equal(returned.RemoteResearch, durable.RemoteResearch);
            Assert.Contains("Provider diagnostic (untrusted)", returned.LastError, StringComparison.Ordinal);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_failed");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileDispatchedAsync_DoesNotPopulateFailureCodeWhenDiagnosticIsRejected()
    {
        var root = CreateTempDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new NullResultTransport(),
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(
                new RemoteResearchDispatchReservation(
                    original.JobId,
                    original.Checkpoint!.Step,
                    opaqueId,
                    now,
                    now.AddMinutes(20)));
            await ingestor.AttachDispatchAsync(
                new NebiusResearchDispatchReceipt(
                    original.JobId,
                    original.Checkpoint.Step,
                    opaqueId,
                    "job-123",
                    now));

            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(opaqueId);
            var oversizedCode = new string('X', 129);
            var client = new FakeServerlessClient
            {
                GetResponse = new NebiusServerlessResponse(
                    HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"FAILED","stateDetails":{"code":"{{oversizedCode}}","message":"ignored with the rejected diagnostic"}}}""")
            };
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit);

            var returned = await reconciler.ReconcileDispatchedAsync(original.JobId, now.AddMinutes(1));

            Assert.Equal(AgentJobState.Failed, returned.State);
            Assert.Equal(RemoteResearchProvenanceState.RemoteFailed, returned.RemoteResearch!.State);
            Assert.Null(returned.RemoteResearch.ProviderFailureCode);
            Assert.Equal("Nebius remote research stage failed.", returned.LastError);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

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
            "nvidea-nebius-failure-provenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeServerlessClient : INebiusServerlessJobClient
    {
        public NebiusServerlessResponse GetResponse { get; set; } = new(HttpStatusCode.OK, "{}");

        public Task<NebiusServerlessResponse> CreateAsync(
            NebiusServerlessJobSpec spec,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> GetAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GetResponse);

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
