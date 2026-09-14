using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchCloudAuditRecoveryIntegrationTests
{
    [Theory]
    [InlineData("research.remote_result_applied")]
    [InlineData("research.remote_result_applied_after_cancel_request")]
    public async Task ReconcileAsync_ResultAppliedWithPendingAudit_RecoversLocallyWithoutLifecycleReplay(
        string eventType)
    {
        var directory = CreateDirectory();
        try
        {
            using var rsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json"));
            var audit = new MemoryAuditTrail();
            var results = new TrackingResultTransport();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                rsa.ExportPkcs8PrivateKeyPem(),
                audit);

            var now = DateTimeOffset.UtcNow;
            var jobId = Guid.NewGuid();
            const string opaqueId = "audit-recovery-opaque-id-abcdefghijklmnop";
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
                "job-audit-recovery-123",
                ResearchJobHandler.PlannedStep,
                now,
                now.AddSeconds(1),
                RemoteResearchProvenanceState.ResultApplied,
                ResultAppliedAt: now.AddMinutes(1),
                WorkItemExpiresAt: now.AddHours(1));
            var pendingAudit = new AuditEvent(
                Guid.NewGuid(),
                now.AddMinutes(1),
                definition.CapabilityId,
                jobId.ToString("N"),
                eventType,
                definition.Risk,
                Allowed: true,
                Approved: false,
                ApprovalScope: string.Empty,
                Summary: eventType == "research.remote_result_applied"
                    ? "Protected remote research result applied exactly once."
                    : "Verified Nebius completion won the cancellation race; protected remote research result applied exactly once.",
                Metadata: new Dictionary<string, string>
                {
                    ["jobType"] = definition.JobType,
                    ["state"] = AgentJobState.Pending.ToString(),
                    ["executionLocation"] = JobExecutionLocation.Local.ToString(),
                    ["attempt"] = "1"
                });
            var stranded = new AgentJobRecord(
                jobId,
                definition,
                AgentJobState.Pending,
                JobExecutionLocation.Local,
                Attempt: 1,
                new AgentJobCheckpoint(ResearchJobHandler.EvidenceStep, "{\"evidence\":\"already-applied\"}", now.AddMinutes(1)),
                ApprovalScope: null,
                LastError: null,
                CreatedAt: now,
                UpdatedAt: now.AddMinutes(1))
            {
                RemoteResearch = provenance,
                PendingAuditEvent = pendingAudit
            };
            await store.SaveAsync(stranded);

            var remote = new RecoveryOnlyRemoteRuntime(ingestor);
            var coordinator = new ResearchCloudExecutionCoordinator(directory, remote);

            var status = await coordinator.ReconcileAsync(jobId);

            Assert.Equal(jobId, status.JobId);
            Assert.Equal(AgentJobState.Pending, status.State);
            Assert.Equal(JobExecutionLocation.Local, status.ExecutionLocation);
            Assert.Equal(1, remote.RecoverPendingAuditCalls);
            Assert.Equal(0, remote.ReconcileReservedCalls);
            Assert.Equal(0, remote.ReconcileDispatchedCalls);
            Assert.Equal(0, remote.ReconcileCancellationCalls);
            Assert.Equal(0, remote.RequestCancellationCalls);
            Assert.Equal(0, remote.DispatchCalls);
            Assert.Equal(1, results.DeleteCalls);
            Assert.Equal(opaqueId, results.LastDeletedOpaqueId);

            var durable = await store.GetAsync(jobId);
            Assert.NotNull(durable);
            Assert.Null(durable!.PendingAuditEvent);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, durable.RemoteResearch!.State);
            Assert.Single(audit.Events.Where(e => e.EventId == pendingAudit.EventId));
            Assert.Equal(eventType, audit.Events.Single(e => e.EventId == pendingAudit.EventId).EventType);

            var recoveredAgain = await ingestor.RecoverPendingAuditAsync(jobId);
            Assert.Null(recoveredAgain.PendingAuditEvent);
            Assert.Single(audit.Events.Where(e => e.EventId == pendingAudit.EventId));
            Assert.Equal(1, results.DeleteCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "nvidea-cloud-audit-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private sealed class RecoveryOnlyRemoteRuntime : IRemoteResearchClientRuntime
    {
        private readonly RemoteResearchResultIngestor _ingestor;

        public RecoveryOnlyRemoteRuntime(RemoteResearchResultIngestor ingestor)
        {
            _ingestor = ingestor;
        }

        public int RecoverPendingAuditCalls { get; private set; }
        public int DispatchCalls { get; private set; }
        public int ReconcileReservedCalls { get; private set; }
        public int ReconcileDispatchedCalls { get; private set; }
        public int RequestCancellationCalls { get; private set; }
        public int ReconcileCancellationCalls { get; private set; }

        public Task<AgentJobRecord> RecoverPendingAuditAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            RecoverPendingAuditCalls++;
            return _ingestor.RecoverPendingAuditAsync(jobId, cancellationToken);
        }

        public Task<AgentJobRecord> DispatchAsync(
            RemoteResearchWorkItem workItem,
            ResearchCloudAuthorization authorization,
            CancellationToken cancellationToken = default)
        {
            DispatchCalls++;
            throw new InvalidOperationException("Restart audit recovery must not dispatch remote work.");
        }

        public Task<AgentJobRecord> ReconcileReservedAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            ReconcileReservedCalls++;
            throw new InvalidOperationException("Restart audit recovery must not reconcile provider reservation state.");
        }

        public Task<AgentJobRecord> ReconcileDispatchedAsync(
            Guid jobId,
            DateTimeOffset? now = null,
            CancellationToken cancellationToken = default)
        {
            ReconcileDispatchedCalls++;
            throw new InvalidOperationException("Restart audit recovery must not poll Nebius.");
        }

        public Task<AgentJobRecord> RequestCancellationAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            RequestCancellationCalls++;
            throw new InvalidOperationException("Restart audit recovery must not request provider cancellation.");
        }

        public Task<AgentJobRecord> ReconcileCancellationAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            ReconcileCancellationCalls++;
            throw new InvalidOperationException("Restart audit recovery must not poll cancellation state.");
        }
    }

    private sealed class TrackingResultTransport : IProtectedResearchResultTransport
    {
        public int DeleteCalls { get; private set; }
        public string? LastDeletedOpaqueId { get; private set; }

        public Task PutAsync(
            ProtectedResearchResultEnvelope envelope,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ProtectedResearchResultEnvelope?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Restart audit recovery must not replay remote-result retrieval.");

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            LastDeletedOpaqueId = opaqueWorkItemId;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
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
}