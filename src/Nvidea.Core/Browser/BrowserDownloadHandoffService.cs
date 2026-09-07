using System.Security.Cryptography;
using System.Text;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Browser;

public sealed record BrowserDownloadHandoffPlan(
    Guid DownloadId,
    string DestinationDirectory,
    string DestinationPath,
    PermissionDecision Decision,
    string ActionId,
    string Summary);

/// <summary>
/// Consequential boundary for releasing a quarantined browser download into user-visible storage.
/// The exact download + destination is bound into a single-use approval scope. Approval material is
/// never persisted; failed or ambiguous exports require a fresh user approval.
/// </summary>
public sealed class BrowserDownloadHandoffService
{
    public const string CapabilityId = "browser.download.handoff";

    private readonly BrowserDownloadQuarantine _quarantine;
    private readonly ICapabilityPermissionPolicy _policy;
    private readonly ScopedApprovalAuthorizer _approvals;
    private readonly IAuditTrail _audit;

    public BrowserDownloadHandoffService(
        BrowserDownloadQuarantine quarantine,
        ICapabilityPermissionPolicy policy,
        ScopedApprovalAuthorizer approvals,
        IAuditTrail audit)
    {
        _quarantine = quarantine ?? throw new ArgumentNullException(nameof(quarantine));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _approvals = approvals ?? throw new ArgumentNullException(nameof(approvals));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<BrowserDownloadHandoffPlan> PrepareAsync(
        Guid downloadId,
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        if (downloadId == Guid.Empty)
            throw new ArgumentException("Download id is required.", nameof(downloadId));
        if (string.IsNullOrWhiteSpace(destinationDirectory))
            throw new ArgumentException("Destination directory is required.", nameof(destinationDirectory));

        var destinationRoot = Path.GetFullPath(destinationDirectory);
        if (!Directory.Exists(destinationRoot))
            throw new DirectoryNotFoundException("The download destination must already exist before approval is requested.");

        var record = await _quarantine.GetAsync(downloadId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Download '{downloadId}' was not found.");
        if (record.State is not (BrowserDownloadState.Ready or BrowserDownloadState.Exported))
            throw new InvalidOperationException($"Download is {record.State} and cannot be handed off.");

        var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, record.SuggestedFileName));
        EnsureDescendant(destinationRoot, destinationPath);
        var actionId = BuildActionId(downloadId, destinationPath);
        var invocation = new CapabilityInvocation(
            CapabilityId,
            actionId,
            new HashSet<DataPermission> { DataPermission.FilesWrite },
            CapabilityRiskLevel.High,
            Consequential: true,
            UntrustedSource: record.SourceUri.Host,
            Summary: $"Export quarantined download '{record.SuggestedFileName}' to the selected folder.");
        var decision = _policy.Evaluate(invocation);
        if (!decision.Allowed || !decision.RequiresApproval || string.IsNullOrWhiteSpace(decision.ApprovalScope))
            throw new InvalidOperationException("Download handoff policy must require an exact approval scope.");

        return new BrowserDownloadHandoffPlan(
            downloadId,
            destinationRoot,
            destinationPath,
            decision,
            actionId,
            invocation.Summary!);
    }

    public async Task<BrowserDownloadExportReceipt> ExportAsync(
        BrowserDownloadHandoffPlan approvedPlan,
        ApprovalGrant? approval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approvedPlan);

        // Rebuild policy input at the last possible moment. A caller cannot alter the destination,
        // action id, or decision after showing the confirmation UI and reuse an old approval.
        var current = await PrepareAsync(
            approvedPlan.DownloadId,
            approvedPlan.DestinationDirectory,
            cancellationToken).ConfigureAwait(false);
        if (!Equivalent(approvedPlan, current))
        {
            await AuditAsync(current, "download.handoff.scope_changed", approved: false,
                "Download handoff scope changed after confirmation was prepared.", cancellationToken).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Download handoff scope changed; request fresh user approval.");
        }

        if (approval is null || !_approvals.TryAuthorize(approval, current.Decision))
        {
            await AuditAsync(current, "download.handoff.awaiting_approval", approved: false,
                "Exact single-use approval is required before the quarantined file can leave NVIDEA state.", cancellationToken).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Exact single-use approval is required for this download and destination.");
        }

        await AuditAsync(current, "download.handoff.started", approved: true,
            "Approved download handoff started.", cancellationToken).ConfigureAwait(false);
        try
        {
            // BrowserDownloadQuarantine still owns byte/hash/path verification and atomic copy.
            // This service is the authorizing boundary and is the only production caller that
            // should pass the quarantine's trusted boolean after consuming the scoped grant.
            var receipt = await _quarantine.ExportAsync(
                current.DownloadId,
                current.DestinationDirectory,
                userApproved: true,
                cancellationToken).ConfigureAwait(false);
            await AuditAsync(current, "download.handoff.succeeded", approved: true,
                "Approved download handoff completed and destination bytes were verified.", cancellationToken).ConfigureAwait(false);
            return receipt;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AuditAsync(current, "download.handoff.cancelled", approved: true,
                "Approved download handoff was cancelled; approval remains consumed.", CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await AuditAsync(current, "download.handoff.failed", approved: true,
                $"Approved download handoff failed ({ex.GetType().Name}); approval remains consumed.", cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private Task AuditAsync(
        BrowserDownloadHandoffPlan plan,
        string eventType,
        bool approved,
        string summary,
        CancellationToken cancellationToken) =>
        _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            CapabilityId,
            plan.ActionId,
            eventType,
            plan.Decision.EffectiveRisk,
            Allowed: true,
            Approved: approved,
            plan.Decision.ApprovalScope,
            summary,
            new Dictionary<string, string>
            {
                ["downloadId"] = plan.DownloadId.ToString("N"),
                ["destinationFingerprint"] = Fingerprint(plan.DestinationPath),
                ["permissions"] = string.Join(',', plan.Decision.EffectivePermissions.OrderBy(x => x).Select(x => x.ToString()))
            }), cancellationToken);

    private static bool Equivalent(BrowserDownloadHandoffPlan left, BrowserDownloadHandoffPlan right) =>
        left.DownloadId == right.DownloadId
        && string.Equals(left.DestinationDirectory, right.DestinationDirectory, PathComparison)
        && string.Equals(left.DestinationPath, right.DestinationPath, PathComparison)
        && string.Equals(left.ActionId, right.ActionId, StringComparison.Ordinal)
        && string.Equals(left.Decision.ApprovalScope, right.Decision.ApprovalScope, StringComparison.Ordinal);

    private static string BuildActionId(Guid downloadId, string destinationPath) =>
        $"export:{downloadId:N}:{Fingerprint(destinationPath)}";

    private static string Fingerprint(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static void EnsureDescendant(string directory, string path)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, PathComparison))
            throw new UnauthorizedAccessException("Download handoff path escaped the selected destination directory.");
    }
}
