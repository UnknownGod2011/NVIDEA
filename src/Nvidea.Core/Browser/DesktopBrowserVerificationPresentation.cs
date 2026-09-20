namespace Nvidea.Core.Browser;

/// <summary>
/// Payload-free browser evidence intended for judge/demo surfaces. This projection deliberately
/// excludes URLs, locators, typed values, page text, verification details, errors and rationale.
/// It is evidence of the local safety/verification pipeline, not proof that a website is truthful.
/// </summary>
public sealed record DesktopBrowserVerificationPresentation(
    bool Verified,
    string Status,
    string ExecutionEvidence,
    string PermissionEvidence,
    string PostStateEvidence,
    int ActionCount,
    int ApprovalCount);

public static class DesktopBrowserVerificationProjector
{
    public static DesktopBrowserVerificationPresentation Project(IReadOnlyList<BrowserActionReceipt>? receipts)
    {
        if (receipts is null || receipts.Count == 0)
            return NotVerified("No completed browser action evidence is available.");

        var durable = receipts.Select(static receipt => new DurableBrowserActionEvidence(
            receipt.ActionId,
            receipt.Action.Kind,
            receipt.Decision.Risk,
            receipt.Decision.Allowed,
            receipt.Decision.RequiresApproval,
            receipt.ApprovalGranted,
            receipt.DriverReportedSuccess,
            receipt.Verified,
            receipt.StartedAt,
            receipt.CompletedAt)).ToArray();

        return ProjectDurable(durable);
    }

    internal static DesktopBrowserVerificationPresentation ProjectDurable(IReadOnlyList<DurableBrowserActionEvidence>? actions)
    {
        if (actions is null || actions.Count == 0)
            return NotVerified("No completed browser action evidence is available.");

        var approvalCount = 0;
        var ids = new HashSet<Guid>();
        foreach (var action in actions)
        {
            if (action is null
                || action.ActionId == Guid.Empty
                || !ids.Add(action.ActionId)
                || action.CompletedAt < action.StartedAt
                || !action.Allowed
                || !action.DriverReportedSuccess
                || !action.PostStateVerified)
            {
                return NotVerified("Browser execution is incomplete or lacks verified post-action evidence.", actions.Count, approvalCount);
            }

            if (action.Risk == BrowserRiskLevel.Blocked)
                return NotVerified("A blocked browser action cannot be presented as verified.", actions.Count, approvalCount);

            if (action.ApprovalObserved && !action.RequiredApproval)
                return NotVerified("Browser approval evidence is structurally inconsistent.", actions.Count, approvalCount);

            if (action.RequiredApproval)
            {
                if (!action.ApprovalObserved)
                    return NotVerified("A consequential browser action lacks explicit approval evidence.", actions.Count, approvalCount);
                approvalCount++;
            }
        }

        return new DesktopBrowserVerificationPresentation(
            Verified: true,
            Status: "VERIFIED browser execution",
            ExecutionEvidence: $"{actions.Count} action(s) executed through the guarded browser runtime.",
            PermissionEvidence: approvalCount == 0
                ? "No action in this run required consequential-action approval."
                : $"{approvalCount} consequential action(s) carry explicit approval evidence.",
            PostStateEvidence: "Every executed action has observed post-action verification.",
            ActionCount: actions.Count,
            ApprovalCount: approvalCount);
    }

    internal static DesktopBrowserVerificationPresentation NotVerifiedDurable(string reason) => NotVerified(reason);

    private static DesktopBrowserVerificationPresentation NotVerified(
        string reason,
        int actionCount = 0,
        int approvalCount = 0) =>
        new(
            Verified: false,
            Status: "NOT VERIFIED browser execution",
            ExecutionEvidence: reason,
            PermissionEvidence: "Permission evidence is not sufficient for judge verification.",
            PostStateEvidence: "Post-action verification is not complete.",
            ActionCount: Math.Max(0, actionCount),
            ApprovalCount: Math.Max(0, approvalCount));
}
