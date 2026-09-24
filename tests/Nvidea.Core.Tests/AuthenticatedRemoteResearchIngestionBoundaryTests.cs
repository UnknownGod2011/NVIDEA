using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class AuthenticatedRemoteResearchIngestionBoundaryTests
{
    [Fact]
    public async Task IngestAsync_RejectsUnsignedResultBeforeStateMutation()
    {
        using var client = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        var root = CreateTempDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var job = CreateDispatchedJob(now);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            await store.SaveAsync(job);
            var inner = new MemoryResultTransport();
            await inner.PutAsync(Protect(job, client, now));
            var authenticated = new AuthenticatedResearchResultTransport(inner, worker.ExportSubjectPublicKeyInfoPem());
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, authenticated, client.ExportPkcs8PrivateKeyPem(), audit);

            await Assert.ThrowsAsync<CryptographicException>(() => ingestor.IngestAsync(job.JobId, now.AddMinutes(2)));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(AgentJobState.Running, persisted!.State);
            Assert.Equal(JobExecutionLocation.NebiusServerless, persisted.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, persisted.RemoteResearch!.State);
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_result_applied");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task IngestAsync_RejectsResultSignedByUnpinnedWorkerBeforeDecryption()
    {
        using var client = RSA.Create(2048);
        using var wrongClient = RSA.Create(2048);
        using var legitimateWorker = RSA.Create(2048);
        using var pinnedWorker = RSA.Create(2048);
        var root = CreateTempDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var job = CreateDispatchedJob(now);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            await store.SaveAsync(job);
            var inner = new MemoryResultTransport();
            var envelope = Protect(job, client, now);
            await inner.PutAsync(envelope with
            {
                WorkerSignature = RemoteResearchWorkerSignature.Sign(
                    envelope,
                    legitimateWorker.ExportPkcs8PrivateKeyPem())
            });
            var authenticated = new AuthenticatedResearchResultTransport(
                inner,
                pinnedWorker.ExportSubjectPublicKeyInfoPem());
            var audit = new MemoryAuditTrail();
            // The deliberately wrong decryption key is an ordering sentinel: the worker pin must reject
            // the envelope before result-key unwrap/decryption can exercise this private key.
            var ingestor = new RemoteResearchResultIngestor(
                store,
                authenticated,
                wrongClient.ExportPkcs8PrivateKeyPem(),
                audit);

            await Assert.ThrowsAsync<CryptographicException>(() => ingestor.IngestAsync(job.JobId, now.AddMinutes(2)));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(AgentJobState.Running, persisted!.State);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, persisted.RemoteResearch!.State);
            Assert.Null(persisted.RemoteResearch.ResultAppliedAt);
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_result_applied");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task IngestAsync_RejectsCrossJobReplaySignedForDifferentRemoteJobBeforeDecryption()
    {
        using var client = RSA.Create(2048);
        using var wrongClient = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        var root = CreateTempDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var job = CreateDispatchedJob(now);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            await store.SaveAsync(job);
            var inner = new MemoryResultTransport();
            var envelope = Protect(job, client, now);
            var signed = envelope with
            {
                WorkerSignature = RemoteResearchWorkerSignature.Sign(envelope, worker.ExportPkcs8PrivateKeyPem())
            };
            var replayed = signed with { RemoteJobId = "aijob-attacker-replay" };
            await inner.PutAsync(replayed);
            var authenticated = new AuthenticatedResearchResultTransport(inner, worker.ExportSubjectPublicKeyInfoPem());
            // Deliberately provide the wrong result-decryption key. Signature verification must reject
            // the replay before this private key can be exercised by ResearchResultProtector.Unprotect.
            var ingestor = new RemoteResearchResultIngestor(
                store,
                authenticated,
                wrongClient.ExportPkcs8PrivateKeyPem(),
                new MemoryAuditTrail());

            await Assert.ThrowsAsync<CryptographicException>(() => ingestor.IngestAsync(job.JobId, now.AddMinutes(2)));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, persisted!.RemoteResearch!.State);
            Assert.Null(persisted.RemoteResearch.ResultAppliedAt);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static AgentJobRecord CreateDispatchedJob(DateTimeOffset now)
    {
        const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
        const string remoteJobId = "aijob-nebius-auth-boundary";
        var checkpoint = new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now);
        var definition = new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true,
            MaxAttempts: 3);
        var provenance = new RemoteResearchProvenance(
            ResearchWorkItemProtector.ProtocolVersion,
            opaqueId,
            remoteJobId,
            checkpoint.Step,
            checkpoint.SavedAt,
            now.AddSeconds(1),
            RemoteResearchProvenanceState.Dispatched,
            WorkItemExpiresAt: now.AddHours(1));
        return new AgentJobRecord(
            Guid.NewGuid(), definition, AgentJobState.Running, JobExecutionLocation.NebiusServerless,
            Attempt: 1, checkpoint, ApprovalScope: null, LastError: null,
            CreatedAt: now, UpdatedAt: now.AddSeconds(1), RemoteResearch: provenance);
    }

    private static ProtectedResearchResultEnvelope Protect(AgentJobRecord job, RSA client, DateTimeOffset now)
    {
        var provenance = job.RemoteResearch!;
        var result = new RemoteResearchStageResult(
            job.JobId,
            provenance.InputCheckpointStep,
            provenance.OpaqueWorkItemId,
            provenance.RemoteJobId!,
            new JobStepResult(false, ResearchJobHandler.EvidenceStep, "{\"evidence\":\"verified\"}"),
            now.AddMinutes(1),
            now.AddHours(1));
        return ResearchResultProtector.Protect(result, client.ExportSubjectPublicKeyInfoPem());
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-auth-ingest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class MemoryResultTransport : IProtectedResearchResultTransport
    {
        private readonly Dictionary<string, ProtectedResearchResultEnvelope> _items = new(StringComparer.Ordinal);
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default)
        {
            _items[envelope.OpaqueWorkItemId] = envelope;
            return Task.CompletedTask;
        }
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
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
