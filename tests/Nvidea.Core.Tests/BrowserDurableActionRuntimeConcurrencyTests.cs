using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

/// <summary>
/// Structural and behavioral regression coverage for the browser verification linearization boundary.
/// These tests intentionally avoid Chromium/DPAPI so they can guard the concurrency contract
/// wherever the Core test assembly can run.
/// </summary>
public sealed class BrowserDurableActionRuntimeConcurrencyTests
{
    [Fact]
    public void TransitionGate_IsPrivateInstanceState_AndCannotBecomeProductAuthority()
    {
        var runtimeType = typeof(BrowserDurableActionRuntime);
        var gate = runtimeType.GetField("_transitionGate", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(gate);
        Assert.True(gate!.IsPrivate);
        Assert.False(gate.IsStatic);
        Assert.Equal(typeof(SemaphoreSlim), gate.FieldType);
    }

    [Fact]
    public void AllReceiptMutatingOperations_AndJudgeRead_UseSingleSerializationBoundary()
    {
        var runtimeType = typeof(BrowserDurableActionRuntime);
        var serialize = runtimeType.GetMethod(
            "SerializeTransitionAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(serialize);
        Assert.True(serialize!.IsPrivate);
        Assert.True(serialize.IsGenericMethodDefinition);

        var serializedOperations = new[]
        {
            nameof(BrowserDurableActionRuntime.CreateAsync),
            nameof(BrowserDurableActionRuntime.RunNextStepAsync),
            nameof(BrowserDurableActionRuntime.ResumeAfterApprovalAndRunNextStepAsync),
            nameof(BrowserDurableActionRuntime.RearmApprovalAsync),
            nameof(BrowserDurableActionRuntime.CancelAsync),
            nameof(BrowserDurableActionRuntime.CompleteAmbiguousRunningWithoutVerificationAsync),
            nameof(BrowserDurableActionRuntime.ReadVerificationPresentationAsync),
        };

        foreach (var operation in serializedOperations)
        {
            var method = runtimeType.GetMethod(
                operation,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            Assert.False(method!.IsPublic);
            Assert.False(method.IsStatic);
        }

        // The runtime must expose no public instance methods declared by itself. Product code receives
        // the narrower BrowserProductRuntime instead, so neither serialization nor evidence authority
        // can accidentally become a public synchronization primitive.
        Assert.Empty(runtimeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
    }

    [Fact]
    public async Task SerializationBoundary_BlocksCompetingTransition_UntilCurrentTransitionSettles()
    {
        var runtime = CreateRuntimeWithoutDependencies();
        var serialize = GetSerializationMethod<int>();
        var firstEntered = NewSignal();
        var releaseFirst = NewSignal();
        var secondEntered = NewSignal();
        var order = new List<string>();

        var first = InvokeSerializedAsync(
            runtime,
            serialize,
            async token =>
            {
                order.Add("first-enter");
                firstEntered.TrySetResult();
                await releaseFirst.Task.WaitAsync(token);
                order.Add("first-exit");
                return 1;
            });

        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = InvokeSerializedAsync(
            runtime,
            serialize,
            token =>
            {
                token.ThrowIfCancellationRequested();
                order.Add("second-enter");
                secondEntered.TrySetResult();
                return Task.FromResult(2);
            });

        // The competing transition has been started while the first transition is deliberately paused.
        // It must not enter the protected section or observe/mutate the transient clear/execute state.
        await Task.Delay(50);
        Assert.False(secondEntered.Task.IsCompleted);
        Assert.False(second.IsCompleted);

        releaseFirst.TrySetResult();

        Assert.Equal(1, await first.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(2, await second.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(new[] { "first-enter", "first-exit", "second-enter" }, order);
    }

    [Fact]
    public async Task SerializationBoundary_CancelledWaiter_NeverEntersCriticalSection()
    {
        var runtime = CreateRuntimeWithoutDependencies();
        var serialize = GetSerializationMethod<int>();
        var firstEntered = NewSignal();
        var releaseFirst = NewSignal();
        var cancelledEntered = 0;

        var first = InvokeSerializedAsync(
            runtime,
            serialize,
            async token =>
            {
                firstEntered.TrySetResult();
                await releaseFirst.Task.WaitAsync(token);
                return 1;
            });

        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        using var cancellation = new CancellationTokenSource();
        var cancelled = InvokeSerializedAsync(
            runtime,
            serialize,
            token =>
            {
                Interlocked.Increment(ref cancelledEntered);
                return Task.FromResult(2);
            },
            cancellation.Token);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        Assert.Equal(0, Volatile.Read(ref cancelledEntered));

        releaseFirst.TrySetResult();
        Assert.Equal(1, await first.WaitAsync(TimeSpan.FromSeconds(5)));
    }

#pragma warning disable SYSLIB0050 // FormatterServices is used only to exercise a dependency-free private synchronization helper.
    private static BrowserDurableActionRuntime CreateRuntimeWithoutDependencies() =>
        (BrowserDurableActionRuntime)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(
            typeof(BrowserDurableActionRuntime));
#pragma warning restore SYSLIB0050

    private static MethodInfo GetSerializationMethod<T>()
    {
        var method = typeof(BrowserDurableActionRuntime).GetMethod(
            "SerializeTransitionAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.MakeGenericMethod(typeof(T));
    }

    private static Task<T> InvokeSerializedAsync<T>(
        BrowserDurableActionRuntime runtime,
        MethodInfo method,
        Func<CancellationToken, Task<T>> transition,
        CancellationToken cancellationToken = default)
    {
        // The uninitialized runtime intentionally has no orchestrator/verification dependencies. Install only
        // the private gate required by this focused test so no browser, DPAPI, job-store, or provider authority
        // is constructed merely to prove semaphore behavior.
        var gate = typeof(BrowserDurableActionRuntime).GetField(
            "_transitionGate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(gate);
        if (gate!.GetValue(runtime) is null)
            gate.SetValue(runtime, new SemaphoreSlim(1, 1));

        try
        {
            return Assert.IsAssignableFrom<Task<T>>(
                method.Invoke(runtime, new object?[] { transition, cancellationToken }));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
