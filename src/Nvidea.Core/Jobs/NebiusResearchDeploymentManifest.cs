using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nvidea.Core.Jobs;

public sealed record NebiusResearchSecretReferenceSummary(
    string EnvironmentVariable,
    string ReferenceType);

public sealed record NebiusResearchStorageMappingSummary(
    string EndpointHost,
    string Region,
    string BucketSha256,
    string ClientPrefix,
    string WorkerSourcePath,
    string WorkerContainerPath,
    string Mode);

public sealed record NebiusResearchComputeSummary(
    string Platform,
    string Preset,
    string Timeout,
    string DiskType,
    long DiskSizeBytes);

public sealed record NebiusResearchDeploymentManifest(
    string SchemaVersion,
    string WorkerImageDigest,
    NebiusResearchComputeSummary Compute,
    NebiusResearchStorageMappingSummary Storage,
    string WorkerEnvelopePublicKeySha256,
    string ClientVerificationPublicKeySha256,
    IReadOnlyList<NebiusResearchSecretReferenceSummary> Secrets,
    string DeploymentFingerprintSha256)
{
    public const string CurrentSchemaVersion = "nvidea.nebius.research-deployment.v1";
}

/// <summary>
/// Produces a deterministic, redacted description of the live research deployment. The manifest
/// intentionally excludes access tokens, Object Storage credentials, MysteryBox ids/version ids,
/// project/subnet ids, PEM text, and payload data. It is suitable for attaching to contract-run
/// evidence so a later run can prove that the immutable worker and non-secret topology were unchanged.
/// </summary>
public static class NebiusResearchDeploymentManifestBuilder
{
    public static NebiusResearchDeploymentManifest Build(
        NebiusResearchDispatchOptions dispatchOptions,
        NebiusObjectStorageClientOptions objectStorageOptions)
    {
        ArgumentNullException.ThrowIfNull(dispatchOptions);
        ArgumentNullException.ThrowIfNull(objectStorageOptions);

        // Reuse the same topology validation as the production/live composition boundary.
        NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(dispatchOptions, objectStorageOptions);

        var workerDigest = ExtractWorkerDigest(dispatchOptions.WorkerImage);
        var plaintext = dispatchOptions.EnvironmentVariables ?? new Dictionary<string, string>();
        if (!plaintext.TryGetValue(NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable, out var clientPublicKeyPem)
            || string.IsNullOrWhiteSpace(clientPublicKeyPem))
        {
            throw new InvalidOperationException("A plaintext client verification public key is required to build a deployment manifest.");
        }

        var transportRoot = plaintext[NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable].Trim();
        var volume = (dispatchOptions.Volumes ?? Array.Empty<NebiusServerlessVolumeMount>())
            .Single(candidate => string.Equals(candidate.ContainerPath, transportRoot, StringComparison.Ordinal));

        var secrets = (dispatchOptions.SecretEnvironmentVariables ?? new Dictionary<string, NebiusMysteryBoxSecretRef>())
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(static pair => new NebiusResearchSecretReferenceSummary(
                pair.Key,
                string.IsNullOrWhiteSpace(pair.Value.VersionId) ? "primary-version" : "version-pinned"))
            .ToArray();

        var compute = new NebiusResearchComputeSummary(
            dispatchOptions.Platform.Trim(),
            dispatchOptions.Preset.Trim(),
            dispatchOptions.Timeout.Trim(),
            dispatchOptions.Disk.Type.Trim(),
            dispatchOptions.Disk.SizeBytes);

        var endpoint = new Uri(objectStorageOptions.Endpoint);
        var storage = new NebiusResearchStorageMappingSummary(
            endpoint.Host.ToLowerInvariant(),
            objectStorageOptions.Region.Trim(),
            Sha256Hex(objectStorageOptions.Bucket.Trim()),
            NormalizePrefix(objectStorageOptions.Prefix),
            NormalizePrefix(volume.SourcePath ?? string.Empty),
            volume.ContainerPath.Trim(),
            volume.Mode.Trim());

        var workerKeyFingerprint = PublicKeyFingerprint(dispatchOptions.WorkerPublicKeyPem, "worker envelope public key");
        var clientKeyFingerprint = PublicKeyFingerprint(clientPublicKeyPem, "client verification public key");

        var unsigned = new
        {
            schemaVersion = NebiusResearchDeploymentManifest.CurrentSchemaVersion,
            workerImageDigest = workerDigest,
            compute,
            storage,
            workerEnvelopePublicKeySha256 = workerKeyFingerprint,
            clientVerificationPublicKeySha256 = clientKeyFingerprint,
            secrets
        };
        var canonical = JsonSerializer.Serialize(unsigned, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var fingerprint = Sha256Hex(canonical);

        return new NebiusResearchDeploymentManifest(
            NebiusResearchDeploymentManifest.CurrentSchemaVersion,
            workerDigest,
            compute,
            storage,
            workerKeyFingerprint,
            clientKeyFingerprint,
            secrets,
            fingerprint);
    }

    public static string ToJson(NebiusResearchDeploymentManifest manifest, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = indented
        });
    }

    private static string ExtractWorkerDigest(string image)
    {
        if (string.IsNullOrWhiteSpace(image))
            throw new InvalidOperationException("Worker image is required to build a deployment manifest.");
        var marker = image.LastIndexOf("@sha256:", StringComparison.OrdinalIgnoreCase);
        if (marker <= 0 || marker + 8 + 64 != image.Length)
            throw new InvalidOperationException("Deployment manifest requires a digest-pinned worker image.");
        var digest = image[(marker + 8)..].ToLowerInvariant();
        if (digest.Length != 64 || !digest.All(static ch => Uri.IsHexDigit(ch)))
            throw new InvalidOperationException("Deployment manifest worker image digest is invalid.");
        return $"sha256:{digest}";
    }

    private static string PublicKeyFingerprint(string pem, string description)
    {
        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException($"The {description} is not a valid RSA public key PEM.");
        }
        return Sha256Hex(rsa.ExportSubjectPublicKeyInfo());
    }

    private static string NormalizePrefix(string prefix) => prefix.Trim().Trim('/');

    private static string Sha256Hex(string value) => Sha256Hex(Encoding.UTF8.GetBytes(value));

    private static string Sha256Hex(ReadOnlySpan<byte> value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
}
