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

    internal BrowserProductRuntime(BrowserHostRuntime host, SessionEvidenceLedger? sessionEvidence = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _sessionEvidence = new BrowserSessionEvidenceRecorder(sessionEvidence ?? SessionEvidenceLedger.ProcessLocal);
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

    public Task<IReadOnlyList<BrowserDownloadRecord>> ListDownloadsAsync(
        CancellationToken cancellationToken = default) =>
        _host.ListDownloadsAsync(cancellationToken);

    public Task<BrowserDownloadHandoffPlan> PrepareDownloadHandoffAsync(
        Guid downloadId,
        string destinationDirectory,
        CancellationToken cancellationToken = default) =>
        _host.PrepareDownloadHandoffAsync(downloadId, destinationDirectory, cancellationToken);

    public Task<BrowserDownloadExportReceipt> ApproveAndExportDownloadAsync(
        BrowserDownloadHandoffPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default) =>
        _host.ApproveAndExportDownloadAsync(approvedPlan, exactScope, cancellationToken);

    public Task<BrowserDownloadDiscardPlan> PrepareDownloadDiscardAsync(
        Guid downloadId,
        CancellationToken cancellationToken = default) =>
        _host.PrepareDownloadDiscardAsync(downloadId, cancellationToken);

    public Task<BrowserDownloadDiscardReceipt> ApproveAndDiscardDownloadAsync(
        BrowserDownloadDiscardPlan approvedPlan,
        string exactScope,
        CancellationToken cancellationToken = default) =>
        _host.ApproveAndDiscardDownloadAsync(approvedPlan, exactScope, cancellationToken);
}
