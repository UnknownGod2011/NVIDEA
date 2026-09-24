using System.Text.Json;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class WorkerAuthenticationJudgeEvidenceTests
{
    [Fact]
    public void Projection_accepts_only_canonical_public_fingerprint()
    {
        var fingerprint = new string('A', 64);

        var evidence = WorkerAuthenticationJudgeEvidence.FromFingerprint(fingerprint);

        Assert.Equal(fingerprint, evidence.VerificationKeySha256);
        Assert.Equal("RSA-PSS/SHA-256", evidence.SignatureScheme);
        Assert.Contains(fingerprint, evidence.ToStatusText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void Projection_rejects_noncanonical_fingerprints(string fingerprint)
    {
        Assert.Throws<InvalidOperationException>(() => WorkerAuthenticationJudgeEvidence.FromFingerprint(fingerprint));
    }

    [Fact]
    public void Serialized_projection_has_no_field_for_key_material_or_deployment_secret_references()
    {
        var evidence = WorkerAuthenticationJudgeEvidence.FromFingerprint(new string('B', 64));

        var json = JsonSerializer.Serialize(evidence);

        Assert.Contains("VerificationKeySha256", json, StringComparison.Ordinal);
        Assert.DoesNotContain("PEM", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PrivateKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MysteryBox", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecretId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecretVersion", json, StringComparison.OrdinalIgnoreCase);
    }
}
