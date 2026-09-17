using Microsoft.Playwright;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Browser;

/// <summary>
/// Creates a Chromium persistent context rooted exclusively in the NVIDEA-owned browser profile.
/// Existing tabs are never adopted on startup: browser-managed authenticated/profile state such as
/// cookies and local storage may persist, but every runtime begins on a fresh explicitly-permitted
/// page so stale tabs cannot silently become agent-visible context. Browser downloads are accepted
/// only into NVIDEA-owned bounded staging/quarantine and require a separate explicit export handoff.
/// This raw transport boundary is assembly-internal so product/plugin code cannot obtain direct
/// Playwright execution authority around BrowserProductRuntime and BrowserHostRuntime policy gates.
/// </summary>
internal static class PersistentBrowserContextFactory
{
    public static Task<PersistentBrowserContextSession> LaunchAsync(
        IPlaywright playwright,
        string stateDirectory,
        Uri startUri,
        PlaywrightBrowserDriverOptions driverOptions,
        bool headless,
        CancellationToken cancellationToken = default) =>
        LaunchAsync(
            playwright,
            stateDirectory,
            startUri,
            driverOptions,
            headless,
            new BrowserDownloadQuarantineOptions(),
            stagingOptions: null,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Internal configuration seam used by deterministic integration coverage and composition tests.
    /// Production callers continue through the public overload and therefore retain conservative
    /// default quotas. Keeping this overload internal prevents UI/model code from casually widening
    /// transient or retained download limits at runtime.
    /// </summary>
    internal static async Task<PersistentBrowserContextSession> LaunchAsync(
        IPlaywright playwright,
        string stateDirectory,
        Uri startUri,
        PlaywrightBrowserDriverOptions driverOptions,
        bool headless,
        BrowserDownloadQuarantineOptions quarantineOptions,
        BrowserDownloadStagingOptions? stagingOptions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
            throw new ArgumentException("State directory is required.", nameof(stateDirectory));

        var fullStateDirectory = Path.GetFullPath(stateDirectory);
        var stateLease = StateDirectoryLease.Acquire(fullStateDirectory);
        try
        {
            return await LaunchOwnedAsync(
                playwright,
                fullStateDirectory,
                startUri,
                driverOptions,
                headless,
                quarantineOptions,
                stagingOptions,
                stateLease,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            stateLease.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Takes ownership of an already-acquired state-directory lease. This seam lets higher-level
    /// composition acquire single-owner state before starting the Playwright transport while keeping
    /// exactly one lease for the persistent-context lifetime. The lease is always released on failed
    /// initialization and otherwise remains attached to the Chromium context until Close.
    /// </summary>
    internal static async Task<PersistentBrowserContextSession> LaunchOwnedAsync(
        IPlaywright playwright,
        string stateDirectory,
        Uri startUri,
        PlaywrightBrowserDriverOptions driverOptions,
        bool headless,
        BrowserDownloadQuarantineOptions quarantineOptions,
        BrowserDownloadStagingOptions? stagingOptions,
        StateDirectoryLease stateLease,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stateLease);

        IBrowserContext? context = null;
        try
        {
            ArgumentNullException.ThrowIfNull(playwright);
            ArgumentNullException.ThrowIfNull(startUri);
            ArgumentNullException.ThrowIfNull(driverOptions);
            ArgumentNullException.ThrowIfNull(quarantineOptions);
            quarantineOptions.Validate();

            if (string.IsNullOrWhiteSpace(stateDirectory))
                throw new ArgumentException("State directory is required.", nameof(stateDirectory));

            var transportPolicy = new BrowserSafetyPolicy();
            if (!transportPolicy.EvaluateObservedLocation(startUri).Allowed)
                throw new ArgumentException("Browser start URI must use HTTPS or HTTP loopback.", nameof(startUri));

            var fullStateDirectory = Path.GetFullPath(stateDirectory);
            var normalizedRequested = Path.TrimEndingDirectorySeparator(fullStateDirectory);
            var normalizedOwned = Path.TrimEndingDirectorySeparator(Path.GetFullPath(stateLease.StateDirectory));
            if (!string.Equals(normalizedRequested, normalizedOwned, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Transferred browser state lease does not own the requested NVIDEA state directory.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var profileDirectory = BrowserProfileOwnership.PrepareOwnedProfile(fullStateDirectory);
            BrowserProfileOwnership.ValidateOwnedProfile(fullStateDirectory, profileDirectory);

            var downloads = new BrowserDownloadQuarantine(fullStateDirectory, quarantineOptions);
            var effectiveStagingOptions = stagingOptions ?? new BrowserDownloadStagingOptions(
                MaxStagingBytes: quarantineOptions.MaxSingleDownloadBytes,
                MaxPartialBytes: quarantineOptions.MaxSingleDownloadBytes);
            effectiveStagingOptions.Validate();
            var staging = new BrowserDownloadStagingGuard(fullStateDirectory, effectiveStagingOptions);
            staging.ReclaimStartupLeftovers();

            context = await playwright.Chromium.LaunchPersistentContextAsync(
                profileDirectory,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = headless,
                    AcceptDownloads = true,
                    DownloadsPath = staging.StagingDirectory,
                    // Playwright documents that request routing does not intercept Service Worker
                    // traffic. Blocking workers is therefore part of the transport boundary rather
                    // than an optimization: authenticated state must not bypass the HTTPS policy.
                    ServiceWorkers = ServiceWorkerPolicy.Block
                }).WaitAsync(cancellationToken).ConfigureAwait(false);

            context.Close += (_, _) => stateLease.Dispose();

            // Enforce transport before network dispatch for every page/popup in this context. Context
            // routing covers redirects as separate requests; unsafe requests are aborted before cookies,
            // form data, or authenticated state can cross a remote plaintext channel. The executor's
            // post-action observed-location check remains as independent defense in depth.
            await context.RouteAsync("**/*", async route =>
            {
                if (Uri.TryCreate(route.Request.Url, UriKind.Absolute, out var requestUri)
                    && transportPolicy.EvaluateObservedLocation(requestUri).Allowed)
                {
                    await route.ContinueAsync().ConfigureAwait(false);
                    return;
                }

                await route.AbortAsync("blockedbyclient").ConfigureAwait(false);
            }).WaitAsync(cancellationToken).ConfigureAwait(false);

            foreach (var existing in context.Pages.ToArray())
                await SafeCloseAsync(existing).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            var page = await context.NewPageAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            await page.GotoAsync(startUri.AbsoluteUri, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = driverOptions.ActionTimeoutMilliseconds
            }).WaitAsync(cancellationToken).ConfigureAwait(false);

            var driver = new PlaywrightBrowserSessionDriver(context, page, driverOptions, downloads, staging);
            return new PersistentBrowserContextSession(profileDirectory, context, driver, downloads, staging);
        }
        catch
        {
            if (context is not null)
                await SafeCloseAsync(context).ConfigureAwait(false);
            stateLease.Dispose();
            throw;
        }
    }

    private static async Task SafeCloseAsync(IPage page)
    {
        try { await page.CloseAsync(new PageCloseOptions { RunBeforeUnload = false }).ConfigureAwait(false); }
        catch { }
    }

    private static async Task SafeCloseAsync(IBrowserContext context)
    {
        try { await context.CloseAsync().ConfigureAwait(false); }
        catch { }
    }
}

internal sealed record PersistentBrowserContextSession(
    string ProfileDirectory,
    IBrowserContext Context,
    PlaywrightBrowserSessionDriver Driver,
    BrowserDownloadQuarantine Downloads,
    BrowserDownloadStagingGuard DownloadStaging);
