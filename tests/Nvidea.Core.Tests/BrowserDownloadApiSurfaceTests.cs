using System.Reflection;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadApiSurfaceTests
{
    [Fact]
    public void LowLevelExportPrimitiveIsNotPublicApi()
    {
        var export = typeof(BrowserDownloadQuarantine).GetMethod(
            "ExportAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(export);
        Assert.False(export!.IsPublic);
        Assert.True(export.IsAssembly);
    }
}
