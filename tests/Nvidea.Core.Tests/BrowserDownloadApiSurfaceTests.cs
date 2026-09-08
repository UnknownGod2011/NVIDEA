using System.Reflection;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadApiSurfaceTests
{
    [Theory]
    [InlineData("ExportAsync")]
    [InlineData("DiscardAsync")]
    public void LowLevelMutationPrimitivesAreNotPublicApi(string methodName)
    {
        var method = typeof(BrowserDownloadQuarantine).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.False(method!.IsPublic);
        Assert.True(method.IsAssembly);
    }
}
