using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class ResearchDispatchBindingLifecycleIntegrationTests
{
    [Fact]
    public async Task DispatchWithReservationAsync_PublishesBindingOnlyAfterDurableAttachment()
    {
        var root = CreateTempDirectory();
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var workItems = new MemoryWorkItemTransport();
            var bindings = new MemoryBindingTransport();
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit,
                workItems);
            var publisher = new ResearchDispatchBindingPublisher(bindings, clientRsa.ExportPkcs8PrivateKeyPem());
            bindings.OnPut = async binding =>
            {
                var persisted = await store.GetAsync(job.JobId);
                Assert.NotNull(persisted);
                Assert.Equal(JobExecutionLocation.NebiusServerless, persisted!.ExecutionLocation);
                Assert.Equal(RemoteResearchProvenanceState.Dispatched, persisted.RemoteResearch!.State);
                Assert.Equal("remote-nebius-42", persisted.RemoteResearch.RemoteJobId);
                Assert.Equal(persisted.RemoteResearch.OpaqueWorkItemId, binding.OpaqueWorkItemId);
            };

            var serverless = new FakeServerlessClient
            {
                CreateResponse = new NebiusServerlessResponse(HttpStatusCode.OK, "{\"resourceId\":\"remote-nebius-42\"}")
            };
            var dispatcher = new TwoPhaseNebiusResearchDispatcher(
                serverless,
                workItems,
                CreateOptions(workerRsa),
                publisher);
            var workItem = new RemoteResearchWorkItem(
                job.JobId,
                job.Checkpoint!.Step,
                job.Checkpoint.Payload,
                false,
                now,
                now.AddHours(1));
            var authorization = new ResearchCloudAuthorization(
                job.JobId,
                job.Checkpoint.Step,
                true,
                ResearchWorkItemProtector.DisclosureVersion,
                now);

            var dispatched = await dispatcher.DispatchWithReservationAsync(workItem, authorization, ingestor);

            Assert.Equal("remote-nebius-42", dispatched.RemoteResearch!.RemoteJobId);
            Assert.Equal(1, bindings.PutCalls);
            var binding = await bindings.GetAsync(dispatched.RemoteResearch.OpaqueWorkItemId);
            Assert.NotNull(binding);
            var verified = ResearchDispatchBindingProtector.Verify(
                binding!,
                dispatched.RemoteResearch.OpaqueWorkItemId,
                clientRsa.ExportSubjectPublicKeyInfoPem());
            Assert.Equal("remote-nebius-42", verified.RemoteJobId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileReservedAsync_PublishesExactlyRecoveredAuthoritativeId()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var bindings = new MemoryBindingTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(
                new RemoteResearchDispatchReservation(job.JobId, job.Checkpoint!.Step, opaqueId, now, now.AddHours(1)));

            var expectedName = ResearchDispatchBindingProtector.GetDeterministicRemoteJobName(opaqueId);
            var client = new FakeServerlessClient
            {
                ListResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"items":[{"metadata":{"id":"recovered-job-77","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}]}"""),
                GetResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"recovered-job-77","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}""")
            };
            var publisher = new ResearchDispatchBindingPublisher(bindings, clientRsa.ExportPkcs8PrivateKeyPem());
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit, publisher);

            var attached = await reconciler.ReconcileReservedAsync(job.JobId);

            Assert.Equal("recovered-job-77", attached.RemoteResearch!.RemoteJobId);
            var binding = await bindings.GetAsync(opaqueId);
            Assert.NotNull(binding);
            var verified = ResearchDispatchBindingProtector.Verify(
                binding!, opaqueId, clientRsa.ExportSubjectPublicKeyInfoPem());
            Assert.Equal("recovered-job-77", verified.RemoteJobId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReconcileDispatchedAsync_IsIdempotentAndConflictingBindingBlocksProviderRead()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var bindings = new MemoryBindingTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);
            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            await ingestor.ReserveDispatchAsync(
                new RemoteResearchDispatchReservation(job.JobId, job.Checkpoint!.Step, opaqueId, now, now.AddHours(1)));
            await ingestor.AttachDispatchAsync(
                new NebiusResearchDispatchReceipt(job.JobId, job.Checkpoint.Step, opaqueId, "job-123", now));

            var expectedName = ResearchDispatchBindingProtector.GetDeterministicRemoteJobName(opaqueId);
            var client = new FakeServerlessClient
            {
                GetResponse = new NebiusServerlessResponse(HttpStatusCode.OK,
                    $$"""{"metadata":{"id":"job-123","name":"{{expectedName}}"},"status":{"state":"RUNNING"}}""")
            };
            var publisher = new ResearchDispatchBindingPublisher(bindings, clientRsa.ExportPkcs8PrivateKeyPem());
            var reconciler = new NebiusResearchLifecycleReconciler(store, client, ingestor, audit, publisher);

            await reconciler.ReconcileDispatchedAsync(job.JobId, now.AddMinutes(1));
            await reconciler.ReconcileDispatchedAsync(job.JobId, now.AddMinutes(2));
            Assert.Equal(1, bindings.PutCalls);
            Assert.Equal(2, client.GetCalls);

            var conflictingBindings = new MemoryBindingTransport();
            await conflictingBindings.PutAsync(ResearchDispatchBindingProtector.Sign(
                opaqueId,
                "different-job",
                now,
                now.AddMinutes(30),
                clientRsa.ExportPkcs8PrivateKeyPem()));
            var conflictingPublisher = new ResearchDispatchBindingPublisher(
                conflictingBindings,
                clientRsa.ExportPkcs8PrivateKeyPem());
            var guardedClient = new FakeServerlessClient { GetResponse = client.GetResponse };
            var guardedReconciler = new NebiusResearchLifecycleReconciler(
                store, guardedClient, ingestor, audit, conflictingPublisher);

            await Assert.ThrowsAsync<CryptographicException>(() =>
                guardedReconciler.ReconcileDispatchedAsync(job.JobId, now.AddMinutes(3)));
            Assert.Equal(0, guardedClient.GetCalls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusResearchDispatchOptions CreateOptions(RSA workerRsa) => new(
        WorkerImage: "registry.example/nvidea-worker:sha256-test",
        WorkerPublicKeyPem: workerRsa.ExportSubjectPublicKeyInfoPem(),
        ContainerCommand: "dotnet",
        Platform: "cpu-d3",
        Preset: "1vcpu-4gb",
        Timeout: "3600s",
        SubnetId: "subnet-test",
        Disk: new NebiusServerlessDiskSpec("network-ssd", 10L * 1024 * 1024 * 1024));

    private static AgentJobRecord CreatePendingJob(DateTimeOffset now)
    {
        var definition = new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true,
            MaxAttempts: 3);
        return new AgentJobRecord(
            Guid.NewGuid(), definition, AgentJobState.Pending, JobExecutionLocation.Local, 0,
            new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now),
            null, null, now, now);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-lifecycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeServerlessClient : INebiusServerlessJobClient
    {
        public NebiusServerlessResponse CreateResponse { get; set; } = new(HttpStatusCode.OK, "{}");
        public NebiusServerlessResponse GetResponse { get; set; } = new(HttpStatusCode.OK, "{}");
        public NebiusServerlessResponse ListResponse { get; set; } = new(HttpStatusCode.OK, "{\"items\":[]}");
        public int GetCalls { get; private set; }

        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateResponse);

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            return Task.FromResult(GetResponse);
        }

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ListResponse);

        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{}"));
    }

    private sealed class MemoryWorkItemTransport : IProtectedResearchWorkItemTransport
    {
        private readonly Dictionary<string, ProtectedResearchWorkItemEnvelope> _items = new(StringComparer.Ordinal);
        public Task PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken = default)
        {
            _items.Add(envelope.OpaqueWorkItemId, envelope);
            return Task.CompletedTask;
        }
        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(opaqueWorkItemId, out var value);
            return Task.FromResult(value);
        }
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            _items.Remove(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        private readonly Dictionary<string, ProtectedResearchDispatchBinding> _items = new(StringComparer.Ordinal);
        public int PutCalls { get; private set; }
        public Func<ProtectedResearchDispatchBinding, Task>? OnPut { get; set; }

        public async Task PutAsync(ProtectedResearchDispatchBinding binding, CancellationToken cancellationToken = default)
        {
            if (_items.ContainsKey(binding.OpaqueWorkItemId))
                throw new InvalidOperationException("binding already exists");
            if (OnPut is not null)
                await OnPut(binding);
            _items.Add(binding.OpaqueWorkItemId, binding);
            PutCalls++;
        }

        public Task<ProtectedResearchDispatchBinding?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(opaqueWorkItemId, out var value);
            return Task.FromResult(value);
        }

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            _items.Remove(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
