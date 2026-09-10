namespace Nvidea.Core.Jobs;

/// <summary>
/// UI-independent projection of durable research lifecycle authority.
/// Keeps Windows controls from re-implementing local-vs-remote safety decisions or
/// inferring provider authority from display text.
/// </summary>
public sealed record ResearchProductUiState(
    bool StartEnabled,
    bool ResumeEnabled,
    string ResumeLabel,
    bool ReconcileEnabled,
    bool CancelEnabled,
    string CloudDisclosureText)
{
    public const string ResumeNextStageLabel = "Resume next stage";
    public const string RearmInterruptedStageLabel = "Re-arm interrupted stage";

    public static ResearchProductUiState Project(
        ResearchJobStatus? status,
        bool runtimeAvailable,
        bool operationInProgress,
        bool hasActiveJob,
        bool remoteLifecycleAvailable,
        bool remoteDispatchEnabled)
    {
        if (remoteDispatchEnabled && !remoteLifecycleAvailable)
        {
            throw new ArgumentException(
                "Remote dispatch cannot be enabled when provider lifecycle support is unavailable.",
                nameof(remoteDispatchEnabled));
        }

        var cloudDisclosure = CloudDisclosure(remoteLifecycleAvailable, remoteDispatchEnabled);
        var resumeLabel = status?.CanRecoverInterrupted == true
            ? RearmInterruptedStageLabel
            : ResumeNextStageLabel;

        if (operationInProgress)
        {
            return new ResearchProductUiState(
                StartEnabled: false,
                ResumeEnabled: false,
                ResumeLabel: resumeLabel,
                ReconcileEnabled: false,
                CancelEnabled: runtimeAvailable && hasActiveJob,
                CloudDisclosureText: cloudDisclosure);
        }

        var requiresReconciliation = status?.RequiresRemoteReconciliation == true;
        var resumeEnabled = status is not null
            && !requiresReconciliation
            && (status.CanRunNextStep || status.CanRecoverInterrupted);
        var reconcileEnabled = requiresReconciliation && remoteLifecycleAvailable;
        var cancelEnabled = status?.CanCancel == true
            && (!requiresReconciliation || remoteLifecycleAvailable);

        return new ResearchProductUiState(
            StartEnabled: runtimeAvailable,
            ResumeEnabled: resumeEnabled,
            ResumeLabel: resumeLabel,
            ReconcileEnabled: reconcileEnabled,
            CancelEnabled: cancelEnabled,
            CloudDisclosureText: cloudDisclosure);
    }

    private static string CloudDisclosure(bool remoteLifecycleAvailable, bool remoteDispatchEnabled)
    {
        if (!remoteLifecycleAvailable)
        {
            return "Cloud execution is locked: local Nemotron + Tavily research is available, but Nebius lifecycle controls remain disabled until the live deployment contract is proven and composed.";
        }

        return remoteDispatchEnabled
            ? "Nebius lifecycle + new dispatch are enabled in this composition. Consequential cloud execution still requires explicit scoped approval."
            : "Nebius lifecycle reconciliation is available; new Serverless dispatch remains locked.";
    }
}
