using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalAgentApiSurfaceTests
{
    [Fact]
    public void PublicConstructors_DoNotAcceptPrivilegedBrowserHostRuntime()
    {
        var constructors = typeof(BrowserGoalAgent)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(constructors);
        Assert.DoesNotContain(
            constructors,
            constructor => constructor.GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(BrowserHostRuntime)));
    }

    [Fact]
    public void PublicConstruction_RemainsAvailableThroughLeastAuthorityHostContract()
    {
        var constructor = typeof(BrowserGoalAgent)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(candidate =>
            {
                var parameters = candidate.GetParameters();
                return parameters.Length >= 2
                    && parameters[0].ParameterType == typeof(IBrowserGoalHost);
            });

        Assert.NotNull(constructor);
    }

    [Fact]
    public void TrustedCoreConstruction_RetainsNonPublicPrivilegedAdapterPath()
    {
        var constructor = typeof(BrowserGoalAgent)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .SingleOrDefault(candidate =>
            {
                var parameters = candidate.GetParameters();
                return parameters.Length >= 2
                    && parameters[0].ParameterType == typeof(BrowserHostRuntime);
            });

        Assert.NotNull(constructor);
    }
}
