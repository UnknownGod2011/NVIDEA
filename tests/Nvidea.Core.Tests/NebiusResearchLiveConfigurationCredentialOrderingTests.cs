using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchLiveConfigurationCredentialOrderingTests
{
    private const string AccessKeyVariable = "NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID";
    private const string SecretKeyVariable = "NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY";

    [Fact]
    public void InvalidObjectStorageEndpoint_IsRejectedBeforeStaticCredentialsAreRead()
    {
        var reads = new List<string>();
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT"] = "https://evil.example",
            ["NVIDEA_LIVE_OBJECT_STORAGE_REGION"] = "eu-north1",
            [AccessKeyVariable] = "must-not-be-read",
            [SecretKeyVariable] = "must-not-be-read"
        };

        string? Reader(string name)
        {
            reads.Add(name);
            return environment.GetValueOrDefault(name);
        }

        var exception = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchLiveConfigurationLoader.Load(Reader));

        Assert.Contains("OBJECT_STORAGE_ENDPOINT", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessKeyVariable, reads);
        Assert.DoesNotContain(SecretKeyVariable, reads);
        Assert.Equal(
            new[]
            {
                "NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT",
                "NVIDEA_LIVE_OBJECT_STORAGE_REGION"
            },
            reads);
    }

    [Fact]
    public void RegionMismatch_IsRejectedBeforeStaticCredentialsAreRead()
    {
        var reads = new List<string>();
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT"] = "https://storage.eu-west1.nebius.cloud",
            ["NVIDEA_LIVE_OBJECT_STORAGE_REGION"] = "eu-north1",
            [AccessKeyVariable] = "must-not-be-read",
            [SecretKeyVariable] = "must-not-be-read"
        };

        string? Reader(string name)
        {
            reads.Add(name);
            return environment.GetValueOrDefault(name);
        }

        Assert.Throws<InvalidOperationException>(
            () => NebiusResearchLiveConfigurationLoader.Load(Reader));

        Assert.DoesNotContain(AccessKeyVariable, reads);
        Assert.DoesNotContain(SecretKeyVariable, reads);
    }

    [Theory]
    [InlineData("https://storage.eu-north1.nebius.cloud", "eu-north1")]
    [InlineData("https://storage.us-central1.nebius.cloud/", "us-central1")]
    public void TrustedRegionalOrigin_PassesEarlyBoundary(string endpoint, string region)
    {
        NebiusResearchLiveConfigurationLoader.ValidateObjectStorageEndpointAndRegion(endpoint, region);
    }

    [Theory]
    [InlineData("http://storage.eu-north1.nebius.cloud", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud:444", "eu-north1")]
    [InlineData("https://user:pass@storage.eu-north1.nebius.cloud", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud.evil.example", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud/path", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud/?x=1", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud", "EU-NORTH1")]
    public void UntrustedEarlyBoundary_IsRejected(string endpoint, string region)
    {
        Assert.Throws<InvalidOperationException>(
            () => NebiusResearchLiveConfigurationLoader.ValidateObjectStorageEndpointAndRegion(endpoint, region));
    }
}
