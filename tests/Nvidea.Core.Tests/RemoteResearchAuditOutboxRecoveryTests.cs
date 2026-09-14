using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchAuditOutboxRecoveryTests
{
    [Fact]
    public async Task IngestAsync_AuditAppendFailure_LeavesRecoverableDurableOutboxWithoutEarlyCleanup()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new MemoryResultTransport();
            var audit = new FailOnceAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            await store.SaveAsync(original);

            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
            const string remoteJobId = "aijob-nebius-audit-outbox";
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

            audit.FailNextAppend = true;
            await Assert.ThrowsAsync<IOException>(() => ingestor.IngestAsync(original.JobId, now.AddMinutes(2)));

            var stranded = await store.GetAsync(original.JobId);
            Assert.NotNull(stranded);
            Assert.Equal(JobExecutionLocation.Local, stranded!.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, stranded.RemoteResearch!.State);
            Assert.Equal(ResearchJobHandler.EvidenceStep, stranded.Checkpoint!.Step);
            Assert.NotNull(stranded.PendingAuditEvent);
            Assert.Equal("research.remote_result_applied", stranded.PendingAuditEvent!.EventType);
            Assert.True(results.Contains(opaqueId));
            Assert.DoesNotContain(audit.Events, e => e.EventType == "research.remote_result_applied");

            var restarted = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);
            var recovered = await restarted.RecoverPendingAuditAsync(original.JobId);

            Assert.Null(recovered.PendingAuditEvent);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, recovered.RemoteResearch!.State);
            Assert.False(results.Contains(opaqueId));
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            var recoveredAgain = await restarted.RecoverPendingAuditAsync(original.JobId);
            Assert.Null(recoveredAgain.PendingAuditEvent);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FlushAsync_AlreadyAppendedPendingEvent_ClearsMarkerWithoutDuplicate()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FailOnceAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            var pending = CreateAudit(original, Guid.NewGuid());
            var durable = original with { PendingAuditEvent = pending };
            await store.SaveAsync(durable);
            audit.Events.Add(pending);

            var outbox = new DurableJobAuditOutbox(store, audit);
            var recovered = await outbox.FlushAsync(durable);

            Assert.Null(recovered.PendingAuditEvent);
            Assert.Single(audit.Events);
            Assert.Equal(pending.EventId, audit.Events[0].EventId);
            Assert.Null((await store.GetAsync(original.JobId))!.PendingAuditEvent);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FlushAsync_ConflictingExistingEventId_FailsClosedAndKeepsMarker()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new FailOnceAuditTrail();
            var now = DateTimeOffset.UtcNow;
            var original = CreatePendingJob(now);
            var eventId = Guid.NewGuid();
            var pending = CreateAudit(original, eventId);
            var durable = original with { PendingAuditEvent = pending };
            await store.SaveAsync(durable);
            audit.Events.Add(pending with { Summary = "Conflicting forged content." });

            var outbox = new DurableJobAuditOutbox(store, audit);
            await Assert.ThrowsAsync<InvalidDataException>(() => outbox.FlushAsync(durable));

            var after = await store.GetAsync(original.JobId);
            Assert.NotNull(after!.PendingAuditEvent);
            Assert.Equal(eventId, after.PendingAuditEvent!.EventId);
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

    private static AuditEvent CreateAudit(AgentJobRecord job, Guid eventId) =>
        new(
            eventId,
            DateTimeOffset.UtcNow,
            job.Definition.CapabilityId,
            job.JobId.ToString("N"),
            "research.remote_result_applied",
            job.Definition.Risk,
            Allowed: true,
            Approved: false,
            ApprovalScope: string.Empty,
            Summary: "Protected remote research result applied exactly once.",
            Metadata: new Dictionary<string, string>
            {
                ["jobType"] = job.Definition.JobType,
                ["state"] = job.State.ToString(),
                ["executionLocation"] = job.ExecutionLocation.ToString(),
                ["attempt"] = job.Attempt.ToString()
            });

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-audit-outbox-" + Guid.NewGuid().ToString("N"));
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
