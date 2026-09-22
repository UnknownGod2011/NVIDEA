using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DemoRecordingGateTests
{
    private static readonly SessionEvidenceKind[] Required =
    {
        SessionEvidenceKind.NemotronInferenceCompleted,
        SessionEvidenceKind.MemoryInfluencedInvocation,
        SessionEvidenceKind.TavilyResearchCompletedWithCitations,
        SessionEvidenceKind.BrowserPostStateVerified,
        SessionEvidenceKind.ConsequentialApprovalGateExercised,
        SessionEvidenceKind.NebiusBackgroundExecutionObserved
    };

    [Fact]
    public void Complete_ordered_production_evidence_opens_gate()
    {
        var snapshot = Snapshot(Required);
        var result = DemoRecordingGate.Evaluate(snapshot, Required);
        Assert.True(result.CanRecord);
        Assert.Empty(result.MissingMilestones);
    }

    [Fact]
    public void Missing_milestone_fails_closed()
    {
        var snapshot = Snapshot(Required.Where(x => x != SessionEvidenceKind.BrowserPostStateVerified));
        var result = DemoRecordingGate.Evaluate(snapshot, Required);
        Assert.False(result.CanRecord);
        Assert.Equal(new[] { SessionEvidenceKind.BrowserPostStateVerified }, result.MissingMilestones);
    }

    [Fact]
    public void Complete_but_out_of_order_evidence_fails_closed()
    {
        var reordered = Required.ToArray();
        (reordered[3], reordered[4]) = (reordered[4], reordered[3]);
        var result = DemoRecordingGate.Evaluate(Snapshot(reordered), Required);
        Assert.False(result.CanRecord);
        Assert.Empty(result.MissingMilestones);
        Assert.Contains("out of contract order", result.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_or_duplicate_contract_fails_closed()
    {
        Assert.False(DemoRecordingGate.Evaluate(Snapshot(Required), Array.Empty<SessionEvidenceKind>()).CanRecord);
        Assert.False(DemoRecordingGate.Evaluate(Snapshot(Required), new[] { Required[0], Required[0] }).CanRecord);
    }

    private static SessionEvidenceSnapshot Snapshot(IEnumerable<SessionEvidenceKind> kinds)
    {
        var start = new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);
        return new SessionEvidenceSnapshot(kinds.Select((kind, index) => new SessionEvidenceEntry(kind, start.AddSeconds(index))).ToArray());
    }
}
