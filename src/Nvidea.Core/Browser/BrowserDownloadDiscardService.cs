using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Browser;

public sealed record BrowserDownloadDiscardPlan(
    Guid DownloadId,
    PermissionDecision Decision,
    string ActionId,
    string Summary,
    string SuggestedFileName,
    long? LengthBytes,
    string? Sha256);

public sealed record BrowserDownloadDiscardReceipt(
    Guid DownloadId,
    BrowserDownloadState PreviousState,
    DateTimeOffset DiscardedAt);

/// <summary>
/// Consequential boundary for intentionally deleting a quarantined browser payload.
/// The exact download id and verified digest are bound into a short-lived single-use approval.
/// Model/browser code cannot reclaim quota without a trusted human confirmation.
/// </summary>
public sealed class BrowserDownloadDiscardService
{
    public const string CapabilityId = "browser.download.discard";

    private readonly BrowserDownloadQuarantine _quarantine;
    private readonly ICapabilityPermissionPolicy _policy;
    private readonly ScopedApprovalAuthorizer _approvals;
    private readonly IAuditTrail _audit;

    public BrowserDownloadDiscardService(
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

    public async Task<BrowserDownloadDiscardPlan> PrepareAsync(
        Guid downloadId,
        CancellationToken cancellationToken = default)
    {
        if (downloadId == Guid.Empty)
            throw new ArgumentException("Download id is required.", nameof(downloadId));

        var record = await _quarantine.GetAsync(downloadId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Download '{downloadId}' was not found.");
        if (record.State is not (BrowserDownloadState.Ready or BrowserDownloadState.Exported))
            throw new InvalidOperationException($"Download is {record.State} and has no retained payload eligible for discard.");
        if (record.LengthBytes is null || string.IsNullOrWhiteSpace(record.Sha256))
            throw new InvalidDataException("Download metadata is incomplete and cannot authorize discard.");

        var actionId = $"discard:{downloadId:N}:{record.Sha256.ToLowerInvariant()}";
        var invocation = new CapabilityInvocation(
            CapabilityId,
            actionId,
            new HashSet<DataPermission> { DataPermission.FilesWrite },
            CapabilityRiskLevel.High,
            Consequential: true,
            UntrustedSource: record.SourceUri.Host,
            Summary: $"Permanently discard quarantined download '{record.SuggestedFileName}' from NVIDEA storage.");
        var decision = _policy.Evaluate(invocation);
        if (!decision.Allowed || !decision.RequiresApproval || string.IsNullOrWhiteSpace(decision.ApprovalScope))
            throw new InvalidOperationException("Download discard policy must require an exact approval scope.");

        return new BrowserDownloadDiscardPlan(
            downloadId,
            decision,
            actionId,
            invocation.Summary!,
            record.SuggestedFileName,
            record.LengthBytes,
            record.Sha256);
    }

    public async Task<BrowserDownloadDiscardReceipt> DiscardAsync(
        BrowserDownloadDiscardPlan approvedPlan,
        ApprovalGrant? approval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approvedPlan);

        var current = await PrepareAsync(approvedPlan.DownloadId, cancellationToken).ConfigureAwait(false);
        if (!Equivalent(approvedPlan, current))
        {
            await AuditAsync(current, "download.discard.scope_changed", approved: false,
                "Download discard scope changed after confirmation was prepared.", cancellationToken).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Download discard scope changed; request fresh user approval.");
        }

        if (approval is null || !_approvals.TryAuthorize(approval, current.Decision))
        {
            await AuditAsync(current, "download.discard.awaiting_approval", approved: false,
                "Exact single-use approval is required before a quarantined payload can be deleted.", cancellationToken).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Exact single-use approval is required to discard this quarantined download.");
        }

        await AuditAsync(current, "download.discard.started", approved: true,
            "Approved quarantine discard started.", cancellationToken).ConfigureAwait(false);
        try
        {
            var receipt = await _quarantine.DiscardAsync(current.DownloadId, userApproved: true, cancellationToken).ConfigureAwait(false);
            await AuditAsync(current, "download.discard.succeeded", approved: true,
                "Approved quarantined payload was deleted and quota was reclaimed.", cancellationToken).ConfigureAwait(false);
            return receipt;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AuditAsync(current, "download.discard.cancelled", approved: true,
                "Approved quarantine discard was cancelled; approval remains consumed.", CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await AuditAsync(current, "download.discard.failed", approved: true,
                $"Approved quarantine discard failed ({ex.GetType().Name}); approval remains consumed.", cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private Task AuditAsync(
        BrowserDownloadDiscardPlan plan,
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
                ["sha256"] = plan.Sha256 ?? string.Empty,
                ["lengthBytes"] = plan.LengthBytes?.ToString() ?? string.Empty,
                ["permissions"] = string.Join(',', plan.Decision.EffectivePermissions.OrderBy(x => x).Select(x => x.ToString()))
            }), cancellationToken);

    private static bool Equivalent(BrowserDownloadDiscardPlan left, BrowserDownloadDiscardPlan right) =>
        left.DownloadId == right.DownloadId
        && string.Equals(left.ActionId, right.ActionId, StringComparison.Ordinal)
        && string.Equals(left.Decision.ApprovalScope, right.Decision.ApprovalScope, StringComparison.Ordinal)
        && string.Equals(left.Sha256, right.Sha256, StringComparison.OrdinalIgnoreCase)
        && left.LengthBytes == right.LengthBytes;
}
