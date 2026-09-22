using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class CompositionIssuedFacadeLifetimeTests
{
    [Fact]
    public async Task IssuedFacade_OperationStartedBeforeDisposal_KeepsDisposalBehindOperation()
    {
        var gate = new CompositionLifetimeGate();
        var facade = new TestIssuedFacade(gate);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var operation = facade.RunAsync(async () =>
        {
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;

        var disposal = gate.BeginDisposeAsync().AsTask();
        await Task.Yield();
        Assert.False(disposal.IsCompleted);

        release.SetResult();
        await operation;

        await using var disposalLease = await disposal;
        Assert.NotNull(disposalLease);
    }

    [Fact]
    public async Task IssuedFacade_OperationStartedAfterDisposal_FailsClosed()
    {
        var gate = new CompositionLifetimeGate();
        var facade = new TestIssuedFacade(gate);

        await using var disposalLease = await gate.BeginDisposeAsync();
        Assert.NotNull(disposalLease);

        var invoked = false;
        await Assert.ThrowsAsync<ObjectDisposedException>(() => facade.RunAsync(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        }));
        Assert.False(invoked);
    }

    [Fact]
    public async Task IssuedFacade_CancelledWhileWaitingForDisposalBoundary_DoesNotInvokeOperation()
    {
        var gate = new CompositionLifetimeGate();
        var facade = new TestIssuedFacade(gate);
        await using var blocker = await gate.AcquireAsync();
        using var cts = new CancellationTokenSource();

        var invoked = false;
        var waiting = facade.RunAsync(() =>
        {
            invoked = true;
            return Task.CompletedTask;
        }, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.False(invoked);
    }

    private sealed class TestIssuedFacade
    {
        private readonly CompositionLifetimeGate _lifetime;

        public TestIssuedFacade(CompositionLifetimeGate lifetime) => _lifetime = lifetime;

        public async Task RunAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            await using var lease = await _lifetime.AcquireAsync(cancellationToken);
            await operation();
        }
    }
}
