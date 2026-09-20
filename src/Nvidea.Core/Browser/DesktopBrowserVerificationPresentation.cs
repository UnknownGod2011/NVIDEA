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

        var approvalCount = 0;
        foreach (var receipt in receipts)
        {
            if (receipt is null
                || receipt.ActionId == Guid.Empty
                || receipt.CompletedAt < receipt.StartedAt
                || !receipt.Decision.Allowed
                || !receipt.DriverReportedSuccess
                || !receipt.Verified)
            {
                return NotVerified("Browser execution is incomplete or lacks verified post-action evidence.", receipts.Count, approvalCount);
            }

            if (receipt.Decision.Risk == BrowserRiskLevel.Blocked)
                return NotVerified("A blocked browser action cannot be presented as verified.", receipts.Count, approvalCount);

            if (receipt.Decision.RequiresApproval)
            {
                if (!receipt.ApprovalGranted)
                    return NotVerified("A consequential browser action lacks explicit approval evidence.", receipts.Count, approvalCount);
                approvalCount++;
            }
        }

        return new DesktopBrowserVerificationPresentation(
            Verified: true,
            Status: "VERIFIED browser execution",
            ExecutionEvidence: $"{receipts.Count} action(s) executed through the guarded browser runtime.",
            PermissionEvidence: approvalCount == 0
                ? "No action in this run required consequential-action approval."
                : $"{approvalCount} consequential action(s) carry explicit approval evidence.",
            PostStateEvidence: "Every executed action has observed post-action verification.",
            ActionCount: receipts.Count,
            ApprovalCount: approvalCount);
    }

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
