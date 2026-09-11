using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class ProviderHttpClientFactoryTests
{
    [Fact]
    public void CreateNoRedirectHandler_DisablesAutomaticRedirects()
    {
        using var handler = ProviderHttpClientFactory.CreateNoRedirectHandler();

        Assert.False(handler.AllowAutoRedirect);
    }

    [Fact]
    public void CreateNoRedirectClient_CanBeConstructedAndDisposed()
    {
        using var client = ProviderHttpClientFactory.CreateNoRedirectClient();

        Assert.NotNull(client);
    }
}
