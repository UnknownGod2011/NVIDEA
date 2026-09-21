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

        var externallyCallable = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.IsAssembly && !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "CancelAsync",
                "CompleteAmbiguousRunningWithoutVerificationAsync",
                "Create",
                "CreateAsync",
                "CreateForTesting",
                "CreateWindows",
                "ReadVerificationPresentationAsync",
                "RearmApprovalAsync",
                "ResumeAfterApprovalAndRunNextStepAsync",
                "RunNextStepAsync"
            },
            externallyCallable);
    }

    [Fact]
    public void TestObserverFactory_RemainsInternal_AndRequiresPayloadFreeObserverContract()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;
        var factory = type.GetMethod(
            "CreateForTesting",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(factory);
        Assert.True(factory!.IsAssembly);
        Assert.False(factory.IsPublic);

        var parameters = factory.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal("ResumableJobOrchestrator", parameters[0].ParameterType.Name);
        Assert.Equal("BrowserVerificationRuntime", parameters[1].ParameterType.Name);
        Assert.Equal("IBrowserVerificationLifecycleObserver", parameters[2].ParameterType.Name);
        Assert.False(parameters[2].ParameterType.IsPublic);
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
    public void TransitionSerializationGate_IsPrivateImplementationDetail()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;
        var gate = type.GetMethod(
            "SerializeTransitionAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(gate);
        Assert.True(gate!.IsPrivate);
        Assert.Null(type.GetProperty("TransitionGate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
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

    [Fact]
    public void AmbiguousCompletion_IsExplicitlyNamedAsNonVerificationPath()
    {
        var type = typeof(BrowserProductRuntime).Assembly.GetType(
            "Nvidea.Core.Desktop.BrowserDurableActionRuntime",
            throwOnError: true)!;

        Assert.NotNull(type.GetMethod(
            "CompleteAmbiguousRunningWithoutVerificationAsync",
            BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.Null(type.GetMethod(
            "CompleteAmbiguousRunningAsync",
            BindingFlags.Instance | BindingFlags.NonPublic));
    }
}
