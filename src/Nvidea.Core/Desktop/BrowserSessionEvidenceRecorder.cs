using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Fail-closed projection from trusted browser outcomes into the payload-free judge-session ledger.
/// It never interprets browser/site text and never grants authority; it only observes outcomes that
/// have already crossed the durable browser verification and exact-approval boundaries.
/// </summary>
internal sealed class BrowserSessionEvidenceRecorder
{
    private readonly SessionEvidenceLedger _ledger;

    public BrowserSessionEvidenceRecorder(SessionEvidenceLedger ledger) =>
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    /// <summary>
    /// Records browser proof only for a terminal completed outcome carrying the trusted verified-step
    /// checkpoint projection. Failed, cancelled, waiting, retryable, pending and ambiguous-running
    /// outcomes cannot qualify even if malformed callers attach unrelated data.
    /// </summary>
    public void ObserveOutcome(BrowserJobOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome.State == AgentJobState.Completed && outcome.VerifiedStep is not null)
            _ledger.Record(SessionEvidenceKind.BrowserPostStateVerified);
    }

    /// <summary>
    /// Called only after the trusted host accepts the exact scope and returns from its approval-resume
    /// boundary. Merely displaying a prompt, denying it, cancelling it, or submitting a mismatched
    /// scope never calls this method.
    /// </summary>
    public void ObserveAcceptedConsequentialApproval() =>
        _ledger.Record(SessionEvidenceKind.ConsequentialApprovalGateExercised);
}
