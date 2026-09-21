using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserDurableActionRuntimeApiSurfaceTests
{
    [Fact]
    public void Runtime_IsInternal_AndExposesOnlyDurableOperationsFactoriesAndPayloadFreeRead()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;

        Assert.False(type.IsPublic);

        var declared = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "CancelAsync",
                "Create",
                "CreateAsync",
                "CreateWindows",
                "ReadVerificationPresentationAsync",
                "RearmApprovalAsync",
                "ResumeAfterApprovalAndRunNextStepAsync",
                "RunNextStepAsync"
            },
            declared);
    }

    [Fact]
    public void Runtime_DoesNotExposeRawOrchestratorOrVerificationAuthority()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;

        var exposedFieldsOrProperties = type
            .GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(member => member.MemberType is MemberTypes.Field or MemberTypes.Property)
            .ToArray();

        Assert.Empty(exposedFieldsOrProperties);
    }

    [Fact]
    public void VerificationRead_ReturnsOnlyPayloadFreePresentation()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;
        var method = type.GetMethod(
            "ReadVerificationPresentationAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.Equal(
            typeof(Task<DesktopBrowserVerificationPresentation>),
            method!.ReturnType);
    }
}
