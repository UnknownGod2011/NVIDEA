using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class BrowserSessionEvidenceRecorderTests
{
    [Fact]
    public void ObserveOutcome_RecordsOnlyCompletedVerifiedCheckpoint()
    {
        var ledger = new SessionEvidenceLedger(() => new DateTimeOffset(2026, 9, 16, 0, 0, 0, TimeSpan.Zero));
        var recorder = new BrowserSessionEvidenceRecorder(ledger);
        var verified = new BrowserGoalVerifiedStep(
            Guid.NewGuid(), BrowserActionKind.Click,
            new Uri("https://example.com/before"), new Uri("https://example.com/after"),
            "trusted postcondition matched", DateTimeOffset.UtcNow);

        recorder.ObserveOutcome(new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Failed, "failed", VerifiedStep: verified));
        recorder.ObserveOutcome(new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Cancelled, "cancelled", VerifiedStep: verified));
        recorder.ObserveOutcome(new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Running, "ambiguous", VerifiedStep: verified));
        recorder.ObserveOutcome(new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Completed, "completed without proof"));

        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.BrowserPostStateVerified));

        recorder.ObserveOutcome(new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Completed, "verified", VerifiedStep: verified));

        Assert.True(ledger.Snapshot().Contains(SessionEvidenceKind.BrowserPostStateVerified));
    }

    [Fact]
    public void AcceptedApproval_RecordsOnlyExplicitAcceptedBoundary()
    {
        var ledger = new SessionEvidenceLedger(() => new DateTimeOffset(2026, 9, 16, 0, 0, 0, TimeSpan.Zero));
        var recorder = new BrowserSessionEvidenceRecorder(ledger);

        // Displaying or observing a waiting prompt is descriptive and must not count as approval.
        recorder.ObserveOutcome(new BrowserJobOutcome(
            Guid.NewGuid(),
            AgentJobState.WaitingForApproval,
            "waiting",
            new BrowserApprovalPrompt(
                Guid.NewGuid(), "scope", BrowserActionKind.Click,
                "summary", "target", DateTimeOffset.UtcNow)));

        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.ConsequentialApprovalGateExercised));

        recorder.ObserveAcceptedConsequentialApproval();
        recorder.ObserveAcceptedConsequentialApproval();

        var snapshot = ledger.Snapshot();
        Assert.True(snapshot.Contains(SessionEvidenceKind.ConsequentialApprovalGateExercised));
        Assert.Single(snapshot.Entries.Where(x => x.Kind == SessionEvidenceKind.ConsequentialApprovalGateExercised));
    }
}
