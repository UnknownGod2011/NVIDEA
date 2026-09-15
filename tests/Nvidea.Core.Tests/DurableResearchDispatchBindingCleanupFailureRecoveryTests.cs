using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchDispatchBindingCleanupFailureRecoveryTests
{
    private const string EnvelopeSha256 = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string OpaqueId = "cLeAnUpFaIlUrEpUbLiShEdBiNdInG8xK3mQ6v";
    private const string RemoteJobId = "remote-cleanup-failure-43";

    [Fact]
    public async Task PublishedBinding_ResultAuditSettlesButCleanupFails_RestartRetriesCleanupWithoutDuplicateAuditOrPublication()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var now = DateTimeOffset.UtcNow;
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new FailFirstDeleteResultTransport();
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(store, results, clientRsa.ExportPkcs8PrivateKeyPem(), audit);

            var job = await CreateDispatchedJobAsync(store, ingestor, now);
            var remote = new RemoteResearchStageResult(
                job.JobId,
                job.Checkpoint!.Step,
                OpaqueId,
                RemoteJobId,
                new JobStepResult(false, ResearchJobHandler.EvidenceStep, "{\"cleanupRestart\":\"verified\"}"),
                now.AddMinutes(1),
                now.AddHours(1));
            await results.PutAsync(ResearchResultProtector.Protect(remote, clientRsa.ExportSubjectPublicKeyInfoPem()));

            var bindingTransport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(bindingTransport, clientRsa.ExportPkcs8PrivateKeyPem());
            var observer = new IngestingObserver(ingestor, now.AddMinutes(2));
            var firstCoordinator = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            // Result state and audit are durable, but the first protected-result deletion fails.
            // The exception prevents binding completion from clearing its independent obligation.
            await Assert.ThrowsAsync<InvalidOperationException>(() => firstCoordinator.EnsurePublishedAsync(job.JobId));

            var interrupted = await store.GetAsync(job.JobId);
            Assert.NotNull(interrupted);
            Assert.Equal(AgentJobState.Pending, interrupted!.State);
            Assert.Equal(JobExecutionLocation.Local, interrupted.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, interrupted.RemoteResearch!.State);
            Assert.NotNull(interrupted.PendingResearchDispatchBinding);
            Assert.Null(interrupted.PendingAuditEvent);
            Assert.NotNull(interrupted.PendingProtectedPayloadCleanup);
            Assert.Equal(1, bindingTransport.PutCalls);
            Assert.Equal(1, results.DeleteAttempts);
            Assert.Contains(results.Items.Keys, key => key == OpaqueId);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));

            // Simulate restart. Cleanup recovery must not replay result ingestion or audit append.
            var restartedIngestor = new RemoteResearchResultIngestor(
                store, results, clientRsa.ExportPkcs8PrivateKeyPem(), audit);
            var afterCleanupRecovery = await restartedIngestor.RecoverPendingCleanupAsync(job.JobId);

            Assert.Null(afterCleanupRecovery.PendingAuditEvent);
            Assert.Null(afterCleanupRecovery.PendingProtectedPayloadCleanup);
            Assert.NotNull(afterCleanupRecovery.PendingResearchDispatchBinding);
            Assert.Equal(2, results.DeleteAttempts);
            Assert.DoesNotContain(results.Items.Keys, key => key == OpaqueId);
            Assert.Single(audit.Events.Where(e => e.EventType == "research.remote_result_applied"));
            Assert.Equal(ResearchJobHandler.EvidenceStep, afterCleanupRecovery.Checkpoint!.Step);

            // Binding recovery owns only its already-published V2 obligation. Idempotent replay
            // observes the exact existing binding and clears local bookkeeping without another put.
            var restartedPublisher = new ResearchDispatchBindingPublisher(
                bindingTransport, clientRsa.ExportPkcs8PrivateKeyPem());
            var restartedCoordinator = new DurableResearchDispatchBindingObligation(store, restartedPublisher);
            var converged = await restartedCoordinator.EnsurePublishedAsync(job.JobId);

            Assert.Null(converged.PendingResearchDispatchBinding);
            Assert.Null(converged.PendingAuditEvent);
            Assert.Null(converged.PendingProtectedPayloadCleanup);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, converged.RemoteResearch!.State);
            Assert.Equal(ResearchJobHandler.EvidenceStep, converged.Checkpoint!.Step);
            Assert.Equal(1, bindingTransport.PutCalls);
            Assert.Equal(2, results.DeleteAttempts);
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
            UpdatedAt = DateTimeOffset.UtcNow
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

    private sealed class FailFirstDeleteResultTransport : IProtectedResearchResultTransport
    {
        private bool _failedDelete;
        public Dictionary<string, ProtectedResearchResultEnvelope> Items { get; } = new(StringComparer.Ordinal);
        public int DeleteAttempts { get; private set; }

        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default)
        {
            Items[envelope.OpaqueWorkItemId] = envelope;
            return Task.CompletedTask;
        }

        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            Items.TryGetValue(opaqueWorkItemId, out var value);
            return Task.FromResult(value);
        }

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            DeleteAttempts++;
            if (!_failedDelete)
            {
                _failedDelete = true;
                throw new IOException("Injected protected-result cleanup failure after durable result audit.");
            }
            Items.Remove(opaqueWorkItemId);
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-cleanup-failure-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
