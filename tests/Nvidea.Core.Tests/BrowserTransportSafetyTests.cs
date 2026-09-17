using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserTransportSafetyTests
{
    private static BrowserObservation Observation(string url) =>
        new(new Uri(url), "Test", Array.Empty<BrowserElement>(), string.Empty, DateTimeOffset.UtcNow);

    [Fact]
    public void Navigation_AllowsHttps()
    {
        var decision = new BrowserSafetyPolicy().Evaluate(
            new BrowserAction(BrowserActionKind.Navigate, Destination: new Uri("https://example.com/account")),
            Observation("https://example.com"));

        Assert.True(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Medium, decision.Risk);
    }

    [Theory]
    [InlineData("http://example.com/account")]
    [InlineData("http://192.0.2.1/account")]
    public void Navigation_BlocksRemotePlainHttp(string destination)
    {
        var decision = new BrowserSafetyPolicy().Evaluate(
            new BrowserAction(BrowserActionKind.Navigate, Destination: new Uri(destination)),
            Observation("https://example.com"));

        Assert.False(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Blocked, decision.Risk);
        Assert.Contains("plaintext", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://localhost:3000/")]
    [InlineData("http://127.0.0.1:5173/")]
    [InlineData("http://[::1]:8080/")]
    public void Navigation_AllowsLoopbackHttpForLocalDevelopment(string destination)
    {
        var decision = new BrowserSafetyPolicy().Evaluate(
            new BrowserAction(BrowserActionKind.Navigate, Destination: new Uri(destination)),
            Observation("http://localhost"));

        Assert.True(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Medium, decision.Risk);
    }
}
