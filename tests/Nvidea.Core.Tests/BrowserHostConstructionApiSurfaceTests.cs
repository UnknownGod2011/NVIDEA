using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserHostConstructionApiSurfaceTests
{
    [Fact]
    public void BrowserHostRuntime_HasNoPublicConstructionPath()
    {
        var hostType = typeof(BrowserHostRuntime);

        Assert.Empty(hostType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        var publicFactories = hostType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => hostType.IsAssignableFrom(UnwrapTaskResult(method.ReturnType)))
            .Select(method => method.Name)
            .ToArray();

        Assert.Empty(publicFactories);
    }

    private static Type UnwrapTaskResult(Type returnType)
    {
        if (returnType.IsGenericType
            && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return returnType.GetGenericArguments()[0];
        }

        return returnType;
    }
}
