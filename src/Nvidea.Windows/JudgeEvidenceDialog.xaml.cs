using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class JudgeEvidenceDialog : Window
{
    public JudgeEvidenceDialog(DesktopResearchReadiness readiness, SessionEvidenceSnapshot sessionEvidence)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        ArgumentNullException.ThrowIfNull(sessionEvidence);
        InitializeComponent();
        ProviderReadinessText.Text = BuildReadinessSummary(readiness);
        SessionEvidenceText.Text = BuildSessionEvidenceSummary(sessionEvidence);
    }

    private static string BuildReadinessSummary(DesktopResearchReadiness readiness)
    {
        var local = readiness.LocalResearchReady ? "Tavily-backed local research: READY" : "Tavily-backed local research: NOT READY";
        var lifecycle = readiness.NebiusLifecycleReady ? "Nebius remote lifecycle: READY" : readiness.NebiusLifecycleRequested ? "Nebius remote lifecycle: REQUESTED, NOT READY" : "Nebius remote lifecycle: NOT ENABLED";
        var dispatch = readiness.NebiusDispatchReady ? "Nebius Serverless dispatch: READY" : readiness.NebiusDispatchRequested ? "Nebius Serverless dispatch: REQUESTED, NOT READY" : "Nebius Serverless dispatch: NOT ENABLED";
        return $"{local}\n{lifecycle}\n{dispatch}";
    }

    private static string BuildSessionEvidenceSummary(SessionEvidenceSnapshot snapshot)
    {
        if (snapshot.Entries.Count == 0)
            return "No verified production milestones observed in this process yet.";

        return string.Join("\n", snapshot.Entries.Select(entry =>
            $"✓ {Label(entry.Kind)} · {entry.FirstObservedAt.ToLocalTime():HH:mm:ss}"));
    }

    private static string Label(SessionEvidenceKind kind) => kind switch
    {
        SessionEvidenceKind.NemotronInferenceCompleted => "Nemotron inference completed",
        SessionEvidenceKind.MemoryInfluencedInvocation => "Retrieved memory influenced an invocation",
        SessionEvidenceKind.TavilyResearchCompletedWithCitations => "Tavily research completed with validated citations",
        SessionEvidenceKind.BrowserPostStateVerified => "Browser post-state verified",
        SessionEvidenceKind.ConsequentialApprovalGateExercised => "Consequential-action approval gate exercised",
        SessionEvidenceKind.NebiusBackgroundExecutionObserved => "Nebius background execution observed",
        _ => "Unknown evidence kind"
    };
}