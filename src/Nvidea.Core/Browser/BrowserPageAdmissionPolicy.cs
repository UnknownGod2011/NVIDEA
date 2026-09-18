namespace Nvidea.Core.Browser;

/// <summary>
/// Single admission predicate for pages that may become agent-visible. This deliberately composes
/// the transport policy with the optional task host allowlist so popup/tab adoption cannot become a
/// weaker boundary than request routing or explicit navigation.
/// </summary>
internal sealed class BrowserPageAdmissionPolicy
{
    private readonly BrowserSafetyPolicy _transportPolicy = new();
    private readonly IReadOnlySet<string> _allowedHosts;

    public BrowserPageAdmissionPolicy(IReadOnlySet<string>? allowedHosts = null)
    {
        _allowedHosts = new HashSet<string>(allowedHosts ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public bool IsAllowed(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return false;

        // EvaluateObservedLocation is the canonical HTTP(S) transport boundary: HTTPS or loopback
        // HTTP only, no embedded URI credentials, and no non-web schemes.
        if (!_transportPolicy.EvaluateObservedLocation(uri).Allowed)
            return false;

        return _allowedHosts.Count == 0 || _allowedHosts.Contains(uri.IdnHost);
    }
}
