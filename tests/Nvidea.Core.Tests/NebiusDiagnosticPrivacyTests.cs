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
}
