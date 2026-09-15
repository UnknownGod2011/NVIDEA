using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchDispatchBindingAuthorityRaceTests
{
    private const string EnvelopeSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    public static IEnumerable<object[]> AuthorityMutations()
    {
        yield return new object[] { "opaque-id", (Func<AgentJobRecord, AgentJobRecord>)(job => job with { RemoteResearch = job.RemoteResearch! with { OpaqueWorkItemId = "substituted-opaque-id-after-publication" } }) };
        yield return new object[] { "envelope-digest", (Func<AgentJobRecord, AgentJobRecord>)(job => job with { RemoteWorkItemEnvelopeSha256 = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb" }) };
        yield return new object[] { "expiry", (Func<AgentJobRecord, AgentJobRecord>)(job => job with { RemoteResearch = job.RemoteResearch! with { ExpiresAt = job.RemoteResearch.ExpiresAt.AddMinutes(5) } }) };
    }

    [Theory]
    [MemberData(nameof(AuthorityMutations))]
    public async Task EnsurePublishedAsync_PostPublishAuthoritySubstitution_FailsClosedAndPreservesExactObligation(
        string mutationName,
        Func<AgentJobRecord, AgentJobRecord> mutate)
    {
        _ = mutationName;
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
                var mutated = mutate(latest) with { UpdatedAt = DateTimeOffset.UtcNow };
                Assert.True(await store.CompareExchangeAsync(latest, mutated));
            });
            var obligation = new DurableResearchDispatchBindingObligation(store, publisher, observer);

            await Assert.ThrowsAsync<InvalidOperationException>(() => obligation.EnsurePublishedAsync(job.JobId));

            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.NotNull(persisted!.PendingResearchDispatchBinding);
            Assert.Equal("remote-authority-race-42", persisted.PendingResearchDispatchBinding!.RemoteJobId);
            Assert.Equal("mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt", persisted.PendingResearchDispatchBinding.OpaqueWorkItemId);
            Assert.Equal(EnvelopeSha256, persisted.PendingResearchDispatchBinding.EnvelopeSha256);
            Assert.Equal(job.RemoteResearch!.ExpiresAt, persisted.PendingResearchDispatchBinding.ExpiresAt);
            Assert.Equal(1, transport.PutCalls);
            Assert.Equal(1, observer.Calls);
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
            job.JobId, job.Checkpoint.Step, opaqueId, "remote-authority-race-42", now));
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-binding-authority-race-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
