using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class TavilyEndpointTrustTests
{
    [Fact]
    public void Validate_accepts_exact_standard_tavily_endpoint()
    {
        var options = Options("https://api.tavily.com/");

        options.Validate();
    }

    [Theory]
    [InlineData("http://api.tavily.com/")]
    [InlineData("https://api.tavily.com:444/")]
    [InlineData("https://user:pass@api.tavily.com/")]
    [InlineData("https://evil.tavily.com/")]
    [InlineData("https://api.tavily.com.evil.example/")]
    [InlineData("https://tavily.com/")]
    public void Validate_rejects_untrusted_tavily_endpoint(string endpoint)
    {
        var options = Options(endpoint);

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void Client_rejects_untrusted_endpoint_before_any_http_request()
    {
        var handler = new CountingHandler();
        using var http = new HttpClient(handler);
        var options = Options("https://user:secret@api.tavily.com/");

        Assert.Throws<InvalidOperationException>(() => new TavilyResearchClient(http, options));
        Assert.Equal(0, handler.SendCount);
    }

    private static TavilyOptions Options(string endpoint) => new()
    {
        ApiKey = "test-key",
        BaseUri = new Uri(endpoint),
        RequestTimeout = TimeSpan.FromSeconds(2),
        MaxAttempts = 1
    };

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
