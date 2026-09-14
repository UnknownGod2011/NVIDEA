using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserPostconditionPrivacyTests
{
    [Fact]
    public void UrlMismatch_DoesNotEchoExpectedOrObservedCredentials()
    {
        const string expectedSecret = "expected-secret-token";
        const string observedSecret = "observed-secret-token";
        var postcondition = new BrowserPostcondition(
            BrowserPostconditionKind.UrlEquals,
            $"https://user:password@example.com/callback?token={expectedSecret}#complete");
        var observation = new BrowserObservation(
            new Uri($"https://proxy-user:proxy-password@example.com/callback?token={observedSecret}"),
            "Example",
            Array.Empty<BrowserElement>(),
            "ready",
            DateTimeOffset.UtcNow,
            SnapshotId: Guid.NewGuid().ToString("N"));

        var result = BrowserPostconditionEvaluator.VerifyOne(postcondition, observation);

        Assert.False(result.Verified);
        Assert.Equal(
            "Fresh browser URL did not match the expected navigation destination.",
            result.Detail);
        Assert.DoesNotContain(expectedSecret, result.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain(observedSecret, result.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("password", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UrlMatch_RemainsExactWhileIgnoringOnlyFragment()
    {
        var postcondition = new BrowserPostcondition(
            BrowserPostconditionKind.UrlEquals,
            "https://example.com/callback?state=abc#expected-fragment");
        var observation = new BrowserObservation(
            new Uri("https://example.com/callback?state=abc#observed-fragment"),
            "Example",
            Array.Empty<BrowserElement>(),
            "ready",
            DateTimeOffset.UtcNow,
            SnapshotId: Guid.NewGuid().ToString("N"));

        var result = BrowserPostconditionEvaluator.VerifyOne(postcondition, observation);

        Assert.True(result.Verified);
        Assert.Equal("Exact URL postcondition verified.", result.Detail);
    }

    [Fact]
    public void UrlMismatch_StillTreatsQueryAsPartOfVerification()
    {
        var postcondition = new BrowserPostcondition(
            BrowserPostconditionKind.UrlEquals,
            "https://example.com/callback?state=expected");
        var observation = new BrowserObservation(
            new Uri("https://example.com/callback?state=observed"),
            "Example",
            Array.Empty<BrowserElement>(),
            "ready",
            DateTimeOffset.UtcNow,
            SnapshotId: Guid.NewGuid().ToString("N"));

        var result = BrowserPostconditionEvaluator.VerifyOne(postcondition, observation);

        Assert.False(result.Verified);
        Assert.Equal(
            "Fresh browser URL did not match the expected navigation destination.",
            result.Detail);
    }
}
