using System.Reflection;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class NvideaCompositionRootBrowserLifetimeBoundaryTests
{
    [Fact]
    public void BrowserLifetimeSynchronizationAndAuthorityRemainPrivate()
    {
        var type = typeof(NvideaCompositionRoot);
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        var gate = Assert.Single(fields, static field => field.Name == "_browserGate");
        Assert.Equal(typeof(SemaphoreSlim), gate.FieldType);
        Assert.True(gate.IsPrivate);

        var host = Assert.Single(fields, static field => field.Name == "_browser");
        Assert.Equal(typeof(BrowserHostRuntime), host.FieldType);
        Assert.True(host.IsPrivate);

        var product = Assert.Single(fields, static field => field.Name == "_browserProduct");
        Assert.Equal(typeof(BrowserProductRuntime), product.FieldType);
        Assert.True(product.IsPrivate);

        Assert.DoesNotContain(
            type.GetMembers(BindingFlags.Instance | BindingFlags.Public),
            static member => member switch
            {
                FieldInfo field => IsRawBrowserAuthority(field.FieldType),
                PropertyInfo property => IsRawBrowserAuthority(property.PropertyType),
                MethodInfo method => IsRawBrowserAuthority(method.ReturnType)
                    || method.GetParameters().Any(static parameter => IsRawBrowserAuthority(parameter.ParameterType)),
                _ => false
            });
    }

    [Fact]
    public void BrowserProductAcquisitionRemainsCancellationAwareAndAsync()
    {
        var method = typeof(NvideaCompositionRoot).GetMethod(
            nameof(NvideaCompositionRoot.GetBrowserProductAsync),
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<BrowserProductRuntime>), method!.ReturnType);
        var parameter = Assert.Single(method.GetParameters());
        Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
        Assert.True(parameter.HasDefaultValue);
    }

    private static bool IsRawBrowserAuthority(Type candidate)
    {
        if (candidate == typeof(BrowserHostRuntime)
            || candidate == typeof(BrowserDurableActionRuntime)
            || candidate == typeof(DurableBrowserVerificationReceiptStore)
            || candidate == typeof(DurableBrowserVerificationPublisher))
            return true;

        if (candidate.IsArray)
            return IsRawBrowserAuthority(candidate.GetElementType()!);
        if (!candidate.IsGenericType)
            return false;
        return candidate.GetGenericArguments().Any(IsRawBrowserAuthority);
    }
}
