using System.Net;
using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchDispatchBindingLifecycleRaceTests
{
    private const string EnvelopeSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task EnsurePublishedAsync_PostPublishRealCancellationPipeline_ConvergesWithoutDuplicateSideEffects()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var audit = new MemoryAuditTrail();
            var results = new EmptyResultTransport();
            var ingestor = new RemoteResearchResultIngestor(store, results, clientRsa.ExportPkcs8PrivateKeyPem(), audit);
            var job = await CreateDispatchedJobAsync(store, ingestor, DateTimeOffset.UtcNow);
            var serverless = new RecordingServerlessClient();
            var reconciler = new NebiusResearchLifecycleReconciler(store, serverless, ingestor, audit);
            var transport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(transport, clientRsa.ExportPkcs8PrivateKeyPem());
            var observer = new LifecycleObserver(async (jobId, _) =>
            {
                var cancelled = await reconciler.RequestCancellationAsync(jobId);
                Assert.Equal(RemoteResearchProvenanceState.CancelRequested, cancelled.RemoteResearch!.State);
                Assert.Null(cancelled.PendingAuditEvent);
            });
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            var completed = await obligation.EnsurePublishedAsync(job.JobId);

            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, completed.RemoteResearch!.State);
            Assert.Null(completed.PendingResearchDispatchBinding);
            Assert.Null(completed.PendingAuditEvent);
            Assert.Equal(1, transport.PutCalls);
            Assert.Equal(1, serverless.CancelCalls);
            Assert.Equal("remote-lifecycle-42", serverless.LastCancelledId);
            Assert.Equal(1, observer.Calls);
            Assert.Single((await audit.ReadAllAsync()).Where(e => e.EventType == "research.remote_cancel_requested"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<AgentJobRecord> CreateDispatchedJobAsync(JsonAgentJobStore store, RemoteResearchResultIngestor ingestor, DateTimeOffset now)
    {
        var definition = new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission> { DataPermission.NetworkAccess },
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true,
            MaxAttempts: 3);
        var job = new AgentJobRecord(
            Guid.NewGuid(), definition, AgentJobState.Pending, JobExecutionLocation.Local, 0,
            new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now),
            null, null, now, now);
        await store.SaveAsync(job);
        const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
        await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
            job.JobId, job.Checkpoint!.Step, opaqueId, now, now.AddHours(1)));
        var attached = await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
            job.JobId, job.Checkpoint.Step, opaqueId, "remote-lifecycle-42", now));
        var envelopeBound = attached with { RemoteWorkItemEnvelopeSha256 = EnvelopeSha256, UpdatedAt = DateTimeOffset.UtcNow };
        Assert.True(await store.CompareExchangeAsync(attached, envelopeBound));
        return envelopeBound;
    }

    private sealed class LifecycleObserver : IResearchDispatchBindingCompletionObserver
    {
        private readonly Func<Guid, PendingResearchDispatchBinding, Task> _action;
        public int Calls { get; private set; }
        public LifecycleObserver(Func<Guid, PendingResearchDispatchBinding, Task> action) => _action = action;
        public async Task AfterPublishedAsync(Guid jobId, PendingResearchDispatchBinding obligation, CancellationToken cancellationToken)
        {
            Calls++;
            await _action(jobId, obligation);
        }
    }

    private sealed class RecordingServerlessClient : INebiusServerlessJobClient
    {
        public int CancelCalls { get; private set; }
        public string? LastCancelledId { get; private set; }
        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            LastCancelledId = remoteJobId;
            return Task.FromResult(new NebiusServerlessResponse(HttpStatusCode.OK, "{}"));
        }
        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Create must not be called.");
        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Get must not be called.");
        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("List must not be called.");
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

    private sealed class EmptyResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.FromResult<ProtectedResearchResultEnvelope?>(null);
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        private readonly List<AuditEvent> _events = new();
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (!_events.Any(existing => existing.Id == auditEvent.Id)) _events.Add(auditEvent);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AuditEvent>>(_events.ToArray());
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-lifecycle-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
