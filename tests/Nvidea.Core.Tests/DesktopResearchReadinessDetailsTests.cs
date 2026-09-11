using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DesktopResearchReadinessDetailsTests
{
    [Fact]
    public void ToDetailsText_ShowsReadyBlockedAndLockedStatesWithoutSecrets()
    {
        var values = new Dictionary<string, string?>
        {
            ["TAVILY_API_KEY"] = "tavily-secret-value",
            ["NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN"] = "provider-secret-value"
        };
        var readiness = DesktopResearchReadiness
            .InspectEnvironment(
                new DesktopResearchCloudMode(LifecycleEnabled: true, DispatchEnabled: false),
                name => values.GetValueOrDefault(name))
            .WithRuntimeState(lifecycleReady: false, dispatchReady: false);

        var text = readiness.ToDetailsText();

        Assert.Contains("Local Tavily research: ready", text, StringComparison.Ordinal);
        Assert.Contains("Nebius lifecycle recovery: blocked", text, StringComparison.Ordinal);
        Assert.Contains("New Nebius Serverless dispatch: locked", text, StringComparison.Ordinal);
        Assert.Contains("NVIDEA_LIVE_SERVERLESS_PROJECT_ID", text, StringComparison.Ordinal);
        Assert.DoesNotContain("tavily-secret-value", text, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-secret-value", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToDetailsText_ReportsValidatedCapabilitiesAsReady()
    {
        var readiness = new DesktopResearchReadiness(
            LocalResearchReady: true,
            NebiusLifecycleRequested: true,
            NebiusLifecycleReady: true,
            NebiusDispatchRequested: true,
            NebiusDispatchReady: true,
            Array.Empty<string>());

        var text = readiness.ToDetailsText();

        Assert.Contains("Local Tavily research: ready", text, StringComparison.Ordinal);
        Assert.Contains("Nebius lifecycle recovery: ready", text, StringComparison.Ordinal);
        Assert.Contains("New Nebius Serverless dispatch: ready", text, StringComparison.Ordinal);
        Assert.Contains("read-only", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No readiness blockers", text, StringComparison.Ordinal);
    }
}
