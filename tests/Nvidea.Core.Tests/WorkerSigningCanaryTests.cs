using System.Security.Cryptography;
using System.Text.Json;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class WorkerSigningCanaryTests
{
    private static readonly DateTimeOffset Clock = DateTimeOffset.Parse("2026-10-14T00:00:00Z");

    [Fact]
    public void Fresh_challenge_roundtrips_and_valid_worker_signature_verifies_once()
    {
        using var worker = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Clock);
        var encoded = WorkerSigningCanary.EncodeChallenge(challenge);
        // Decode is intentionally clock-bound; avoid accepting a stale challenge in tests.
        var decoded = WorkerSigningCanary.DecodeChallenge(encoded, Clock);
        Assert.Equal(challenge, decoded);

        var proof = WorkerSigningCanary.Sign(decoded, worker.ExportPkcs8PrivateKeyPem(), Clock);
        var verifier = new WorkerSigningCanaryVerifier(challenge);
        var verified = verifier.VerifyAndConsume(proof, worker.ExportSubjectPublicKeyInfoPem(), Clock);
        Assert.Equal(WorkerResultVerificationPublicKeyTrust.GetSha256Fingerprint(worker.ExportSubjectPublicKeyInfoPem()),
            verified.VerificationKeySha256);
        Assert.Equal(Clock, verified.VerifiedAt);
        Assert.Throws<CryptographicException>(() => verifier.VerifyAndConsume(proof, worker.ExportSubjectPublicKeyInfoPem(), Clock));
    }

    [Fact]
    public void Wrong_worker_identity_fails_without_downgrade()
    {
        using var worker = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Clock);
        var proof = WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Clock);
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.Verify(challenge, proof, other.ExportSubjectPublicKeyInfoPem(), Clock));
    }

    [Fact]
    public void Proof_replay_against_fresh_challenge_fails()
    {
        using var worker = RSA.Create(2048);
        var first = WorkerSigningCanary.Create(Clock);
        var second = WorkerSigningCanary.Create(Clock);
        Assert.NotEqual(first.Nonce, second.Nonce);
        var proof = WorkerSigningCanary.Sign(first, worker.ExportPkcs8PrivateKeyPem(), Clock);
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.Verify(second, proof, worker.ExportSubjectPublicKeyInfoPem(), Clock));
    }

    [Fact]
    public void Signed_timestamp_and_nonce_tampering_fail_closed()
    {
        using var worker = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Clock);
        var proof = WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Clock);
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.Verify(challenge, proof with { ExpiresAt = proof.ExpiresAt.AddSeconds(1) },
                worker.ExportSubjectPublicKeyInfoPem(), Clock));
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.Verify(challenge, proof with { Nonce = WorkerSigningCanary.Create(Clock).Nonce },
                worker.ExportSubjectPublicKeyInfoPem(), Clock));
    }

    [Fact]
    public void Expired_or_future_or_oversized_challenges_fail_closed()
    {
        using var worker = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Clock);
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Clock.AddMinutes(3)));
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Clock.AddMinutes(-1)));
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.Sign(challenge with { ExpiresAt = Clock.AddMinutes(10) },
                worker.ExportPkcs8PrivateKeyPem(), Clock));
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.DecodeChallenge(new string('A', 2049), Clock));
        Assert.Throws<CryptographicException>(() =>
            WorkerSigningCanary.DecodeChallenge("not-valid%", Clock));
    }

    [Fact]
    public void Proof_cannot_be_reused_as_remote_research_result_signature()
    {
        using var worker = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Clock);
        var proof = WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Clock);
        var envelope = new ProtectedResearchResultEnvelope(
            ResearchResultProtector.ProtocolVersion, "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt", "remote-job",
            "wrapped", "nonce", "ciphertext", "tag", Clock, Clock.AddHours(1));
        Assert.Throws<CryptographicException>(() =>
            RemoteResearchWorkerSignature.Verify(envelope, proof.SignatureBase64,
                worker.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Public_verification_result_and_proof_have_no_private_or_deployment_fields()
    {
        using var worker = RSA.Create(2048);
        var challenge = WorkerSigningCanary.Create(Clock);
        var proof = WorkerSigningCanary.Sign(challenge, worker.ExportPkcs8PrivateKeyPem(), Clock);
        var verification = WorkerSigningCanary.Verify(challenge, proof, worker.ExportSubjectPublicKeyInfoPem(), Clock);
        var json = JsonSerializer.Serialize(new { proof, verification });
        Assert.DoesNotContain("PRIVATE KEY", json, StringComparison.Ordinal);
        Assert.DoesNotContain("MysteryBox", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecretId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResearchPayload", json, StringComparison.OrdinalIgnoreCase);
    }
}
