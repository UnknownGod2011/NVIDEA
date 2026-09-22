using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DemoRecordingContractTests
{
    [Fact]
    public void RequiredSequence_IsCanonicalCompleteAndUnique()
    {
        var expected = new[]
        {
            SessionEvidenceKind.NemotronInferenceCompleted,
            SessionEvidenceKind.MemoryInfluencedInvocation,
            SessionEvidenceKind.TavilyResearchCompletedWithCitations,
            SessionEvidenceKind.BrowserPostStateVerified,
            SessionEvidenceKind.ConsequentialApprovalGateExercised,
            SessionEvidenceKind.NebiusBackgroundExecutionObserved
        };

        Assert.Equal(expected, DemoRecordingContract.RequiredSequence);
        Assert.Equal(expected.Length, DemoRecordingContract.RequiredSequence.Distinct().Count());
        Assert.All(DemoRecordingContract.RequiredSequence, kind => Assert.True(Enum.IsDefined(kind)));
    }
}
