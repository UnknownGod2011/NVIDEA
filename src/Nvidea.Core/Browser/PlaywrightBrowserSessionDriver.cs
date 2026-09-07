using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace Nvidea.Core.Browser;

/// <summary>
/// Session-aware browser driver that keeps the agent on newly-created permitted pages in a
/// Playwright context. Popups/new tabs are adopted only after their URL satisfies the same
/// HTTP(S)/host boundary as normal navigation. Cross-boundary popups are closed without being
/// observed or interacted with by the agent. Browser downloads are captured into the configured
/// NVIDEA quarantine before a Download action is allowed to return successfully.
/// </summary>
public sealed class PlaywrightBrowserSessionDriver : IBrowserDriver
{
    private readonly IBrowserContext _context;
    private readonly PlaywrightBrowserDriverOptions _options;
    private readonly BrowserDownloadQuarantine? _downloads;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _actionGate = new(1, 1);
    private readonly ConcurrentQueue<IPage> _newPages = new();
    private readonly ConcurrentQueue<PendingDownloadCapture> _downloadCaptures = new();
    private long _downloadSequence;
    private IPage _activePage;

    public PlaywrightBrowserSessionDriver(
        IBrowserContext context,
        IPage initialPage,
        PlaywrightBrowserDriverOptions? options = null,
        BrowserDownloadQuarantine? downloads = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _activePage = initialPage ?? throw new ArgumentNullException(nameof(initialPage));
        _options = options ?? new PlaywrightBrowserDriverOptions();
        _downloads = downloads;

        if (!_context.Pages.Any(page => ReferenceEquals(page, initialPage)))
            throw new ArgumentException("Initial page must belong to the supplied browser context.", nameof(initialPage));

        ValidatePermittedPage(initialPage);
        foreach (var page in _context.Pages)
            SubscribeToPage(page);
        _context.Page += OnPageCreated;
    }

    public async Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
    {
        var page = await ResolveActivePageAsync(cancellationToken).ConfigureAwait(false);
        return await CreatePageDriver(page).ObserveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(BrowserAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _actionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (action.Kind == BrowserActionKind.Download && _downloads is null)
                throw new InvalidOperationException("Browser downloads require an NVIDEA quarantine boundary.");

            var page = await ResolveActivePageAsync(cancellationToken).ConfigureAwait(false);
            var downloadBaseline = Interlocked.Read(ref _downloadSequence);
            await CreatePageDriver(page).ExecuteAsync(action, cancellationToken).ConfigureAwait(false);

            if (action.Kind == BrowserActionKind.Download)
            {
                // Playwright emits Page.Download when a transfer starts, while SaveAsAsync waits for
                // the bytes to finish. Do not report the browser action as complete until a capture
                // initiated by this active page after the click has reached durable quarantine.
                await AwaitDownloadCaptureAsync(page, downloadBaseline, cancellationToken).ConfigureAwait(false);
            }

            // A click can synchronously create a popup/new tab whose Page event fires while its URL is
            // still about:blank. Give only already-created candidate pages a short bounded window to
            // commit navigation so a permitted popup can become the verifier's next active page. This
            // never waits for or discovers unrelated future pages and never interacts with the popup.
            if (action.Kind == BrowserActionKind.Click && !_newPages.IsEmpty)
                await ResolvePostClickPageAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _actionGate.Release();
        }
    }

    public Task<IReadOnlyList<BrowserDownloadRecord>> ListDownloadsAsync(CancellationToken cancellationToken = default)
    {
        if (_downloads is null)
            return Task.FromResult<IReadOnlyList<BrowserDownloadRecord>>(Array.Empty<BrowserDownloadRecord>());
        return _downloads.ListAsync(cancellationToken);
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

    private async Task AwaitDownloadCaptureAsync(IPage sourcePage, long baseline, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(
            Math.Min(120_000, Math.Max(1_000, _options.ActionTimeoutMilliseconds)));

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pendingCount = _downloadCaptures.Count;
            for (var i = 0; i < pendingCount && _downloadCaptures.TryDequeue(out var capture); i++)
            {
                if (capture.Sequence <= baseline)
                    continue;
                if (!ReferenceEquals(capture.Page, sourcePage))
                {
                    // The capture itself continues into quarantine, but it cannot prove this action.
                    continue;
                }

                await capture.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;
            await Task.Delay(
                remaining < TimeSpan.FromMilliseconds(50) ? remaining : TimeSpan.FromMilliseconds(50),
                cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Download action did not produce a verified quarantined payload within the browser action timeout.");
    }

    private async Task ResolvePostClickPageAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(
            Math.Min(1_000, Math.Max(250, _options.ActionTimeoutMilliseconds)));

        do
        {
            await ResolveActivePageAsync(cancellationToken).ConfigureAwait(false);
            if (_newPages.IsEmpty)
                return;

            var delay = deadline - DateTimeOffset.UtcNow;
            if (delay <= TimeSpan.Zero)
                return;

            await Task.Delay(
                delay < TimeSpan.FromMilliseconds(50) ? delay : TimeSpan.FromMilliseconds(50),
                cancellationToken).ConfigureAwait(false);
        }
        while (DateTimeOffset.UtcNow < deadline);
    }

    private async Task<IPage> ResolveActivePageAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_context.Pages.Any(page => !page.IsClosed))
                throw new InvalidOperationException("Browser context has no open pages.");

            // BrowserContext.Page is emitted for newly-created tabs/popups. Consume only the pages
            // observed through that event rather than assuming BrowserContext.Pages has any
            // documented creation-order semantics.
            var pendingCount = _newPages.Count;
            for (var i = 0; i < pendingCount && _newPages.TryDequeue(out var candidate); i++)
            {
                if (candidate.IsClosed)
                    continue;

                if (IsPermitted(candidate.Url))
                {
                    _activePage = candidate;
                    continue;
                }

                if (TryParseWebUri(candidate.Url, out _))
                {
                    await SafeCloseAsync(candidate).ConfigureAwait(false);
                    continue;
                }

                // A freshly-created page may still be about:blank when the event fires. Keep it
                // pending until a subsequent browser boundary call can classify its final URL.
                _newPages.Enqueue(candidate);
            }

            if (_activePage.IsClosed || !IsPermitted(_activePage.Url))
            {
                var fallback = _context.Pages.FirstOrDefault(page => !page.IsClosed && IsPermitted(page.Url));
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

    private void OnPageCreated(object? sender, IPage page)
    {
        SubscribeToPage(page);
        if (!ReferenceEquals(page, _activePage))
            _newPages.Enqueue(page);
    }

    private void SubscribeToPage(IPage page) => page.Download += OnDownload;

    private void OnDownload(object? sender, IDownload download)
    {
        if (_downloads is null)
            return;

        var sequence = Interlocked.Increment(ref _downloadSequence);
        var page = download.Page;
        var capture = CaptureDownloadAsync(download);
        _downloadCaptures.Enqueue(new PendingDownloadCapture(sequence, page, capture));
    }

    private async Task<BrowserDownloadRecord> CaptureDownloadAsync(IDownload download)
    {
        if (_downloads is null)
            throw new InvalidOperationException("Browser download quarantine is not configured.");
        if (!TryParseWebUri(download.Page.Url, out var sourceUri) || !IsPermitted(download.Page.Url))
            throw new InvalidOperationException("Download source page is outside the permitted web boundary.");

        return await _downloads.CaptureAsync(
            sourceUri,
            download.SuggestedFilename,
            async (path, cancellationToken) =>
            {
                await download.SaveAsAsync(path).WaitAsync(cancellationToken).ConfigureAwait(false);
                var failure = await download.FailureAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(failure))
                    throw new InvalidDataException("Playwright reported that the browser download failed before quarantine completion.");
            },
            CancellationToken.None).ConfigureAwait(false);
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

    private sealed record PendingDownloadCapture(long Sequence, IPage Page, Task<BrowserDownloadRecord> Task);
}

public sealed record BrowserSessionSnapshot(IReadOnlyList<BrowserSessionPage> Pages)
{
    public BrowserSessionPage ActivePage => Pages.Single(page => page.IsActive);
}

public sealed record BrowserSessionPage(bool IsActive, Uri? Url, bool IsPermitted);
