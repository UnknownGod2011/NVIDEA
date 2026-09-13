using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchResultAuditOrderingTests
{
    [Fact]
    public async Task ReserveDispatchAsync_RejectsMalformedAuditIdentityBeforeReservationCas()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now) with
            {
                Definition = CreatePendingJob(now).Definition with { CapabilityId = "research.deep\nforged" }
            };
            await store.SaveAsync(original);
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new MemoryResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                    original.JobId,
                    original.Checkpoint!.Step,
                    "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt",
                    now.AddSeconds(1))));

            var durable = await store.GetAsync(original.JobId);
            Assert.NotNull(durable);
            Assert.Equal(AgentJobState.Pending, durable!.State);
            Assert.Equal(JobExecutionLocation.Local, durable.ExecutionLocation);
            Assert.Null(durable.RemoteResearch);
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_dispatch_reserved");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task AttachDispatchAsync_RejectsMalformedAuditIdentityBeforeAttachCas()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);
            var ingestor = new RemoteResearchResultIngestor(
                store,
                new MemoryResultTransport(),
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            var reserved = await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                original.JobId,
                original.Checkpoint!.Step,
                opaqueId,
                now.AddSeconds(1)));
            var corrupted = reserved with
            {
                Definition = reserved.Definition with { CapabilityId = "research.deep\nforged" }
            };
            await store.SaveAsync(corrupted);
            audit.Events.Clear();

            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
                    original.JobId,
                    original.Checkpoint.Step,
                    opaqueId,
                    "aijob-nebius-123",
                    now.AddSeconds(2))));

            var durable = await store.GetAsync(original.JobId);
            Assert.NotNull(durable);
            Assert.Equal(AgentJobState.Running, durable!.State);
            Assert.Equal(JobExecutionLocation.Local, durable.ExecutionLocation);
            Assert.Equal(0, durable.Attempt);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, durable.RemoteResearch!.State);
            Assert.Null(durable.RemoteResearch.RemoteJobId);
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_dispatched");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task IngestAsync_RejectsMalformedAuditIdentityBeforeResultApplyCasOrCleanup()
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

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            const string remoteJobId = "aijob-nebius-123";
            await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
                original.JobId,
                original.Checkpoint!.Step,
                opaqueId,
                now.AddSeconds(1)));
            var attached = await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
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

            await store.SaveAsync(attached with
            {
                Definition = attached.Definition with { CapabilityId = "research.deep\nforged" }
            });
            audit.Events.Clear();

            await Assert.ThrowsAnyAsync<ArgumentException>(() =>
                ingestor.IngestAsync(original.JobId, now.AddMinutes(2)));

            var durable = await store.GetAsync(original.JobId);
            Assert.NotNull(durable);
            Assert.Equal(AgentJobState.Running, durable!.State);
            Assert.Equal(JobExecutionLocation.NebiusServerless, durable.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.Dispatched, durable.RemoteResearch!.State);
            Assert.Equal(ResearchJobHandler.PlannedStep, durable.Checkpoint!.Step);
            Assert.True(results.Contains(opaqueId));
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_result_applied");
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-remote-audit-ordering-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class MemoryResultTransport : IProtectedResearchResultTransport
    {
        private readonly Dictionary<string, ProtectedResearchResultEnvelope> _items = new(StringComparer.Ordinal);

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
