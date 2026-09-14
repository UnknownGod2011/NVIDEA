using System.Net;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class NebiusDiagnosticPrivacyTests
{
    [Fact]
    public void FromResponse_DoesNotExposeRawProviderBody()
    {
        const string secret = "Bearer super-secret-nebius-token";
        const string signedUrl = "https://storage.example.test/result?token=sensitive-query-value";
        var body = $$"""
            {
              "error": {
                "message": "request failed: {{secret}} {{signedUrl}}"
              }
            }
            """;

        var exception = NebiusApiException.FromResponse(HttpStatusCode.BadGateway, body);

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal("Provider response diagnostics quarantined.", exception.ResponseExcerpt);
        Assert.DoesNotContain(secret, exception.ResponseExcerpt, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", exception.ResponseExcerpt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", exception.ResponseExcerpt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret-nebius-token", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("token=", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteAsync_FinalTransportFailure_DoesNotExposeRawNetworkDiagnostic()
    {
        const string secret = "Bearer super-secret-nebius-transport-token";
        const string signedUrl = "https://storage.example.test/work?token=transport-query-secret";
        var handler = new ThrowingTransportHandler(() =>
            new HttpRequestException(
                $"socket failure while sending {secret} to {signedUrl}",
                inner: null,
                HttpStatusCode.BadGateway));
        using var httpClient = new HttpClient(handler);
        var client = new NebiusTokenFactoryClient(
            httpClient,
            new NebiusOptions
            {
                ApiKey = "test-key",
                MaxAttempts = 2,
                RequestTimeout = TimeSpan.FromSeconds(5)
            });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CompleteAsync(new AgentRequest(
                new[] { new ChatMessage("user", "hello") })));

        Assert.Equal(2, handler.CallCount);
        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal("Nebius transport request failed; provider/network diagnostics quarantined.", exception.Message);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("transport-query-secret", exception.ToString(), StringComparison.Ordinal);
    }

    private sealed class ThrowingTransportHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestException> _exceptionFactory;

        public ThrowingTransportHandler(Func<HttpRequestException> exceptionFactory)
        {
            _exceptionFactory = exceptionFactory;
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromException<HttpResponseMessage>(_exceptionFactory());
        }
    }
}
