using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Read-only trusted surface for inspecting protected local NVIDEA state without initializing
/// Playwright, Chromium, browser capabilities, approval grants, audit appenders, or filesystem
/// mutation services. The public API intentionally exposes only payload-free audit retention telemetry.
/// </summary>
public sealed class LocalStateRuntime
{
    private readonly AuditRetentionStatusReader _statusReader;
    private readonly SemaphoreSlim _auditGate;

    internal LocalStateRuntime(string browserStateDirectory)
        : this(
            Path.Combine(
                Path.GetFullPath(string.IsNullOrWhiteSpace(browserStateDirectory)
                    ? throw new ArgumentException("Browser state directory is required.", nameof(browserStateDirectory))
                    : browserStateDirectory),
                "audit.jsonl"),
            protector: null,
            retention: AuditRetentionPolicy.Default)
    {
    }

    internal LocalStateRuntime(
        string auditPath,
        ILocalStateProtector? protector,
        AuditRetentionPolicy? retention = null)
    {
        if (string.IsNullOrWhiteSpace(auditPath))
            throw new ArgumentException("Audit path is required.", nameof(auditPath));

        var effectiveRetention = retention ?? AuditRetentionPolicy.Default;
        _statusReader = new AuditRetentionStatusReader(auditPath, protector, effectiveRetention);
        _auditGate = BoundedSegmentedAuditTrail.GetSynchronizationGate(auditPath);
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
}
