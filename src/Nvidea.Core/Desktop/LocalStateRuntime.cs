using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Read-only trusted surface for inspecting protected local NVIDEA state without initializing
/// Playwright, Chromium, browser capabilities, approval grants, or filesystem mutation services.
/// The public API intentionally exposes only payload-free audit retention telemetry.
/// </summary>
public sealed class LocalStateRuntime
{
    private readonly BoundedSegmentedAuditTrail _audit;

    internal LocalStateRuntime(string browserStateDirectory)
    {
        if (string.IsNullOrWhiteSpace(browserStateDirectory))
            throw new ArgumentException("Browser state directory is required.", nameof(browserStateDirectory));

        var root = Path.GetFullPath(browserStateDirectory);
        _audit = new BoundedSegmentedAuditTrail(Path.Combine(root, "audit.jsonl"));
    }

    internal LocalStateRuntime(BoundedSegmentedAuditTrail audit)
    {
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    /// <summary>
    /// Verifies protected audit state, completes any already-authorized retention recovery, and
    /// returns payload-free retention/storage telemetry. It cannot append audit events or confer
    /// browser, approval, export, discard, or delete authority.
    /// </summary>
    public Task<AuditRetentionStatus> GetAuditRetentionStatusAsync(
        CancellationToken cancellationToken = default) =>
        _audit.GetRetentionStatusAsync(cancellationToken);
}
