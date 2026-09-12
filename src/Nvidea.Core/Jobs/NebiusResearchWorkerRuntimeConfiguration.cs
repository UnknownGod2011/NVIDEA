using System.Globalization;
using System.Security.Cryptography;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Complete worker runtime configuration loaded through one injectable environment reader.
/// Public bootstrap and timing/destination configuration is validated before provider or worker
/// credentials are accessed, making secret-read ordering observable and regression-testable.
/// </summary>
public sealed record NebiusResearchWorkerRuntimeConfiguration(
    NebiusResearchWorkerBootstrapTrust BootstrapTrust,
    NebiusOptions Nebius,
    TavilyOptions Tavily,
    TimeSpan BindingPollInterval,
    TimeSpan BindingMaxWait,
    string WorkerPrivateKeyPem)
{
    public const string NebiusBaseUrlEnvironmentVariable = "NVIDEA_NEBIUS_BASE_URL";
    public const string NebiusApiKeyEnvironmentVariable = "NEBIUS_API_KEY";
    public const string TavilyApiKeyEnvironmentVariable = "TAVILY_API_KEY";
    public const string WorkerPrivateKeyEnvironmentVariable = "NVIDEA_WORKER_PRIVATE_KEY_PEM";
    public const string BindingPollSecondsEnvironmentVariable = "NVIDEA_BINDING_POLL_SECONDS";
    public const string BindingWaitSecondsEnvironmentVariable = "NVIDEA_BINDING_WAIT_SECONDS";

    public static NebiusResearchWorkerRuntimeConfiguration Load(Func<string, string?>? environmentReader = null)
    {
        var read = environmentReader ?? Environment.GetEnvironmentVariable;

        // This phase must remain credential-free. Any malformed public bootstrap or timing/provider
        // destination configuration fails without touching provider or worker private credentials.
        var bootstrapTrust = NebiusResearchWorkerBootstrapTrust.Load(read);
        var bindingPollInterval = GetOptionalDurationSeconds(
            read,
            BindingPollSecondsEnvironmentVariable,
            TimeSpan.FromSeconds(2));
        var bindingMaxWait = GetOptionalDurationSeconds(
            read,
            BindingWaitSecondsEnvironmentVariable,
            TimeSpan.FromMinutes(5));

        var baseUriText = read(NebiusBaseUrlEnvironmentVariable);
        var baseUri = string.IsNullOrWhiteSpace(baseUriText)
            ? new Uri("https://api.tokenfactory.us-central1.nebius.com/v1/")
            : new Uri(baseUriText, UriKind.Absolute);
        NebiusOptions.ValidateTrustedBaseUri(baseUri);

        var standardModel = EnvironmentOrDefault(read, "NVIDEA_MODEL_STANDARD", NebiusOptions.VerifiedNemotronSuperModel);
        var fastModel = EnvironmentOrDefault(read, "NVIDEA_MODEL_FAST", NebiusOptions.VerifiedNemotronNanoModel);
        var deepModel = EnvironmentOrDefault(read, "NVIDEA_MODEL_DEEP", NebiusOptions.VerifiedNemotronUltraModel);

        // The worker envelope key is the first secret read. Prove that it is the intended usable
        // private RSA decryption identity before touching independent provider credentials.
        var workerPrivateKey = ValidateWorkerPrivateKey(Require(read, WorkerPrivateKeyEnvironmentVariable));

        var nebiusApiKey = Require(read, NebiusApiKeyEnvironmentVariable);
        var tavilyApiKey = Require(read, TavilyApiKeyEnvironmentVariable);

        var nebius = new NebiusOptions
        {
            ApiKey = nebiusApiKey,
            BaseUri = baseUri,
            StandardModel = standardModel,
            FastModel = fastModel,
            DeepModel = deepModel
        };
        nebius.Validate();

        var tavily = new TavilyOptions { ApiKey = tavilyApiKey };
        tavily.Validate();

        return new NebiusResearchWorkerRuntimeConfiguration(
            bootstrapTrust,
            nebius,
            tavily,
            bindingPollInterval,
            bindingMaxWait,
            workerPrivateKey);
    }

    /// <summary>
    /// Validates the secret-backed worker envelope identity used by
    /// <see cref="ResearchWorkItemProtector.Unprotect"/>. The key must be bounded, contain usable
    /// private RSA material of at least 2048 bits, and successfully round-trip the exact OAEP-SHA256
    /// operation used by the protected work-item protocol. A canonical PKCS#8 PEM is returned so
    /// downstream worker construction does not need to reinterpret arbitrary PEM encodings.
    /// </summary>
    public static string ValidateWorkerPrivateKey(string pem)
    {
        if (string.IsNullOrWhiteSpace(pem)
            || pem.Length > 65536
            || pem.Any(static character => char.IsControl(character) && character is not '\r' and not '\n'))
        {
            throw new InvalidOperationException("The worker envelope private key is missing or invalid.");
        }

        using var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException("The worker envelope private key is not valid RSA private-key PEM.", exception);
        }

        if (rsa.KeySize < 2048)
            throw new InvalidOperationException("The worker envelope RSA private key must be at least 2048 bits.");

        byte[]? probe = null;
        byte[]? wrapped = null;
        byte[]? unwrapped = null;
        try
        {
            // Exporting private parameters rejects public-only material before we attempt the
            // protocol-level decryption capability proof.
            _ = rsa.ExportParameters(includePrivateParameters: true);

            probe = RandomNumberGenerator.GetBytes(32);
            wrapped = rsa.Encrypt(probe, RSAEncryptionPadding.OaepSHA256);
            unwrapped = rsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);
            if (!CryptographicOperations.FixedTimeEquals(probe, unwrapped))
                throw new CryptographicException("Worker envelope RSA capability proof did not round-trip.");

            return rsa.ExportPkcs8PrivateKeyPem();
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException(
                "The worker envelope key must contain usable RSA private material for OAEP-SHA256 decryption.",
                exception);
        }
        finally
        {
            if (probe is not null)
                CryptographicOperations.ZeroMemory(probe);
            if (wrapped is not null)
                CryptographicOperations.ZeroMemory(wrapped);
            if (unwrapped is not null)
                CryptographicOperations.ZeroMemory(unwrapped);
        }
    }

    private static string Require(Func<string, string?> read, string name)
    {
        var value = read(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Required worker environment variable '{name}' is missing.")
            : value;
    }

    private static string EnvironmentOrDefault(
        Func<string, string?> read,
        string variableName,
        string fallback)
    {
        var value = read(variableName);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static TimeSpan GetOptionalDurationSeconds(
        Func<string, string?> read,
        string name,
        TimeSpan fallback)
    {
        var raw = read(name);
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || !double.IsFinite(seconds)
            || seconds <= 0)
        {
            throw new InvalidOperationException(
                $"Worker environment variable '{name}' must be a positive number of seconds.");
        }

        return TimeSpan.FromSeconds(seconds);
    }
}
