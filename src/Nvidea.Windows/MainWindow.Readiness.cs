using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private DesktopResearchReadiness? _researchReadiness;

    private void ResearchReadinessWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeVoiceUi();
        RefreshResearchReadiness();
        ResearchWindow_Loaded(sender, e);
    }

    private void ResearchReadinessDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshResearchReadiness();
        var readiness = _researchReadiness;
        if (readiness is null) return;

        // The dialog receives only the narrow ephemeral-evidence read/reset capabilities. It has
        // no reference to durable memory, job, browser, download or audit stores, so "new demo
        // session" cannot become a destructive product-state reset.
        var dialog = new JudgeEvidenceDialog(
            readiness,
            _root.Desktop.SessionEvidenceSnapshot,
            _root.Desktop.ResetSessionEvidence) { Owner = this };
        dialog.ShowDialog();
    }

    private void RefreshResearchReadiness()
    {
        DesktopResearchReadiness readiness;
        try
        {
            var cloudMode = DesktopResearchCloudMode.FromEnvironment();
            readiness = DesktopResearchReadiness.InspectEnvironment(cloudMode).WithRuntimeState(
                lifecycleReady: _root.Research?.RemoteLifecycleAvailable == true,
                dispatchReady: _root.Research?.RemoteDispatchEnabled == true);
        }
        catch
        {
            readiness = new DesktopResearchReadiness(
                LocalResearchReady: _root.Research?.LocalExecutionAvailable == true,
                NebiusLifecycleRequested: false,
                NebiusLifecycleReady: _root.Research?.RemoteLifecycleAvailable == true,
                NebiusDispatchRequested: false,
                NebiusDispatchReady: _root.Research?.RemoteDispatchEnabled == true,
                new[] { "Desktop research readiness could not re-read the current opt-in flags; restart NVIDEA after verifying the documented configuration names." });
        }

        _researchReadiness = readiness;
        ResearchReadinessText.Text = readiness.ToStatusText();
        ResearchReadinessDetailsButton.ToolTip = "Open runtime-derived architecture and session evidence. Secret values, provider IDs, payloads, and raw provider errors are never displayed.";
    }
}