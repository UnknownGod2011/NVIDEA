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
        if (readiness is null)
            return;

        // Keep judge-visible evidence bound to the same runtime-derived readiness object used by
        // the product instead of maintaining a separate demo-only source of truth. The dialog
        // deliberately reports unavailable providers as unavailable and never exposes secrets.
        var dialog = new JudgeEvidenceDialog(readiness) { Owner = this };
        dialog.ShowDialog();
    }

    private void RefreshResearchReadiness()
    {
        DesktopResearchReadiness readiness;
        try
        {
            var cloudMode = DesktopResearchCloudMode.FromEnvironment();
            readiness = DesktopResearchReadiness
                .InspectEnvironment(cloudMode)
                .WithRuntimeState(
                    lifecycleReady: _root.Research?.RemoteLifecycleAvailable == true,
                    dispatchReady: _root.Research?.RemoteDispatchEnabled == true);
        }
        catch
        {
            // Successful composition has already validated provider construction. This catch is for
            // a later environment mutation (for example a malformed opt-in flag) and intentionally
            // avoids displaying the raw exception or any environment value.
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
        ResearchReadinessDetailsButton.ToolTip =
            "Open runtime-derived architecture evidence. Secret values, provider IDs, payloads, and raw provider errors are never displayed.";
    }
}
