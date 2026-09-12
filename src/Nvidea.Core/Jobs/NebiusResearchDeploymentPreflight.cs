using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Credential-free Object Storage namespace identity used when proving that the desktop S3 client
/// and the Serverless-mounted worker address the same protected transport objects.
/// </summary>
public sealed record NebiusObjectStorageTransportAlignment(string Bucket, string Prefix);

/// <summary>
/// Validates the deployment-only contract required by Nvidea.Worker before a live Nebius
/// Serverless research job is submitted. This deliberately validates references/topology,
/// never secret values.
/// </summary>
public static class NebiusResearchDeploymentPreflight
{
    public const string TransportRootEnvironmentVariable = "NVIDEA_TRANSPORT_ROOT";
    public const string ClientPublicKeyEnvironmentVariable = "NVIDEA_CLIENT_PUBLIC_KEY_PEM";

    private static readonly string[] RequiredSecretEnvironmentVariables =
    {
        "NEBIUS_API_KEY",
        "TAVILY_API_KEY",
        "NVIDEA_WORKER_PRIVATE_KEY_PEM"
    };

    public static void Validate(NebiusResearchDispatchOptions options) =>
        ValidateCore(options, requireClientPublicKey: true);

    /// <summary>
    /// Validates every credential-free deployment-shape invariant that can be established before
    /// the client signing key is loaded. The final <see cref="Validate"/> call remains mandatory
    /// after the client public key has been derived from the private key.
    /// </summary>
    public static void ValidateCredentialFreeTopology(NebiusResearchDispatchOptions options) =>
        ValidateCore(options, requireClientPublicKey: false);

    private static void ValidateCore(NebiusResearchDispatchOptions options, bool requireClientPublicKey)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateDigestPinnedWorkerImage(options.WorkerImage);

        var plaintext = options.EnvironmentVariables ?? new Dictionary<string, string>();
        var secrets = options.SecretEnvironmentVariables ?? new Dictionary<string, NebiusMysteryBoxSecretRef>();
        var volumes = options.Volumes ?? Array.Empty<NebiusServerlessVolumeMount>();

        if (!plaintext.TryGetValue(TransportRootEnvironmentVariable, out var transportRoot)
            || string.IsNullOrWhiteSpace(transportRoot))
        {
            throw new InvalidOperationException(
                $"Live Nebius research requires plaintext infrastructure variable '{TransportRootEnvironmentVariable}'.");
        }

        transportRoot = transportRoot.Trim();
        if (!transportRoot.StartsWith('/', StringComparison.Ordinal)
            || transportRoot.Length > 1024
            || transportRoot.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                $"'{TransportRootEnvironmentVariable}' must be a bounded absolute Linux container path.");
        }

        var matchingVolumes = volumes
            .Where(volume => string.Equals(volume.ContainerPath, transportRoot, StringComparison.Ordinal))
            .ToArray();
        if (matchingVolumes.Length != 1)
        {
            throw new InvalidOperationException(
                $"'{TransportRootEnvironmentVariable}' must exactly match one configured Nebius volume ContainerPath.");
        }
        if (!string.Equals(matchingVolumes[0].Mode, "READ_WRITE", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The NVIDEA research transport volume must be READ_WRITE because the worker publishes protected results and control-plane artifacts.");
        }

        foreach (var requiredSecret in RequiredSecretEnvironmentVariables)
        {
            if (plaintext.ContainsKey(requiredSecret))
                throw new InvalidOperationException(
                    $"Worker credential '{requiredSecret}' must never be supplied as plaintext environment configuration.");

            if (!secrets.TryGetValue(requiredSecret, out var secretRef) || secretRef is null)
            {
                throw new InvalidOperationException(
                    $"Live Nebius research requires '{requiredSecret}' as a Nebius MysteryBox secret reference.");
            }

            ValidateMysteryBoxSecretReference(requiredSecret, secretRef);
        }

        if (requireClientPublicKey)
        {
            var hasClientPublicKey =
                (plaintext.TryGetValue(ClientPublicKeyEnvironmentVariable, out var publicKey) && !string.IsNullOrWhiteSpace(publicKey))
                || (secrets.TryGetValue(ClientPublicKeyEnvironmentVariable, out var publicKeySecret)
                    && publicKeySecret is not null
                    && (!string.IsNullOrWhiteSpace(publicKeySecret.SecretId) || !string.IsNullOrWhiteSpace(publicKeySecret.VersionId)));
            if (!hasClientPublicKey)
            {
                throw new InvalidOperationException(
                    $"Live Nebius research requires '{ClientPublicKeyEnvironmentVariable}' so the worker can verify authoritative dispatch bindings.");
            }
        }

        if (plaintext.ContainsKey("NVIDEA_WORKER_PRIVATE_KEY_PEM"))
            throw new InvalidOperationException("The worker private key must remain secret-backed.");
    }

    /// <summary>
    /// Enforces the single immutable-image policy used by credential-free deployment validation
    /// and final live dry-run validation. Keeping this predicate here prevents the paid-run gate
    /// and the early secret-read gate from drifting apart.
    /// </summary>
    public static void ValidateDigestPinnedWorkerImage(string workerImage)
    {
        if (string.IsNullOrWhiteSpace(workerImage)
            || workerImage.Length > 2048
            || workerImage.Any(char.IsControl)
            || workerImage.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("Live Nebius research requires a bounded worker image reference.");
        }

        var marker = workerImage.LastIndexOf("@sha256:", StringComparison.OrdinalIgnoreCase);
        if (marker <= 0 || marker + 8 + 64 != workerImage.Length)
        {
            throw new InvalidOperationException(
                "Live Nebius research requires a digest-pinned worker image in registry/path@sha256:<64-hex> form; mutable tags are not accepted by the preflight.");
        }

        var digest = workerImage[(marker + 8)..];
        if (digest.Length != 64 || !digest.All(static character => Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                "Live Nebius research requires a digest-pinned worker image in registry/path@sha256:<64-hex> form; mutable tags are not accepted by the preflight.");
        }
    }

    /// <summary>
    /// Validates the worker envelope key before any client signing key or provider credential is
    /// required. Private PEM material is rejected explicitly because this value is propagated
    /// through deployment/evidence plumbing that must remain public-only.
    /// </summary>
    public static void ValidateWorkerPublicKey(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)
            || pem.Length > 65536
            || pem.Any(char.IsControl))
        {
            throw new InvalidOperationException("The worker envelope public key is missing or invalid.");
        }

        if (pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
            throw new InvalidOperationException("The worker envelope public key must contain public-only RSA key material.");

        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException("The worker envelope public key is not a valid RSA public key PEM.");
        }

        if (rsa.KeySize < 2048)
            throw new InvalidOperationException("The worker envelope public key must be at least 2048 bits.");
    }

    /// <summary>
    /// Validates the resource-id forms documented by Nebius MysteryBox. A secret id uses the
    /// mbsec- prefix and a version id uses mbsecver-. When both are supplied, they are treated
    /// as an explicitly version-pinned reference; when only SecretId is supplied, the current
    /// primary version is selected by the provider.
    /// </summary>
    public static void ValidateMysteryBoxSecretReference(string environmentVariableName, NebiusMysteryBoxSecretRef secretRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentVariableName);
        ArgumentNullException.ThrowIfNull(secretRef);

        var secretId = NormalizeMysteryBoxId(secretRef.SecretId);
        var versionId = NormalizeMysteryBoxId(secretRef.VersionId);
        if (secretId is null && versionId is null)
        {
            throw new InvalidOperationException(
                $"Worker credential '{environmentVariableName}' must reference a MysteryBox secret or secret version.");
        }

        if (secretId is not null && !IsNebiusResourceId(secretId, "mbsec-"))
            throw new InvalidOperationException(
                $"Worker credential '{environmentVariableName}' has an invalid MysteryBox secret id.");

        if (versionId is not null && !IsNebiusResourceId(versionId, "mbsecver-"))
            throw new InvalidOperationException(
                $"Worker credential '{environmentVariableName}' has an invalid MysteryBox version id.");

        if (versionId is not null && secretId is null)
        {
            throw new InvalidOperationException(
                $"Worker credential '{environmentVariableName}' must include its MysteryBox secret id when pinning a version id.");
        }
    }

    /// <summary>
    /// Proves that the native S3 client and the Serverless-mounted worker resolve the protected
    /// transport namespaces to the same bucket objects without accepting any credential-bearing
    /// client configuration. The client prefix must equal the mounted volume SourcePath.
    /// </summary>
    public static void ValidateObjectStorageAlignment(
        NebiusResearchDispatchOptions dispatchOptions,
        NebiusObjectStorageTransportAlignment alignment)
    {
        ArgumentNullException.ThrowIfNull(dispatchOptions);
        ArgumentNullException.ThrowIfNull(alignment);
        ValidateCredentialFreeTopology(dispatchOptions);

        var plaintext = dispatchOptions.EnvironmentVariables ?? new Dictionary<string, string>();
        var transportRoot = plaintext[TransportRootEnvironmentVariable].Trim();
        var volume = (dispatchOptions.Volumes ?? Array.Empty<NebiusServerlessVolumeMount>())
            .Single(candidate => string.Equals(candidate.ContainerPath, transportRoot, StringComparison.Ordinal));

        var bucket = NormalizeBucket(alignment.Bucket);
        var source = NormalizeBucket(volume.Source);
        if (!string.Equals(bucket, source, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Native Object Storage bucket must exactly match the Serverless research transport volume source.");
        }

        var clientPrefix = NormalizeObjectPrefix(alignment.Prefix, "Object Storage client prefix");
        var mountedSourcePath = NormalizeObjectPrefix(volume.SourcePath ?? string.Empty, "Serverless volume source path");
        if (!string.Equals(clientPrefix, mountedSourcePath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Native Object Storage prefix must exactly match the Serverless research transport volume SourcePath.");
        }
    }

    /// <summary>
    /// Backward-compatible adapter for callers that already hold a validated runtime client
    /// options object. Secret fields are deliberately discarded before alignment validation.
    /// </summary>
    public static void ValidateObjectStorageAlignment(
        NebiusResearchDispatchOptions dispatchOptions,
        NebiusObjectStorageClientOptions objectStorageOptions)
    {
        ArgumentNullException.ThrowIfNull(objectStorageOptions);
        ValidateObjectStorageAlignment(
            dispatchOptions,
            new NebiusObjectStorageTransportAlignment(objectStorageOptions.Bucket, objectStorageOptions.Prefix));
    }

    private static string? NormalizeMysteryBoxId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > 128 || normalized.Any(char.IsControl) || normalized.Any(char.IsWhiteSpace))
            return string.Empty;
        return normalized;
    }

    private static bool IsNebiusResourceId(string value, string prefix)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal) || value.Length <= prefix.Length)
            return false;

        return value.AsSpan(prefix.Length).ToArray().All(static character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private static string NormalizeBucket(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 63
            || value.Any(char.IsControl)
            || value.Contains('/')
            || value.Contains('\\'))
        {
            throw new InvalidOperationException("Research Object Storage bucket/source must be a bounded bucket name.");
        }

        return value.Trim();
    }

    private static string NormalizeObjectPrefix(string value, string description)
    {
        if (value.Length > 128 || value.Any(char.IsControl) || value.Contains('\\'))
            throw new InvalidOperationException($"{description} is invalid.");

        var normalized = value.Trim().Trim('/');
        if (normalized.Contains("..", StringComparison.Ordinal)
            || normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Any(static segment => segment is "." or ".."))
        {
            throw new InvalidOperationException($"{description} must remain within the configured bucket namespace.");
        }

        return normalized;
    }
}
