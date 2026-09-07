using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserSafetyPolicyObservationTests
{
    private readonly BrowserSafetyPolicy _policy = new();

    [Fact]
    public void Evaluate_BlocksTypingWhenAccessibilityRefPointsToSensitiveField()
    {
        var observation = Observation(new BrowserElement(
            "e-7", "textbox", "Password", null, true, true, true));
        var action = new BrowserAction(
            BrowserActionKind.Type,
            BrowserLocator.Accessibility("e-7"),
            Value: "sample-value");

        var decision = _policy.Evaluate(action, observation);

        Assert.False(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Blocked, decision.Risk);
    }

    [Fact]
    public void Evaluate_RequiresApprovalWhenAccessibilityRefPointsToSubmitButton()
    {
        var observation = Observation(new BrowserElement(
            "e-9", "button", "Submit application", null, true, true, false));
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.Accessibility("e-9"));

        var decision = _policy.Evaluate(action, observation);

        Assert.True(decision.Allowed);
        Assert.True(decision.RequiresApproval);
        Assert.Equal(BrowserRiskLevel.High, decision.Risk);
    }

    [Fact]
    public void Evaluate_ObservedSensitiveNameOverridesBenignPlannerMetadata()
    {
        var observation = Observation(new BrowserElement(
            "e-11", "textbox", "One-time passcode", null, true, true, true));
        var action = new BrowserAction(
            BrowserActionKind.Type,
            new BrowserLocator(BrowserLocatorKind.AccessibilityRef, "e-11", "Notes", "textbox"),
            Value: "sample-value");

        var decision = _policy.Evaluate(action, observation);

        Assert.False(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Blocked, decision.Risk);
    }

    private static BrowserObservation Observation(params BrowserElement[] elements) => new(
        new Uri("https://example.com/form"),
        "Form",
        elements,
        "Form controls",
        DateTimeOffset.UtcNow,
        SnapshotId: "snapshot");
}
