using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDeploymentEvidenceVerifierTests
{
    [Fact]
    public void VerifyJson_AcceptsMatchingRedactedArtifacts()
    {
        var manifest = ValidManifest();
        var fingerprint = manifest.DeploymentFingerprintSha256;
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
        var manifest = ValidManifest();
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
    public void VerifyJson_RejectsManifestWhoseContentsWereChangedAfterFingerprinting()
    {
        var manifest = ValidManifest();
        var tamperedManifest = manifest with
        {
            Compute = manifest.Compute with { Preset = "different-preset" }
        };
        var pass = NebiusResearchPassEvidenceBuilder.Build(
            manifest.DeploymentFingerprintSha256,
            DateTimeOffset.UtcNow,
            1,
            1,
            1);

        var exception = Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                NebiusResearchDeploymentManifestBuilder.ToJson(tamperedManifest, indented: false),
                NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)));

        Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyJson_RejectsUnsupportedSchemasAndInvalidPassCounts()
    {
        var manifest = ValidManifest();
        var fingerprint = manifest.DeploymentFingerprintSha256;
        var manifestJson = NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false)
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
                NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false),
                invalidPassJson));
    }

    [Fact]
    public void VerifyFiles_ReadsOnlyBoundedRedactedArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-evidence-verifier-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var manifest = ValidManifest();
            var fingerprint = manifest.DeploymentFingerprintSha256;
            var manifestPath = Path.Combine(root, "manifest.json");
            var passPath = Path.Combine(root, "pass.json");
            File.WriteAllText(manifestPath, NebiusResearchDeploymentManifestBuilder.ToJson(manifest));
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

    [Fact]
    public void VerifyFiles_RejectsMalformedArtifactWithoutLeakingFilePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-evidence-verifier-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var manifestPath = Path.Combine(root, "manifest-secret-location.json");
            var passPath = Path.Combine(root, "pass.json");
            File.WriteAllText(manifestPath, "{ malformed-json }");
            File.WriteAllText(passPath, "{}");

            var error = Assert.Throws<InvalidDataException>(() =>
                NebiusResearchDeploymentEvidenceVerifier.VerifyFiles(manifestPath, passPath));

            Assert.Contains("deployment manifest", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(root, error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("manifest-secret-location.json", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void VerifyFiles_RejectsOversizedArtifactBeforeJsonParsing()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-evidence-verifier-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var manifestPath = Path.Combine(root, "oversized-manifest.json");
            var passPath = Path.Combine(root, "pass.json");
            File.WriteAllText(manifestPath, new string('x', (256 * 1024) + 1));
            File.WriteAllText(passPath, "{}");

            var error = Assert.Throws<InvalidDataException>(() =>
                NebiusResearchDeploymentEvidenceVerifier.VerifyFiles(manifestPath, passPath));

            Assert.Contains("size limit", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(root, error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static NebiusResearchDeploymentManifest ValidManifest()
    {
        var manifest = new NebiusResearchDeploymentManifest(
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
            string.Empty);

        var unsigned = new
        {
            schemaVersion = manifest.SchemaVersion,
            workerImageDigest = manifest.WorkerImageDigest,
            compute = manifest.Compute,
            storage = manifest.Storage,
            workerEnvelopePublicKeySha256 = manifest.WorkerEnvelopePublicKeySha256,
            clientVerificationPublicKeySha256 = manifest.ClientVerificationPublicKeySha256,
            secrets = manifest.Secrets
        };
        var canonical = JsonSerializer.Serialize(unsigned, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return manifest with { DeploymentFingerprintSha256 = fingerprint };
    }
}
