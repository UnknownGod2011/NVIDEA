using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDeploymentWorkerKeyTrustTests
{
    [Fact]
    public void ValidateWorkerPublicKey_ReturnsSameCanonicalIdentityAsProtocolTrust()
    {
        using var worker = RSA.Create(2048);
        var publicPem = worker.ExportSubjectPublicKeyInfoPem();

        var deploymentCanonical = NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(publicPem);
        var protocolCanonical = WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(publicPem);

        Assert.Equal(protocolCanonical, deploymentCanonical);
        Assert.Contains("BEGIN PUBLIC KEY", deploymentCanonical, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE KEY", deploymentCanonical, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateWorkerPublicKey_RejectsPrivateMaterial()
    {
        using var worker = RSA.Create(2048);

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(worker.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateWorkerPublicKey_RejectsWeakRsa()
    {
        using var worker = RSA.Create(1024);

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(worker.ExportSubjectPublicKeyInfoPem()));

        Assert.Contains("at least 2048", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateWorkerPublicKey_RejectsMalformedPem()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(
                "-----BEGIN PUBLIC KEY-----\nnot-base64\n-----END PUBLIC KEY-----"));

        Assert.Contains("RSA public-key PEM", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateWorkerPublicKey_RejectsOversizedPemBeforeParsing()
    {
        var oversized = new string('A', 65537);

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(oversized));

        Assert.Contains("missing or invalid", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateWorkerPublicKey_RejectsEmbeddedControlCharacter()
    {
        using var worker = RSA.Create(2048);
        var publicPem = worker.ExportSubjectPublicKeyInfoPem();
        var poisoned = publicPem.Insert(publicPem.IndexOf('\n') + 1, "\u0001");

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(poisoned));

        Assert.Contains("missing or invalid", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
