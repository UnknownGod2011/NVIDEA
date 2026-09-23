using System.Reflection;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

/// <summary>
/// Locks the deterministic composition seam to test-only, least-authority construction.
/// If this seam grows production/provider authority, these tests must fail rather than
/// silently turning qualification infrastructure into an alternate application API.
/// </summary>
public sealed class NvideaCompositionRootDeterministicApiTests
{
    [Fact]
    public void Deterministic_factory_is_non_public_static_and_returns_the_real_root()
    {
        var method = GetFactory();

        Assert.True(method.IsAssembly);
        Assert.True(method.IsStatic);
        Assert.False(method.IsPublic);
        Assert.Equal(typeof(Task<NvideaCompositionRoot>), method.ReturnType);
    }

    [Fact]
    public void Deterministic_factory_accepts_only_least_authority_dependencies()
    {
        var parameters = GetFactory().GetParameters();

        Assert.Collection(parameters,
            parameter => Assert.Equal(typeof(IAgentInferenceClient), parameter.ParameterType),
            parameter => Assert.Equal(typeof(Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>), parameter.ParameterType),
            parameter => Assert.Equal(typeof(string), parameter.ParameterType),
            parameter =>
            {
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.HasDefaultValue);
            });
    }

    [Fact]
    public void Public_composition_api_does_not_expose_deterministic_or_browser_host_injection()
    {
        var publicMethods = typeof(NvideaCompositionRoot).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        Assert.DoesNotContain(publicMethods, method => method.Name == "CreateDeterministicAsync");
        Assert.DoesNotContain(publicMethods.SelectMany(method => method.GetParameters()), parameter =>
            parameter.ParameterType == typeof(ICrashConsistentBrowserGoalHost) ||
            parameter.ParameterType == typeof(Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>));
    }

    private static MethodInfo GetFactory()
        => typeof(NvideaCompositionRoot).GetMethod(
               "CreateDeterministicAsync",
               BindingFlags.NonPublic | BindingFlags.Static)
           ?? throw new InvalidOperationException("Deterministic composition factory is missing.");
}
