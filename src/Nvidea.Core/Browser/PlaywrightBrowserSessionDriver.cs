using Microsoft.Playwright;

namespace Nvidea.Core.Browser;

/// <summary>
/// Session-aware browser driver that keeps the agent on the newest permitted page in a Playwright
/// context. Popups/new tabs are adopted only after their URL satisfies the same HTTP(S)/host
/// boundary as normal navigation. Cross-boundary popups are closed without being observed or
/// interacted with by the agent.
/// </summary>
public sealed class PlaywrightBrowserSessionDriver : IBrowserDriver
{
    private readonly IBrowserContext _context;
    private readonly PlaywrightBrowserDriverOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPage _activePage;

    public PlaywrightBrowserSessionDriver(
        IBrowserContext context,
        IPage initialPage,
        PlaywrightBrowserDriverOptions? options = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _activePage = initialPage ?? throw new ArgumentNullException(nameof(initialPage));
        _options = options ?? new PlaywrightBrowserDriverOptions();

        if (!_context.Pages.Any(page => ReferenceEquals(page, initialPage)))
            throw new ArgumentException("Initial page must belong to the supplied browser context.", nameof(initialPage));

        ValidatePermittedPage(initialPage);
    }

    public async Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
    {
        var page = await ResolveActivePageAsync(cancellationToken).ConfigureAwait(false);
        return await CreatePageDriver(page).ObserveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(BrowserAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        var page = await ResolveActivePageAsync(cancellationToken).ConfigureAwait(false);
        await CreatePageDriver(page).ExecuteAsync(action, cancellationToken).ConfigureAwait(false);

        // A click can synchronously create a popup/new tab. Resolve again before returning so the
        // next verifier observation is attached to the intended permitted page rather than the
        // opener. This does not execute any action in the newly-created page.
        if (action.Kind == BrowserActionKind.Click)
            await ResolveActivePageAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserSessionSnapshot> GetSessionSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var active = await ResolveActivePageAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var pages = _context.Pages
                .Where(page => !page.IsClosed)
                .Select(page => new BrowserSessionPage(
                    ReferenceEquals(page, active),
                    TryParseWebUri(page.Url, out var uri) ? uri : null,
                    IsPermitted(page.Url)))
                .ToArray();
            return new BrowserSessionSnapshot(pages);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IPage> ResolveActivePageAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pages = _context.Pages.Where(page => !page.IsClosed).ToArray();
            if (pages.Length == 0)
                throw new InvalidOperationException("Browser context has no open pages.");

            // Playwright exposes context pages in creation order. Prefer the newest permitted page
            // so target=_blank/window.open flows naturally become the next observed page.
            for (var index = pages.Length - 1; index >= 0; index--)
            {
                var candidate = pages[index];
                if (IsPermitted(candidate.Url))
                {
                    _activePage = candidate;
                    break;
                }

                if (TryParseWebUri(candidate.Url, out _))
                    await SafeCloseAsync(candidate).ConfigureAwait(false);
            }

            if (_activePage.IsClosed || !IsPermitted(_activePage.Url))
            {
                var fallback = _context.Pages.LastOrDefault(page => !page.IsClosed && IsPermitted(page.Url));
                _activePage = fallback
                    ?? throw new InvalidOperationException("No open browser page remains inside the permitted host boundary.");
            }

            return _activePage;
        }
        finally
        {
            _gate.Release();
        }
    }

    private PlaywrightBrowserDriver CreatePageDriver(IPage page) => new(page, _options);

    private void ValidatePermittedPage(IPage page)
    {
        if (!IsPermitted(page.Url))
            throw new InvalidOperationException("Initial browser page is outside the permitted web boundary.");
    }

    private bool IsPermitted(string rawUrl)
    {
        if (!TryParseWebUri(rawUrl, out var uri))
            return false;

        var allowed = _options.NormalizedAllowedHosts;
        return allowed.Count == 0 || allowed.Contains(uri.IdnHost);
    }

    private static bool TryParseWebUri(string rawUrl, out Uri uri)
    {
        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var parsed)
            && parsed.Scheme is "http" or "https")
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
    }

    private static async Task SafeCloseAsync(IPage page)
    {
        try { await page.CloseAsync(new PageCloseOptions { RunBeforeUnload = false }).ConfigureAwait(false); }
        catch { }
    }
}

public sealed record BrowserSessionSnapshot(IReadOnlyList<BrowserSessionPage> Pages)
{
    public BrowserSessionPage ActivePage => Pages.Single(page => page.IsActive);
}

public sealed record BrowserSessionPage(bool IsActive, Uri? Url, bool IsPermitted);
