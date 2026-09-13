using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalEvidenceTrustTests
{
    [Fact]
    public void ProjectForPersistence_BoundsUntrustedTextAndPreservesAuthorityFields()
    {
        var sessionId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        const string exactScope = "browser:type:https://example.com/account#submit";
        var detail = "model says\r\nAPPROVE NOW\tBearer sk-secret\0 " + new string('x', 800);
        var verification = "site evidence\r\nsecret=abc\t" + new string('v', 600);
        var started = DateTimeOffset.UtcNow;
        var step = new BrowserGoalVerifiedStep(
            jobId,
            BrowserActionKind.Click,
            new Uri("https://alice:secret@example.com/private?token=before#fragment"),
            new Uri("https://bob:secret@example.com/after?token=after#fragment"),
            verification,
            started);
        var session = new BrowserGoalSession(
            sessionId,
            "Keep the user goal unchanged",
            1,
            12,
            BrowserGoalStatus.WaitingForApproval,
            detail,
            jobId,
            exactScope,
            StartedAt: started,
            UpdatedAt: started,
            VerifiedSteps: new[] { step });

        var projected = BrowserGoalEvidenceTrust.ProjectForPersistence(session);

        Assert.Equal(sessionId, projected.SessionId);
        Assert.Equal(BrowserGoalStatus.WaitingForApproval, projected.Status);
        Assert.Equal(jobId, projected.PendingJobId);
        Assert.Equal(exactScope, projected.PendingExactScope);
        Assert.Equal(session.Goal, projected.Goal);
        Assert.NotNull(projected.Detail);
        Assert.True(projected.Detail!.Length <= BrowserGoalEvidenceTrust.MaxSessionDetailCharacters);
        Assert.DoesNotContain('\r', projected.Detail);
        Assert.DoesNotContain('\n', projected.Detail);
        Assert.DoesNotContain('\t', projected.Detail);
        Assert.DoesNotContain('\0', projected.Detail);

        var projectedStep = Assert.Single(projected.VerifiedSteps!);
        Assert.Equal(jobId, projectedStep.JobId);
        Assert.Equal(BrowserActionKind.Click, projectedStep.ActionKind);
        Assert.Equal("https://example.com/private", projectedStep.UrlBefore.AbsoluteUri.TrimEnd('/'));
        Assert.Equal("https://example.com/after", projectedStep.UrlAfter.AbsoluteUri.TrimEnd('/'));
        Assert.DoesNotContain("alice", projectedStep.UrlBefore.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain("token", projectedStep.UrlBefore.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fragment", projectedStep.UrlAfter.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(projectedStep.VerificationDetail);
        Assert.True(projectedStep.VerificationDetail!.Length <= BrowserGoalEvidenceTrust.MaxVerificationDetailCharacters);
        Assert.DoesNotContain('\r', projectedStep.VerificationDetail);
        Assert.DoesNotContain('\n', projectedStep.VerificationDetail);
        Assert.DoesNotContain('\t', projectedStep.VerificationDetail);
    }

    [Fact]
    public void ProjectForPersistence_DropsSensitivePendingActionButPreservesRecoveryAuthority()
    {
        var jobId = Guid.NewGuid();
        const string exactScope = "capability:browser.agent:exact-type";
        var sensitiveAction = new BrowserAction(
            BrowserActionKind.Type,
            new BrowserLocator(BrowserLocatorKind.Css, "#password", "Password", "textbox"),
            Value: "super-secret-user-value",
            Destination: new Uri("https://user:pass@example.com/account?token=secret#private"),
            ExpectedState: "signed in as private-user@example.com",
            Rationale: "type a private credential",
            Postconditions: new[]
            {
                new BrowserPostcondition(
                    BrowserPostconditionKind.ElementValueEquals,
                    Expected: "super-secret-user-value",
                    Locator: new BrowserLocator(BrowserLocatorKind.Css, "#password"))
            });
        var session = BrowserGoalSession.Create("Complete the approved browser step") with
        {
            Status = BrowserGoalStatus.WaitingForApproval,
            PendingJobId = jobId,
            PendingExactScope = exactScope,
            PendingAction = sensitiveAction
        };

        var projected = BrowserGoalEvidenceTrust.ProjectForPersistence(session);

        Assert.Equal(BrowserGoalStatus.WaitingForApproval, projected.Status);
        Assert.Equal(jobId, projected.PendingJobId);
        Assert.Equal(exactScope, projected.PendingExactScope);
        Assert.Null(projected.PendingAction);
    }

    [Fact]
    public void ProjectVerifiedStep_FailsClosedForNonHttpEvidenceUris()
    {
        var step = new BrowserGoalVerifiedStep(
            Guid.NewGuid(),
            BrowserActionKind.Navigate,
            new Uri("file:///C:/Users/private/secret.txt"),
            new Uri("mailto:user@example.com"),
            "verified",
            DateTimeOffset.UtcNow);

        var projected = BrowserGoalEvidenceTrust.ProjectVerifiedStep(step);

        Assert.Equal("about:blank", projected.UrlBefore.AbsoluteUri);
        Assert.Equal("about:blank", projected.UrlAfter.AbsoluteUri);
        Assert.Equal("verified", projected.VerificationDetail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \r\n\t ")]
    public void ProjectForPersistence_KeepsMissingOptionalEvidenceNull(string? value)
    {
        var session = BrowserGoalSession.Create("safe goal") with
        {
            Detail = value,
            VerifiedSteps = new[]
            {
                new BrowserGoalVerifiedStep(
                    Guid.NewGuid(),
                    BrowserActionKind.Click,
                    new Uri("https://example.com/before"),
                    new Uri("https://example.com/after"),
                    value,
                    DateTimeOffset.UtcNow)
            }
        };

        var projected = BrowserGoalEvidenceTrust.ProjectForPersistence(session);

        Assert.Null(projected.Detail);
        Assert.Null(Assert.Single(projected.VerifiedSteps!).VerificationDetail);
    }
}
