using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class MainWindow
{
    private DesktopResearchReadiness? _researchReadiness;

    private void ResearchReadinessWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshResearchReadiness();
        ResearchWindow_Loaded(sender, e);
    }

    private void ResearchReadinessDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshResearchReadiness();
        var readiness = _researchReadiness;
        if (readiness is null)
            return;

        MessageBox.Show(
            readiness.ToDetailsText(),
            "NVIDEA research readiness",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
            "Read-only capability details. Secret values, provider IDs, payloads, and raw provider errors are never displayed.";
    }
}
