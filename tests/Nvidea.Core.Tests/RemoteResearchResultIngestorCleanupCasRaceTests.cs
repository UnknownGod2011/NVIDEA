using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchResultIngestorCleanupCasRaceTests
{
    private const string OpaqueId = "cLeAnUpCaSrAcE7qR4nT9x2mV6kP8s";
    private const string RemoteJobId = "remote-cleanup-cas-race-61";

    [Fact]
    public async Task BothDeletesSucceed_ConcurrentTerminalProgressWinsFirstCleanupCas_IngestorConvergesWithoutReplay()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var now = DateTimeOffset.UtcNow;
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var transport = new ConcurrentProgressTransport(store);
            var ingestor = new RemoteResearchResultIngestor(
                store, transport, clientRsa.ExportPkcs8PrivateKeyPem(), audit, transport);

            var job = await CreateDispatchedJobAsync(store, ingestor, now);
            transport.TargetJobId = job.JobId;
            await transport.PutWorkItemAsync();

            var remote = new RemoteResearchStageResult(
                job.JobId,
                job.Checkpoint!.Step,
                OpaqueId,
                RemoteJobId,
                new JobStepResult(false, ResearchJobHandler.EvidenceStep, "{\"cleanupCas\":\"verified\"}"),
                now.AddMinutes(1),
                now.AddHours(1));
            await ((IProtectedResearchResultTransport)transport).PutAsync(
                ResearchResultProtector.Protect(remote, clientRsa.ExportSubjectPublicKeyInfoPem()));

            var applied = await ingestor.IngestAsync(job.JobId, now.AddMinutes(2));

            Assert.Equal(AgentJobState.Completed, applied.State);
            Assert.Equal(JobExecutionLocation.Local, applied.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, applied.RemoteResearch!.State);
            Assert.Equal(ResearchJobHandler.EvidenceStep, applied.Checkpoint!.Step);
            Assert.Null(applied.PendingAuditEvent);
            Assert.Null(applied.PendingProtectedPayloadCleanup);
            Assert.False(transport.ResultExists);
            Assert.False(transport.WorkItemExists);
            Assert.Equal(1, transport.ResultDeleteCalls);
            Assert.Equal(1, transport.WorkItemDeleteCalls);
            Assert.Equal(1, transport.ConcurrentTransitions);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            // A later recovery pass has no durable cleanup debt, so it must not replay transport
            // deletion, result application, or audit publication after the bounded CAS retry converged.
            var recovered = await ingestor.RecoverPendingCleanupAsync(job.JobId);
            Assert.Equal(AgentJobState.Completed, recovered.State);
            Assert.Null(recovered.PendingProtectedPayloadCleanup);
            Assert.Equal(1, transport.ResultDeleteCalls);
            Assert.Equal(1, transport.WorkItemDeleteCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<AgentJobRecord> CreateDispatchedJobAsync(
        JsonAgentJobStore store,
        RemoteResearchResultIngestor ingestor,
        DateTimeOffset now)
    {
        var definition = new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true,
            MaxAttempts: 3);
        var original = new AgentJobRecord(
            Guid.NewGuid(), definition, AgentJobState.Pending, JobExecutionLocation.Local, 0,
            new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now),
            null, null, now, now);
        await store.SaveAsync(original);
        await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
            original.JobId, original.Checkpoint!.Step, OpaqueId, now, now.AddHours(1)));
        return await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
            original.JobId, original.Checkpoint.Step, OpaqueId, RemoteJobId, now));
    }

    private sealed class ConcurrentProgressTransport : IProtectedResearchResultTransport, IProtectedResearchWorkItemTransport
    {
        private readonly JsonAgentJobStore _store;
        private ProtectedResearchResultEnvelope? _result;
        private bool _transitioned;

        public ConcurrentProgressTransport(JsonAgentJobStore store) => _store = store;

        public Guid TargetJobId { get; set; }
        public bool ResultExists => _result is not null;
        public bool WorkItemExists { get; private set; }
        public int ResultDeleteCalls { get; private set; }
        public int WorkItemDeleteCalls { get; private set; }
        public int ConcurrentTransitions { get; private set; }

        public Task PutWorkItemAsync()
        {
            WorkItemExists = true;
            return Task.CompletedTask;
        }

        Task IProtectedResearchResultTransport.PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken)
        {
            _result = envelope;
            return Task.CompletedTask;
        }

        Task<ProtectedResearchResultEnvelope?> IProtectedResearchResultTransport.GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
            Task.FromResult(_result?.OpaqueWorkItemId == opaqueWorkItemId ? _result : null);

        Task IProtectedResearchResultTransport.DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken)
        {
            ResultDeleteCalls++;
            if (_result?.OpaqueWorkItemId == opaqueWorkItemId)
                _result = null;
            return Task.CompletedTask;
        }

        Task IProtectedResearchWorkItemTransport.PutAsync(ProtectedResearchWorkItemEnvelope envelope, CancellationToken cancellationToken)
        {
            WorkItemExists = true;
            return Task.CompletedTask;
        }

        Task<ProtectedResearchWorkItemEnvelope?> IProtectedResearchWorkItemTransport.GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken) =>
            Task.FromResult<ProtectedResearchWorkItemEnvelope?>(null);

        async Task IProtectedResearchWorkItemTransport.DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken)
        {
            WorkItemDeleteCalls++;
            WorkItemExists = false;

            // This executes after both external deletions but before cleanup-marker ClearAsync. It
            // creates a real stale-current CAS without a test hook in production code.
            if (_transitioned)
                return;
            _transitioned = true;

            var current = await _store.GetAsync(TargetJobId, cancellationToken)
                ?? throw new InvalidOperationException("Injected concurrent job disappeared.");
            if (current.State != AgentJobState.Pending
                || current.RemoteResearch?.State != RemoteResearchProvenanceState.ResultApplied
                || current.PendingProtectedPayloadCleanup is null
                || current.PendingAuditEvent is not null)
            {
                throw new InvalidOperationException("Injected concurrency boundary was reached in an unexpected durable state.");
            }

            var progressed = current with
            {
                State = AgentJobState.Completed,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            if (!await _store.CompareExchangeAsync(current, progressed, cancellationToken))
                throw new InvalidOperationException("Injected concurrent terminal progress lost its deterministic CAS.");
            ConcurrentTransitions++;
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (!Events.Any(existing => existing.EventId == auditEvent.EventId))
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-ingestor-cleanup-cas-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
