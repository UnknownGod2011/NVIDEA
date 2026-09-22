using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class CompositionLifetimeGateTests
{
    [Fact]
    public async Task BeginDispose_waits_for_active_acquisition_then_rejects_new_callers()
    {
        var gate = new CompositionLifetimeGate();
        await using var acquisition = await gate.AcquireAsync();

        var disposeTask = gate.BeginDisposeAsync().AsTask();
        await Task.Delay(25);
        Assert.False(disposeTask.IsCompleted);

        await acquisition.DisposeAsync();
        var disposal = await disposeTask;
        Assert.NotNull(disposal);

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await gate.AcquireAsync().AsTask());

        await disposal!.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await gate.AcquireAsync().AsTask());
    }

    [Fact]
    public async Task Waiting_acquisition_that_loses_to_disposal_fails_closed()
    {
        var gate = new CompositionLifetimeGate();
        await using var first = await gate.AcquireAsync();

        var disposeTask = gate.BeginDisposeAsync().AsTask();
        var waiter = gate.AcquireAsync().AsTask();

        await first.DisposeAsync();
        var disposal = await disposeTask;
        Assert.NotNull(disposal);
        Assert.False(waiter.IsCompletedSuccessfully);

        await disposal!.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await waiter);
    }

    [Fact]
    public async Task Cancelled_waiter_does_not_poison_gate()
    {
        var gate = new CompositionLifetimeGate();
        await using var first = await gate.AcquireAsync();
        using var cancellation = new CancellationTokenSource();

        var waiter = gate.AcquireAsync(cancellation.Token).AsTask();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await waiter);

        await first.DisposeAsync();
        await using var second = await gate.AcquireAsync();
    }

    [Fact]
    public async Task Disposal_is_idempotent_and_never_reopens_gate()
    {
        var gate = new CompositionLifetimeGate();
        var first = await gate.BeginDisposeAsync();
        Assert.NotNull(first);
        await first!.DisposeAsync();

        var second = await gate.BeginDisposeAsync();
        Assert.Null(second);
        await gate.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await gate.AcquireAsync().AsTask());
    }
}
