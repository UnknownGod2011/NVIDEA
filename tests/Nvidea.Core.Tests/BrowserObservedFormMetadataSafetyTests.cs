using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserObservedFormMetadataSafetyTests
{
    private readonly BrowserSafetyPolicy _policy = new();

    [Theory]
    [InlineData("password", null)]
    [InlineData("text", "current-password")]
    [InlineData("text", "one-time-code")]
    [InlineData("text", "cc-number")]
    [InlineData("text", "cc-csc")]
    public void AccessibilityRefTyping_IsBlockedByObservedNonSecretFormMetadata(string inputType, string? autoComplete)
    {
        var observation = Observation(new BrowserElement(
            "nv-1", "textbox", "Details", null, true, true, true,
            InputType: inputType, AutoComplete: autoComplete));
        var action = new BrowserAction(BrowserActionKind.Type, BrowserLocator.Accessibility("nv-1"), "synthetic-value");

        var decision = _policy.Evaluate(action, observation);

        Assert.Equal(BrowserRiskLevel.Blocked, decision.Risk);
        Assert.False(decision.Allowed);
        Assert.False(decision.RequiresApproval);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("username")]
    [InlineData("organization")]
    [InlineData("street-address")]
    public void AccessibilityRefTyping_BenignAutocompleteRemainsMediumRisk(string autoComplete)
    {
        var observation = Observation(new BrowserElement(
            "nv-1", "textbox", "Details", null, true, true, true,
            InputType: "text", AutoComplete: autoComplete));
        var action = new BrowserAction(BrowserActionKind.Type, BrowserLocator.Accessibility("nv-1"), "synthetic-value");

        var decision = _policy.Evaluate(action, observation);

        Assert.Equal(BrowserRiskLevel.Medium, decision.Risk);
        Assert.True(decision.Allowed);
        Assert.False(decision.RequiresApproval);
    }

    [Fact]
    public void SensitiveObservedMetadata_DoesNotRequireSecretValueToBePresent()
    {
        var element = new BrowserElement(
            "nv-1", "textbox", "Details", null, true, true, true,
            InputType: "password", AutoComplete: "current-password");
        var observation = Observation(element);

        var decision = _policy.Evaluate(
            new BrowserAction(BrowserActionKind.Click, BrowserLocator.Accessibility("nv-1")), observation);

        Assert.Null(element.Value);
        Assert.Equal(BrowserRiskLevel.High, decision.Risk);
        Assert.True(decision.Allowed);
        Assert.True(decision.RequiresApproval);
    }

    private static BrowserObservation Observation(params BrowserElement[] elements) => new(
        new Uri("https://example.test/form"),
        "Synthetic form",
        elements,
        "Synthetic form",
        DateTimeOffset.UtcNow);
}