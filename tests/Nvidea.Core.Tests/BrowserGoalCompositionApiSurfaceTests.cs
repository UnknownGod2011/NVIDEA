using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

/// <summary>
/// Locks the least-authority composition boundary for durable browser goals.
/// These tests are intentionally provider/Chromium-free so API regressions can be
/// detected without starting a browser or making a model call.
/// </summary>
public sealed class BrowserGoalCompositionApiSurfaceTests
{
    [Fact]
    public void CompositionRoot_PublishesLeastAuthorityGoalAgentInterface()
    {
        var method = typeof(NvideaCompositionRoot).GetMethod(
            nameof(NvideaCompositionRoot.CreateBrowserGoalAgentAsync),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<IBrowserGoalAgent>), method!.ReturnType);
        Assert.DoesNotContain(
            method.ReturnType.GenericTypeArguments,
            type => type == typeof(BrowserGoalAgent));
    }

    [Fact]
    public void GoalAgentInterface_ContainsOnlyDurableUserTransactions()
    {
        var methods = typeof(IBrowserGoalAgent)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "ApproveAndContinueAsync", "CancelAsync", "ResumeAsync", "RunUntilPauseAsync" },
            methods.Select(method => method.Name).ToArray());

        Assert.All(methods, method =>
        {
            Assert.True(typeof(Task).IsAssignableFrom(method.ReturnType));
            Assert.Contains(method.GetParameters(), parameter => parameter.ParameterType == typeof(CancellationToken));
        });
    }

    [Fact]
    public void CompositionRoot_DoesNotPubliclyExposeRawGoalAgentOrLifetimeAuthority()
    {
        var publicMethods = typeof(NvideaCompositionRoot)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        Assert.DoesNotContain(publicMethods, method => Exposes(method, typeof(BrowserGoalAgent)));
        Assert.DoesNotContain(publicMethods, method => Exposes(method, typeof(CompositionLifetimeGate)));
    }

    private static bool Exposes(MethodInfo method, Type forbiddenType)
    {
        if (ContainsType(method.ReturnType, forbiddenType)) return true;
        return method.GetParameters().Any(parameter => ContainsType(parameter.ParameterType, forbiddenType));
    }

    private static bool ContainsType(Type candidate, Type forbiddenType)
    {
        if (candidate == forbiddenType) return true;
        if (candidate.IsArray) return ContainsType(candidate.GetElementType()!, forbiddenType);
        return candidate.IsGenericType && candidate.GetGenericArguments().Any(type => ContainsType(type, forbiddenType));
    }
}
