using System.Security.Cryptography;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchWorkItemEnvelopeCommitmentTests
{
    [Fact]
    public void ComputeSha256_IsDeterministic_AndCommitsToCiphertext()
    {
        var envelope = CreateEnvelope("ciphertext-a");

        var first = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope);
        var second = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope);
        var substituted = ResearchWorkItemEnvelopeCommitment.ComputeSha256(
            envelope with { Ciphertext = Convert.ToBase64String("ciphertext-b"u8.ToArray()) });

        Assert.Equal(64, first.Length);
        Assert.Equal(first, second);
        Assert.NotEqual(first, substituted);
    }

    [Fact]
    public void VerifyEnvelopeBound_RejectsSubstitutedEnvelopeWithSameOpaqueId()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var envelope = CreateEnvelope("original-ciphertext");
        var digest = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope);
        var now = DateTimeOffset.UtcNow;
        var binding = ResearchDispatchBindingProtector.SignEnvelopeBound(
            envelope.OpaqueWorkItemId,
            "remote-job-123",
            digest,
            now,
            now.AddMinutes(10),
            privateKey);

        var verified = ResearchDispatchBindingProtector.VerifyEnvelopeBound(
            binding,
            envelope,
            publicKey,
            now.AddSeconds(1));
        Assert.Equal(digest, verified.WorkItemEnvelopeSha256);

        var substituted = envelope with
        {
            Ciphertext = Convert.ToBase64String("attacker-valid-ciphertext"u8.ToArray())
        };
        Assert.Throws<CryptographicException>(() =>
            ResearchDispatchBindingProtector.VerifyEnvelopeBound(
                binding,
                substituted,
                publicKey,
                now.AddSeconds(1)));
    }

    [Fact]
    public async Task Waiter_RejectsLegacyBindingWhenExactEnvelopeAuthorityIsRequired()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var envelope = CreateEnvelope("ciphertext");
        var now = DateTimeOffset.UtcNow;
        var legacy = ResearchDispatchBindingProtector.Sign(
            envelope.OpaqueWorkItemId,
            "remote-job-legacy",
            now,
            now.AddMinutes(10),
            privateKey);
        var transport = new SingleBindingTransport(legacy);
        var waiter = new ResearchDispatchBindingWaiter(
            transport,
            publicKey,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<CryptographicException>(() =>
            waiter.WaitAsync(envelope.OpaqueWorkItemId, envelope));
        Assert.Equal(1, transport.ReadCount);
    }

    [Fact]
    public async Task DurableCommitment_IsImmutableAcrossEnvelopeSubstitution()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nvidea-envelope-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var envelope = CreateEnvelope("ciphertext-a");
            var now = DateTimeOffset.UtcNow;
            var definition = new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true);
            var checkpoint = new AgentJobCheckpoint("research.plan", "{}", now);
            var provenance = new RemoteResearchProvenance(
                ProtocolVersion: ResearchWorkItemProtector.ProtocolVersion,
                OpaqueWorkItemId: envelope.OpaqueWorkItemId,
                RemoteJobId: null,
                InputCheckpointStep: checkpoint.Step,
                InputCheckpointSavedAt: checkpoint.SavedAt,
                DispatchedAt: now,
                State: RemoteResearchProvenanceState.DispatchReserved,
                WorkItemExpiresAt: envelope.ExpiresAt);
            var record = new AgentJobRecord(
                JobId: Guid.NewGuid(),
                Definition: definition,
                State: AgentJobState.Running,
                ExecutionLocation: JobExecutionLocation.Local,
                Attempt: 0,
                Checkpoint: checkpoint,
                ApprovalScope: null,
                LastError: null,
                CreatedAt: now,
                UpdatedAt: now,
                RemoteResearch: provenance);
            await store.SaveAsync(record);

            var durable = new DurableResearchEnvelopeCommitment(store);
            var committed = await durable.AttachAsync(record.JobId, envelope);
            Assert.Equal(
                ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope),
                committed.RemoteWorkItemEnvelopeSha256);

            var substituted = envelope with
            {
                Ciphertext = Convert.ToBase64String("ciphertext-b"u8.ToArray())
            };
            await Assert.ThrowsAsync<CryptographicException>(() =>
                durable.AttachAsync(record.JobId, substituted));

            var reloaded = await store.GetAsync(record.JobId);
            Assert.Equal(committed.RemoteWorkItemEnvelopeSha256, reloaded!.RemoteWorkItemEnvelopeSha256);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".tmp"))
                File.Delete(path + ".tmp");
        }
    }

    [Fact]
    public async Task Publisher_CreateOnceRejectsDifferentEnvelopeCommitment()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var envelopeA = CreateEnvelope("ciphertext-a");
        var envelopeB = envelopeA with
        {
            Ciphertext = Convert.ToBase64String("ciphertext-b"u8.ToArray())
        };
        var digestA = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelopeA);
        var digestB = ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelopeB);
        var transport = new SingleBindingTransport();
        var publisher = new ResearchDispatchBindingPublisher(transport, privateKey);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        var first = await publisher.PublishEnvelopeBoundAsync(
            envelopeA.OpaqueWorkItemId,
            "remote-job-123",
            digestA,
            expiresAt);
        var repeated = await publisher.PublishEnvelopeBoundAsync(
            envelopeA.OpaqueWorkItemId,
            "remote-job-123",
            digestA,
            expiresAt);

        Assert.Equal(first.Signature, repeated.Signature);
        Assert.Equal(1, transport.WriteCount);
        await Assert.ThrowsAsync<CryptographicException>(() =>
            publisher.PublishEnvelopeBoundAsync(
                envelopeA.OpaqueWorkItemId,
                "remote-job-123",
                digestB,
                expiresAt));
    }

    private static ProtectedResearchWorkItemEnvelope CreateEnvelope(string ciphertext)
    {
        var now = DateTimeOffset.UtcNow;
        return new ProtectedResearchWorkItemEnvelope(
            ResearchWorkItemProtector.ProtocolVersion,
            "abcdefghijklmnopqrstuvwx12345678",
            Convert.ToBase64String("wrapped-key"u8.ToArray()),
            Convert.ToBase64String("nonce-123456"u8.ToArray()),
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ciphertext)),
            Convert.ToBase64String("auth-tag-1234567"u8.ToArray()),
            now,
            now.AddMinutes(30));
    }

    private sealed class SingleBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        private ProtectedResearchDispatchBinding? _binding;

        public SingleBindingTransport(ProtectedResearchDispatchBinding? binding = null)
        {
            _binding = binding;
        }

        public int ReadCount { get; private set; }
        public int WriteCount { get; private set; }

        public Task PutAsync(
            ProtectedResearchDispatchBinding binding,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_binding is not null)
                throw new InvalidOperationException("Binding already exists.");
            _binding = binding;
            WriteCount++;
            return Task.CompletedTask;
        }

        public Task<ProtectedResearchDispatchBinding?> GetAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            return Task.FromResult(_binding);
        }

        public Task DeleteAsync(
            string opaqueWorkItemId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _binding = null;
            return Task.CompletedTask;
        }
    }
}
