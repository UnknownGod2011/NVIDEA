using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class SessionEvidenceLedgerTests
{
    [Fact]
    public void Record_IsIdempotentAndPreservesFirstObservation()
    {
        var times = new Queue<DateTimeOffset>(new[]
        {
            new DateTimeOffset(2026, 9, 16, 1, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 16, 1, 1, 0, TimeSpan.Zero)
        });
        var ledger = new SessionEvidenceLedger(() => times.Dequeue());

        ledger.Record(SessionEvidenceKind.NemotronInferenceCompleted);
        ledger.Record(SessionEvidenceKind.NemotronInferenceCompleted);

        var entry = Assert.Single(ledger.Snapshot().Entries);
        Assert.Equal(new DateTimeOffset(2026, 9, 16, 1, 0, 0, TimeSpan.Zero), entry.FirstObservedAt);
    }

    [Fact]
    public void Snapshot_ContainsOnlyClosedKindAndTimestampProjection()
    {
        var ledger = new SessionEvidenceLedger(() => DateTimeOffset.UnixEpoch);
        ledger.Record(SessionEvidenceKind.TavilyResearchCompletedWithCitations);
        ledger.Record(SessionEvidenceKind.BrowserPostStateVerified);

        var snapshot = ledger.Snapshot();

        Assert.Equal(2, snapshot.Entries.Count);
        Assert.True(snapshot.Contains(SessionEvidenceKind.TavilyResearchCompletedWithCitations));
        Assert.True(snapshot.Contains(SessionEvidenceKind.BrowserPostStateVerified));
        Assert.All(snapshot.Entries, entry => Assert.Equal(DateTimeOffset.UnixEpoch, entry.FirstObservedAt));
    }

    [Fact]
    public void Clear_DropsSessionProofWithoutExternalSideEffects()
    {
        var ledger = new SessionEvidenceLedger();
        ledger.Record(SessionEvidenceKind.ConsequentialApprovalGateExercised);

        ledger.Clear();

        Assert.Empty(ledger.Snapshot().Entries);
    }

    [Fact]
    public void Record_RejectsUnknownKinds()
    {
        var ledger = new SessionEvidenceLedger();
        Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Record((SessionEvidenceKind)999));
    }
}
