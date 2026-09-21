using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserDurableActionRuntimeApiSurfaceTests
{
    [Fact]
    public void Runtime_IsInternal_AndExposesOnlyCreateAndAdvanceDurableOperations()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;

        Assert.False(type.IsPublic);

        var declared = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "CreateAsync", "RunNextStepAsync" }, declared);
    }

    [Fact]
    public void Runtime_DoesNotExposeRawOrchestratorOrVerificationAuthority()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;

        var publicMembers = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Empty(publicMembers.Where(member => member.MemberType is MemberTypes.Field or MemberTypes.Property));
    }
}
