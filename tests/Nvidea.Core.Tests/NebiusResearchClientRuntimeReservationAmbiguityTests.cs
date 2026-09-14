using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchClientRuntimeReservationAmbiguityTests
{
    [Fact]
    public async Task ReconcileReservedAsync_AuditSettled_RecoversExactProviderJobWithoutDuplicateCreate_AndPublishesV2Binding()
    {
        var root = CreateTempDirectory();
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            var workItem = new RemoteResearchWorkItem(
                original.JobId,
                original.Checkpoint!.Step,
                original.Checkpoint.Payload,
                ContainsPrivateOsData: false,
                CreatedAt: now,
                ExpiresAt: now.AddHours(1));
            var envelope = ResearchWorkItemProtector.Protect(
                workItem,
                workerRsa.ExportSubjectPublicKeyInfoPem());
            var atomic = new AtomicRemoteResearchDispatchReservation(store, audit);
            var reserved = await atomic.ReserveAsync(
                new RemoteResearchDispatchReservation(
                    original.JobId,
                    original.Checkpoint.Step,
                    envelope.OpaqueWorkItemId,
                    now,
                    envelope.ExpiresAt),
                envelope);

            Assert.Null(reserved.PendingAuditEvent);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, reserved.RemoteResearch!.State);
            Assert.NotNull(reserved.RemoteWorkItemEnvelopeSha256);

            const string authoritativeRemoteId = "job-runtime-recovered-42";
            var expectedName = NebiusResearchLifecycleReconciler.GetDeterministicRemoteJobName(
                envelope.OpaqueWorkItemId);
            var serverless = new CountingServerlessClient
            {
                ListResponse = new NebiusServerlessResponse(
                    HttpStatusCode.OK,
                    $$"""{"items":[{"metadata":{"id":"{{authoritativeRemoteId}}","name":"{{expectedName}}"},"status":{"state":"RUNNING"}},{"metadata":{"id":"unrelated-job","name":"unrelated"},"status":{"state":"RUNNING"}}]}"""),
                GetResponse = new NebiusServerlessResponse(
                    HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"{{authoritativeRemoteId}}","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}""")
            };
            var workItems = new NoSharedTransportAccessWorkItemTransport();
            var results = new EmptyResultTransport();
            var bindings = new MemoryBindingTransport();
            var runtime = NebiusResearchClientRuntime.Create(
                store,
                serverless,
                workItems,
                results,
                bindings,
                CreateOptions(workerRsa),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            var recovered = await runtime.ReconcileReservedAsync(original.JobId);

            Assert.Equal(JobExecutionLocation.NebiusServerless, recovered.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, recovered.RemoteResearch!.State);
            Assert.Equal(authoritativeRemoteId, recovered.RemoteResearch.RemoteJobId);
            Assert.Equal(0, serverless.CreateCalls);
            Assert.Equal(1, serverless.ListCalls);
            Assert.Equal(1, serverless.GetCalls);
            Assert.Equal(authoritativeRemoteId, serverless.LastGetId);
            Assert.Equal(0, workItems.GetCalls);
            Assert.Equal(0, workItems.PutCalls);

            Assert.Equal(1, bindings.PutCalls);
            var binding = await bindings.GetAsync(envelope.OpaqueWorkItemId);
            Assert.NotNull(binding);
            var verified = ResearchDispatchBindingProtector.Verify(
                binding!,
                envelope.OpaqueWorkItemId,
                clientRsa.ExportSubjectPublicKeyInfoPem());
            Assert.Equal(ResearchDispatchBindingProtector.EnvelopeBoundProtocolVersion, verified.ProtocolVersion);
            Assert.Equal(authoritativeRemoteId, verified.RemoteJobId);
            Assert.NotNull(verified.WorkItemEnvelopeSha256);
            Assert.True(ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(
                reserved.RemoteWorkItemEnvelopeSha256!,
                verified.WorkItemEnvelopeSha256!));

            var persisted = await store.GetAsync(original.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(authoritativeRemoteId, persisted!.RemoteResearch!.RemoteJobId);
            Assert.Null(persisted.PendingAuditEvent);
            Assert.True(ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(
                reserved.RemoteWorkItemEnvelopeSha256!,
                persisted.RemoteWorkItemEnvelopeSha256!));
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

    private static NebiusResearchDispatchOptions CreateOptions(RSA workerRsa) => new(
        WorkerImage: "registry.example/nvidea-worker:sha256-test",
        WorkerPublicKeyPem: workerRsa.ExportSubjectPublicKeyInfoPem(),
        ContainerCommand: "dotnet",
        Platform: "cpu-d3",
        Preset: "1vcpu-4gb",
        Timeout: "3600s",
        SubnetId: "subnet-test",
        Disk: new NebiusServerlessDiskSpec("network-ssd", 10L * 1024 * 1024 * 1024));

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-runtime-reservation-ambiguity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CountingServerlessClient : INebiusServerlessJobClient
    {
        public NebiusServerlessResponse ListResponse { get; init; } = new(HttpStatusCode.OK, "{\"items\":[]}");
        public NebiusServerlessResponse GetResponse { get; init; } = new(HttpStatusCode.NotFound, "{}");
        public int CreateCalls { get; private set; }
        public int ListCalls { get; private set; }
        public int GetCalls { get; private set; }
        public string? LastGetId { get; private set; }

        public Task<NebiusServerlessResponse> CreateAsync(
            NebiusServerlessJobSpec spec,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(new NebiusServerlessResponse(
                HttpStatusCode.OK,
                "{\"resourceId\":\"duplicate-create-must-not-run\"}"));
        }

        public Task<NebiusServerlessResponse> GetAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetCalls++;
            LastGetId = remoteJobId;
            return Task.FromResult(GetResponse);
        }

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ListCalls++;
            return Task.FromResult(ListResponse);
        }

        public Task<NebiusServerlessResponse> CancelAsync(
            string remoteJobId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Reservation reconciliation must not cancel provider work.");
    }

    private sealed class NoSharedTransportAccessWorkItemTransport : IProtectedResearchWorkItemTransport
    {
        public int GetCalls { get; private set; }
        public int PutCalls { get; private set; }

        public Task PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PutCalls++;
            throw new InvalidOperationException("Ambiguous reservation recovery must not rewrite shared work-item state.");
        }

        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            throw new InvalidOperationException("Ambiguous reservation recovery must not trust shared work-item state.");
        }

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ProtectedResearchResultEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        private readonly Dictionary<string, ProtectedResearchDispatchBinding> _items = new(StringComparer.Ordinal);
        public int PutCalls { get; private set; }

        public Task PutAsync(
            ProtectedResearchDispatchBinding binding,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_items.TryAdd(binding.OpaqueWorkItemId, binding))
                throw new InvalidOperationException("Binding already exists.");
            PutCalls++;
            return Task.CompletedTask;
        }

        public Task<ProtectedResearchDispatchBinding?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue(opaqueWorkItemId, out var binding);
            return Task.FromResult(binding);
        }

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Remove(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        private readonly List<AuditEvent> _events = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_events.Any(existing => existing.Id == auditEvent.Id))
                _events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AuditEvent>>(_events.ToArray());
        }
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
