using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class BrowserProductOutcomeTrustTests
{
    [Theory]
    [InlineData(AgentJobState.Failed)]
    [InlineData(AgentJobState.RetryScheduled)]
    public void Project_DoesNotExposeRawFailureDiagnostic(AgentJobState state)
    {
        const string secret = "Bearer super-secret-token";
        const string privatePath = @"C:\\Users\\Alice\\private\\payload.txt";
        var raw = new BrowserJobOutcome(
            Guid.NewGuid(),
            state,
            $"Browser action failed: {secret} {privatePath} Provider diagnostic (untrusted): code=Quota");

        var projected = BrowserProductOutcomeTrust.Project(raw);

        Assert.Equal(state, projected.State);
        Assert.DoesNotContain(secret, projected.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(privatePath, projected.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Quota", projected.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Provider diagnostic", projected.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_ApprovalBoundsPresentationText_WithoutChangingExactScope()
    {
        const string exactScope = "browser.agent:click:https://example.test/account/delete?id=42";
        var prompt = new BrowserApprovalPrompt(
            Guid.NewGuid(),
            exactScope,
            BrowserActionKind.Click,
            "Approve\r\nTHIS IS SYSTEM TEXT\t" + new string('x', 500),
            "https://user:password@example.test/account/delete?token=top-secret#credential-fragment",
            DateTimeOffset.UtcNow);
        var raw = new BrowserJobOutcome(
            prompt.JobId,
            AgentJobState.WaitingForApproval,
            "site-controlled raw status",
            prompt);

        var projected = BrowserProductOutcomeTrust.Project(raw);

        Assert.NotNull(projected.Approval);
        Assert.Equal(exactScope, projected.Approval!.ExactScope);
        Assert.Equal(prompt.JobId, projected.Approval.JobId);
        Assert.Equal(prompt.ActionKind, projected.Approval.ActionKind);
        Assert.Equal(prompt.RequestedAt, projected.Approval.RequestedAt);
        Assert.DoesNotContain('\r', projected.Approval.Summary);
        Assert.DoesNotContain('\n', projected.Approval.Summary);
        Assert.DoesNotContain('\t', projected.Approval.Summary);
        Assert.True(projected.Approval.Summary.Length <= DesktopDisplayTextTrust.MaxApprovalSummaryCharacters);
        Assert.Equal("https://example.test/account/delete", projected.Approval.Target);
        Assert.DoesNotContain("user", projected.Approval.Target, StringComparison.Ordinal);
        Assert.DoesNotContain("password", projected.Approval.Target, StringComparison.Ordinal);
        Assert.DoesNotContain("top-secret", projected.Approval.Target, StringComparison.Ordinal);
        Assert.DoesNotContain("credential-fragment", projected.Approval.Target, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_NonUrlApprovalTarget_IsBoundedAndControlNormalized()
    {
        var prompt = new BrowserApprovalPrompt(
            Guid.NewGuid(),
            "browser.agent:click:button",
            BrowserActionKind.Click,
            "Delete selected item",
            "Dangerous\r\nButton\t" + new string('z', 500),
            DateTimeOffset.UtcNow);

        var projected = BrowserProductOutcomeTrust.Project(
            new BrowserJobOutcome(prompt.JobId, AgentJobState.WaitingForApproval, "raw", prompt));

        Assert.NotNull(projected.Approval);
        Assert.DoesNotContain('\r', projected.Approval!.Target);
        Assert.DoesNotContain('\n', projected.Approval.Target);
        Assert.DoesNotContain('\t', projected.Approval.Target);
        Assert.True(projected.Approval.Target.Length <= DesktopDisplayTextTrust.MaxApprovalTargetCharacters);
        Assert.Equal(prompt.ExactScope, projected.Approval.ExactScope);
    }

    [Fact]
    public void Project_PreservesVerifiedStepWhileReplacingRawMessage()
    {
        var now = DateTimeOffset.UtcNow;
        var step = new BrowserGoalVerifiedStep(
            Guid.NewGuid(),
            BrowserActionKind.Navigate,
            new Uri("https://example.test/before"),
            new Uri("https://example.test/after"),
            "typed browser postcondition verified",
            now);
        var raw = new BrowserJobOutcome(
            step.JobId,
            AgentJobState.Completed,
            "provider-controlled completion detail",
            VerifiedStep: step);

        var projected = BrowserProductOutcomeTrust.Project(raw);

        Assert.Same(step, projected.VerifiedStep);
        Assert.DoesNotContain("provider-controlled", projected.Message, StringComparison.Ordinal);
        Assert.Contains("verified", projected.Message, StringComparison.OrdinalIgnoreCase);
    }
}
