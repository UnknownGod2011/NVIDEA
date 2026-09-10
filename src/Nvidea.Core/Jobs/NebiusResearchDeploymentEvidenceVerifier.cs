using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nvidea.Core.Jobs;

public sealed record NebiusResearchDeploymentEvidenceVerification(
    string DeploymentFingerprintSha256,
    DateTimeOffset CompletedAtUtc,
    int RemoteStageCount,
    int EvidenceItemCount,
    int ValidatedCitationCount);

/// <summary>
/// Verifies that a redacted preflight deployment manifest and a later redacted live PASS artifact
/// refer to the exact same immutable deployment fingerprint. This verifier requires no credentials,
/// provider access, or secret resolution and intentionally consumes only already-redacted artifacts.
/// </summary>
public static class NebiusResearchDeploymentEvidenceVerifier
{
    private const int MaximumArtifactLength = 256 * 1024;

    public static NebiusResearchDeploymentEvidenceVerification VerifyFiles(
        string manifestPath,
        string passEvidencePath)
    {
        var manifestJson = ReadBoundedArtifact(manifestPath, "deployment manifest");
        var passJson = ReadBoundedArtifact(passEvidencePath, "PASS evidence");
        return VerifyJson(manifestJson, passJson);
    }

    public static NebiusResearchDeploymentEvidenceVerification VerifyJson(
        string manifestJson,
        string passEvidenceJson)
    {
        var manifest = Deserialize<NebiusResearchDeploymentManifest>(manifestJson, "deployment manifest");
        var passEvidence = Deserialize<NebiusResearchPassEvidence>(passEvidenceJson, "PASS evidence");

        if (!string.Equals(
                manifest.SchemaVersion,
                NebiusResearchDeploymentManifest.CurrentSchemaVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("Deployment manifest schema version is unsupported.");
        }

        if (!string.Equals(
                passEvidence.SchemaVersion,
                NebiusResearchPassEvidence.CurrentSchemaVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("PASS evidence schema version is unsupported.");
        }

        ValidateFingerprint(manifest.DeploymentFingerprintSha256, "deployment manifest");
        ValidateFingerprint(passEvidence.DeploymentFingerprintSha256, "PASS evidence");

        var recomputedManifestFingerprint = ComputeManifestFingerprint(manifest);
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(recomputedManifestFingerprint),
                Convert.FromHexString(manifest.DeploymentFingerprintSha256)))
        {
            throw new InvalidDataException("Deployment manifest fingerprint does not match its redacted deployment contents.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(manifest.DeploymentFingerprintSha256),
                Convert.FromHexString(passEvidence.DeploymentFingerprintSha256)))
        {
            throw new InvalidDataException("Deployment fingerprint mismatch: PASS evidence does not correspond to the supplied preflight manifest.");
        }

        // Reuse the production PASS builder as the canonical validation boundary for timestamp and
        // bounded count invariants. No values are transformed except fingerprint normalization.
        var validatedPass = NebiusResearchPassEvidenceBuilder.Build(
            passEvidence.DeploymentFingerprintSha256,
            passEvidence.CompletedAtUtc,
            passEvidence.RemoteStageCount,
            passEvidence.EvidenceItemCount,
            passEvidence.ValidatedCitationCount);

        return new NebiusResearchDeploymentEvidenceVerification(
            validatedPass.DeploymentFingerprintSha256,
            validatedPass.CompletedAtUtc,
            validatedPass.RemoteStageCount,
            validatedPass.EvidenceItemCount,
            validatedPass.ValidatedCitationCount);
    }

    private static string ComputeManifestFingerprint(NebiusResearchDeploymentManifest manifest)
    {
        if (manifest.Compute is null || manifest.Storage is null || manifest.Secrets is null)
            throw new InvalidDataException("Deployment manifest is structurally incomplete.");

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
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static T Deserialize<T>(string json, string description)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumArtifactLength)
            throw new InvalidDataException($"The {description} is missing or exceeds the verifier size limit.");

        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                   ?? throw new InvalidDataException($"The {description} is empty or invalid.");
        }
        catch (JsonException)
        {
            throw new InvalidDataException($"The {description} is not valid JSON.");
        }
    }

    private static string ReadBoundedArtifact(string path, string description)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"A {description} path is required.", nameof(path));

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDataException($"The {description} path is invalid.");
        }

        try
        {
            var info = new FileInfo(fullPath);
            if (!info.Exists || info.Length <= 0 || info.Length > MaximumArtifactLength)
                throw new InvalidDataException($"The {description} is missing or exceeds the verifier size limit.");
            return File.ReadAllText(fullPath);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"The {description} could not be read.");
        }
    }

    private static void ValidateFingerprint(string? fingerprint, string description)
    {
        if (string.IsNullOrWhiteSpace(fingerprint)
            || fingerprint.Length != 64
            || fingerprint.Any(static character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidDataException($"The {description} contains an invalid deployment fingerprint.");
        }
    }
}
