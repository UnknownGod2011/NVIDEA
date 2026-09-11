using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DesktopResearchReadinessTests
{
    [Fact]
    public void InspectEnvironment_DoesNotExposeSecretValues()
    {
        var values = new Dictionary<string, string?>
        {
            ["TAVILY_API_KEY"] = null,
            ["NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN"] = "super-secret-token"
        };
        var readiness = DesktopResearchReadiness.InspectEnvironment(
            new DesktopResearchCloudMode(LifecycleEnabled: true, DispatchEnabled: false),
            name => values.GetValueOrDefault(name));

        Assert.False(readiness.LocalResearchReady);
        Assert.Contains(readiness.Blockers, x => x.Contains("TAVILY_API_KEY", StringComparison.Ordinal));
        Assert.DoesNotContain(readiness.Blockers, x => x.Contains("super-secret-token", StringComparison.Ordinal));
    }

    [Fact]
    public void InspectEnvironment_ListsMissingNebiusConfigurationNamesOnly()
    {
        var readiness = DesktopResearchReadiness.InspectEnvironment(
            new DesktopResearchCloudMode(LifecycleEnabled: true, DispatchEnabled: false),
            _ => null);

        Assert.Contains("Nebius lifecycle: NVIDEA_LIVE_SERVERLESS_PROJECT_ID is missing.", readiness.Blockers);
        Assert.Contains("Nebius lifecycle: NVIDEA_LIVE_OBJECT_STORAGE_BUCKET is missing.", readiness.Blockers);
        Assert.False(readiness.NebiusLifecycleReady);
    }

    [Fact]
    public void WithRuntimeState_RequiresRequestedAndValidatedCapabilities()
    {
        var values = RequiredConfiguration().ToDictionary(x => x, _ => (string?)"configured");
        values["TAVILY_API_KEY"] = "configured";
        var readiness = DesktopResearchReadiness.InspectEnvironment(
            new DesktopResearchCloudMode(LifecycleEnabled: true, DispatchEnabled: true),
            name => values.GetValueOrDefault(name));

        var ready = readiness.WithRuntimeState(lifecycleReady: true, dispatchReady: true);

        Assert.True(ready.LocalResearchReady);
        Assert.True(ready.NebiusLifecycleReady);
        Assert.True(ready.NebiusDispatchReady);
        Assert.Empty(ready.Blockers);
    }

    [Fact]
    public void WithRuntimeState_CloudFailureAddsOnlyGenericProviderFailure()
    {
        var readiness = DesktopResearchReadiness.InspectEnvironment(
            new DesktopResearchCloudMode(LifecycleEnabled: true, DispatchEnabled: false),
            _ => "configured");

        var blocked = readiness.WithRuntimeState(false, false, cloudPreflightFailed: true);

        Assert.Contains(blocked.Blockers, x => x.Contains("contract probe", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(blocked.Blockers, x => x.Contains("configured", StringComparison.Ordinal));
    }

    private static string[] RequiredConfiguration() =>
    {
        "NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN",
        "NVIDEA_LIVE_SERVERLESS_PROJECT_ID",
        "NVIDEA_LIVE_WORKER_IMAGE",
        "NVIDEA_LIVE_SUBNET_ID",
        "NVIDEA_LIVE_PLATFORM",
        "NVIDEA_LIVE_PRESET",
        "NVIDEA_LIVE_TIMEOUT",
        "NVIDEA_LIVE_DISK_TYPE",
        "NVIDEA_LIVE_DISK_SIZE_BYTES",
        "NVIDEA_LIVE_TRANSPORT_SOURCE",
        "NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE",
        "NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE",
        "NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT",
        "NVIDEA_LIVE_OBJECT_STORAGE_REGION",
        "NVIDEA_LIVE_OBJECT_STORAGE_BUCKET",
        "NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID",
        "NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY",
        "NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID",
        "NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID",
        "NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID"
    };
}
