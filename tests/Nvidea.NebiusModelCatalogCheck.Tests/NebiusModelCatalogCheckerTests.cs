using System.Text;
using Nvidea.NebiusModelCatalogCheck;

namespace Nvidea.NebiusModelCatalogCheck.Tests;

public sealed class NebiusModelCatalogCheckerTests
{
    private static readonly RequiredModelSet Required = new(
        "nvidia/nvidia-nemotron-3-nano-30b-a3b",
        "nvidia/nemotron-3-super-120b-a12b",
        "nvidia/Nemotron-3-Ultra-550b-a55b");

    [Fact]
    public void Evaluate_PassesWhenAllRequiredModelsExist()
    {
        var json = Catalog(
            Required.Fast,
            Required.Standard,
            Required.Deep,
            "other/model");

        var result = NebiusModelCatalogChecker.Evaluate(
            Encoding.UTF8.GetBytes(json),
            "captured",
            Required,
            endpointHost: null);

        Assert.True(result.Passed);
        Assert.Equal(4, result.CatalogModelCount);
        Assert.Empty(result.FailureCodes);
        Assert.All(result.RequiredModels, item => Assert.True(item.Present));
        Assert.Equal(64, result.CatalogSha256?.Length);
        Assert.Null(result.EndpointHost);
    }

    [Fact]
    public void Evaluate_FailsClosedWhenDeepTierDriftsOutOfCatalog()
    {
        var result = NebiusModelCatalogChecker.Evaluate(
            Encoding.UTF8.GetBytes(Catalog(Required.Fast, Required.Standard)),
            "captured",
            Required,
            endpointHost: null);

        Assert.False(result.Passed);
        Assert.Equal(new[] { "missing_deep_model" }, result.FailureCodes);
        Assert.False(result.RequiredModels.Single(item => item.Tier == "deep").Present);
    }

    [Fact]
    public void Evaluate_UsesExactCaseSensitiveModelIds()
    {
        var result = NebiusModelCatalogChecker.Evaluate(
            Encoding.UTF8.GetBytes(Catalog(
                Required.Fast,
                Required.Standard,
                Required.Deep.ToLowerInvariant())),
            "captured",
            Required,
            endpointHost: null);

        Assert.False(result.Passed);
        Assert.Contains("missing_deep_model", result.FailureCodes);
    }

    [Fact]
    public void Evaluate_RejectsDuplicateModelIds()
    {
        var exception = Assert.Throws<CatalogCheckException>(() =>
            NebiusModelCatalogChecker.Evaluate(
                Encoding.UTF8.GetBytes(Catalog(Required.Fast, Required.Fast, Required.Standard, Required.Deep)),
                "captured",
                Required,
                endpointHost: null));

        Assert.Equal("catalog_duplicate_model_id", exception.Code);
    }

    [Fact]
    public void Evaluate_RejectsDuplicateJsonProperties()
    {
        var json = $$"""
        {
          "data": [
            { "id": "{{Required.Fast}}", "id": "{{Required.Standard}}" },
            { "id": "{{Required.Deep}}" }
          ]
        }
        """;

        var exception = Assert.Throws<CatalogCheckException>(() =>
            NebiusModelCatalogChecker.Evaluate(
                Encoding.UTF8.GetBytes(json),
                "captured",
                Required,
                endpointHost: null));

        Assert.Equal("catalog_duplicate_json_property", exception.Code);
    }

    [Fact]
    public void Evaluate_RejectsMalformedCatalogShape()
    {
        var exception = Assert.Throws<CatalogCheckException>(() =>
            NebiusModelCatalogChecker.Evaluate(
                Encoding.UTF8.GetBytes("{}"),
                "captured",
                Required,
                endpointHost: null));

        Assert.Equal("catalog_shape_invalid", exception.Code);
    }

    [Fact]
    public void Evaluate_DoesNotExposeEndpointForCapturedEvidence()
    {
        var result = NebiusModelCatalogChecker.Evaluate(
            Encoding.UTF8.GetBytes(Catalog(Required.Fast, Required.Standard, Required.Deep)),
            "captured",
            Required,
            endpointHost: "should-not-be-emitted.example");

        Assert.Null(result.EndpointHost);
    }

    [Fact]
    public void Evaluate_IncludesOnlyHostForLiveEvidence()
    {
        var result = NebiusModelCatalogChecker.Evaluate(
            Encoding.UTF8.GetBytes(Catalog(Required.Fast, Required.Standard, Required.Deep)),
            "live",
            Required,
            endpointHost: "api.tokenfactory.us-central1.nebius.com");

        Assert.Equal("api.tokenfactory.us-central1.nebius.com", result.EndpointHost);
    }

    [Theory]
    [InlineData("nebius.com")]
    [InlineData("api.nebius.com")]
    [InlineData("api.tokenfactory.us-central1.nebius.com")]
    [InlineData("API.TOKENFACTORY.US-CENTRAL1.NEBIUS.COM")]
    public void IsTrustedNebiusHost_AcceptsOnlyExactDomainOrRealSubdomain(string host)
    {
        Assert.True(Program.IsTrustedNebiusHost(host));
    }

    [Theory]
    [InlineData("")]
    [InlineData("evilnebius.com")]
    [InlineData("nebius.com.evil.example")]
    [InlineData("api.tokenfactory.us-central1.nebius.com.evil.example")]
    [InlineData("not-nebius.com")]
    public void IsTrustedNebiusHost_RejectsLookalikes(string host)
    {
        Assert.False(Program.IsTrustedNebiusHost(host));
    }

    [Fact]
    public void ValidateTrustedEndpoint_RejectsSuffixLookalikeBeforeCredentialedRequestPath()
    {
        var exception = Assert.Throws<CatalogCheckException>(() =>
            Program.ValidateTrustedEndpoint(new Uri("https://evilnebius.com/v1/")));

        Assert.Equal("untrusted_nebius_endpoint", exception.Code);
    }

    [Fact]
    public void ValidateTrustedEndpoint_RejectsHttpAndEmbeddedUserInfo()
    {
        var httpException = Assert.Throws<CatalogCheckException>(() =>
            Program.ValidateTrustedEndpoint(new Uri("http://api.tokenfactory.us-central1.nebius.com/v1/")));
        var userInfoException = Assert.Throws<CatalogCheckException>(() =>
            Program.ValidateTrustedEndpoint(new Uri("https://user:pass@api.tokenfactory.us-central1.nebius.com/v1/")));

        Assert.Equal("untrusted_nebius_endpoint", httpException.Code);
        Assert.Equal("untrusted_nebius_endpoint", userInfoException.Code);
    }

    private static string Catalog(params string[] ids)
    {
        var entries = string.Join(",", ids.Select(id => $"{{\"id\":\"{id}\",\"object\":\"model\"}}"));
        return $"{{\"object\":\"list\",\"data\":[{entries}]}}";
    }
}
