using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class TwoPhaseNebiusResearchDispatcherTests
{
    [Fact]
    public async Task DispatchWithReservationAsync_PersistsReservationBeforeNebiusCreate()
    {
        var root = CreateTempDirectory();
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var transport = new MemoryWorkItemTransport();
            var results = new EmptyResultTransport();
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);
            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit,
                transport);

            var serverless = new InspectingServerlessClient(async spec =>
            {
                var persisted = await store.GetAsync(job.JobId);
                Assert.NotNull(persisted);
                Assert.Equal(AgentJobState.Running, persisted!.State);
                Assert.Equal(JobExecutionLocation.Local, persisted.ExecutionLocation);
                Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, persisted.RemoteResearch!.State);
                Assert.Null(persisted.RemoteResearch.RemoteJobId);
                Assert.Contains(persisted.RemoteResearch.OpaqueWorkItemId, spec.Arguments, StringComparison.Ordinal);
                return new NebiusServerlessResponse(HttpStatusCode.OK, "{\"resourceId\":\"remote-nebius-42\"}");
            });
            var dispatcher = new TwoPhaseNebiusResearchDispatcher(serverless, transport, CreateOptions(workerRsa));
            var workItem = new RemoteResearchWorkItem(
                job.JobId,
                job.Checkpoint!.Step,
                job.Checkpoint.Payload,
                ContainsPrivateOsData: false,
                CreatedAt: now,
                ExpiresAt: now.AddHours(1));
            var authorization = new ResearchCloudAuthorization(
                job.JobId,
                job.Checkpoint.Step,
                Approved: true,
                ResearchWorkItemProtector.DisclosureVersion,
                GrantedAt: now);

            var dispatched = await dispatcher.DispatchWithReservationAsync(workItem, authorization, ingestor);

            Assert.Equal(1, serverless.CreateCalls);
            Assert.Equal(JobExecutionLocation.NebiusServerless, dispatched.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, dispatched.RemoteResearch!.State);
            Assert.Equal("remote-nebius-42", dispatched.RemoteResearch.RemoteJobId);
            Assert.Equal(1, dispatched.Attempt);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_dispatch_reserved");
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_dispatched");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DispatchWithReservationAsync_AmbiguousCreateLeavesReservationFailClosed()
    {
        var root = CreateTempDirectory();
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var transport = new MemoryWorkItemTransport();
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                new MemoryAuditTrail(),
                transport);
            var dispatcher = new TwoPhaseNebiusResearchDispatcher(
                new InspectingServerlessClient(_ => Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{}"))),
                transport,
                CreateOptions(workerRsa));
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

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                dispatcher.DispatchWithReservationAsync(workItem, authorization, ingestor));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(AgentJobState.Running, persisted!.State);
            Assert.Equal(JobExecutionLocation.Local, persisted.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, persisted.RemoteResearch!.State);
            Assert.Null(persisted.RemoteResearch.RemoteJobId);
            Assert.Equal(0, persisted.Attempt);
            Assert.True(transport.Contains(persisted.RemoteResearch.OpaqueWorkItemId));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DispatchWithReservationAsync_ReservationFailureCleansPreparedCiphertextAndSkipsNebius()
    {
        var root = CreateTempDirectory();
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var transport = new MemoryWorkItemTransport();
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now) with { State = AgentJobState.Running };
            await store.SaveAsync(job);
            var serverless = new InspectingServerlessClient(_ => throw new InvalidOperationException("must not be called"));
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                new MemoryAuditTrail(),
                transport);
            var dispatcher = new TwoPhaseNebiusResearchDispatcher(serverless, transport, CreateOptions(workerRsa));
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

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                dispatcher.DispatchWithReservationAsync(workItem, authorization, ingestor));

            Assert.Equal(0, serverless.CreateCalls);
            Assert.Equal(0, transport.Count);
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-two-phase-dispatch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class InspectingServerlessClient : INebiusServerlessJobClient
    {
        private readonly Func<NebiusServerlessJobSpec, Task<NebiusServerlessResponse>> _create;
        public int CreateCalls { get; private set; }

        public InspectingServerlessClient(Func<NebiusServerlessJobSpec, Task<NebiusServerlessResponse>> create) => _create = create;

        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return _create(spec);
        }

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class MemoryWorkItemTransport : IProtectedResearchWorkItemTransport
    {
        private readonly Dictionary<string, ProtectedResearchWorkItemEnvelope> _items = new(StringComparer.Ordinal);
        public int Count => _items.Count;
        public bool Contains(string id) => _items.ContainsKey(id);

        public Task PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken = default)
        {
            _items[envelope.OpaqueWorkItemId] = envelope;
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

    private sealed class EmptyResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.FromResult<ProtectedResearchResultEnvelope?>(null);
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
