namespace Nvidea.Core.Security;

/// <summary>
/// Creates HTTP clients for credential-bearing provider calls. Redirects are deliberately disabled
/// so bearer tokens, API keys, prompts, research payloads, and other private request content cannot
/// be forwarded to a different origin by an HTTP redirect.
/// </summary>
internal static class ProviderHttpClientFactory
{
    internal static HttpClient CreateNoRedirectClient()
    {
        return new HttpClient(CreateNoRedirectHandler(), disposeHandler: true);
    }

    internal static HttpClientHandler CreateNoRedirectHandler()
    {
        return new HttpClientHandler
        {
            AllowAutoRedirect = false
        };
    }
}
