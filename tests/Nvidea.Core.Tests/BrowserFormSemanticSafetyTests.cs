using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserFormSemanticSafetyTests
{
    private readonly BrowserSafetyPolicy _policy = new();
    private static readonly BrowserObservation Observation = new(
        new Uri("https://safe.example/form"), "Form", Array.Empty<BrowserElement>(), "", DateTimeOffset.UtcNow);

    [Theory]
    [InlineData("input[autocomplete='current-password']")]
    [InlineData("input[autocomplete='new-password']")]
    [InlineData("input[autocomplete='one-time-code']")]
    [InlineData("input[autocomplete='cc-number']")]
    [InlineData("input[autocomplete='cc-csc']")]
    [InlineData("input[autocomplete='cc-exp']")]
    [InlineData("input[autocomplete='transaction-amount']")]
    public void Type_CssLocatorWithSensitiveAutocompleteSemantic_IsBlocked(string selector)
    {
        var action = new BrowserAction(
            BrowserActionKind.Type,
            new BrowserLocator(BrowserLocatorKind.Css, selector),
            Value: "synthetic-value");

        var decision = _policy.Evaluate(action, Observation);

        Assert.False(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Blocked, decision.Risk);
        Assert.False(decision.RequiresApproval);
    }

    [Theory]
    [InlineData("input[autocomplete='email']")]
    [InlineData("input[autocomplete='username']")]
    [InlineData("input[autocomplete='organization']")]
    [InlineData("input[autocomplete='street-address']")]
    public void Type_CssLocatorWithNonSecretAutocompleteSemantic_IsNotCredentialBlocked(string selector)
    {
        var action = new BrowserAction(
            BrowserActionKind.Type,
            new BrowserLocator(BrowserLocatorKind.Css, selector),
            Value: "synthetic-value");

        var decision = _policy.Evaluate(action, Observation);

        Assert.True(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Medium, decision.Risk);
        Assert.False(decision.RequiresApproval);
    }
}
