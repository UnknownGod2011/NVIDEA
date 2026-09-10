using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDeploymentEvidenceVerifierTests
{
    [Fact]
    public void VerifyJson_AcceptsMatchingRedactedArtifacts()
    {
        var fingerprint = new string('a', 64);
        var manifest = MinimalManifest(fingerprint);
        var pass = NebiusResearchPassEvidenceBuilder.Build(
            fingerprint,
            new DateTimeOffset(2026, 9, 10, 4, 0, 0, TimeSpan.Zero),
            remoteStageCount: 3,
            evidenceItemCount: 8,
            validatedCitationCount: 5);

        var verification = NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
            NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false),
            NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false));

        Assert.Equal(fingerprint, verification.DeploymentFingerprintSha256);
        Assert.Equal(3, verification.RemoteStageCount);
        Assert.Equal(8, verification.EvidenceItemCount);
        Assert.Equal(5, verification.ValidatedCitationCount);
    }

    [Fact]
    public void VerifyJson_RejectsFingerprintMismatch()
    {
        var manifest = MinimalManifest(new string('a', 64));
        var pass = NebiusResearchPassEvidenceBuilder.Build(
            new string('b', 64),
            DateTimeOffset.UtcNow,
            1,
            1,
            1);

        var exception = Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false),
                NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)));

        Assert.Contains("fingerprint mismatch", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyJson_RejectsUnsupportedSchemasAndInvalidPassCounts()
    {
        var fingerprint = new string('c', 64);
        var manifestJson = NebiusResearchDeploymentManifestBuilder.ToJson(MinimalManifest(fingerprint), indented: false)
            .Replace(NebiusResearchDeploymentManifest.CurrentSchemaVersion, "unsupported", StringComparison.Ordinal);
        var pass = NebiusResearchPassEvidenceBuilder.Build(fingerprint, DateTimeOffset.UtcNow, 1, 1, 1);

        Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                manifestJson,
                NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)));

        var invalidPassJson = NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)
            .Replace("\"validatedCitationCount\":1", "\"validatedCitationCount\":2", StringComparison.Ordinal);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                NebiusResearchDeploymentManifestBuilder.ToJson(MinimalManifest(fingerprint), indented: false),
                invalidPassJson));
    }

    [Fact]
    public void VerifyFiles_ReadsOnlyBoundedRedactedArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-evidence-verifier-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var fingerprint = new string('d', 64);
            var manifestPath = Path.Combine(root, "manifest.json");
            var passPath = Path.Combine(root, "pass.json");
            File.WriteAllText(manifestPath, NebiusResearchDeploymentManifestBuilder.ToJson(MinimalManifest(fingerprint)));
            File.WriteAllText(passPath, NebiusResearchPassEvidenceBuilder.ToJson(
                NebiusResearchPassEvidenceBuilder.Build(fingerprint, DateTimeOffset.UtcNow, 2, 4, 2)));

            var verification = NebiusResearchDeploymentEvidenceVerifier.VerifyFiles(manifestPath, passPath);

            Assert.Equal(fingerprint, verification.DeploymentFingerprintSha256);
            Assert.Equal(2, verification.RemoteStageCount);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusResearchDeploymentManifest MinimalManifest(string fingerprint) => new(
        NebiusResearchDeploymentManifest.CurrentSchemaVersion,
        "sha256:" + new string('e', 64),
        new NebiusResearchComputeSummary("cpu-d3", "1vcpu-4gb", "900s", "network-ssd", 10_000_000_000),
        new NebiusResearchStorageMappingSummary(
            "storage.eu-north1.nebius.cloud",
            "eu-north1",
            new string('f', 64),
            "nvidea-research",
            "nvidea-research",
            "/mnt/nvidea-research",
            "READ_WRITE"),
        new string('1', 64),
        new string('2', 64),
        new[]
        {
            new NebiusResearchSecretReferenceSummary("NEBIUS_API_KEY", "version-pinned", new string('3', 64))
        },
        fingerprint);
}
