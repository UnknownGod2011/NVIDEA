using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchResultIngestorTests
{
    [Fact]
    public async Task IngestAsync_AppliesVerifiedResultOnceAndReturnsExecutionLocal()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new MemoryResultTransport();
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var receipt = new NebiusResearchDispatchReceipt(
                original.JobId,
                original.Checkpoint!.Step,
                "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt",
                "aijob-nebius-123",
                now.AddSeconds(1));

            var attached = await ingestor.AttachDispatchAsync(receipt);
            Assert.Equal(AgentJobState.Running, attached.State);
            Assert.Equal(JobExecutionLocation.NebiusServerless, attached.ExecutionLocation);
            Assert.NotNull(attached.RemoteResearch);

            var remote = new RemoteResearchStageResult(
                original.JobId,
                original.Checkpoint.Step,
                receipt.OpaqueWorkItemId,
                receipt.RemoteJobId,
                new JobStepResult(
                    Completed: false,
                    CheckpointStep: ResearchJobHandler.EvidenceStep,
                    CheckpointPayload: "{\"evidence\":\"verified\"}"),
                now.AddMinutes(1),
                now.AddHours(1));
            await results.PutAsync(ResearchResultProtector.Protect(remote, clientRsa.ExportSubjectPublicKeyInfoPem()));

            var applied = await ingestor.IngestAsync(original.JobId, now.AddMinutes(2));

            Assert.Equal(AgentJobState.Pending, applied.State);
            Assert.Equal(JobExecutionLocation.Local, applied.ExecutionLocation);
            Assert.Equal(ResearchJobHandler.EvidenceStep, applied.Checkpoint!.Step);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, applied.RemoteResearch!.State);
            Assert.NotNull(applied.RemoteResearch.ResultAppliedAt);
            Assert.Contains(audit.Events, e => e.EventType == "research.remote_result_applied");
            await Assert.ThrowsAsync<InvalidOperationException>(() => ingestor.IngestAsync(original.JobId, now.AddMinutes(3)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task IngestAsync_RejectsLocalCheckpointChangedAfterDispatch()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new MemoryResultTransport();
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            var ingestor = new RemoteResearchResultIngestor(store, results, clientRsa.ExportPkcs8PrivateKeyPem(), audit);
            var receipt = new NebiusResearchDispatchReceipt(
                original.JobId,
                original.Checkpoint!.Step,
                "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt",
                "aijob-nebius-123",
                now.AddSeconds(1));
            var attached = await ingestor.AttachDispatchAsync(receipt);

            await store.SaveAsync(attached with
            {
                Checkpoint = new AgentJobCheckpoint(ResearchJobHandler.EvidenceStep, "newer", now.AddMinutes(1)),
                UpdatedAt = now.AddMinutes(1)
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => ingestor.IngestAsync(original.JobId, now.AddMinutes(2)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CompareExchangeAsync_RejectsStaleExpectedVersion()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            var newer = original with { State = AgentJobState.Running, UpdatedAt = now.AddSeconds(1) };
            await store.SaveAsync(newer);

            var staleReplacement = original with { State = AgentJobState.Completed, UpdatedAt = now.AddSeconds(2) };
            var exchanged = await store.CompareExchangeAsync(original, staleReplacement);

            Assert.False(exchanged);
            Assert.Equal(AgentJobState.Running, (await store.GetAsync(original.JobId))!.State);
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
            BenefitsFromBackgroundExecution: false,
            MaxAttempts: 3);
        return new AgentJobRecord(
            Guid.NewGuid(),
            definition,
            AgentJobState.Pending,
            JobExecutionLocation.Local,
            Attempt: 0,
            new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now),
            ApprovalScope: null,
            LastError: null,
            CreatedAt: now,
            UpdatedAt: now);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-remote-ingest-" + Guid.NewGuid().ToString("N"));
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
