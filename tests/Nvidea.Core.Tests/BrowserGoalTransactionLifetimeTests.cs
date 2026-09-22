using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalTransactionLifetimeTests
{
    [Fact]
    public async Task Execute_fails_closed_before_binding()
    {
        var boundary = new BrowserGoalTransactionLifetime();
        var invoked = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.ExecuteAsync<int>(_ =>
        {
            invoked = true;
            return Task.FromResult(1);
        }));

        Assert.False(invoked);
    }

    [Fact]
    public void Binding_is_one_time()
    {
        var boundary = new BrowserGoalTransactionLifetime();
        var gate = new CompositionLifetimeGate();
        boundary.Bind(gate);

        Assert.Throws<InvalidOperationException>(() => boundary.Bind(gate));
    }

    [Fact]
    public async Task In_flight_transaction_holds_disposal_until_operation_finishes()
    {
        var boundary = new BrowserGoalTransactionLifetime();
        var gate = new CompositionLifetimeGate();
        boundary.Bind(gate);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var operation = boundary.ExecuteAsync(async _ =>
        {
            entered.SetResult();
            await release.Task.ConfigureAwait(false);
            return 42;
        });
        await entered.Task.ConfigureAwait(false);

        var dispose = gate.DisposeAsync().AsTask();
        Assert.False(dispose.IsCompleted);

        release.SetResult();
        Assert.Equal(42, await operation.ConfigureAwait(false));
        await dispose.ConfigureAwait(false);
    }

    [Fact]
    public async Task Transaction_after_disposal_fails_before_operation_executes()
    {
        var boundary = new BrowserGoalTransactionLifetime();
        var gate = new CompositionLifetimeGate();
        boundary.Bind(gate);
        await gate.DisposeAsync().ConfigureAwait(false);
        var invoked = false;

        await Assert.ThrowsAsync<ObjectDisposedException>(() => boundary.ExecuteAsync<int>(_ =>
        {
            invoked = true;
            return Task.FromResult(1);
        }));

        Assert.False(invoked);
    }

    [Fact]
    public async Task Cancellation_while_waiting_does_not_execute_operation_or_corrupt_gate()
    {
        var boundary = new BrowserGoalTransactionLifetime();
        var gate = new CompositionLifetimeGate();
        boundary.Bind(gate);
        await using var blocker = await gate.AcquireAsync().ConfigureAwait(false);
        using var cancellation = new CancellationTokenSource();
        var invoked = false;

        var waiting = boundary.ExecuteAsync<int>(_ =>
        {
            invoked = true;
            return Task.FromResult(1);
        }, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.False(invoked);
    }
}
