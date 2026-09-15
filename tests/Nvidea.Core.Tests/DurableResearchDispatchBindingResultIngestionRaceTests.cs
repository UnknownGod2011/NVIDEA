using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchDispatchBindingResultIngestionRaceTests
{
    private const string EnvelopeSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string OpaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
    private const string RemoteJobId = "remote-result-race-42";

    [Theory]
    [InlineData(false, AgentJobState.Pending, ResearchJobHandler.EvidenceStep)]
    [InlineData(true, AgentJobState.Completed, ResearchJobHandler.CompletedStep)]
    public async Task EnsurePublishedAsync_RealResultIngestionWinsPostPublishRace_PreservesIndependentDurableWork(
        bool resultCompletesJob,
        AgentJobState expectedState,
        string outputStep)
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var now = DateTimeOffset.UtcNow;
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var results = new MemoryResultTransport();
            var audit = new MemoryAuditTrail();
            var ingestor = new RemoteResearchResultIngestor(
                store,
                results,
                clientRsa.ExportPkcs8PrivateKeyPem(),
                audit);

            var job = await CreateDispatchedJobAsync(store, ingestor, now);
            var remote = new RemoteResearchStageResult(
                job.JobId,
                job.Checkpoint!.Step,
                OpaqueId,
                RemoteJobId,
                new JobStepResult(
                    Completed: resultCompletesJob,
                    CheckpointStep: outputStep,
                    CheckpointPayload: "{\"race\":\"verified\"}"),
                now.AddMinutes(1),
                now.AddHours(1));
            await results.PutAsync(ResearchResultProtector.Protect(remote, clientRsa.ExportSubjectPublicKeyInfoPem()));

            var bindingTransport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(bindingTransport, clientRsa.ExportPkcs8PrivateKeyPem());
            var observer = new IngestingObserver(ingestor, now.AddMinutes(2));
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            var completed = await obligation.EnsurePublishedAsync(job.JobId);

            Assert.Equal(expectedState, completed.State);
            Assert.Equal(JobExecutionLocation.Local, completed.ExecutionLocation);
            Assert.Equal(RemoteResearchProvenanceState.ResultApplied, completed.RemoteResearch!.State);
            Assert.Equal(outputStep, completed.Checkpoint!.Step);
            Assert.Null(completed.PendingResearchDispatchBinding);
            Assert.Null(completed.PendingAuditEvent);
            Assert.Null(completed.PendingProtectedPayloadCleanup);
            Assert.Equal(1, bindingTransport.PutCalls);
            Assert.Equal(1, observer.Calls);
            Assert.Equal(1, results.DeleteCalls);
            Assert.DoesNotContain(results.Items.Keys, key => key == OpaqueId);
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
        public int Calls { get; private set; }

        public IngestingObserver(RemoteResearchResultIngestor ingestor, DateTimeOffset now)
        {
            _ingestor = ingestor;
            _now = now;
        }

        public async Task AfterPublishedAsync(Guid jobId, PendingResearchDispatchBinding obligation, CancellationToken cancellationToken)
        {
            Calls++;
            await _ingestor.IngestAsync(jobId, _now, cancellationToken);
        }
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

    private sealed class MemoryResultTransport : IProtectedResearchResultTransport
    {
        public Dictionary<string, ProtectedResearchResultEnvelope> Items { get; } = new(StringComparer.Ordinal);
        public int DeleteCalls { get; private set; }

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
            DeleteCalls++;
            Items.Remove(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (!Events.Any(existing => existing.EventId == auditEvent.EventId)) Events.Add(auditEvent);
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-result-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
