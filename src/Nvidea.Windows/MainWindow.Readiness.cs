using System.Windows;
using Nvidea.Core.Browser;
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

    private async void ResearchReadinessDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshResearchReadiness();
        var readiness = _researchReadiness;
        if (readiness is null) return;

        DesktopDurableResearchReceipt? durableReceipt = null;
        var runtime = _root.Research;
        if (runtime is not null && _activeResearchJobId is { } jobId)
        {
            try
            {
                var receipt = await runtime.ReadCompletedReceiptAsync(jobId);
                durableReceipt = new DesktopDurableResearchReceipt(
                    receipt.PlanSha256,
                    receipt.EvidenceSha256,
                    receipt.SynthesisSha256,
                    receipt.PlannedQueryCount,
                    receipt.EvidenceSourceCount,
                    receipt.ValidatedCitationCount,
                    receipt.PlannedQueryCount >= 2,
                    receipt.HasMachineVerifiableCitations,
                    RestartStable: true);
            }
            catch
            {
                // Incomplete, legacy, corrupt, or concurrently changing research must never become
                // green judge evidence. The dialog remains usable and explicitly shows no receipt.
            }
        }

        DesktopBrowserVerificationPresentation? browserVerification = null;
        try
        {
            // The product runtime exposes only the payload-free presentation read owned by the
            // durable browser runtime. Reading through it preserves the same serialized boundary
            // used by evidence-affecting browser transitions, so the judge dialog cannot observe
            // a half-settled or stale-green receipt.
            var browser = await _root.GetBrowserProductAsync();
            browserVerification = await browser.ReadVerificationPresentationAsync();
        }
        catch
        {
            // Browser startup/read failure, missing Playwright, corrupt protected evidence, or a
            // concurrently invalidated receipt must fail closed. Judge evidence remains usable for
            // provider/research/session proof while browser verification stays NOT VERIFIED.
        }

        var dialog = new JudgeEvidenceDialog(
            readiness,
            _root.Desktop.SessionEvidenceSnapshot,
            _root.Desktop.ResetSessionEvidence,
            durableReceipt,
            browserVerification) { Owner = this };
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
