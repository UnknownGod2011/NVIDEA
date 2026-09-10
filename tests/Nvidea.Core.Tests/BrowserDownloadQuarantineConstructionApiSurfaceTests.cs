using System.Reflection;
using Nvidea.Core.Browser;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadQuarantineConstructionApiSurfaceTests
{
    [Fact]
    public void ConcreteQuarantineHasNoPublicConstructionPath()
    {
        var type = typeof(BrowserDownloadQuarantine);

        Assert.Empty(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public));

        var publicFactories = type
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => method.ReturnType == type || method.ReturnType == typeof(Task<BrowserDownloadQuarantine>));

        Assert.Empty(publicFactories);
    }

    [Fact]
    public void TrustedCoreConstructionSeamsRemainAvailableInternally()
    {
        var constructors = typeof(BrowserDownloadQuarantine)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Contains(constructors, constructor =>
        {
            var parameters = constructor.GetParameters();
            return constructor.IsAssembly
                && parameters.Length == 2
                && parameters[0].ParameterType == typeof(string)
                && parameters[1].ParameterType == typeof(ILocalStateProtector);
        });

        Assert.Contains(constructors, constructor =>
        {
            var parameters = constructor.GetParameters();
            return constructor.IsAssembly
                && parameters.Length == 3
                && parameters[0].ParameterType == typeof(string)
                && parameters[1].ParameterType == typeof(BrowserDownloadQuarantineOptions)
                && parameters[2].ParameterType == typeof(ILocalStateProtector);
        });
    }

    [Fact]
    public void ReadAndCaptureMethodsRemainAvailableToTrustedComposedInstances()
    {
        var publicMethods = typeof(BrowserDownloadQuarantine)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("CaptureAsync", publicMethods);
        Assert.Contains("ListAsync", publicMethods);
        Assert.Contains("GetAsync", publicMethods);
    }
}
