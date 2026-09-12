using System.Globalization;
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

        // Secrets are deliberately read only after all credential-free trust checks above pass.
        var nebiusApiKey = Require(read, NebiusApiKeyEnvironmentVariable);
        var tavilyApiKey = Require(read, TavilyApiKeyEnvironmentVariable);
        var workerPrivateKey = Require(read, WorkerPrivateKeyEnvironmentVariable);

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
