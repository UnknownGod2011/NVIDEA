using System.Security.Cryptography;
using System.Text.Json;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchWorkerSignatureTests
{
    [Fact]
    public void Signature_VerifiesOnlyWithPinnedWorkerIdentity()
    {
        using var worker = RSA.Create(2048); using var other = RSA.Create(2048); var envelope = Envelope("job-a");
        var signature = RemoteResearchWorkerSignature.Sign(envelope, worker.ExportPkcs8PrivateKeyPem());
        RemoteResearchWorkerSignature.Verify(envelope, signature, worker.ExportSubjectPublicKeyInfoPem());
        Assert.Throws<CryptographicException>(() => RemoteResearchWorkerSignature.Verify(envelope, signature, other.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Signature_RejectsForgedSignature()
    {
        using var worker = RSA.Create(2048); var envelope = Envelope("job-a"); var signature = Convert.FromBase64String(RemoteResearchWorkerSignature.Sign(envelope, worker.ExportPkcs8PrivateKeyPem())); signature[0] ^= 1;
        Assert.Throws<CryptographicException>(() => RemoteResearchWorkerSignature.Verify(envelope, Convert.ToBase64String(signature), worker.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Signature_RejectsCrossJobReplay()
    {
        using var worker = RSA.Create(2048); var envelope = Envelope("job-a"); var signature = RemoteResearchWorkerSignature.Sign(envelope, worker.ExportPkcs8PrivateKeyPem());
        Assert.Throws<CryptographicException>(() => RemoteResearchWorkerSignature.Verify(envelope with { RemoteJobId = "job-b" }, signature, worker.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Signature_RejectsReportReceiptCiphertextSubstitution()
    {
        using var worker = RSA.Create(2048); var envelope = Envelope("job-a"); var signature = RemoteResearchWorkerSignature.Sign(envelope, worker.ExportPkcs8PrivateKeyPem());
        Assert.Throws<CryptographicException>(() => RemoteResearchWorkerSignature.Verify(envelope with { Ciphertext = Convert.ToBase64String(new byte[] { 9, 9, 9 }) }, signature, worker.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void SignedEnvelope_RoundTripsWorkerSignatureWithoutSelfReferentialCommitment()
    {
        using var worker = RSA.Create(2048); var unsigned = Envelope("job-a");
        var signature = RemoteResearchWorkerSignature.Sign(unsigned, worker.ExportPkcs8PrivateKeyPem());
        var signed = unsigned with { WorkerSignature = signature };
        var json = JsonSerializer.Serialize(signed, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var roundTrip = JsonSerializer.Deserialize<ProtectedResearchResultEnvelope>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(roundTrip); Assert.Equal(signature, roundTrip!.WorkerSignature);
        RemoteResearchWorkerSignature.Verify(roundTrip, roundTrip.WorkerSignature!, worker.ExportSubjectPublicKeyInfoPem());
    }

    [Fact]
    public void AuthenticatedBoundary_RejectsForgedWorkerBeforeClientKeyDecryption()
    {
        using var worker = RSA.Create(2048); using var wrongClient = RSA.Create(2048); var envelope = Envelope("job-a");
        var signature = Convert.FromBase64String(RemoteResearchWorkerSignature.Sign(envelope, worker.ExportPkcs8PrivateKeyPem())); signature[0] ^= 1;
        var ex = Assert.Throws<CryptographicException>(() => AuthenticatedResearchResultProtector.Unprotect(envelope, Convert.ToBase64String(signature), worker.ExportSubjectPublicKeyInfoPem(), wrongClient.ExportPkcs8PrivateKeyPem()));
        Assert.Contains("worker signature", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthenticatedBoundary_RejectsWrongPinnedWorkerBeforeMalformedCiphertextIsParsed()
    {
        using var worker = RSA.Create(2048); using var other = RSA.Create(2048); using var client = RSA.Create(2048); var envelope = Envelope("job-a");
        var signature = RemoteResearchWorkerSignature.Sign(envelope, worker.ExportPkcs8PrivateKeyPem());
        var malformed = envelope with { Ciphertext = "not-base64" };
        var ex = Assert.Throws<CryptographicException>(() => AuthenticatedResearchResultProtector.Unprotect(malformed, signature, other.ExportSubjectPublicKeyInfoPem(), client.ExportPkcs8PrivateKeyPem()));
        Assert.Contains("worker signature", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ProtectedResearchResultEnvelope Envelope(string remoteJobId)
    {
        var now = DateTimeOffset.Parse("2026-09-24T00:00:00Z");
        return new(ResearchResultProtector.ProtocolVersion, "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt", remoteJobId, Convert.ToBase64String(new byte[] { 1, 2, 3 }), Convert.ToBase64String(new byte[] { 4, 5, 6 }), Convert.ToBase64String(new byte[] { 7, 8, 9 }), Convert.ToBase64String(new byte[] { 10, 11, 12 }), now, now.AddHours(1));
    }
}
