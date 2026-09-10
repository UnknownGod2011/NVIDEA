using System.Reflection;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class PersistentBrowserTransportApiSurfaceTests
{
    [Fact]
    public void PersistentBrowserTransport_IsNotExportedFromCoreAssembly()
    {
        Assert.False(typeof(PersistentBrowserContextFactory).IsPublic);
        Assert.False(typeof(PersistentBrowserContextSession).IsPublic);

        var exported = typeof(PersistentBrowserContextFactory).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(typeof(PersistentBrowserContextFactory).FullName, exported);
        Assert.DoesNotContain(typeof(PersistentBrowserContextSession).FullName, exported);
    }

    [Fact]
    public void TrustedCoreTestAssembly_RetainsInternalLaunchSeam()
    {
        var launchMethods = typeof(PersistentBrowserContextFactory)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.Name == "LaunchAsync")
            .ToArray();

        Assert.NotEmpty(launchMethods);
        Assert.All(launchMethods, method =>
            Assert.Equal(typeof(PersistentBrowserContextSession), method.ReturnType.GetGenericArguments().Single()));
    }
}
