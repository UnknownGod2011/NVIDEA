using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchWorkerRuntimeConfigurationTests
{
    [Fact]
    public void Load_ValidConfigurationReadsSecretsOnlyAfterPublicTrustChecks()
    {
        using var client = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(
            client.ExportSubjectPublicKeyInfoPem(),
            worker.ExportPkcs8PrivateKeyPem());

        var configuration = NebiusResearchWorkerRuntimeConfiguration.Load(name =>
        {
            reads.Add(name);
            return values.GetValueOrDefault(name);
        });

        Assert.Equal("/mnt/nvidea", configuration.BootstrapTrust.TransportRoot);
        Assert.Equal("nebius-test-key", configuration.Nebius.ApiKey);
        Assert.Equal("tavily-test-key", configuration.Tavily.ApiKey);
        Assert.Equal(TimeSpan.FromSeconds(3), configuration.BindingPollInterval);
        Assert.Equal(TimeSpan.FromSeconds(45), configuration.BindingMaxWait);
        Assert.Equal(values["NVIDEA_WORKER_PRIVATE_KEY_PEM"], configuration.WorkerPrivateKeyPem);

        var firstSecretRead = reads.FindIndex(static name =>
            name is "NEBIUS_API_KEY" or "TAVILY_API_KEY" or "NVIDEA_WORKER_PRIVATE_KEY_PEM");
        Assert.True(firstSecretRead > 0);

        AssertReadBefore(reads, "NVIDEA_TRANSPORT_ROOT", "NEBIUS_API_KEY");
        AssertReadBefore(reads, "NVIDEA_CLIENT_PUBLIC_KEY_PEM", "NEBIUS_API_KEY");
        AssertReadBefore(reads, "NVIDEA_BINDING_POLL_SECONDS", "NEBIUS_API_KEY");
        AssertReadBefore(reads, "NVIDEA_BINDING_WAIT_SECONDS", "NEBIUS_API_KEY");
        AssertReadBefore(reads, "NVIDEA_NEBIUS_BASE_URL", "NEBIUS_API_KEY");
        AssertReadBefore(reads, "NVIDEA_MODEL_STANDARD", "NEBIUS_API_KEY");
        AssertReadBefore(reads, "NVIDEA_MODEL_FAST", "NEBIUS_API_KEY");
        AssertReadBefore(reads, "NVIDEA_MODEL_DEEP", "NEBIUS_API_KEY");
    }

    [Fact]
    public void Load_InvalidBootstrapPreventsEveryProviderAndWorkerSecretRead()
    {
        var reads = new List<string>();
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["NVIDEA_TRANSPORT_ROOT"] = "relative/path",
            ["NVIDEA_CLIENT_PUBLIC_KEY_PEM"] = "not-used",
            ["NEBIUS_API_KEY"] = "must-not-be-read",
            ["TAVILY_API_KEY"] = "must-not-be-read",
            ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = "must-not-be-read"
        };

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerRuntimeConfiguration.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Equal(new[] { "NVIDEA_TRANSPORT_ROOT" }, reads);
        AssertNoSecretsRead(reads);
    }

    [Fact]
    public void Load_UntrustedNebiusEndpointPreventsEveryProviderAndWorkerSecretRead()
    {
        using var client = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(
            client.ExportSubjectPublicKeyInfoPem(),
            worker.ExportPkcs8PrivateKeyPem());
        values["NVIDEA_NEBIUS_BASE_URL"] = "https://nebius.com.evil.example/v1/";

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerRuntimeConfiguration.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.Contains("NVIDEA_NEBIUS_BASE_URL", reads);
        AssertNoSecretsRead(reads);
    }

    [Fact]
    public void Load_InvalidTimingPreventsEveryProviderAndWorkerSecretRead()
    {
        using var client = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(
            client.ExportSubjectPublicKeyInfoPem(),
            worker.ExportPkcs8PrivateKeyPem());
        values["NVIDEA_BINDING_WAIT_SECONDS"] = "NaN";

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerRuntimeConfiguration.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        AssertNoSecretsRead(reads);
    }

    private static Dictionary<string, string> CreateValues(string clientPublicKey, string workerPrivateKey) =>
        new(StringComparer.Ordinal)
        {
            ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea",
            ["NVIDEA_CLIENT_PUBLIC_KEY_PEM"] = clientPublicKey,
            ["NVIDEA_BINDING_POLL_SECONDS"] = "3",
            ["NVIDEA_BINDING_WAIT_SECONDS"] = "45",
            ["NVIDEA_NEBIUS_BASE_URL"] = "https://api.tokenfactory.us-central1.nebius.com/v1/",
            ["NVIDEA_MODEL_STANDARD"] = "nvidia/nemotron-3-super-120b-a12b",
            ["NVIDEA_MODEL_FAST"] = "nvidia/nvidia-nemotron-3-nano-30b-a3b",
            ["NVIDEA_MODEL_DEEP"] = "nvidia/Nemotron-3-Ultra-550b-a55b",
            ["NEBIUS_API_KEY"] = "nebius-test-key",
            ["TAVILY_API_KEY"] = "tavily-test-key",
            ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = workerPrivateKey
        };

    private static void AssertNoSecretsRead(IReadOnlyCollection<string> reads)
    {
        Assert.DoesNotContain("NEBIUS_API_KEY", reads);
        Assert.DoesNotContain("TAVILY_API_KEY", reads);
        Assert.DoesNotContain("NVIDEA_WORKER_PRIVATE_KEY_PEM", reads);
    }

    private static void AssertReadBefore(IReadOnlyList<string> reads, string earlier, string later)
    {
        var earlierIndex = reads.IndexOf(earlier);
        var laterIndex = reads.IndexOf(later);
        Assert.True(earlierIndex >= 0, $"Expected '{earlier}' to be read.");
        Assert.True(laterIndex >= 0, $"Expected '{later}' to be read.");
        Assert.True(earlierIndex < laterIndex, $"Expected '{earlier}' to be read before '{later}'.");
    }
}
