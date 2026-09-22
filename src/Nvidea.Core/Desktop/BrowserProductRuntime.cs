using Nvidea.Core.Browser;

namespace Nvidea.Core.Desktop;

/// <summary>Product-facing browser authority boundary.</summary>
public sealed class BrowserProductRuntime
{
    private readonly BrowserHostRuntime _host;
    private readonly BrowserSessionEvidenceRecorder _sessionEvidence;
    private readonly Func<CancellationToken, Task<DesktopBrowserVerificationPresentation>> _verificationReader;
    private CompositionLifetimeGate _lifetime;
    private bool _compositionLifetimeBound;

    internal BrowserProductRuntime(BrowserHostRuntime host, BrowserDurableActionRuntime durableActions, SessionEvidenceLedger? sessionEvidence = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(durableActions);
        _sessionEvidence = new BrowserSessionEvidenceRecorder(sessionEvidence ?? SessionEvidenceLedger.ProcessLocal);
        _verificationReader = durableActions.ReadVerificationPresentationAsync;
        // Keeps assembly-internal host/test composition functional. The trusted desktop root replaces
        // this unpublished local gate exactly once with its shutdown authority before publication.
        _lifetime = new CompositionLifetimeGate();
    }

    internal BrowserProductRuntime BindCompositionLifetime(CompositionLifetimeGate lifetime)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        if (_compositionLifetimeBound)
            throw new InvalidOperationException("Browser product lifetime authority is already bound.");
        _lifetime = lifetime;
        _compositionLifetimeBound = true;
        return this;
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
        return BrowserProductOutcomeTrust.Project(await _host.CancelAsync(jobId, cancellationToken).ConfigureAwait(false));
    }

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
