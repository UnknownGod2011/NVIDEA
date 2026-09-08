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

        _statusReader = new AuditRetentionStatusReader(
            auditPath,
            protector,
            retention ?? AuditRetentionPolicy.Default);
    }

    /// <summary>
    /// Returns payload-free retention/storage telemetry using only read operations. This surface
    /// cannot initialize a browser, append audit events, create approval grants, export/discard
    /// downloads, or delete/repair local state.
    /// </summary>
    public Task<AuditRetentionStatus> GetAuditRetentionStatusAsync(
        CancellationToken cancellationToken = default) =>
        _statusReader.ReadAsync(cancellationToken);
}
