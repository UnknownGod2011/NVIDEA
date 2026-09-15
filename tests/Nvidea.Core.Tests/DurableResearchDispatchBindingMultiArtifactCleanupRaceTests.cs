using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchDispatchBindingMultiArtifactCleanupRaceTests
{
    private const string EnvelopeSha256 = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    private const string OpaqueId = "mUlTiArTiFaCtClEaNuPbInDiNg7qR4nT9x";
    private const string RemoteJobId = "remote-multi-cleanup-binding-51";

    [Fact]
    public async Task PublishedBinding_ResultDeleteSucceeds_WorkItemDeleteFails_RestartConvergesBothWithoutReplay() =>
        await RunRecoveryScenarioAsync(ambiguousWorkItemDelete: false);

    [Fact]
    public async Task PublishedBinding_WorkItemDeleteSucceedsButAckIsLost_RestartConvergesWithoutReplayOrRepublish() =>
        await RunRecoveryScenarioAsync(ambiguousWorkItemDelete: true);

    private static async Task RunRecoveryScenarioAsync(bool ambiguousWorkItemDelete)
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var now = DateTimeOffset.UtcNow;
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var transport = new PartialFailureTransport
            {
                FailNextWorkItemDelete = !ambiguousWorkItemDelete,
                LoseNextWorkItemDeleteAcknowledgement = ambiguousWorkItemDelete
            };
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store, transport, clientRsa.ExportPkcs8PrivateKeyPem(), audit, transport);

            var job = await CreateDispatchedJobAsync(store, ingestor, now);
            await transport.PutWorkItemAsync(OpaqueId);
            var remote = new RemoteResearchStageResult(
                job.JobId,
                job.Checkpoint!.Step,
                OpaqueId,
                RemoteJobId,
                new JobStepResult(false, ResearchJobHandler.EvidenceStep, "{\"multiCleanup\":\"verified\"}"),
                now.AddMinutes(1),
                now.AddHours(1));
            await ((IProtectedResearchResultTransport)transport).PutAsync(
                ResearchResultProtector.Protect(remote, clientRsa.ExportSubjectPublicKeyInfoPem()));

            var bindingTransport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(bindingTransport, clientRsa.ExportPkcs8PrivateKeyPem());
            var coordinator = new DurableResearchDispatchBindingObligation(
                store, publisher, new IngestingObserver(ingestor, now.AddMinutes(2)));

            await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.EnsurePublishedAsync(job.JobId));

            var interrupted = await store.GetAsync(job.JobId);
            Assert.NotNull(interrupted);
            Assert.Equal(AgentJobState.Pending, interrupted!.State);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, interrupted.RemoteResearch!.State);
            Assert.Null(interrupted.PendingAuditEvent);
            Assert.NotNull(interrupted.PendingProtectedPayloadCleanup);
            Assert.NotNull(interrupted.PendingResearchDispatchBinding);
            Assert.False(transport.ResultExists);
            Assert.Equal(!ambiguousWorkItemDelete, transport.WorkItemExists);
            Assert.Equal(1, transport.ResultDeleteCalls);
            Assert.Equal(1, transport.WorkItemDeleteCalls);
            Assert.Equal(1, bindingTransport.PutCalls);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            // Recovery deliberately retries both deletions. In the ambiguous case the work item was
            // already removed before the first call lost its acknowledgement; delete must therefore
            // be idempotent and the durable cleanup marker is the only safe source of truth.
            var restartedIngestor = new RemoteResearchResultIngestor(
                store, transport, clientRsa.ExportPkcs8PrivateKeyPem(), audit, transport);
            var recovered = await restartedIngestor.RecoverPendingCleanupAsync(job.JobId);

            Assert.Null(recovered.PendingProtectedPayloadCleanup);
            Assert.NotNull(recovered.PendingResearchDispatchBinding);
            Assert.Null(recovered.PendingAuditEvent);
            Assert.False(transport.ResultExists);
            Assert.False(transport.WorkItemExists);
            Assert.Equal(2, transport.ResultDeleteCalls);
            Assert.Equal(2, transport.WorkItemDeleteCalls);
            Assert.Equal(ResearchJobHandler.EvidenceStep, recovered.Checkpoint!.Step);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            var restartedCoordinator = new DurableResearchDispatchBindingObligation(
                store,
                new ResearchDispatchBindingPublisher(bindingTransport, clientRsa.ExportPkcs8PrivateKeyPem()));
            var converged = await restartedCoordinator.EnsurePublishedAsync(job.JobId);

            Assert.Null(converged.PendingResearchDispatchBinding);
            Assert.Null(converged.PendingProtectedPayloadCleanup);
            Assert.Null(converged.PendingAuditEvent);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, converged.RemoteResearch!.State);
            Assert.Equal(ResearchJobHandler.EvidenceStep, converged.Checkpoint!.Step);
            Assert.Equal(1, bindingTransport.PutCalls);
            Assert.Equal(2, transport.ResultDeleteCalls);
            Assert.Equal(2, transport.WorkItemDeleteCalls);
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

    private sealed class PartialFailureTransport : IProtectedResearchResultTransport, IProtectedResearchWorkItemTransport
    {
        private ProtectedResearchResultEnvelope? _result;
        public bool ResultExists => _result is not null;
        public bool WorkItemExists { get; private set; }
        public bool FailNextWorkItemDelete { get; set; }
        public bool LoseNextWorkItemDeleteAcknowledgement { get; set; }
        public int ResultDeleteCalls { get; private set; }
        public int WorkItemDeleteCalls { get; private set; }

        public Task PutWorkItemAsync(string opaqueId)
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

        Task IProtectedResearchWorkItemTransport.DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken)
        {
            WorkItemDeleteCalls++;
            if (FailNextWorkItemDelete)
            {
                FailNextWorkItemDelete = false;
                throw new IOException("Injected work-item cleanup failure before deletion.");
            }

            WorkItemExists = false;
            if (LoseNextWorkItemDeleteAcknowledgement)
            {
                LoseNextWorkItemDeleteAcknowledgement = false;
                throw new IOException("Injected lost acknowledgement after work-item deletion succeeded.");
            }
            return Task.CompletedTask;
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
                if (existing == binding) return Task.CompletedTask;
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-multi-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
