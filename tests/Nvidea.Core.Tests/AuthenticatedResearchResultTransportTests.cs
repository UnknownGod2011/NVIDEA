using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class AuthenticatedResearchResultTransportTests
{
    [Fact]
    public async Task GetAsync_ReturnsOnlyEnvelopeSignedByPinnedWorker()
    {
        using var worker = RSA.Create(2048);
        var inner = new MemoryTransport();
        var unsigned = Envelope("job-a");
        var signed = unsigned with
        {
            WorkerSignature = RemoteResearchWorkerSignature.Sign(unsigned, worker.ExportPkcs8PrivateKeyPem())
        };
        await inner.PutAsync(signed);
        var transport = new AuthenticatedResearchResultTransport(inner, worker.ExportSubjectPublicKeyInfoPem());

        var result = await transport.GetAsync(signed.OpaqueWorkItemId);

        Assert.Equal(signed, result);
    }

    [Fact]
    public async Task GetAsync_RejectsMissingWorkerSignature()
    {
        using var worker = RSA.Create(2048);
        var inner = new MemoryTransport();
        var envelope = Envelope("job-a");
        await inner.PutAsync(envelope);
        var transport = new AuthenticatedResearchResultTransport(inner, worker.ExportSubjectPublicKeyInfoPem());

        await Assert.ThrowsAsync<CryptographicException>(() => transport.GetAsync(envelope.OpaqueWorkItemId));
    }

    [Fact]
    public async Task GetAsync_RejectsWrongPinnedWorker()
    {
        using var worker = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var inner = new MemoryTransport();
        var unsigned = Envelope("job-a");
        var signed = unsigned with
        {
            WorkerSignature = RemoteResearchWorkerSignature.Sign(unsigned, worker.ExportPkcs8PrivateKeyPem())
        };
        await inner.PutAsync(signed);
        var transport = new AuthenticatedResearchResultTransport(inner, other.ExportSubjectPublicKeyInfoPem());

        await Assert.ThrowsAsync<CryptographicException>(() => transport.GetAsync(signed.OpaqueWorkItemId));
    }

    [Fact]
    public async Task GetAsync_RejectsCiphertextMutationBeforeReturningEnvelope()
    {
        using var worker = RSA.Create(2048);
        var inner = new MemoryTransport();
        var unsigned = Envelope("job-a");
        var signature = RemoteResearchWorkerSignature.Sign(unsigned, worker.ExportPkcs8PrivateKeyPem());
        var tampered = unsigned with
        {
            Ciphertext = Convert.ToBase64String(new byte[] { 99, 98, 97 }),
            WorkerSignature = signature
        };
        await inner.PutAsync(tampered);
        var transport = new AuthenticatedResearchResultTransport(inner, worker.ExportSubjectPublicKeyInfoPem());

        await Assert.ThrowsAsync<CryptographicException>(() => transport.GetAsync(tampered.OpaqueWorkItemId));
    }

    [Fact]
    public async Task GetAsync_RejectsTransportWorkItemSubstitutionBeforeSignatureTrust()
    {
        using var worker = RSA.Create(2048);
        var substituted = Envelope("job-a") with { OpaqueWorkItemId = "different-work-item" };
        var inner = new SubstitutingTransport(substituted);
        var transport = new AuthenticatedResearchResultTransport(inner, worker.ExportSubjectPublicKeyInfoPem());

        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.GetAsync("expected-work-item"));
    }

    private static ProtectedResearchResultEnvelope Envelope(string remoteJobId)
    {
        var now = DateTimeOffset.Parse("2026-09-24T00:00:00Z");
        return new(
            ResearchResultProtector.ProtocolVersion,
            "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt",
            remoteJobId,
            Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            Convert.ToBase64String(new byte[] { 7, 8, 9 }),
            Convert.ToBase64String(new byte[] { 10, 11, 12 }),
            now,
            now.AddHours(1));
    }

    private sealed class MemoryTransport : IProtectedResearchResultTransport
    {
        private readonly Dictionary<string, ProtectedResearchResultEnvelope> _items = new(StringComparer.Ordinal);
        public Task PutAsync(ProtectedResearchResultEnvelope envelope, CancellationToken cancellationToken = default)
        {
            _items[envelope.OpaqueWorkItemId] = envelope;
            return Task.CompletedTask;
        }
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
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

    private sealed class SubstitutingTransport(ProtectedResearchResultEnvelope envelope) : IProtectedResearchResultTransport
    {
        public Task PutAsync(ProtectedResearchResultEnvelope value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ProtectedResearchResultEnvelope?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.FromResult<ProtectedResearchResultEnvelope?>(envelope);
        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
