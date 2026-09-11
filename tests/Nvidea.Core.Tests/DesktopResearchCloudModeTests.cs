using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DesktopResearchCloudModeTests
{
    [Fact]
    public void DefaultsToLocalOnly()
    {
        var mode = DesktopResearchCloudMode.FromEnvironment(_ => null);

        Assert.False(mode.LifecycleEnabled);
        Assert.False(mode.DispatchEnabled);
    }

    [Fact]
    public void LifecycleCanBeEnabledWithoutPaidDispatch()
    {
        var values = new Dictionary<string, string?>
        {
            [DesktopResearchCloudMode.LifecycleEnvironmentVariable] = "true"
        };

        var mode = DesktopResearchCloudMode.FromEnvironment(name => values.GetValueOrDefault(name));

        Assert.True(mode.LifecycleEnabled);
        Assert.False(mode.DispatchEnabled);
    }

    [Fact]
    public void DispatchRequiresLifecycleRecovery()
    {
        var values = new Dictionary<string, string?>
        {
            [DesktopResearchCloudMode.DispatchEnvironmentVariable] = "true"
        };

        var error = Assert.Throws<InvalidOperationException>(
            () => DesktopResearchCloudMode.FromEnvironment(name => values.GetValueOrDefault(name)));

        Assert.Contains(DesktopResearchCloudMode.LifecycleEnvironmentVariable, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAmbiguousBooleanValues()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => DesktopResearchCloudMode.FromEnvironment(name =>
                name == DesktopResearchCloudMode.LifecycleEnvironmentVariable ? "1" : null));

        Assert.Contains("true", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("false", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
