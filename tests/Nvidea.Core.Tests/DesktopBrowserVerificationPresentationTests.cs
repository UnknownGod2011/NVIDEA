using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class DesktopBrowserVerificationPresentationTests
{
    [Fact]
    public void Project_VerifiedApprovedAction_RendersPayloadFreeEvidence()
    {
        const string secret = "PRIVATE-MARKER-DO-NOT-RENDER";
        var receipt = Receipt(requiresApproval: true, approvalGranted: true, verified: true,
            action: new BrowserAction(BrowserActionKind.Type, BrowserLocator.Accessibility("field"), secret, Rationale: secret));

        var result = DesktopBrowserVerificationProjector.Project([receipt]);

        Assert.True(result.Verified);
        Assert.Equal(1, result.ApprovalCount);
        var rendered = string.Join("\n", result.Status, result.ExecutionEvidence, result.PermissionEvidence, result.PostStateEvidence);
        Assert.DoesNotContain(secret, rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("example.test", rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_ApprovalRequiredButNotRecorded_FailsClosed()
    {
        var result = DesktopBrowserVerificationProjector.Project([
            Receipt(requiresApproval: true, approvalGranted: false, verified: true)]);

        Assert.False(result.Verified);
        Assert.Contains("lacks explicit approval", result.ExecutionEvidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_UnverifiedOrBlockedAction_CannotRenderVerified()
    {
        Assert.False(DesktopBrowserVerificationProjector.Project([
            Receipt(requiresApproval: false, approvalGranted: false, verified: false)]).Verified);

        Assert.False(DesktopBrowserVerificationProjector.Project([
            Receipt(requiresApproval: false, approvalGranted: false, verified: true, risk: BrowserRiskLevel.Blocked)]).Verified);
    }

    [Fact]
    public void Project_EmptyEvidence_FailsClosed()
    {
        var result = DesktopBrowserVerificationProjector.Project([]);
        Assert.False(result.Verified);
        Assert.Equal(0, result.ActionCount);
    }

    private static BrowserActionReceipt Receipt(
        bool requiresApproval,
        bool approvalGranted,
        bool verified,
        BrowserRiskLevel risk = BrowserRiskLevel.High,
        BrowserAction? action = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrowserActionReceipt(
            Guid.NewGuid(),
            action ?? new BrowserAction(BrowserActionKind.Click, BrowserLocator.Accessibility("submit")),
            new BrowserActionDecision(risk, requiresApproval, Allowed: true, "policy"),
            now,
            now.AddMilliseconds(1),
            DriverReportedSuccess: true,
            Verified: verified,
            VerificationDetail: "private page detail",
            new Uri("https://example.test/before"),
            new Uri("https://example.test/after"),
            Error: null,
            ApprovalGranted: approvalGranted);
    }
}
