using System.Reflection;
using Nvidea.Core.Desktop;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalCompositionOwnershipTests
{
    [Fact]
    public void TestSeam_IsConstructorOnlyReadonlyAndOptional()
    {
        var rootType = typeof(NvideaCompositionRoot);
        var field = rootType.GetField("_browserGoalHostFactory", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.True(field!.IsInitOnly);

        var constructors = rootType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
        var constructor = Assert.Single(constructors);
        var seam = Assert.Single(constructor.GetParameters().Where(parameter =>
            parameter.ParameterType == typeof(Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>)));

        Assert.True(seam.HasDefaultValue);
        Assert.Null(seam.DefaultValue);
        Assert.Equal(constructor.GetParameters().Length - 1, seam.Position);
    }

    [Fact]
    public void ProductionFactory_DoesNotExposeOrAcceptGoalHostOverride()
    {
        var factory = typeof(NvideaCompositionRoot).GetMethod(
            nameof(NvideaCompositionRoot.CreateFromEnvironmentAsync),
            BindingFlags.Static | BindingFlags.Public);

        Assert.NotNull(factory);
        Assert.Equal(typeof(Task<NvideaCompositionRoot>), factory!.ReturnType);
        Assert.DoesNotContain(factory.GetParameters(), parameter =>
            parameter.ParameterType == typeof(Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>) ||
            parameter.ParameterType == typeof(ICrashConsistentBrowserGoalHost));
    }

    [Fact]
    public void GoalHostOverride_CannotBeMutatedAfterConstruction()
    {
        var rootType = typeof(NvideaCompositionRoot);
        var writableProperties = rootType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.SetMethod is not null)
            .ToArray();

        Assert.DoesNotContain(writableProperties, property =>
            property.PropertyType == typeof(Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>) ||
            property.PropertyType == typeof(ICrashConsistentBrowserGoalHost));
    }
}
