using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchDispatchBindingCompletionRaceTests
{
    private const string EnvelopeSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task EnsurePublishedAsync_PostPublishCancelRequestedRace_ClearsExactObligation()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var job = await CreateDispatchedJobAsync(store, clientRsa, DateTimeOffset.UtcNow);
            var transport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(transport, clientRsa.ExportPkcs8PrivateKeyPem());
            var observer = new MutatingObserver(async (jobId, _) =>
            {
                var latest = (await store.GetAsync(jobId))!;
                var mutated = latest with
                {
                    RemoteResearch = latest.RemoteResearch! with { State = RemoteResearchProvenanceState.CancelRequested },
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                Assert.True(await store.CompareExchangeAsync(latest, mutated));
            });
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            var completed = await obligation.EnsurePublishedAsync(job.JobId);

            Assert.Equal(RemoteResearchProvenanceState.CancelRequested, completed.RemoteResearch!.State);
            Assert.Null(completed.PendingResearchDispatchBinding);
            Assert.Equal(1, transport.PutCalls);
            Assert.Equal(1, observer.Calls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(AgentJobState.Pending, RemoteResearchProvenanceState.ResultApplied)]
    [InlineData(AgentJobState.Completed, RemoteResearchProvenanceState.ResultApplied)]
    [InlineData(AgentJobState.Cancelled, RemoteResearchProvenanceState.Cancelled)]
    [InlineData(AgentJobState.Failed, RemoteResearchProvenanceState.RemoteFailed)]
    [InlineData(AgentJobState.Failed, RemoteResearchProvenanceState.Expired)]
    public async Task EnsurePublishedAsync_PostPublishLegitimateLocalLifecycleTransition_ClearsExactObligation(
        AgentJobState terminalState,
        RemoteResearchProvenanceState provenanceState)
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var job = await CreateDispatchedJobAsync(store, clientRsa, DateTimeOffset.UtcNow);
            var transport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(transport, clientRsa.ExportPkcs8PrivateKeyPem());
            PendingResearchDispatchBinding? publishedObligation = null;
            var observer = new MutatingObserver(async (jobId, pending) =>
            {
                publishedObligation = pending;
                var latest = (await store.GetAsync(jobId))!;
                var now = DateTimeOffset.UtcNow;
                var provenance = latest.RemoteResearch! with
                {
                    State = provenanceState,
                    ResultAppliedAt = provenanceState == RemoteResearchProvenanceState.ResultApplied ? now : null,
                    TerminalAt = provenanceState is RemoteResearchProvenanceState.Cancelled or RemoteResearchProvenanceState.RemoteFailed or RemoteResearchProvenanceState.Expired ? now : null
                };
                var mutated = latest with
                {
                    State = terminalState,
                    ExecutionLocation = JobExecutionLocation.Local,
                    RemoteResearch = provenance,
                    UpdatedAt = now
                };
                Assert.True(await store.CompareExchangeAsync(latest, mutated));
            });
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            var completed = await obligation.EnsurePublishedAsync(job.JobId);

            Assert.NotNull(publishedObligation);
            Assert.Equal("remote-race-42", publishedObligation!.RemoteJobId);
            Assert.Equal(EnvelopeSha256, publishedObligation.EnvelopeSha256);
            Assert.Equal(terminalState, completed.State);
            Assert.Equal(JobExecutionLocation.Local, completed.ExecutionLocation);
            Assert.Equal(provenanceState, completed.RemoteResearch!.State);
            Assert.Null(completed.PendingResearchDispatchBinding);
            Assert.Equal(1, transport.PutCalls);
            Assert.Equal(1, observer.Calls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(AgentJobState.Completed, RemoteResearchProvenanceState.Cancelled)]
    [InlineData(AgentJobState.Cancelled, RemoteResearchProvenanceState.ResultApplied)]
    [InlineData(AgentJobState.Failed, RemoteResearchProvenanceState.ResultApplied)]
    [InlineData(AgentJobState.Running, RemoteResearchProvenanceState.RemoteFailed)]
    public async Task EnsurePublishedAsync_PostPublishInconsistentLifecyclePair_FailsClosedWithObligationPending(
        AgentJobState state,
        RemoteResearchProvenanceState provenanceState)
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var job = await CreateDispatchedJobAsync(store, clientRsa, DateTimeOffset.UtcNow);
            var transport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(transport, clientRsa.ExportPkcs8PrivateKeyPem());
            var observer = new MutatingObserver(async (jobId, _) =>
            {
                var latest = (await store.GetAsync(jobId))!;
                var mutated = latest with
                {
                    State = state,
                    ExecutionLocation = JobExecutionLocation.Local,
                    RemoteResearch = latest.RemoteResearch! with { State = provenanceState },
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                Assert.True(await store.CompareExchangeAsync(latest, mutated));
            });
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            await Assert.ThrowsAsync<InvalidOperationException>(() => obligation.EnsurePublishedAsync(job.JobId));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted?.PendingResearchDispatchBinding);
            Assert.Equal("remote-race-42", persisted!.PendingResearchDispatchBinding!.RemoteJobId);
            Assert.Equal(EnvelopeSha256, persisted.PendingResearchDispatchBinding.EnvelopeSha256);
            Assert.Equal(1, transport.PutCalls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task EnsurePublishedAsync_PostPublishRemoteIdSubstitution_FailsClosedWithObligationPending()
    {
        var root = CreateTempDirectory();
        try
        {
            using var clientRsa = RSA.Create(2048);
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), new PassThroughProtector());
            var job = await CreateDispatchedJobAsync(store, clientRsa, DateTimeOffset.UtcNow);
            var transport = new MemoryBindingTransport();
            var publisher = new ResearchDispatchBindingPublisher(transport, clientRsa.ExportPkcs8PrivateKeyPem());
            var observer = new MutatingObserver(async (jobId, _) =>
            {
                var latest = (await store.GetAsync(jobId))!;
                var mutated = latest with
                {
                    RemoteResearch = latest.RemoteResearch! with { RemoteJobId = "substituted-after-publication" },
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                Assert.True(await store.CompareExchangeAsync(latest, mutated));
            });
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            await Assert.ThrowsAsync<InvalidOperationException>(() => obligation.EnsurePublishedAsync(job.JobId));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.NotNull(persisted!.PendingResearchDispatchBinding);
            Assert.Equal("remote-race-42", persisted.PendingResearchDispatchBinding!.RemoteJobId);
            Assert.Equal("substituted-after-publication", persisted.RemoteResearch!.RemoteJobId);
            Assert.Equal(EnvelopeSha256, persisted.PendingResearchDispatchBinding.EnvelopeSha256);
            Assert.Equal(1, transport.PutCalls);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<AgentJobRecord> CreateDispatchedJobAsync(JsonAgentJobStore store, RSA clientRsa, DateTimeOffset now)
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
        var ingestor = new RemoteResearchResultIngestor(
            store,
            new EmptyResultTransport(),
            clientRsa.ExportPkcs8PrivateKeyPem(),
            new MemoryAuditTrail());
        const string opaqueId = "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt";
        await ingestor.ReserveDispatchAsync(new RemoteResearchDispatchReservation(
            job.JobId, job.Checkpoint!.Step, opaqueId, now, now.AddHours(1)));
        var attached = await ingestor.AttachDispatchAsync(new NebiusResearchDispatchReceipt(
            job.JobId, job.Checkpoint.Step, opaqueId, "remote-race-42", now));
        var envelopeBound = attached with { RemoteWorkItemEnvelopeSha256 = EnvelopeSha256, UpdatedAt = DateTimeOffset.UtcNow };
        Assert.True(await store.CompareExchangeAsync(attached, envelopeBound));
        return envelopeBound;
    }

    private sealed class MutatingObserver : IResearchDispatchBindingCompletionObserver
    {
        private readonly Func<Guid, PendingResearchDispatchBinding, Task> _mutation;
        public int Calls { get; private set; }

        public MutatingObserver(Func<Guid, PendingResearchDispatchBinding, Task> mutation) => _mutation = mutation;

        public async Task AfterPublishedAsync(Guid jobId, PendingResearchDispatchBinding obligation, CancellationToken cancellationToken)
        {
            Calls++;
            await _mutation(jobId, obligation);
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

    private sealed class EmptyResultTransport : IProtectedResearchResultTransport
    {
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchResultEnvelope?>(null);
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        private readonly List<AuditEvent> _events = new();
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (!_events.Any(existing => existing.EventId == auditEvent.EventId)) _events.Add(auditEvent);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(_events.ToArray());
    }

    private sealed class PassThroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-completion-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
