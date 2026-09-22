using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

/// <summary>
/// Locks the public transaction surface that must receive exactly one composition-lifetime lease.
/// These tests are deliberately Chromium-free: they prevent future lifetime work from silently
/// omitting a public goal transaction or introducing a public authority-binding API.
/// </summary>
public sealed class BrowserGoalAgentLifetimeDesignTests
{
    [Fact]
    public void Public_transaction_surface_is_explicit_and_bounded()
    {
        var methods = typeof(BrowserGoalAgent)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(static method => method.ReturnType == typeof(Task<BrowserGoalSession>))
            .Select(static method => method.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(BrowserGoalAgent.ApproveAndContinueAsync),
                nameof(BrowserGoalAgent.CancelAsync),
                nameof(BrowserGoalAgent.ResumeAsync),
                nameof(BrowserGoalAgent.RunUntilPauseAsync)
            },
            methods);
    }

    [Fact]
    public void Composition_lifetime_binding_must_not_be_public_authority()
    {
        var publicMembers = typeof(BrowserGoalAgent)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Select(static member => member.Name)
            .ToArray();

        Assert.DoesNotContain("BindCompositionLifetime", publicMembers);
        Assert.DoesNotContain("CompositionLifetime", publicMembers);
        Assert.DoesNotContain("LifetimeGate", publicMembers);
    }

    [Fact]
    public void Public_transactions_remain_cancellation_aware_async_operations()
    {
        var methods = typeof(BrowserGoalAgent)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(static method => method.ReturnType == typeof(Task<BrowserGoalSession>))
            .ToArray();

        Assert.NotEmpty(methods);
        foreach (var method in methods)
        {
            var cancellation = method.GetParameters().SingleOrDefault(static parameter => parameter.ParameterType == typeof(CancellationToken));
            Assert.NotNull(cancellation);
            Assert.True(cancellation!.HasDefaultValue);
        }
    }
}
