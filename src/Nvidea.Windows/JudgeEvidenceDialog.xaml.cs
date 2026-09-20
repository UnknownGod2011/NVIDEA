using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class JudgeEvidenceDialog : Window
{
    private readonly Func<SessionEvidenceSnapshot> _snapshot;
    private readonly Action _resetSessionEvidence;

    public JudgeEvidenceDialog(DesktopResearchReadiness readiness, Func<SessionEvidenceSnapshot> snapshot, Action resetSessionEvidence, DesktopDurableResearchReceipt? durableResearchReceipt = null)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _resetSessionEvidence = resetSessionEvidence ?? throw new ArgumentNullException(nameof(resetSessionEvidence));
        InitializeComponent();
        ProviderReadinessText.Text = BuildReadinessSummary(readiness);
        DurableResearchReceiptText.Text = BuildDurableResearchSummary(durableResearchReceipt);
        RefreshSessionEvidence();
    }

    private void NewDemoSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmation = MessageBox.Show(this, "Start a new demo evidence session?\n\nThis clears only the ephemeral 'Verified this session' milestones. Durable memory, research jobs, browser state, downloads and the audit trail are preserved.", "New demo session", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;
        _resetSessionEvidence();
        RefreshSessionEvidence();
    }

    private void RefreshSessionEvidence() => SessionEvidenceText.Text = BuildSessionEvidenceSummary(_snapshot());

    private static string BuildReadinessSummary(DesktopResearchReadiness readiness)
    {
        var local = readiness.LocalResearchReady ? "Tavily-backed local research: READY" : "Tavily-backed local research: NOT READY";
        var lifecycle = readiness.NebiusLifecycleReady ? "Nebius remote lifecycle: READY" : readiness.NebiusLifecycleRequested ? "Nebius remote lifecycle: REQUESTED, NOT READY" : "Nebius remote lifecycle: NOT ENABLED";
        var dispatch = readiness.NebiusDispatchReady ? "Nebius Serverless dispatch: READY" : readiness.NebiusDispatchRequested ? "Nebius Serverless dispatch: REQUESTED, NOT READY" : "Nebius Serverless dispatch: NOT ENABLED";
        return $"{local}\n{lifecycle}\n{dispatch}";
    }

    internal static string BuildDurableResearchSummary(DesktopDurableResearchReceipt? receipt)
    {
        if (receipt is null)
            return "No completed durable research receipt is available for the current job.";

        var plan = receipt.MultiQueryPlan ? $"Nemotron plan: VERIFIED · {receipt.PlannedQueryCount} queries" : $"Nemotron plan: VERIFIED · {receipt.PlannedQueryCount} query";
        var evidence = $"Tavily evidence: BOUND · {receipt.EvidenceSourceCount} sources";
        var synthesis = receipt.HasMachineVerifiableCitations
            ? $"Cited synthesis: VERIFIED · {receipt.ValidatedCitationCount} validated citations"
            : "Cited synthesis: NOT VERIFIED";
        var restart = receipt.RestartStable ? "Restart lineage: BOUND" : "Restart lineage: NOT VERIFIED";
        return $"{plan}\n{evidence}\n{synthesis}\n{restart}";
    }

    private static string BuildSessionEvidenceSummary(SessionEvidenceSnapshot snapshot)
    {
        if (snapshot.Entries.Count == 0) return "No verified production milestones observed in this process yet.";
        return string.Join("\n", snapshot.Entries.Select(entry => $"✓ {Label(entry.Kind)} · {entry.FirstObservedAt.ToLocalTime():HH:mm:ss}"));
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
