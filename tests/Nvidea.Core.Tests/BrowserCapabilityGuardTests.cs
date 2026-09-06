using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class BrowserCapabilityGuardTests
{
    [Fact]
    public void Guard_CannotRelaxBrowserBlock()
    {
        var guard = Guard(new CapabilityDescriptor(
            "browser.agent", "1.0.0", "Browser agent",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.Low, false, "Browser automation"));

        var original = new BrowserActionDecision(BrowserRiskLevel.Blocked, false, false, "hard browser block");
        var result = guard.Evaluate(Guid.NewGuid(),
            new BrowserAction(BrowserActionKind.Type, BrowserLocator.ByRole("textbox", "Password")),
            Observation(), original);

        Assert.False(result.Allowed);
        Assert.Equal(BrowserRiskLevel.Blocked, result.Risk);
    }

    [Fact]
    public void Guard_ElevatesRiskWhenCapabilityRequiresApproval()
    {
        var guard = Guard(new CapabilityDescriptor(
            "browser.agent", "1.0.0", "Browser agent",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.High, true, "Browser automation"));

        var original = new BrowserActionDecision(BrowserRiskLevel.Medium, false, true, "normal click");
        var result = guard.Evaluate(Guid.NewGuid(),
            new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("button", "Next")),
            Observation(), original);

        Assert.True(result.Allowed);
        Assert.True(result.RequiresApproval);
        Assert.Equal(BrowserRiskLevel.High, result.Risk);
    }

    [Fact]
    public void Guard_BlocksWhenSkillDidNotDeclareRequiredBrowserPermission()
    {
        var guard = Guard(new CapabilityDescriptor(
            "browser.reader", "1.0.0", "Browser reader",
            new HashSet<DataPermission> { DataPermission.BrowserRead },
            CapabilityRiskLevel.Low, false, "Read only"));

        var original = new BrowserActionDecision(BrowserRiskLevel.Medium, false, true, "normal click");
        var result = guard.Evaluate(Guid.NewGuid(),
            new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("button", "Continue")),
            Observation(), original);

        Assert.False(result.Allowed);
        Assert.Equal(BrowserRiskLevel.Blocked, result.Risk);
    }

    private static BrowserCapabilityGuard Guard(CapabilityDescriptor descriptor) =>
        new(descriptor.Id, new CapabilityPermissionPolicy(new CapabilityRegistry(new[] { descriptor })));

    private static BrowserObservation Observation() =>
        new(new Uri("https://example.com"), "Example", Array.Empty<BrowserElement>(), "", DateTimeOffset.UtcNow);
}
