using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDeploymentEvidenceStrictJsonTests
{
    [Fact]
    public void VerifyJson_RejectsUnknownTopLevelMembers()
    {
        var manifest = ValidManifest();
        var pass = ValidPass(manifest.DeploymentFingerprintSha256);
        var manifestJson = NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false);
        manifestJson = manifestJson.Insert(manifestJson.Length - 1, ",\"unexpectedField\":true");

        var error = Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                manifestJson,
                NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)));

        Assert.Contains("invalid or unsupported JSON", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyJson_RejectsUnknownNestedMembers()
    {
        var manifest = ValidManifest();
        var pass = ValidPass(manifest.DeploymentFingerprintSha256);
        var manifestJson = NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false)
            .Replace("\"compute\":{", "\"compute\":{\"unexpectedField\":true,", StringComparison.Ordinal);

        var error = Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                manifestJson,
                NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)));

        Assert.Contains("invalid or unsupported JSON", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyJson_RejectsDuplicatePropertyNamesAtAnyDepth()
    {
        var manifest = ValidManifest();
        var pass = ValidPass(manifest.DeploymentFingerprintSha256);
        var passJson = NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)
            .Replace(
                "\"remoteStageCount\":3",
                "\"remoteStageCount\":3,\"remoteStageCount\":3",
                StringComparison.Ordinal);

        var topLevel = Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false),
                passJson));
        Assert.Contains("duplicate", topLevel.Message, StringComparison.OrdinalIgnoreCase);

        var manifestJson = NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false)
            .Replace(
                "\"preset\":\"1vcpu-4gb\"",
                "\"preset\":\"1vcpu-4gb\",\"preset\":\"1vcpu-4gb\"",
                StringComparison.Ordinal);

        var nested = Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(
                manifestJson,
                NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false)));
        Assert.Contains("duplicate", nested.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyJson_RejectsCommentsTrailingCommasAndExcessiveDepth()
    {
        var manifest = ValidManifest();
        var pass = ValidPass(manifest.DeploymentFingerprintSha256);
        var manifestJson = NebiusResearchDeploymentManifestBuilder.ToJson(manifest, indented: false);
        var passJson = NebiusResearchPassEvidenceBuilder.ToJson(pass, indented: false);

        var withComment = passJson.Insert(1, "/*comment*/");
        Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(manifestJson, withComment));

        var withTrailingComma = passJson.Insert(passJson.Length - 1, ",");
        Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(manifestJson, withTrailingComma));

        var excessiveDepth = "{\"x\":" + string.Concat(Enumerable.Repeat("[", 40)) + "0" + string.Concat(Enumerable.Repeat("]", 40)) + "}";
        Assert.Throws<InvalidDataException>(() =>
            NebiusResearchDeploymentEvidenceVerifier.VerifyJson(excessiveDepth, passJson));
    }

    private static NebiusResearchPassEvidence ValidPass(string fingerprint) =>
        NebiusResearchPassEvidenceBuilder.Build(
            fingerprint,
            new DateTimeOffset(2026, 9, 10, 5, 0, 0, TimeSpan.Zero),
            remoteStageCount: 3,
            evidenceItemCount: 8,
            validatedCitationCount: 5);

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
