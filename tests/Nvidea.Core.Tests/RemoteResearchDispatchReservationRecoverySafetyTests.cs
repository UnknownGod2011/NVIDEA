using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchDispatchReservationRecoverySafetyTests
{
    [Fact]
    public async Task ResumeReservedAsync_AuditAlreadySettled_RefusesDuplicateProviderCreate()
    {
        var root = CreateTempDirectory();
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var job = CreatePendingJob(now);
            await store.SaveAsync(job);

            var workItem = new RemoteResearchWorkItem(
                job.JobId,
                job.Checkpoint!.Step,
                job.Checkpoint.Payload,
                ContainsPrivateOsData: false,
                CreatedAt: now,
                ExpiresAt: now.AddHours(1));
            var envelope = ResearchWorkItemProtector.Protect(
                workItem,
                workerRsa.ExportSubjectPublicKeyInfoPem());
            var atomic = new AtomicRemoteResearchDispatchReservation(store, audit);
            var reserved = await atomic.ReserveAsync(
                new RemoteResearchDispatchReservation(
                    job.JobId,
                    job.Checkpoint.Step,
                    envelope.OpaqueWorkItemId,
                    now,
                    envelope.ExpiresAt),
                envelope);

            Assert.Null(reserved.PendingAuditEvent);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, reserved.RemoteResearch!.State);
            Assert.NotNull(reserved.RemoteWorkItemEnvelopeSha256);

            var serverless = new CountingServerlessClient();
            var workItems = new NoReadWorkItemTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new EmptyResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit,
                workItems);
            var dispatcher = new TwoPhaseNebiusResearchDispatcher(
                serverless,
                workItems,
                CreateOptions(workerRsa),
                atomicReservation: atomic);

            await Assert.ThrowsAsync<RemoteResearchDispatchReservationRecoveryNotRequiredException>(() =>
                dispatcher.ResumeReservedAsync(job.JobId, ingestor));

            Assert.Equal(0, serverless.CreateCalls);
            Assert.Equal(0, workItems.GetCalls);
            Assert.Equal(0, workItems.PutCalls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-dispatch-recovery-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CountingServerlessClient : INebiusServerlessJobClient
    {
        public int CreateCalls { get; private set; }

        public Task<NebiusServerlessResponse> CreateAsync(
            NebiusServerlessJobSpec spec,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{\"resourceId\":\"duplicate-must-not-run\"}"));
        }

        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NoReadWorkItemTransport : IProtectedResearchWorkItemTransport
    {
        public int GetCalls { get; private set; }
        public int PutCalls { get; private set; }

        public Task PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PutCalls++;
            throw new InvalidOperationException("Recovery must not rewrite the shared work-item transport.");
        }

        public Task<ProtectedResearchWorkItemEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            GetCalls++;
            throw new InvalidOperationException("Recovery must not read the shared work-item transport.");
        }

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
        private readonly List<AuditEvent> _events = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
