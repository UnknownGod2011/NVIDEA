using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Read-only trusted surface for inspecting protected local NVIDEA state without initializing
/// Playwright, Chromium, browser capabilities, approval grants, audit appenders, or filesystem
/// mutation services. The public API intentionally exposes only passive local-state telemetry.
/// </summary>
public sealed class LocalStateRuntime
{
    private readonly AuditRetentionStatusReader _statusReader;
    private readonly SemaphoreSlim _auditGate;
    private readonly BrowserDownloadSnapshotReader _downloadReader;

    internal LocalStateRuntime(string browserStateDirectory)
        : this(
            Path.GetFullPath(string.IsNullOrWhiteSpace(browserStateDirectory)
                ? throw new ArgumentException("Browser state directory is required.", nameof(browserStateDirectory))
                : browserStateDirectory),
            protector: null,
            retention: AuditRetentionPolicy.Default,
            downloadOptions: null)
    {
    }

    internal LocalStateRuntime(
        string browserStateDirectory,
        ILocalStateProtector? protector,
        AuditRetentionPolicy? retention = null,
        BrowserDownloadQuarantineOptions? downloadOptions = null)
    {
        if (string.IsNullOrWhiteSpace(browserStateDirectory))
            throw new ArgumentException("Browser state directory is required.", nameof(browserStateDirectory));

        var browserRoot = Path.GetFullPath(browserStateDirectory);
        var auditPath = Path.Combine(browserRoot, "audit.jsonl");
        var effectiveRetention = retention ?? AuditRetentionPolicy.Default;
        _statusReader = new AuditRetentionStatusReader(auditPath, protector, effectiveRetention);
        _auditGate = BoundedSegmentedAuditTrail.GetSynchronizationGate(auditPath);
        _downloadReader = new BrowserDownloadSnapshotReader(browserRoot, downloadOptions, protector);
    }

    /// <summary>
    /// Returns payload-free retention/storage telemetry using only read operations. This surface
    /// cannot initialize a browser, append audit events, create approval grants, export/discard
    /// downloads, or delete/repair local state. The shared audit gate only prevents this read from
    /// racing a browser append/rotation/prune in the same process; it confers no mutation method.
    /// </summary>
    public async Task<AuditRetentionStatus> GetAuditRetentionStatusAsync(
        CancellationToken cancellationToken = default)
    {
        await _auditGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await _statusReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _auditGate.Release();
        }
    }

    /// <summary>
    /// Returns a sanitized snapshot of stable retained browser downloads plus quota usage without
    /// launching Chromium or performing quarantine recovery. Receiving entries are not exposed as
    /// usable artifacts; they are counted only as pending recovery so the higher-authority browser
    /// runtime can reconcile them later. No payload bytes, exported paths, failure details, full
    /// source URLs, deletion methods, or approval objects cross this boundary.
    /// </summary>
    public Task<BrowserDownloadSnapshot> GetBrowserDownloadSnapshotAsync(
        CancellationToken cancellationToken = default) =>
        _downloadReader.ReadAsync(cancellationToken);
}
