using System.Windows;
using Nvidea.Core.Desktop;

namespace Nvidea.Windows;

public partial class JudgeEvidenceDialog : Window
{
    public JudgeEvidenceDialog(DesktopResearchReadiness readiness)
    {
        ArgumentNullException.ThrowIfNull(readiness);
        InitializeComponent();
        ProviderReadinessText.Text = BuildReadinessSummary(readiness);
    }

    private static string BuildReadinessSummary(DesktopResearchReadiness readiness)
    {
        var local = readiness.LocalResearchReady
            ? "Tavily-backed local research: READY"
            : "Tavily-backed local research: NOT READY";
        var lifecycle = readiness.NebiusLifecycleReady
            ? "Nebius remote lifecycle: READY"
            : readiness.NebiusLifecycleRequested
                ? "Nebius remote lifecycle: REQUESTED, NOT READY"
                : "Nebius remote lifecycle: NOT ENABLED";
        var dispatch = readiness.NebiusDispatchReady
            ? "Nebius Serverless dispatch: READY"
            : readiness.NebiusDispatchRequested
                ? "Nebius Serverless dispatch: REQUESTED, NOT READY"
                : "Nebius Serverless dispatch: NOT ENABLED";

        return $"{local}\n{lifecycle}\n{dispatch}";
    }
}
