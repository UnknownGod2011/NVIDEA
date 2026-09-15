using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchDispatchBindingCleanupCasCompositionTests
{
    private const string EnvelopeSha256 = "abababababababababababababababababababababababababababababababab";
    private const string OpaqueId = "bInDiNgClEaNuPcAsCoMp7qR4nT9x2mV6";
    private const string RemoteJobId = "remote-binding-cleanup-cas-71";

    [Fact]
    public async Task PublishedBinding_BothDeletesSucceed_ConcurrentTerminalProgressWinsCleanupCas_AllObligationsConvergeExactlyOnce()
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
                new JobStepResult(false, ResearchJobHandler.EvidenceStep, "{\"bindingCleanupCas\":\"verified\"}"),
                now.AddMinutes(1),
                now.AddHours(1));
            await ((IProtectedResearchResultTransport)transport).PutAsync(
                ResearchResultProtector.Protect(remote, clientRsa.ExportSubjectPublicKeyInfoPem()));

            var bindingTransport = new MemoryBindingTransport();
            var coordinator = new DurableResearchDispatchBindingObligation(
                store,
                new ResearchDispatchBindingPublisher(bindingTransport, clientRsa.ExportPkcs8PrivateKeyPem()),
                new IngestingObserver(ingestor, now.AddMinutes(2)));

            // Publication happens first. During the completion observer, real result ingestion applies
            // and audits the result, deletes both encrypted artifacts, then the work-item transport
            // commits legitimate terminal progress. Cleanup completion therefore starts from a stale
            // record and must converge through its bounded reload/revalidation path while the binding
            // obligation remains independently durable.
            var observed = await coordinator.EnsurePublishedAsync(job.JobId);

            Assert.Equal(AgentJobState.Completed, observed.State);
            Assert.Equal(JobExecutionLocation.Local, observed.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, observed.RemoteResearch!.State);
            Assert.Equal(ResearchJobHandler.EvidenceStep, observed.Checkpoint!.Step);
            Assert.Null(observed.PendingAuditEvent);
            Assert.Null(observed.PendingProtectedPayloadCleanup);
            Assert.Null(observed.PendingResearchDispatchBinding);
            Assert.False(transport.ResultExists);
            Assert.False(transport.WorkItemExists);
            Assert.Equal(1, transport.ResultDeleteCalls);
            Assert.Equal(1, transport.WorkItemDeleteCalls);
            Assert.Equal(1, transport.ConcurrentTransitions);
            Assert.Equal(1, bindingTransport.PutCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            // Reconstruct both recovery actors. With every durable debt settled, neither may replay
            // transport deletion, result/audit application, or the already-published signed binding.
            var restartedIngestor = new RemoteResearchResultIngestor(
                store, transport, clientRsa.ExportPkcs8PrivateKeyPem(), audit, transport);
            var afterCleanupRecovery = await restartedIngestor.RecoverPendingCleanupAsync(job.JobId);
            var restartedCoordinator = new DurableResearchDispatchBindingObligation(
                store,
                new ResearchDispatchBindingPublisher(bindingTransport, clientRsa.ExportPkcs8PrivateKeyPem()));
            var converged = await restartedCoordinator.EnsurePublishedAsync(job.JobId);

            Assert.Equal(AgentJobState.Completed, afterCleanupRecovery.State);
            Assert.Equal(AgentJobState.Completed, converged.State);
            Assert.Null(converged.PendingAuditEvent);
            Assert.Null(converged.PendingProtectedPayloadCleanup);
            Assert.Null(converged.PendingResearchDispatchBinding);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, converged.RemoteResearch!.State);
            Assert.Equal(ResearchJobHandler.EvidenceStep, converged.Checkpoint!.Step);
            Assert.Equal(1, transport.ResultDeleteCalls);
            Assert.Equal(1, transport.WorkItemDeleteCalls);
            Assert.Equal(1, transport.ConcurrentTransitions);
            Assert.Equal(1, bindingTransport.PutCalls);
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
        var attached = await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
            original.JobId, original.Checkpoint.Step, OpaqueId, RemoteJobId, now));
        var envelopeBound = attached with
        {
            RemoteWorkItemEnvelopeSha256 = EnvelopeSha256,
            UpdatedAt = now.AddSeconds(1)
        };
        Assert.True(await store.CompareExchangeAsync(attached, envelopeBound));
        return envelopeBound;
    }

    private sealed class IngestingObserver : IResearchDispatchBindingCompletionObserver
    {
        private readonly RemoteResearchResultIngestor _ingestor;
        private readonly DateTimeOffset _now;

        public IngestingObserver(RemoteResearchResultIngestor ingestor, DateTimeOffset now)
        {
            _ingestor = ingestor;
            _now = now;
        }

        public Task AfterPublishedAsync(Guid jobId, PendingResearchDispatchBinding obligation, CancellationToken cancellationToken) =>
            _ingestor.IngestAsync(jobId, _now, cancellationToken);
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
            if (_transitioned)
                return;
            _transitioned = true;

            var current = await _store.GetAsync(TargetJobId, cancellationToken)
                ?? throw new InvalidOperationException("Injected concurrent job disappeared.");
            if (current.State != AgentJobState.Pending
                || current.RemoteResearch?.State != RemoteResearchProvenanceState.ResultApplied
                || current.PendingProtectedPayloadCleanup is null
                || current.PendingResearchDispatchBinding is null
                || current.PendingAuditEvent is not null)
            {
                throw new InvalidOperationException("Published-binding cleanup race reached an unexpected durable state.");
            }

            var progressed = current with
            {
                State = AgentJobState.Completed,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            if (!await _store.CompareExchangeAsync(current, progressed, cancellationToken))
                throw new InvalidOperationException("Injected terminal progress lost its deterministic CAS.");
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

    private sealed class MemoryBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        private readonly Dictionary<string, ProtectedResearchDispatchBinding> _items = new(StringComparer.Ordinal);
        public int PutCalls { get; private set; }

        public Task PutAsync(ProtectedResearchDispatchBinding binding, CancellationToken cancellationToken = default)
        {
            if (_items.TryGetValue(binding.OpaqueWorkItemId, out var existing))
            {
                if (existing == binding)
                    return Task.CompletedTask;
                throw new InvalidOperationException("binding already exists");
            }
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

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-cleanup-cas-composition-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
