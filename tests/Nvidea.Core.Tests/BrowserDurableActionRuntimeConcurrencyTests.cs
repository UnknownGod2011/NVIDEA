using System.Reflection;
using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

/// <summary>
/// Structural regression coverage for the browser verification linearization boundary.
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
}
