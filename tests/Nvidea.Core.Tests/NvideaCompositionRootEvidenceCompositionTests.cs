using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NvideaCompositionRootEvidenceCompositionTests
{
    [Fact]
    public void Internal_factory_returns_evidence_observer_without_starting_browser_runtime()
    {
        var inner = new PassiveHost();

        var composed = NvideaCompositionRoot.CreateObservedBrowserGoalHost(inner);

        Assert.IsType<EvidenceObservingBrowserGoalHost>(composed);
        Assert.NotSame(inner, composed);
    }

    private sealed class PassiveHost : ICrashConsistentBrowserGoalHost
    {
        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
