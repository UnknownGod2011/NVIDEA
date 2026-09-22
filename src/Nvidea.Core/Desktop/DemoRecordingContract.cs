namespace Nvidea.Core.Desktop;

/// <summary>
/// Canonical, payload-free production milestone sequence for the judged demo.
/// Keep docs/demo-package.json validated against this contract so the shipped
/// Windows host never needs to parse mutable documentation at runtime.
/// </summary>
public static class DemoRecordingContract
{
    public static IReadOnlyList<SessionEvidenceKind> RequiredSequence { get; } = Array.AsReadOnly(new[]
    {
        SessionEvidenceKind.NemotronInferenceCompleted,
        SessionEvidenceKind.MemoryInfluencedInvocation,
        SessionEvidenceKind.TavilyResearchCompletedWithCitations,
        SessionEvidenceKind.BrowserPostStateVerified,
        SessionEvidenceKind.ConsequentialApprovalGateExercised,
        SessionEvidenceKind.NebiusBackgroundExecutionObserved
    });
}
