using System.Net;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusServerlessEndpointTrustTests
{
    [Fact]
    public void Constructor_AcceptsOfficialEndpointOnStandardTlsPort()
    {
        using var http = new HttpClient(new StubHandler());

        var client = new NebiusServerlessJobClient(
            http,
            new NebiusServerlessOptions(
                "token",
                "project",
                new Uri("https://api.nebius.cloud/")));

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("https://user:password@api.nebius.cloud/")]
    [InlineData("https://api.nebius.cloud:444/")]
    [InlineData("http://api.nebius.cloud/")]
    [InlineData("https://evil-api.nebius.cloud/")]
    [InlineData("https://api.nebius.cloud.evil.example/")]
    public void Constructor_RejectsUnsafeProductionEndpoint(string endpoint)
    {
        using var http = new HttpClient(new StubHandler());

        Assert.Throws<ArgumentException>(() => new NebiusServerlessJobClient(
            http,
            new NebiusServerlessOptions("token", "project", new Uri(endpoint))));
    }

    [Theory]
    [InlineData("https://localhost/")]
    [InlineData("https://127.0.0.1:5000/")]
    public void Constructor_AllowsHttpsLoopbackForExplicitContractTests(string endpoint)
    {
        using var http = new HttpClient(new StubHandler());

        var client = new NebiusServerlessJobClient(
            http,
            new NebiusServerlessOptions("token", "project", new Uri(endpoint)));

        Assert.NotNull(client);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
