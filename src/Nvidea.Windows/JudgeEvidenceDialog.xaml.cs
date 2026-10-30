using System.Windows;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;

namespace Nvidea.Windows;

public partial class JudgeEvidenceDialog : Window
{
    private readonly Func<SessionEvidenceSnapshot> _snapshot;
    private readonly Action _resetSessionEvidence;
    private readonly Func<CancellationToken, Task<WorkerAuthenticationJudgeEvidence>>? _runWorkerCanary;
    private CancellationTokenSource? _workerCanaryCancellation;

    public JudgeEvidenceDialog(DesktopResearchReadiness readiness, Func<SessionEvidenceSnapshot> snapshot, Action resetSessionEvidence, DesktopDurableResearchReceipt? durableResearchReceipt = null, DesktopBrowserVerificationPresentation? browserVerification = null, DesktopResearchJudgePresentation? researchProvenance = null, WorkerAuthenticationJudgeEvidence? workerAuthentication = null, Func<CancellationToken, Task<WorkerAuthenticationJudgeEvidence>>? runWorkerCanary = null)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _resetSessionEvidence = resetSessionEvidence ?? throw new ArgumentNullException(nameof(resetSessionEvidence));
        _runWorkerCanary = runWorkerCanary;
        InitializeComponent();
        ProviderReadinessText.Text = BuildReadinessSummary(readiness);
        WorkerAuthenticationText.Text = BuildWorkerAuthenticationSummary(workerAuthentication);
        RunWorkerCanaryButton.IsEnabled = _runWorkerCanary is not null;
        ResearchProvenanceText.Text = BuildResearchProvenanceSummary(researchProvenance);
        DurableResearchReceiptText.Text = BuildDurableResearchSummary(durableResearchReceipt);
        BrowserVerificationText.Text = BuildBrowserVerificationSummary(browserVerification);
        RefreshSessionEvidence();
    }

    private void NewDemoSessionButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmation = MessageBox.Show(this, "Start a new demo evidence session?\n\nThis clears only the ephemeral 'Verified this session' milestones. Durable memory, research jobs, browser state, downloads and the audit trail are preserved.", "New demo session", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;
        _resetSessionEvidence();
        RefreshSessionEvidence();
    }

    private async void RunWorkerCanaryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_runWorkerCanary is null)
            return;

        if (_workerCanaryCancellation is not null)
        {
            _workerCanaryCancellation.Cancel();
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            "Run a Nebius Serverless signed-worker canary now?\n\nThis creates one remote job and may incur provider cost. It sends no research payload and shows only public verification evidence.",
            "Run signed-worker canary",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
            return;

        _workerCanaryCancellation = new CancellationTokenSource();
        RunWorkerCanaryButton.Content = "Cancel canary";
        try
        {
            var evidence = await _runWorkerCanary(_workerCanaryCancellation.Token);
            WorkerAuthenticationText.Text = BuildWorkerAuthenticationSummary(evidence);
        }
        catch (OperationCanceledException)
        {
            WorkerAuthenticationText.Text = "CANARY FAILED\nThe remote canary was canceled; deployed-worker trust was not established.";
        }
        catch
        {
            WorkerAuthenticationText.Text = "CANARY FAILED\nThe remote proof did not validate for the active deployment; deployed-worker trust was not established.";
        }
        finally
        {
            _workerCanaryCancellation.Dispose();
            _workerCanaryCancellation = null;
            RunWorkerCanaryButton.Content = "Run signed-worker canary…";
        }
    }

    private void RefreshSessionEvidence()
    {
        var snapshot = _snapshot();
        SessionEvidenceText.Text = BuildSessionEvidenceSummary(snapshot);
        RecordingGateText.Text = BuildRecordingGateSummary(DemoRecordingGate.Evaluate(snapshot, DemoRecordingContract.RequiredSequence));
    }

    private static string BuildReadinessSummary(DesktopResearchReadiness readiness)
    {
        var local = readiness.LocalResearchReady ? "Tavily-backed local research: READY" : "Tavily-backed local research: NOT READY";
        var lifecycle = readiness.NebiusLifecycleReady ? "Nebius remote lifecycle: READY" : readiness.NebiusLifecycleRequested ? "Nebius remote lifecycle: REQUESTED, NOT READY" : "Nebius remote lifecycle: NOT ENABLED";
        var dispatch = readiness.NebiusDispatchReady ? "Nebius Serverless dispatch: READY" : readiness.NebiusDispatchRequested ? "Nebius Serverless dispatch: REQUESTED, NOT READY" : "Nebius Serverless dispatch: NOT ENABLED";
        return $"{local}\n{lifecycle}\n{dispatch}";
    }

    internal static string BuildWorkerAuthenticationSummary(WorkerAuthenticationJudgeEvidence? evidence) =>
        evidence is null
            ? "NOT VERIFIED — no valid pinned worker verification identity is available."
            : evidence.ToStatusText();

    internal static string BuildResearchProvenanceSummary(DesktopResearchJudgePresentation? presentation)
    {
        presentation ??= DesktopResearchJudgePresentation.FromEvidence(null);
        return string.Join("\n", presentation.Status, presentation.EvidenceSummary, presentation.DiagnosticSummary);
    }

    internal static string BuildDurableResearchSummary(DesktopDurableResearchReceipt? receipt)
    {
        var presentation = DesktopResearchLineagePresentationProjector.Project(receipt);
        return string.Join("\n", presentation.RenderedFields);
    }

    internal static string BuildBrowserVerificationSummary(DesktopBrowserVerificationPresentation? presentation)
    {
        if (presentation is null) return "NOT VERIFIED browser execution\nNo authoritative browser receipt projection is available for this evidence view.";
        return string.Join("\n", new[] { presentation.Status, presentation.ExecutionEvidence, presentation.PermissionEvidence, presentation.PostStateEvidence });
    }

    internal static string BuildRecordingGateSummary(DemoRecordingGateResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var status = result.CanRecord ? "READY TO RECORD" : "NOT READY TO RECORD";
        if (result.MissingMilestones.Count == 0) return $"{status}\n{result.Reason}";
        var missing = string.Join(", ", result.MissingMilestones.Select(Label));
        return $"{status}\n{result.Reason}\nMissing: {missing}";
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
