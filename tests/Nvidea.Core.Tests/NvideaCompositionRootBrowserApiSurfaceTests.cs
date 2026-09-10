using System.Reflection;
using Nvidea.Core.Desktop;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class NvideaCompositionRootBrowserApiSurfaceTests
{
    [Fact]
    public void CompositionRootExposesProductBrowserBoundaryButNotRawBrowserHost()
    {
        var publicMembers = typeof(NvideaCompositionRoot)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(
            publicMembers,
            static member => member is MethodInfo method
                && method.Name == nameof(NvideaCompositionRoot.GetBrowserProductAsync)
                && ContainsType(method.ReturnType, typeof(BrowserProductRuntime)));

        Assert.DoesNotContain(
            publicMembers,
            static member => member switch
            {
                PropertyInfo property => ContainsType(property.PropertyType, typeof(BrowserHostRuntime)),
                MethodInfo method => ContainsType(method.ReturnType, typeof(BrowserHostRuntime))
                    || method.GetParameters().Any(static parameter => ContainsType(parameter.ParameterType, typeof(BrowserHostRuntime))),
                _ => false
            });
    }

    [Fact]
    public void ProductBrowserBoundaryKeepsLowLevelLifecycleAndObservationMethodsPrivate()
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(BrowserProductRuntime.StartActionAsync),
            nameof(BrowserProductRuntime.ApproveAndResumeAsync),
            nameof(BrowserProductRuntime.CancelAsync),
            nameof(BrowserProductRuntime.ListDownloadsAsync),
            nameof(BrowserProductRuntime.PrepareDownloadHandoffAsync),
            nameof(BrowserProductRuntime.ApproveAndExportDownloadAsync),
            nameof(BrowserProductRuntime.PrepareDownloadDiscardAsync),
            nameof(BrowserProductRuntime.ApproveAndDiscardDownloadAsync)
        };

        var declaredPublicMethods = typeof(BrowserProductRuntime)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.NotEmpty(declaredPublicMethods);
        Assert.All(declaredPublicMethods, method => Assert.Contains(method.Name, allowed));

        Assert.DoesNotContain(declaredPublicMethods, static method => method.Name is
            "CreateActionAsync" or
            "AdvanceActionAsync" or
            "RearmApprovalAsync" or
            "TryReconcileAmbiguousAsync" or
            "ObserveAsync" or
            "GetSessionSnapshotAsync" or
            "GetAuditRetentionStatusAsync");
    }

    private static bool ContainsType(Type candidate, Type expected)
    {
        if (candidate == expected)
            return true;
        if (candidate.IsArray)
            return ContainsType(candidate.GetElementType()!, expected);
        if (!candidate.IsGenericType)
            return false;
        return candidate.GetGenericArguments().Any(argument => ContainsType(argument, expected));
    }
}
