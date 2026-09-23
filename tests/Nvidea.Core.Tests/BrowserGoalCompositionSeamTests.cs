using System.Reflection;
using Nvidea.Core.Desktop;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalCompositionSeamTests
{
    [Fact]
    public void BrowserGoalHostFactory_RemainsPrivateAndLeastAuthority()
    {
        var rootType = typeof(NvideaCompositionRoot);
        var field = rootType.GetField("_browserGoalHostFactory", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        Assert.True(field!.IsPrivate);
        Assert.Equal(
            typeof(Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>),
            field.FieldType);

        var publicMembers = rootType
            .GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
            .Select(member => member.ToString() ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(publicMembers, member => member.Contains("browserGoalHostFactory", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMembers, member => member.Contains(nameof(ICrashConsistentBrowserGoalHost), StringComparison.Ordinal));
    }

    [Fact]
    public void GoalHostResolution_RemainsInternalToCompositionAndCancellationAware()
    {
        var method = typeof(NvideaCompositionRoot).GetMethod(
            "GetBrowserGoalHostUnderLeaseAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.True(method!.IsPrivate);
        Assert.Equal(typeof(Task<ICrashConsistentBrowserGoalHost>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }
}
