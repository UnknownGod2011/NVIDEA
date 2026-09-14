using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchDispatchAuditOutboxTests
{
    [Fact]
    public async Task DispatchWithReservationAsync_ReservationAuditFailure_PreservesOwnedCiphertextAndSkipsNebius()
    {
        var root = CreateTempDirectory();
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var workItems = new MemoryWorkItemTransport();
            var audit = new FailOnceAuditTrail { FailNextAppend = true };
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);

            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit,
                workItems);
            var serverless = new InspectingServerlessClient(_ =>
                throw new InvalidOperationException("Nebius create must not run before the reservation audit is durable."));
            var dispatcher = new TwoPhaseNebiusResearchDispatcher(serverless, workItems, CreateOptions(workerRsa));

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

            await Assert.ThrowsAsync<RemoteResearchDispatchReservationAuditPendingException>(() =>
                dispatcher.DispatchWithReservationAsync(workItem, authorization, ingestor));

            Assert.Equal(0, serverless.CreateCalls);
            var stranded = await store.GetAsync(job.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(AgentJobState.Running, stranded!.State);
            Assert.Equal(JobExecutionLocation.Local, stranded.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, stranded.RemoteResearch!.State);
            Assert.Null(stranded.RemoteResearch.RemoteJobId);
            Assert.NotNull(stranded.PendingAuditEvent);
            Assert.Equal("research.remote_dispatch_reserved", stranded.PendingAuditEvent!.EventType);
            Assert.True(workItems.Contains(stranded.RemoteResearch.OpaqueWorkItemId));
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_dispatch_reserved");

            var restarted = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit,
                workItems);
            var recovered = await restarted.RecoverPendingAuditAsync(job.JobId);

            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, recovered.RemoteResearch!.State);
            Assert.True(workItems.Contains(recovered.RemoteResearch.OpaqueWorkItemId));
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_dispatch_reserved"));
            Assert.Equal(0, serverless.CreateCalls);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                dispatcher.DispatchWithReservationAsync(workItem, authorization, restarted));
            Assert.Equal(0, serverless.CreateCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_dispatch_reserved"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task AttachDispatchAsync_AttachedAuditFailure_LeavesRecoverableExactRemoteBinding()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FailOnceAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            const string opaqueId = "dispatch-audit-opaque-0123456789abcdef";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                job.JobId,
                job.Checkpoint!.Step,
                opaqueId,
                now.AddSeconds(1),
                now.AddHours(1)));

            audit.FailNextAppend = true;
            const string remoteJobId = "remote-nebius-dispatch-audit-42";
            await Assert.ThrowsAsync<IOException>(() => ingestor.AttachDispatchAsync(
                new NebiusResearchDispatchReceipt(
                    job.JobId,
                    job.Checkpoint.Step,
                    opaqueId,
                    remoteJobId,
                    now.AddSeconds(2))));

            var stranded = await store.GetAsync(job.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(AgentJobState.Running, stranded!.State);
            Assert.Equal(JobExecutionLocation.NebiusServerless, stranded.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, stranded.RemoteResearch!.State);
            Assert.Equal(remoteJobId, stranded.RemoteResearch.RemoteJobId);
            Assert.Equal(1, stranded.Attempt);
            Assert.NotNull(stranded.PendingAuditEvent);
            Assert.Equal("research.remote_dispatched", stranded.PendingAuditEvent!.EventType);
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_dispatched");

            var restarted = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var recovered = await restarted.RecoverPendingAuditAsync(job.JobId);

            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(JobExecutionLocation.NebiusServerless, recovered.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, recovered.RemoteResearch!.State);
            Assert.Equal(remoteJobId, recovered.RemoteResearch.RemoteJobId);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_dispatched"));

            var recoveredAgain = await restarted.RecoverPendingAuditAsync(job.JobId);
            Assert.Null(recoveredAgain.PendingAuditEvent);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_dispatched"));
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-dispatch-audit-outbox-" + Guid.NewGuid().ToString("N"));
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
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FailOnceAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();
        public bool FailNextAppend { get; set; }

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (FailNextAppend)
            {
                FailNextAppend = false;
                throw new IOException("Injected audit storage failure.");
            }

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
