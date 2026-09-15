using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class ResearchDispatchBindingRecoveryTests
{
    private const string EnvelopeSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task EnsureAsync_RePublishesExactDurableRemoteId_Idempotently()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = await CreateDispatchedJobAsync(store, audit, clientRsa, now);
            var bindings = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(bindings, clientRsa.ExportPkcs8PrivateKeyPem());
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher);
            var recovery = new ResearchDispatchBindingRecovery(store, publisher, obligation);

            var first = await recovery.EnsureAsync(job.JobId);
            var second = await recovery.EnsureAsync(job.JobId);

            Assert.Equal("remote-recovery-42", first.RemoteResearch!.RemoteJobId);
            Assert.Equal(first.RemoteResearch.RemoteJobId, second.RemoteResearch!.RemoteJobId);
            Assert.Equal(1, bindings.PutCalls);
            Assert.Null(second.PendingResearchDispatchBinding);

            var binding = await bindings.GetAsync(first.RemoteResearch.OpaqueWorkItemId);
            Assert.NotNull(binding);
            var verified = ResearchDispatchBindingProtector.Verify(
                binding!,
                first.RemoteResearch.OpaqueWorkItemId,
                clientRsa.ExportSubjectPublicKeyInfoPem());
            Assert.Equal(ResearchDispatchBindingProtector.EnvelopeBoundProtocolVersion, verified.ProtocolVersion);
            Assert.Equal("remote-recovery-42", verified.RemoteJobId);
            Assert.Equal(EnvelopeSha256, verified.WorkItemEnvelopeSha256);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureAsync_ConflictingBindingFailsClosed_WithoutChangingDurableProvenance()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = await CreateDispatchedJobAsync(store, audit, clientRsa, now);
            var bindings = new MemoryBindingTransport();
            await bindings.PutAsync(ResearchDispatchBindingProtector.Sign(
                job.RemoteResearch!.OpaqueWorkItemId,
                "substituted-remote-id",
                now,
                now.AddMinutes(30),
                clientRsa.ExportPkcs8PrivateKeyPem()));
            var publisher = new ResearchDispatchBindingPublisher(bindings, clientRsa.ExportPkcs8PrivateKeyPem());
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher);
            var recovery = new ResearchDispatchBindingRecovery(store, publisher, obligation);

            await Assert.ThrowsAsync<CryptographicException>(() => recovery.EnsureAsync(job.JobId));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, persisted!.RemoteResearch!.State);
            Assert.Equal("remote-recovery-42", persisted.RemoteResearch.RemoteJobId);
            Assert.NotNull(persisted.PendingResearchDispatchBinding);
            Assert.Equal(EnvelopeSha256, persisted.PendingResearchDispatchBinding!.EnvelopeSha256);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RequestCancellationAsync_RepairsMissingBindingBeforeProviderCancellation()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            using var workerRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = await CreateDispatchedJobAsync(store, audit, clientRsa, now);
            var bindings = new MemoryBindingTransport();
            var serverless = new FakeServerlessClient();
            serverless.OnCancel = async remoteJobId =>
            {
                Assert.Equal("remote-recovery-42", remoteJobId);
                var binding = await bindings.GetAsync(job.RemoteResearch!.OpaqueWorkItemId);
                Assert.NotNull(binding);
                Assert.Equal(ResearchDispatchBindingProtector.EnvelopeBoundProtocolVersion, binding!.ProtocolVersion);
                Assert.Equal(EnvelopeSha256, binding.WorkItemEnvelopeSha256);
            };
            var runtime = NebiusResearchClientRuntime.Create(
                store,
                serverless,
                new MemoryWorkItemTransport(),
                new EmptyResultTransport(),
                bindings,
                CreateOptions(workerRsa),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            var cancelled = await runtime.RequestCancellationAsync(job.JobId);

            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, cancelled.RemoteResearch!.State);
            Assert.Equal(1, bindings.PutCalls);
            Assert.Equal(1, serverless.CancelCalls);
            Assert.Null((await store.GetAsync(job.JobId))!.PendingResearchDispatchBinding);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RequestCancellationAsync_FirstBindingFailure_PersistsExactObligation_AndRestartClearsOnlyAfterSuccess()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            using var workerRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = await CreateDispatchedJobAsync(store, audit, clientRsa, now);
            var bindings = new MemoryBindingTransport { FailPut = true };
            var serverless = new FakeServerlessClient();
            var runtime = NebiusResearchClientRuntime.Create(
                store,
                serverless,
                new MemoryWorkItemTransport(),
                new EmptyResultTransport(),
                bindings,
                CreateOptions(workerRsa),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.RequestCancellationAsync(job.JobId));

            var failed = await store.GetAsync(job.JobId);
            Assert.NotNull(failed);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, failed!.RemoteResearch!.State);
            Assert.Equal(0, serverless.CancelCalls);
            var pending = Assert.IsType<PendingResearchDispatchBinding>(failed.PendingResearchDispatchBinding);
            Assert.Equal(job.RemoteResearch.OpaqueWorkItemId, pending.OpaqueWorkItemId);
            Assert.Equal("remote-recovery-42", pending.RemoteJobId);
            Assert.Equal(EnvelopeSha256, pending.EnvelopeSha256);
            Assert.Equal(job.RemoteResearch.WorkItemExpiresAt, pending.ExpiresAt);

            bindings.FailPut = false;
            var restartedRuntime = NebiusResearchClientRuntime.Create(
                store,
                serverless,
                new MemoryWorkItemTransport(),
                new EmptyResultTransport(),
                bindings,
                CreateOptions(workerRsa),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            var cancelled = await restartedRuntime.RequestCancellationAsync(job.JobId);

            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, cancelled.RemoteResearch!.State);
            Assert.Equal(1, bindings.PutCalls);
            Assert.Equal(1, serverless.CancelCalls);
            var completed = await store.GetAsync(job.JobId);
            Assert.NotNull(completed);
            Assert.Null(completed!.PendingResearchDispatchBinding);
            var binding = await bindings.GetAsync(pending.OpaqueWorkItemId);
            Assert.NotNull(binding);
            Assert.Equal(pending.RemoteJobId, binding!.RemoteJobId);
            Assert.Equal(pending.EnvelopeSha256, binding.WorkItemEnvelopeSha256);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<AgentJobRecord> CreateDispatchedJobAsync(
        JsonAgentJobStore store,
        IAuditTrail audit,
        RSA clientRsa,
        DateTimeOffset now)
    {
        var job = CreatePendingJob(now);
        await store.SaveAsync(job);
        var ingestor = new RemoteResearchResultIngestor(
            store,
            new EmptyResultTransport(),
            clientRsa.ExportPkcs8PrivateKeyPem(),
            audit);
        const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
        await ingestor.ReserveDispatchAsync(
            new RemoteResearchDispatchReservation(
                job.JobId,
                job.Checkpoint!.Step,
                opaqueId,
                now,
                now.AddHours(1)));
        var attached = await ingestor.AttachDispatchAsync(
            new NebiusResearchDispatchReceipt(
                job.JobId,
                job.Checkpoint.Step,
                opaqueId,
                "remote-recovery-42",
                now));
        var envelopeBound = attached with { RemoteWorkItemEnvelopeSha256 = EnvelopeSha256, UpdatedAt = DateTimeOffset.UtcNow };
        Assert.True(await store.CompareExchangeAsync(attached, envelopeBound));
        return envelopeBound;
    }

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
            Guid.NewGuid(),
            definition,
            AgentJobState.Pending,
            JobExecutionLocation.Local,
            0,
            new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now),
            null,
            null,
            now,
            now);
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeServerlessClient : INebiusServerlessJobClient
    {
        public int CancelCalls { get; private set; }
        public Func<string, Task>? OnCancel { get; set; }

        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Binding recovery must not create provider work.");

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Binding recovery must not read provider state before cancellation is requested.");

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Binding recovery must not list provider work.");

        public async Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            if (OnCancel is not null)
                await OnCancel(remoteJobId);
            return new NebiusServerlessResponse(HttpStatusCode.OK, "{}");
        }
    }

    private sealed class MemoryBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        private readonly Dictionary<string, ProtectedResearchDispatchBinding> _items = new(StringComparer.Ordinal);
        public int PutCalls { get; private set; }
        public bool FailPut { get; set; }

        public Task PutAsync(ProtectedResearchDispatchBinding binding, CancellationToken cancellationToken = default)
        {
            if (FailPut)
                throw new InvalidOperationException("injected binding publication failure");
            if (_items.ContainsKey(binding.OpaqueWorkItemId))
                throw new InvalidOperationException("binding already exists");
            _items.Add(binding.OpaqueWorkItemId, binding);
            PutCalls++;
            return Task.CompletedTask;
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

    private sealed class MemoryWorkItemTransport : IProtectedResearchWorkItemTransport
    {
        public Task PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(null);
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
            if (!Events.Any(existing => existing.Id == auditEvent.Id))
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
