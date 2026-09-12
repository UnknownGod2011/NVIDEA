namespace Nvidea.Core.Jobs;

/// <summary>
/// Credential-free worker bootstrap values that must be established before provider or worker
/// secret environment variables are read.
/// </summary>
public sealed record NebiusResearchWorkerBootstrapTrust(
    string TransportRoot,
    string ClientVerificationPublicKeyPem,
    string ClientResultEncryptionPublicKeyPem)
{
    public const string TransportRootEnvironmentVariable = "NVIDEA_TRANSPORT_ROOT";
    public const string ClientPublicKeyEnvironmentVariable = "NVIDEA_CLIENT_PUBLIC_KEY_PEM";
    public const string ClientResultPublicKeyEnvironmentVariable = "NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM";

    /// <summary>
    /// Loads and validates only non-secret worker bootstrap configuration. Callers should invoke
    /// this before loading Token Factory, Tavily, or worker private-key credentials so malformed
    /// public configuration fails without touching provider secrets.
    ///
    /// Dispatch verification and result encryption deliberately use distinct RSA identities. This
    /// prevents compromise or future policy drift in one protocol role from silently conferring
    /// authority in the other role.
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

        var clientResultPublicKey = Require(read, ClientResultPublicKeyEnvironmentVariable);
        var canonicalClientResultPublicKey = ClientResultEnvelopePublicKeyTrust
            .ValidateAndCanonicalize(clientResultPublicKey);

        if (string.Equals(canonicalClientPublicKey, canonicalClientResultPublicKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Client dispatch-verification and result-encryption RSA identities must be distinct.");
        }

        return new NebiusResearchWorkerBootstrapTrust(
            transportRoot,
            canonicalClientPublicKey,
            canonicalClientResultPublicKey);
    }

    private static string Require(Func<string, string?> read, string name)
    {
        var value = read(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Required worker environment variable '{name}' is missing.")
            : value;
    }
}
