using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchWorkerBootstrapTimingTests
{
    [Fact]
    public void Load_UsesConfiguredWorkItemBootstrapWindowBeforeSecretReads()
    {
        using var client = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        using var result = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(
            client.ExportSubjectPublicKeyInfoPem(),
            result.ExportSubjectPublicKeyInfoPem(),
            worker.ExportPkcs8PrivateKeyPem());

        var configuration = NebiusResearchWorkerRuntimeConfiguration.Load(name =>
        {
            reads.Add(name);
            return values.GetValueOrDefault(name);
        });

        Assert.Equal(TimeSpan.FromMilliseconds(750), configuration.WorkItemPollInterval);
        Assert.Equal(TimeSpan.FromSeconds(12), configuration.WorkItemMaxWait);
        AssertReadBefore(reads, "NVIDEA_WORK_ITEM_POLL_SECONDS", "NVIDEA_WORKER_PRIVATE_KEY_PEM");
        AssertReadBefore(reads, "NVIDEA_WORK_ITEM_WAIT_SECONDS", "NVIDEA_WORKER_PRIVATE_KEY_PEM");
    }

    [Fact]
    public void Load_InvalidWorkItemWindowFailsBeforeEverySecretRead()
    {
        using var client = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        using var result = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(
            client.ExportSubjectPublicKeyInfoPem(),
            result.ExportSubjectPublicKeyInfoPem(),
            worker.ExportPkcs8PrivateKeyPem());
        values["NVIDEA_WORK_ITEM_POLL_SECONDS"] = "10";
        values["NVIDEA_WORK_ITEM_WAIT_SECONDS"] = "2";

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerRuntimeConfiguration.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.DoesNotContain("NVIDEA_WORKER_PRIVATE_KEY_PEM", reads);
        Assert.DoesNotContain("NEBIUS_API_KEY", reads);
        Assert.DoesNotContain("TAVILY_API_KEY", reads);
    }

    [Fact]
    public void Load_ExcessiveWorkItemWaitFailsBeforeEverySecretRead()
    {
        using var client = RSA.Create(2048);
        using var worker = RSA.Create(2048);
        using var result = RSA.Create(2048);
        var reads = new List<string>();
        var values = CreateValues(
            client.ExportSubjectPublicKeyInfoPem(),
            result.ExportSubjectPublicKeyInfoPem(),
            worker.ExportPkcs8PrivateKeyPem());
        values["NVIDEA_WORK_ITEM_WAIT_SECONDS"] = "301";

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchWorkerRuntimeConfiguration.Load(name =>
            {
                reads.Add(name);
                return values.GetValueOrDefault(name);
            }));

        Assert.DoesNotContain("NVIDEA_WORKER_PRIVATE_KEY_PEM", reads);
        Assert.DoesNotContain("NEBIUS_API_KEY", reads);
        Assert.DoesNotContain("TAVILY_API_KEY", reads);
    }

    private static Dictionary<string, string> CreateValues(
        string clientPublicKey,
        string resultPublicKey,
        string workerPrivateKey) =>
        new(StringComparer.Ordinal)
        {
            ["NVIDEA_TRANSPORT_ROOT"] = "/mnt/nvidea",
            ["NVIDEA_CLIENT_PUBLIC_KEY_PEM"] = clientPublicKey,
            ["NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM"] = resultPublicKey,
            ["NVIDEA_WORK_ITEM_POLL_SECONDS"] = "0.75",
            ["NVIDEA_WORK_ITEM_WAIT_SECONDS"] = "12",
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

    private static void AssertReadBefore(IReadOnlyList<string> reads, string earlier, string later)
    {
        var earlierIndex = FindIndex(reads, earlier);
        var laterIndex = FindIndex(reads, later);
        Assert.True(earlierIndex >= 0, $"Expected '{earlier}' to be read.");
        Assert.True(laterIndex >= 0, $"Expected '{later}' to be read.");
        Assert.True(earlierIndex < laterIndex, $"Expected '{earlier}' to be read before '{later}'.");
    }

    private static int FindIndex(IReadOnlyList<string> values, string expected)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (string.Equals(values[index], expected, StringComparison.Ordinal))
                return index;
        }

        return -1;
    }
}
