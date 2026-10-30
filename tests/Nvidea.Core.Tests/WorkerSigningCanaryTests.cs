using System.Security.Cryptography;
using System.Text.Json;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class WorkerSigningCanaryTests
{
    private static readonly string Deployment = new('A', 64);
    private static readonly string Job = new('B', 64);
    private static readonly DateTimeOffset Clock = DateTimeOffset.Parse("2026-10-14T00:00:00Z");

    [Fact]
    public void Fresh_challenge_roundtrips_and_valid_worker_signature_verifies_once()
    {
        using var worker = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var challenge = WorkerSigningCanary.Create(Deployment, now);
        var encoded = WorkerSigningCanary.EncodeChallenge(challenge);
        var decoded = WorkerSigningCanary.DecodeChallenge(encoded, now);
        var proof = WorkerSigningCanary.Sign(decoded, worker.ExportPkcs8PrivateKeyPem(), Job, now);
        var verifier = new WorkerSigningCanaryVerifier(challenge, Job);
        var verified = verifier.VerifyAndConsume(proof, worker.ExportSubjectPublicKeyInfoPem(), now);

        Assert.Equal(WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(worker.ExportSubjectPublicKeyInfoPem()), verified.VerificationKeySha256);
        Assert.Equal(now, verified.VerifiedAt);
        Assert.Throws<CryptographicException>(() => verifier.VerifyAndConsume(proof, worker.ExportSubjectPublicKeyInfoPem(), now));
    }

    [Fact]
    public void Wrong_identity_tampering_and_replay_fail_closed()
    {
        using var worker = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Deployment, Clock);
        var proof = WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Job, Clock);

        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Verify(challenge, proof, other.ExportSubjectPublicKeyInfoPem(), Job, Clock));
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Verify(challenge, proof with { Nonce = WorkerSigningCanary.Create(Deployment, Clock).Nonce }, worker.ExportSubjectPublicKeyInfoPem(), Job, Clock));
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Verify(challenge, proof with { DeploymentFingerprintSha256 = new string('C', 64) }, worker.ExportSubjectPublicKeyInfoPem(), Job, Clock));
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Verify(challenge, proof with { JobBindingSha256 = new string('D', 64) }, worker.ExportSubjectPublicKeyInfoPem(), Job, Clock));

        var otherChallenge = WorkerSigningCanary.Create(Deployment, Clock);
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Verify(otherChallenge, proof, worker.ExportSubjectPublicKeyInfoPem(), Job, Clock));
    }

    [Fact]
    public void Expired_future_malformed_and_oversized_values_fail_closed()
    {
        using var worker = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Deployment, Clock);
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Job, Clock.AddMinutes(11)));
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Job, Clock.AddMinutes(-1)));
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.DecodeChallenge(new string('A', 2049), Clock));
        Assert.Throws<CryptographicException>(() => WorkerSigningCanary.DecodeChallenge("not-valid%", Clock));
    }

    [Fact]
    public void Proof_is_domain_separated_and_contains_no_secret_material()
    {
        using var worker = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Deployment, Clock);
        var proof = WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Job, Clock);
        var envelope = new ProtectedResearchResultEnvelope(ResearchResultProtector.ProtocolVersion, "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt", "remote-job", "wrapped", "nonce", "ciphertext", "tag", Clock, Clock.AddHours(1));
        Assert.Throws<CryptographicException>(() => RemoteResearchWorkerSignature.Verify(envelope, proof.SignatureBase64, worker.ExportSubjectPublicKeyInfoPem()));

        var json = JsonSerializer.Serialize(proof);
        Assert.DoesNotContain("PRIVATE KEY", json, StringComparison.Ordinal);
        Assert.DoesNotContain("SecretId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResearchPayload", json, StringComparison.OrdinalIgnoreCase);
    }
}
