namespace Nvidea.Core.Jobs;

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

    public static void Validate(NebiusResearchDispatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

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

            if (!secrets.TryGetValue(requiredSecret, out var secretRef)
                || secretRef is null
                || (string.IsNullOrWhiteSpace(secretRef.SecretId) && string.IsNullOrWhiteSpace(secretRef.VersionId)))
            {
                throw new InvalidOperationException(
                    $"Live Nebius research requires '{requiredSecret}' as a Nebius MysteryBox secret reference.");
            }
        }

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

        if (plaintext.ContainsKey("NVIDEA_WORKER_PRIVATE_KEY_PEM"))
            throw new InvalidOperationException("The worker private key must remain secret-backed.");
    }
}
