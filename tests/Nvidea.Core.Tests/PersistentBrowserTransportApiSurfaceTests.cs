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

    [Fact]
    public void TransportPolicy_AllowsHttpsAndLoopbackButRejectsRemotePlaintext()
    {
        var policy = new BrowserSafetyPolicy();

        Assert.True(policy.EvaluateObservedLocation(new Uri("https://example.com/path")).Allowed);
        Assert.True(policy.EvaluateObservedLocation(new Uri("http://localhost:8080/path")).Allowed);
        Assert.True(policy.EvaluateObservedLocation(new Uri("http://127.0.0.1:8080/path")).Allowed);
        Assert.False(policy.EvaluateObservedLocation(new Uri("http://example.com/path")).Allowed);
        Assert.False(policy.EvaluateObservedLocation(new Uri("file:///C:/sensitive.txt")).Allowed);
    }

    [Fact]
    public void WebSocketTransportPolicy_AllowsWssAndLoopbackWsButRejectsRemotePlaintext()
    {
        var policy = new BrowserSafetyPolicy();

        Assert.True(policy.EvaluateWebSocketTransport(new Uri("wss://example.com/socket")).Allowed);
        Assert.True(policy.EvaluateWebSocketTransport(new Uri("ws://localhost:8080/socket")).Allowed);
        Assert.True(policy.EvaluateWebSocketTransport(new Uri("ws://127.0.0.1:8080/socket")).Allowed);
        Assert.True(policy.EvaluateWebSocketTransport(new Uri("ws://[::1]:8080/socket")).Allowed);
        Assert.False(policy.EvaluateWebSocketTransport(new Uri("ws://example.com/socket")).Allowed);
        Assert.False(policy.EvaluateWebSocketTransport(new Uri("https://example.com/not-a-websocket")).Allowed);
        Assert.False(policy.EvaluateWebSocketTransport(new Uri("file:///C:/sensitive.txt")).Allowed);
    }
}
