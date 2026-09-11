using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusObjectStorageEndpointTrustTests
{
    [Fact]
    public void ExactRegionalOrigin_IsAccepted()
    {
        using var client = new NebiusObjectStorageClient(ValidOptions());
    }

    [Theory]
    [InlineData("http://storage.eu-north1.nebius.cloud")]
    [InlineData("https://storage.eu-north1.nebius.cloud:444")]
    [InlineData("https://user:pass@storage.eu-north1.nebius.cloud")]
    [InlineData("https://evil.example")]
    [InlineData("https://storage.eu-north1.nebius.cloud.evil.example")]
    [InlineData("https://evilstorage.eu-north1.nebius.cloud")]
    [InlineData("https://storage.eu-north1.nebius.cloud/path")]
    [InlineData("https://storage.eu-north1.nebius.cloud/?x=1")]
    public void UntrustedOrigins_AreRejected(string endpoint)
    {
        var options = ValidOptions() with { Endpoint = endpoint };

        Assert.Throws<ArgumentException>(() => new NebiusObjectStorageClient(options));
    }

    [Fact]
    public void EndpointRegion_MustMatchConfiguredRegion()
    {
        var options = ValidOptions() with
        {
            Endpoint = "https://storage.eu-north1.nebius.cloud",
            Region = "eu-west1"
        };

        Assert.Throws<ArgumentException>(() => new NebiusObjectStorageClient(options));
    }

    [Theory]
    [InlineData("../eu-north1")]
    [InlineData("EU-NORTH1")]
    [InlineData("eu_north1")]
    [InlineData("-eu-north1")]
    [InlineData("eu-north1-")]
    public void InvalidRegionIdentifiers_AreRejected(string region)
    {
        var options = ValidOptions() with { Region = region };

        Assert.Throws<ArgumentException>(() => new NebiusObjectStorageClient(options));
    }

    private static NebiusObjectStorageClientOptions ValidOptions() => new(
        Endpoint: "https://storage.eu-north1.nebius.cloud",
        Region: "eu-north1",
        Bucket: "nvidea-research",
        AccessKeyId: "test-access-key",
        SecretAccessKey: "test-secret-key");
}
