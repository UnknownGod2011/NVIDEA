using System.Reflection;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadStagingGuardApiSurfaceTests
{
    [Fact]
    public void PathBackedStagingAuthorityHasNoPublicConstructor()
    {
        var publicConstructors = typeof(BrowserDownloadStagingGuard)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        Assert.Empty(publicConstructors);
    }

    [Fact]
    public void TrustedCoreConstructionSeamRemainsAssemblyInternal()
    {
        var constructor = typeof(BrowserDownloadStagingGuard).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(BrowserDownloadStagingOptions) },
            modifiers: null);

        Assert.NotNull(constructor);
        Assert.True(constructor!.IsAssembly);
    }

    [Fact]
    public void PublicTypeDoesNotExposeStaticFactoryThatReconstructsAuthority()
    {
        var factories = typeof(BrowserDownloadStagingGuard)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => method.ReturnType == typeof(BrowserDownloadStagingGuard));

        Assert.Empty(factories);
    }
}
