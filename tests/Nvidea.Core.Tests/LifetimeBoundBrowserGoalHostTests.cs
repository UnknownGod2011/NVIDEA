using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class LifetimeBoundBrowserGoalHostTests
{
    [Fact]
    public async Task InFlightHostCall_HoldsCompositionDisposalUntilCallCompletes()
    {
        var lifetime = new CompositionLifetimeGate();
        var inner = new BlockingHost();
        var host = new LifetimeBoundBrowserGoalHost(inner, lifetime);

        var operation = host.ObserveAsync();
        await inner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var disposeTask = lifetime.BeginDisposeAsync().AsTask();
        await Task.Delay(50);
        Assert.False(disposeTask.IsCompleted);

        inner.Release.TrySetResult();
        await operation;

        var disposalLease = await disposeTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(disposalLease);
        await disposalLease!.DisposeAsync();
    }

    [Fact]
    public async Task HostCall_AfterCompositionDisposal_FailsBeforeInnerAuthorityRuns()
    {
        var lifetime = new CompositionLifetimeGate();
        var inner = new BlockingHost(released: true);
        var host = new LifetimeBoundBrowserGoalHost(inner, lifetime);

        var disposalLease = await lifetime.BeginDisposeAsync();
        Assert.NotNull(disposalLease);
        await disposalLease!.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => host.ObserveAsync());
        Assert.Equal(0, inner.ObserveCalls);
    }

    [Fact]
    public async Task CancelledWait_DoesNotInvokeInnerAuthorityOrCorruptLifetime()
    {
        var lifetime = new CompositionLifetimeGate();
        var heldLease = await lifetime.AcquireAsync();
        var inner = new BlockingHost(released: true);
        var host = new LifetimeBoundBrowserGoalHost(inner, lifetime);
        using var cancellation = new CancellationTokenSource();

        var waiting = host.ObserveAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.Equal(0, inner.ObserveCalls);

        await heldLease.DisposeAsync();
        var successful = await host.ObserveAsync();
        Assert.NotNull(successful);
        Assert.Equal(1, inner.ObserveCalls);
    }

    private sealed class BlockingHost : ICrashConsistentBrowserGoalHost
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _observeCalls;

        internal BlockingHost(bool released = false)
        {
            if (released) _release.TrySetResult();
        }

        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release => _release;
        internal int ObserveCalls => Volatile.Read(ref _observeCalls);

        public async Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _observeCalls);
            Entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new BrowserObservation(new Uri("https://example.test/"), "Example", Array.Empty<BrowserElement>(), Array.Empty<string>());
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
