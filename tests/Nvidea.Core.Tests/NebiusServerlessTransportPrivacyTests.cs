using System.Net;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusServerlessTransportPrivacyTests
{
    [Fact]
    public async Task ListAsync_QuarantinesRetryAndFinalTransportDiagnostics()
    {
        const string secret = "super-secret-serverless-token";
        const string raw = "Bearer " + secret + " via https://proxy.example/fail?token=serverless-query-secret";
        var calls = 0;
        var handler = new ThrowingHandler((_, _) =>
        {
            calls++;
            throw new HttpRequestException(raw, new InvalidOperationException(raw), HttpStatusCode.ServiceUnavailable);
        });
        using var http = new HttpClient(handler);
        var client = new NebiusServerlessJobClient(
            http,
            new NebiusServerlessOptions(
                "test-access-token",
                "project-123",
                MaxRetries: 1));

        var failure = await Assert.ThrowsAsync<HttpRequestException>(() => client.ListAsync());

        Assert.Equal(2, calls);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        Assert.Null(failure.InnerException);
        Assert.Equal(
            "Nebius Serverless transport request failed; provider/network diagnostics quarantined.",
            failure.Message);
        Assert.DoesNotContain(secret, failure.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", failure.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", failure.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListAsync_QuarantinesRetryAndFinalTimeoutDiagnostics()
    {
        const string secret = "super-secret-serverless-timeout-token";
        const string raw = "Bearer " + secret + " via https://proxy.example/timeout?token=timeout-query-secret";
        var calls = 0;
        var handler = new ThrowingHandler((_, _) =>
        {
            calls++;
            throw new TaskCanceledException(raw, new InvalidOperationException(raw));
        });
        using var http = new HttpClient(handler);
        var client = new NebiusServerlessJobClient(
            http,
            new NebiusServerlessOptions(
                "test-access-token",
                "project-123",
                MaxRetries: 1));

        var failure = await Assert.ThrowsAsync<TimeoutException>(() => client.ListAsync());

        Assert.Equal(2, calls);
        Assert.Null(failure.InnerException);
        Assert.Equal(
            "Nebius Serverless request timed out; provider/network diagnostics quarantined.",
            failure.Message);
        Assert.DoesNotContain(secret, failure.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", failure.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", failure.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

        public ThrowingHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) =>
            _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_handler(request, cancellationToken));
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }
}
