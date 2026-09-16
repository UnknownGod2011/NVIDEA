using Nvidea.Core.Browser;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Evidence-only decorator for the browser-goal host contract. It observes trusted host outcomes
/// after the underlying authority has completed; it never interprets site content, grants approval,
/// retries an action, or changes browser execution semantics.
///
/// This provides a single reusable seam for multi-step goal execution while BrowserHostRuntime
/// remains the authority for safety, durable jobs, exact approvals and post-action verification.
/// </summary>
internal sealed class EvidenceObservingBrowserGoalHost : ICrashConsistentBrowserGoalHost
{
    private readonly ICrashConsistentBrowserGoalHost _inner;
    private readonly BrowserSessionEvidenceRecorder _evidence;

    public EvidenceObservingBrowserGoalHost(
        BrowserHostRuntime host,
        SessionEvidenceLedger? ledger = null)
        : this(new BrowserHostAdapter(host), ledger)
    {
    }

    public EvidenceObservingBrowserGoalHost(
        ICrashConsistentBrowserGoalHost inner,
        SessionEvidenceLedger? ledger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _evidence = new BrowserSessionEvidenceRecorder(ledger ?? SessionEvidenceLedger.ProcessLocal);
    }

    public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) =>
        _inner.ObserveAsync(cancellationToken);

    public async Task<BrowserJobOutcome> StartActionAsync(
        BrowserAction action,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _inner.StartActionAsync(action, cancellationToken).ConfigureAwait(false);
        _evidence.ObserveOutcome(outcome);
        return outcome;
    }

    public async Task<BrowserJobOutcome> ApproveAndResumeAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        // Approval evidence is intentionally downstream of the trusted host call. Exceptions,
        // cancellation and exact-scope rejection therefore cannot become judge evidence.
        var outcome = await _inner.ApproveAndResumeAsync(jobId, exactScope, cancellationToken).ConfigureAwait(false);
        _evidence.ObserveAcceptedConsequentialApproval();
        _evidence.ObserveOutcome(outcome);
        return outcome;
    }

    public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _inner.CancelAsync(jobId, cancellationToken);

    public Task<BrowserJobOutcome> CreateActionAsync(
        Guid jobId,
        BrowserAction action,
        CancellationToken cancellationToken = default) =>
        _inner.CreateActionAsync(jobId, action, cancellationToken);

    public async Task<BrowserJobOutcome> AdvanceActionAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _inner.AdvanceActionAsync(jobId, cancellationToken).ConfigureAwait(false);
        _evidence.ObserveOutcome(outcome);
        return outcome;
    }

    public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _inner.GetAsync(jobId, cancellationToken);

    public Task<BrowserJobOutcome> RearmApprovalAsync(
        Guid jobId,
        string exactScope,
        CancellationToken cancellationToken = default) =>
        _inner.RearmApprovalAsync(jobId, exactScope, cancellationToken);

    /// <summary>
    /// Narrow authority-preserving adapter used only to compose the production BrowserHostRuntime
    /// behind this evidence decorator. Every call is a direct delegation; no approval, retry,
    /// recovery or browser-state semantics are introduced here.
    /// </summary>
    private sealed class BrowserHostAdapter : ICrashConsistentBrowserGoalHost
    {
        private readonly BrowserHostRuntime _host;

        public BrowserHostAdapter(BrowserHostRuntime host) =>
            _host = host ?? throw new ArgumentNullException(nameof(host));

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) =>
            _host.ObserveAsync(cancellationToken);

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) =>
            _host.StartActionAsync(action, cancellationToken);

        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default) =>
            _host.CreateActionAsync(jobId, action, cancellationToken);

        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            _host.AdvanceActionAsync(jobId, cancellationToken);

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            _host.GetAsync(jobId, cancellationToken);

        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            _host.RearmApprovalAsync(jobId, exactScope, cancellationToken);

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            _host.ApproveAndResumeAsync(jobId, exactScope, cancellationToken);

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            _host.CancelAsync(jobId, cancellationToken);
    }
}
