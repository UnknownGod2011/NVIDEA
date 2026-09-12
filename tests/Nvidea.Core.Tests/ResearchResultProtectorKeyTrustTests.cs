using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchResultProtectorKeyTrustTests
{
    [Fact]
    public void ResultProtector_ValidRsa2048PublicAndPrivateKeys_RoundTrip()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var result = ValidResult(now);

        var envelope = ResearchResultProtector.Protect(result, rsa.ExportSubjectPublicKeyInfoPem());
        var restored = ResearchResultProtector.Unprotect(
            envelope,
            rsa.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1));

        Assert.Equal(result, restored);
    }

    [Fact]
    public void Protect_RejectsPrivateKeyUsedAsClientPublicIdentity()
    {
        using var rsa = RSA.Create(2048);

        Assert.Throws<InvalidOperationException>(() =>
            ResearchResultProtector.Protect(ValidResult(DateTimeOffset.UtcNow), rsa.ExportPkcs8PrivateKeyPem()));
    }

    [Fact]
    public void Protect_RejectsWeakClientPublicKey()
    {
        using var rsa = RSA.Create(1024);

        Assert.Throws<InvalidOperationException>(() =>
            ResearchResultProtector.Protect(ValidResult(DateTimeOffset.UtcNow), rsa.ExportSubjectPublicKeyInfoPem()));
    }

    [Fact]
    public void Protect_RejectsMalformedClientPublicKey()
    {
        const string malformed = "-----BEGIN PUBLIC KEY-----\nnot-base64\n-----END PUBLIC KEY-----";

        Assert.Throws<InvalidOperationException>(() =>
            ResearchResultProtector.Protect(ValidResult(DateTimeOffset.UtcNow), malformed));
    }

    [Fact]
    public void Protect_RejectsOversizedClientPublicKey()
    {
        var oversized = new string('A', 65537);

        Assert.Throws<InvalidOperationException>(() =>
            ResearchResultProtector.Protect(ValidResult(DateTimeOffset.UtcNow), oversized));
    }

    [Fact]
    public void Protect_RejectsControlCharacterInClientPublicKey()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportSubjectPublicKeyInfoPem();
        var contaminated = pem.Insert(10, "\0");

        Assert.Throws<InvalidOperationException>(() =>
            ResearchResultProtector.Protect(ValidResult(DateTimeOffset.UtcNow), contaminated));
    }

    [Fact]
    public void Unprotect_RejectsPublicOnlyKeyUsedAsClientPrivateIdentity()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchResultProtector.Protect(ValidResult(now), rsa.ExportSubjectPublicKeyInfoPem());

        Assert.Throws<InvalidOperationException>(() => ResearchResultProtector.Unprotect(
            envelope,
            rsa.ExportSubjectPublicKeyInfoPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public void Unprotect_RejectsWeakClientPrivateKey()
    {
        using var recipient = RSA.Create(2048);
        using var weak = RSA.Create(1024);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchResultProtector.Protect(ValidResult(now), recipient.ExportSubjectPublicKeyInfoPem());

        Assert.Throws<InvalidOperationException>(() => ResearchResultProtector.Unprotect(
            envelope,
            weak.ExportPkcs8PrivateKeyPem(),
            now.AddMinutes(1)));
    }

    [Fact]
    public void Unprotect_RejectsMalformedClientPrivateKey()
    {
        using var recipient = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchResultProtector.Protect(ValidResult(now), recipient.ExportSubjectPublicKeyInfoPem());
        const string malformed = "-----BEGIN PRIVATE KEY-----\nnot-base64\n-----END PRIVATE KEY-----";

        Assert.Throws<InvalidOperationException>(() => ResearchResultProtector.Unprotect(
            envelope,
            malformed,
            now.AddMinutes(1)));
    }

    [Fact]
    public void Unprotect_RejectsOversizedClientPrivateKey()
    {
        using var recipient = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchResultProtector.Protect(ValidResult(now), recipient.ExportSubjectPublicKeyInfoPem());
        var oversized = new string('A', 65537);

        Assert.Throws<InvalidOperationException>(() => ResearchResultProtector.Unprotect(
            envelope,
            oversized,
            now.AddMinutes(1)));
    }

    [Fact]
    public void Unprotect_RejectsControlCharacterInClientPrivateKey()
    {
        using var rsa = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var envelope = ResearchResultProtector.Protect(ValidResult(now), rsa.ExportSubjectPublicKeyInfoPem());
        var contaminated = rsa.ExportPkcs8PrivateKeyPem().Insert(10, "\0");

        Assert.Throws<InvalidOperationException>(() => ResearchResultProtector.Unprotect(
            envelope,
            contaminated,
            now.AddMinutes(1)));
    }

    [Fact]
    public void ClientResultPublicTrust_CanonicalizesToSubjectPublicKeyInfo()
    {
        using var rsa = RSA.Create(2048);
        var expected = rsa.ExportSubjectPublicKeyInfoPem();

        var canonical = ClientResultEnvelopePublicKeyTrust.ValidateAndCanonicalize(expected);

        Assert.Equal(expected, canonical);
    }

    [Fact]
    public void ClientResultPrivateTrust_CanonicalizesToPkcs8()
    {
        using var rsa = RSA.Create(2048);
        var expected = rsa.ExportPkcs8PrivateKeyPem();

        var canonical = ClientResultEnvelopePrivateKeyTrust.ValidateAndCanonicalize(expected);

        Assert.Equal(expected, canonical);
    }

    private static RemoteResearchStageResult ValidResult(DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            ResearchJobHandler.PlannedStep,
            "mY7FhPlAdtPz9xL4b8gU1cKqN3sW6vRt",
            "aijob-result-key-trust",
            new JobStepResult(
                Completed: false,
                CheckpointStep: ResearchJobHandler.EvidenceStep,
                CheckpointPayload: "{\"prepared\":true}"),
            now,
            now.AddHours(2));
}
