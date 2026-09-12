namespace Nvidea.Core.Jobs;

/// <summary>
/// Credential-free worker bootstrap values that must be established before provider or worker
/// secret environment variables are read.
/// </summary>
public sealed record NebiusResearchWorkerBootstrapTrust(
    string TransportRoot,
    string ClientVerificationPublicKeyPem)
{
    public const string TransportRootEnvironmentVariable = "NVIDEA_TRANSPORT_ROOT";
    public const string ClientPublicKeyEnvironmentVariable = "NVIDEA_CLIENT_PUBLIC_KEY_PEM";

    /// <summary>
    /// Loads and validates only non-secret worker bootstrap configuration. Callers should invoke
    /// this before loading Token Factory, Tavily, or worker private-key credentials so malformed
    /// public configuration fails without touching provider secrets.
    /// </summary>
    public static NebiusResearchWorkerBootstrapTrust Load(Func<string, string?>? environmentReader = null)
    {
        var read = environmentReader ?? Environment.GetEnvironmentVariable;

        var transportRoot = Require(read, TransportRootEnvironmentVariable).Trim();
        if (!transportRoot.StartsWith('/', StringComparison.Ordinal)
            || transportRoot.Length > 1024
            || transportRoot.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                $"'{TransportRootEnvironmentVariable}' must be a bounded absolute Linux container path.");
        }

        var clientPublicKey = Require(read, ClientPublicKeyEnvironmentVariable);
        var canonicalClientPublicKey = NebiusResearchDeploymentPreflight
            .ValidateClientVerificationPublicKey(clientPublicKey);

        return new NebiusResearchWorkerBootstrapTrust(transportRoot, canonicalClientPublicKey);
    }

    private static string Require(Func<string, string?> read, string name)
    {
        var value = read(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Required worker environment variable '{name}' is missing.")
            : value;
    }
}
