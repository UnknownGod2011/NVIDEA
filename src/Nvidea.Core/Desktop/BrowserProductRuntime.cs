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
    private readonly Func<CancellationToken, Task<DesktopBrowserVerificationPresentation>> _verificationReader;
    private readonly CompositionLifetimeGate _lifetime;

    /// <summary>
    /// Trusted composition path for production browser verification. Product code receives only a
    /// payload-free read delegate owned by the durable browser runtime; it never receives the
    /// publisher, protected receipt store, raw durable evidence, or approval authority.
    /// Requiring <paramref name="durableActions"/> prevents construction of a product runtime that
    /// can execute browser actions while being disconnected from their authoritative verification.
    /// The shared composition lifetime prevents an already-issued facade from starting work after
    /// root shutdown and makes root disposal wait for any product operation already in flight.
    /// </summary>
    internal BrowserProductRuntime(
        BrowserHostRuntime host,
        BrowserDurableActionRuntime durableActions,
        CompositionLifetimeGate lifetime,
        SessionEvidenceLedger? sessionEvidence = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(durableActions);
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _sessionEvidence = new BrowserSessionEvidenceRecorder(sessionEvidence ?? SessionEvidenceLedger.ProcessLocal);
        _verificationReader = durableActions.ReadVerificationPresentationAsync;
    }

    public async Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var outcome = await _host.StartActionAsync(action, cancellationToken).ConfigureAwait(false);
        var projected = BrowserProductOutcomeTrust.Project(outcome);
        _sessionEvidence.ObserveOutcome(projected);
        return projected;
    }

    public async Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var outcome = await _host.ApproveAndResumeAsync(jobId, exactScope, cancellationToken).ConfigureAwait(false);
        _sessionEvidence.ObserveAcceptedConsequentialApproval();
        var projected = BrowserProductOutcomeTrust.Project(outcome);
        _sessionEvidence.ObserveOutcome(projected);
        return projected;
    }

    public async Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var outcome = await _host.CancelAsync(jobId, cancellationToken).ConfigureAwait(false);
        return BrowserProductOutcomeTrust.Project(outcome);
    }

    /// <summary>Returns only the Core-owned, payload-free durable browser verification projection.</summary>
    public async Task<DesktopBrowserVerificationPresentation> ReadVerificationPresentationAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        return await _verificationReader(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BrowserDownloadRecord>> ListDownloadsAsync(CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        return await _host.ListDownloadsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserDownloadHandoffPlan> PrepareDownloadHandoffAsync(Guid downloadId, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        return await _host.PrepareDownloadHandoffAsync(downloadId, destinationDirectory, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserDownloadExportReceipt> ApproveAndExportDownloadAsync(BrowserDownloadHandoffPlan approvedPlan, string exactScope, CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var receipt = await _host.ApproveAndExportDownloadAsync(approvedPlan, exactScope, cancellationToken).ConfigureAwait(false);
        _sessionEvidence.ObserveAcceptedConsequentialApproval();
        return receipt;
    }

    public async Task<BrowserDownloadDiscardPlan> PrepareDownloadDiscardAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        return await _host.PrepareDownloadDiscardAsync(downloadId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserDownloadDiscardReceipt> ApproveAndDiscardDownloadAsync(BrowserDownloadDiscardPlan approvedPlan, string exactScope, CancellationToken cancellationToken = default)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        var receipt = await _host.ApproveAndDiscardDownloadAsync(approvedPlan, exactScope, cancellationToken).ConfigureAwait(false);
        _sessionEvidence.ObserveAcceptedConsequentialApproval();
        return receipt;
    }
}
