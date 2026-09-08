using Microsoft.Playwright;

namespace Nvidea.Core.Browser;

/// <summary>
/// Creates a Chromium persistent context rooted exclusively in the NVIDEA-owned browser profile.
/// Existing tabs are never adopted on startup: browser-managed authenticated/profile state such as
/// cookies and local storage may persist, but every runtime begins on a fresh explicitly-permitted
/// page so stale tabs cannot silently become agent-visible context. Browser downloads are accepted
/// only into NVIDEA-owned bounded staging/quarantine and require a separate explicit export handoff.
/// </summary>
public static class PersistentBrowserContextFactory
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
        ArgumentNullException.ThrowIfNull(playwright);
        ArgumentNullException.ThrowIfNull(startUri);
        ArgumentNullException.ThrowIfNull(driverOptions);
        ArgumentNullException.ThrowIfNull(quarantineOptions);
        quarantineOptions.Validate();

        if (!startUri.IsAbsoluteUri || startUri.Scheme is not ("http" or "https"))
            throw new ArgumentException("Browser start URI must be absolute HTTP(S).", nameof(startUri));

        var profileDirectory = BrowserProfileOwnership.PrepareOwnedProfile(stateDirectory);
        BrowserProfileOwnership.ValidateOwnedProfile(stateDirectory, profileDirectory);

        var downloads = new BrowserDownloadQuarantine(stateDirectory, quarantineOptions);
        var effectiveStagingOptions = stagingOptions ?? new BrowserDownloadStagingOptions(
            MaxStagingBytes: quarantineOptions.MaxSingleDownloadBytes,
            MaxPartialBytes: quarantineOptions.MaxSingleDownloadBytes);
        effectiveStagingOptions.Validate();
        var staging = new BrowserDownloadStagingGuard(stateDirectory, effectiveStagingOptions);

        // No browser context exists yet, so every file in NVIDEA's dedicated Playwright staging
        // directory is necessarily a crash/abnormal-shutdown leftover. Reclaim it before launch so
        // stale bytes cannot consume the next transfer's transient quota. Reclamation itself is
        // fail-closed and refuses recursive/reparse-point deletion.
        staging.ReclaimStartupLeftovers();

        IBrowserContext? context = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            context = await playwright.Chromium.LaunchPersistentContextAsync(
                profileDirectory,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = headless,
                    AcceptDownloads = true,
                    DownloadsPath = staging.StagingDirectory
                }).WaitAsync(cancellationToken).ConfigureAwait(false);

            // Chromium may restore pages from a previous persistent-context run. Keep the useful
            // authenticated/profile state, but never trust restored tabs as current agent context.
            // Closing these pages is not a site action and does not clear profile credentials.
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

public sealed record PersistentBrowserContextSession(
    string ProfileDirectory,
    IBrowserContext Context,
    PlaywrightBrowserSessionDriver Driver,
    BrowserDownloadQuarantine Downloads,
    BrowserDownloadStagingGuard DownloadStaging);
