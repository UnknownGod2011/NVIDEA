using Nvidea.Core.Browser;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Product-facing browser authority boundary. UI/plugin callers receive only the browser operations
/// that preserve NVIDEA's durable-job, exact-approval, download-quarantine and cancellation policy.
/// The underlying <see cref="BrowserHostRuntime"/> remains trusted infrastructure and is never
/// returned to product callers. Browser outcomes are projected again here so durable/provider/tool
/// diagnostics and raw site-controlled presentation text never become product API output.
/// </summary>
public sealed class BrowserProductRuntime
{
    private readonly BrowserHostRuntime _host;
    private readonly BrowserSessionEvidenceRecorder _sessionEvidence;
    private readonly DurableBrowserVerificationPublisher? _verificationPublisher;
    private readonly Func<CancellationToken, Task<DesktopBrowserVerificationPresentation>>? _verificationReader;

    internal BrowserProductRuntime(
        BrowserHostRuntime host,
        SessionEvidenceLedger? sessionEvidence = null,
        DurableBrowserVerificationPublisher? verificationPublisher = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _sessionEvidence = new BrowserSessionEvidenceRecorder(sessionEvidence ?? SessionEvidenceLedger.ProcessLocal);
        _verificationPublisher = verificationPublisher;
    }

    /// <summary>
    /// Trusted composition path for production browser verification. Product code receives only a
    /// payload-free read delegate owned by the durable browser runtime; it never receives the
    /// publisher, protected receipt store, raw durable evidence, or approval authority.
    /// </summary>
    internal BrowserProductRuntime(
        BrowserHostRuntime host,
        BrowserDurableActionRuntime durableActions,
        SessionEvidenceLedger? sessionEvidence = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(durableActions);
        _sessionEvidence = new BrowserSessionEvidenceRecorder(sessionEvidence ?? SessionEvidenceLedger.ProcessLocal);
        _verificationReader = durableActions.ReadVerificationPresentationAsync;
    }

    public async Task<BrowserJobOutcome> StartActionAsync(
        BrowserAction action,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _host.StartActionAsync(action, cancellationToken).ConfigureAwait(false);
        var projected = BrowserProductOutcomeTrust.Project(outcome);
        _sessionEvidence.ObserveOutcome(projected);
        return projected;
    }

    public async Task<BrowserJobOutcome> ApproveAndResumeAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _host.ApproveAndResumeAsync(jobId, exactScope, cancellationToken).ConfigureAwait(false);
        // Reaching this point proves the trusted host accepted the exact scope and completed its
        // approval-resume boundary. Rejected/mismatched scopes and cancellation throw before here.
        _sessionEvidence.ObserveAcceptedConsequentialApproval();
        var projected = BrowserProductOutcomeTrust.Project(outcome);
        _sessionEvidence.ObserveOutcome(projected);
        return projected;
    }

    public async Task<BrowserJobOutcome> CancelAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _host.CancelAsync(jobId, cancellationToken).ConfigureAwait(false);
        return BrowserProductOutcomeTrust.Project(outcome);
    }

    /// <summary>
    /// Returns only the Core-owned, payload-free durable browser verification projection suitable for
    /// judge-facing UI. Production composition prefers the least-authority durable-runtime reader.
    /// Legacy/internal publisher composition remains supported during migration; absent either source,
    /// this fails closed rather than deriving a green state from live milestones or raw browser outcomes.
    /// </summary>
    public Task<DesktopBrowserVerificationPresentation> ReadVerificationPresentationAsync(
        CancellationToken cancellationToken = default)
    {
        if (_verificationReader is not null)
            return _verificationReader(cancellationToken);
        if (_verificationPublisher is not null)
            return _verificationPublisher.ReadPresentationAsync(cancellationToken);

        return Task.FromResult(DesktopBrowserVerificationProjector.NotVerifiedDurable(
            "Durable browser verification evidence is not connected to this runtime."));
    }

    public Task<IReadOnlyList<BrowserDownloadRecord>> ListDownloadsAsync(
        CancellationToken cancellationToken = default) =>
        _host.ListDownloadsAsync(cancellationToken);

    public Task<BrowserDownloadHandoffPlan> PrepareDownloadHandoffAsync(
        Guid downloadId,
        string destinationDirectory,
        CancellationToken cancellationToken = default) =>
        _host.PrepareDownloadHandoffAsync(downloadId, destinationDirectory, cancellationToken);

    public async Task<BrowserDownloadExportReceipt> ApproveAndExportDownloadAsync(
        BrowserDownloadHandoffPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _host
            .ApproveAndExportDownloadAsync(approvedPlan, exactScope, cancellationToken)
            .ConfigureAwait(false);
        // Export is a consequential filesystem write. Count it only after the trusted host has
        // validated the exact scope and the verified quarantine handoff has actually completed.
        // Rejected scope, cancellation, validation failure, and failed export all throw before here.
        _sessionEvidence.ObserveAcceptedConsequentialApproval();
        return receipt;
    }

    public Task<BrowserDownloadDiscardPlan> PrepareDownloadDiscardAsync(
        Guid downloadId,
        CancellationToken cancellationToken = default) =>
        _host.PrepareDownloadDiscardAsync(downloadId, cancellationToken);

    public async Task<BrowserDownloadDiscardReceipt> ApproveAndDiscardDownloadAsync(
        BrowserDownloadDiscardPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _host
            .ApproveAndDiscardDownloadAsync(approvedPlan, exactScope, cancellationToken)
            .ConfigureAwait(false);
        // Discard is an irreversible filesystem delete. Evidence is downstream of successful
        // exact-scope authorization and deletion, never merely downstream of showing a prompt.
        // Rejected scope, cancellation, validation failure, and failed deletion all throw before here.
        _sessionEvidence.ObserveAcceptedConsequentialApproval();
        return receipt;
    }
}
