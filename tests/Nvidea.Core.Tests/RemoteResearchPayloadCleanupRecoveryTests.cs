using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchPayloadCleanupRecoveryTests
{
    [Fact]
    public async Task Ingest_cleanup_failure_keeps_durable_cleanup_intent_and_restart_drains_it_without_replaying_result()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new FailOnceDeleteResultTransport();
            var audit = new InMemoryAuditTrail();
            var now = DateTimeOffset.Parse("2026-09-14T13:00:00Z");
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            const string remoteJobId = "aijob-nebius-cleanup-recovery";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                original.JobId,
                original.Checkpoint!.Step,
                opaqueId,
                now.AddSeconds(1)));
            await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
                original.JobId,
                original.Checkpoint.Step,
                opaqueId,
                remoteJobId,
                now.AddSeconds(2)));

            var remote = new RemoteResearchStageResult(
                original.JobId,
                original.Checkpoint.Step,
                opaqueId,
                remoteJobId,
                new JobStepResult(
                    Completed: false,
                    CheckpointStep: ResearchJobHandler.EvidenceStep,
                    CheckpointPayload: "{\"evidence\":\"verified\"}"),
                now.AddMinutes(1),
                now.AddHours(1));
            await results.PutAsync(ResearchResultProtector.Protect(remote, clientRsa.ExportSubjectPublicKeyInfoPem()));
            results.FailNextDelete = true;

            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ingestor.IngestAsync(original.JobId, now.AddMinutes(2)));
            Assert.Contains("cleanup remains pending", failure.Message, StringComparison.OrdinalIgnoreCase);

            var stranded = await store.GetAsync(original.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, stranded!.RemoteResearch!.State);
            Assert.Null(stranded.PendingAuditEvent);
            Assert.NotNull(stranded.PendingProtectedPayloadCleanup);
            Assert.Equal(opaqueId, stranded.PendingProtectedPayloadCleanup!.OpaqueWorkItemId);
            Assert.True(results.Contains(opaqueId));
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            var restarted = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var recovered = await restarted.RecoverPendingAuditAsync(original.JobId);

            Assert.Null(recovered.PendingAuditEvent);
            Assert.Null(recovered.PendingProtectedPayloadCleanup);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, recovered.RemoteResearch!.State);
            Assert.False(results.Contains(opaqueId));
            Assert.Equal(2, results.DeleteCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            var recoveredAgain = await restarted.RecoverPendingAuditAsync(original.JobId);
            Assert.Null(recoveredAgain.PendingProtectedPayloadCleanup);
            Assert.Equal(2, results.DeleteCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Recovery_clears_marker_when_artifact_was_already_deleted_before_crash()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new FailOnceDeleteResultTransport();
            var audit = new InMemoryAuditTrail();
            var now = DateTimeOffset.Parse("2026-09-14T13:00:00Z");
            var job = CreateAppliedJob(now);
            await store.SaveAsync(job);

            var restarted = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var recovered = await restarted.RecoverPendingCleanupAsync(job.JobId);

            Assert.Null(recovered.PendingProtectedPayloadCleanup);
            Assert.Equal(1, results.DeleteCalls);
            Assert.Null((await store.GetAsync(job.JobId))!.PendingProtectedPayloadCleanup);
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

    private static AgentJobRecord CreateAppliedJob(DateTimeOffset now)
    {
        const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
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
            AgentJobState.Completed,
            JobExecutionLocation.Local,
            Attempt: 1,
            new AgentJobCheckpoint(ResearchJobHandler.EvidenceStep, "{\"evidence\":true}", now),
            ApprovalScope: null,
            LastError: null,
            CreatedAt: now.AddMinutes(-10),
            UpdatedAt: now,
            RemoteResearch: new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                opaqueId,
                "aijob-nebius-cleanup-recovery",
                ResearchJobHandler.PlannedStep,
                now.AddMinutes(-10),
                now.AddMinutes(-9),
                RemoteResearchProvenanceState.ResultApplied,
                ResultAppliedAt: now),
            PendingProtectedPayloadCleanup: new PendingProtectedPayloadCleanup(
                Guid.NewGuid(),
                opaqueId,
                now));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-payload-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FailOnceDeleteResultTransport : IProtectedResearchResultTransport
    {
        private readonly Dictionary<string, ProtectedResearchResultEnvelope> _items = new(StringComparer.Ordinal);
        public bool FailNextDelete { get; set; }
        public int DeleteCalls { get; private set; }
        public bool Contains(string opaqueWorkItemId) => _items.ContainsKey(opaqueWorkItemId);

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
            DeleteCalls++;
            if (FailNextDelete)
            {
                FailNextDelete = false;
                throw new IOException("Injected protected payload deletion failure.");
            }

            _items.Remove(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
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
