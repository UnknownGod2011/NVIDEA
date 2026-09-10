using System.Reflection;
using Nvidea.Core.Desktop;
using Nvidea.Core.Security;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalSessionStoreApiSurfaceTests
{
    [Fact]
    public void PathBackedGoalStoreHasNoPublicConstructionPath()
    {
        Assert.Empty(typeof(JsonBrowserGoalSessionStore).GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        Assert.Contains(
            typeof(JsonBrowserGoalSessionStore).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic),
            static constructor =>
            {
                var parameters = constructor.GetParameters();
                return constructor.IsAssembly
                    && parameters.Length == 2
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType == typeof(ILocalStateProtector);
            });
    }

    [Fact]
    public void GoalSessionStoreContractRemainsPublicForLeastAuthorityComposition()
    {
        Assert.True(typeof(IBrowserGoalSessionStore).IsPublic);
        Assert.Contains(typeof(IBrowserGoalSessionStore), typeof(JsonBrowserGoalSessionStore).GetInterfaces());
    }

    [Fact]
    public void RawSaveAuthorityCannotBeBootstrappedThroughPublicConcreteConstruction()
    {
        var save = typeof(JsonBrowserGoalSessionStore).GetMethod(
            nameof(IBrowserGoalSessionStore.SaveAsync),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(save);
        Assert.Empty(typeof(JsonBrowserGoalSessionStore).GetConstructors(BindingFlags.Instance | BindingFlags.Public));
    }
}
