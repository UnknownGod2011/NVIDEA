using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserVerificationActionLifecycleTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "nvidea-browser-verification-lifecycle-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AdmitAsync_ClearsStaleEvidenceBeforeDurableCreate()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        await File.WriteAllTextAsync(receiptPath, "stale-green-evidence");

        var createObservedReceipt = true;
        var expected = CreateJob(AgentJobState.Pending);

        var actual = await lifecycle.AdmitAsync(_ =>
        {
            createObservedReceipt = File.Exists(receiptPath);
            return Task.FromResult(expected);
        });

        Assert.Same(expected, actual);
        Assert.False(createObservedReceipt);
        Assert.False(File.Exists(receiptPath));
    }

    [Fact]
    public async Task AdmitAsync_ClearFailure_PreventsDurableCreate()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        Directory.CreateDirectory(receiptPath);
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        var createCalled = false;

        await Assert.ThrowsAnyAsync<Exception>(() => lifecycle.AdmitAsync(_ =>
        {
            createCalled = true;
            return Task.FromResult(CreateJob(AgentJobState.Pending));
        }));

        Assert.False(createCalled);
    }

    [Fact]
    public async Task AdvanceAsync_ClearsStaleEvidenceBeforeExecutionAttempt()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        await File.WriteAllTextAsync(receiptPath, "stale-green-evidence");

        var executionObservedReceipt = true;
        var authoritative = CreateJob(AgentJobState.WaitingForApproval);

        var result = await lifecycle.AdvanceAsync(_ =>
        {
            executionObservedReceipt = File.Exists(receiptPath);
            return Task.FromResult(authoritative);
        });

        Assert.Same(authoritative, result.AuthoritativeJob);
        Assert.False(executionObservedReceipt);
        Assert.False(File.Exists(receiptPath));
        Assert.False(result.Published);
        Assert.False(result.PublicationFailed);
    }

    [Fact]
    public async Task AdvanceAsync_ClearFailure_PreventsExecutionAttempt()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        Directory.CreateDirectory(receiptPath);
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        var executionCalled = false;

        await Assert.ThrowsAnyAsync<Exception>(() => lifecycle.AdvanceAsync(_ =>
        {
            executionCalled = true;
            return Task.FromResult(CreateJob(AgentJobState.Completed));
        }));

        Assert.False(executionCalled);
    }

    [Fact]
    public async Task AdvanceAsync_NonCompletedRecord_IsReturnedUnchangedWithoutPublication()
    {
        Directory.CreateDirectory(_root);
        var runtime = BrowserVerificationRuntime.Create(
            Path.Combine(_root, "receipt.protected"),
            new ReversibleTestProtector());
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication);
        var authoritative = CreateJob(AgentJobState.WaitingForApproval);

        var result = await lifecycle.AdvanceAsync(_ => Task.FromResult(authoritative));

        Assert.Same(authoritative, result.AuthoritativeJob);
        Assert.False(result.Published);
        Assert.False(result.PublicationFailed);
        Assert.False(result.EvidenceVerified);
    }

    [Fact]
    public async Task AdvanceAsync_ObserverRunsAfterClearAndBeforeExecution_WithoutPayloadAuthority()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        var runtime = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        await File.WriteAllTextAsync(receiptPath, "stale-green-evidence");

        var observerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseObserver = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new PausingObserver(observerEntered, releaseObserver);
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication, observer);
        var executionCalled = false;

        var advance = lifecycle.AdvanceAsync(_ =>
        {
            executionCalled = true;
            return Task.FromResult(CreateJob(AgentJobState.WaitingForApproval));
        });

        await observerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(BrowserVerificationLifecycleStage.ExecutionEvidenceCleared, observer.LastStage);
        Assert.False(File.Exists(receiptPath));
        Assert.False(executionCalled);
        Assert.False(advance.IsCompleted);

        releaseObserver.TrySetResult();
        await advance.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(executionCalled);
    }

    [Fact]
    public async Task AdvanceAsync_CancelWhileObserverPaused_NeverExecutesBrowserBoundary()
    {
        Directory.CreateDirectory(_root);
        var runtime = BrowserVerificationRuntime.Create(
            Path.Combine(_root, "receipt.protected"),
            new ReversibleTestProtector());
        var observerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseObserver = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observer = new PausingObserver(observerEntered, releaseObserver);
        var lifecycle = new BrowserVerificationActionLifecycle(runtime.Publication, observer);
        var executionCalled = false;
        using var cancellation = new CancellationTokenSource();

        var advance = lifecycle.AdvanceAsync(_ =>
        {
            executionCalled = true;
            return Task.FromResult(CreateJob(AgentJobState.Completed));
        }, cancellation.Token);

        await observerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => advance);
        Assert.False(executionCalled);
    }

    private static AgentJobRecord CreateJob(AgentJobState state)
    {
        var now = DateTimeOffset.UtcNow;
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                "browser.action",
                "browser.agent",
                new HashSet<DataPermission> { DataPermission.BrowserRead },
                CapabilityRiskLevel.Medium,
                ContainsPrivateOsData: true,
                BenefitsFromBackgroundExecution: false),
            state,
            JobExecutionLocation.Local,
            Attempt: 0,
            Checkpoint: null,
            ApprovalScope: null,
            LastError: null,
            CreatedAt: now,
            UpdatedAt: now);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
        }
    }

    private sealed class PausingObserver : IBrowserVerificationLifecycleObserver
    {
        private readonly TaskCompletionSource _entered;
        private readonly TaskCompletionSource _release;

        internal PausingObserver(TaskCompletionSource entered, TaskCompletionSource release)
        {
            _entered = entered;
            _release = release;
        }

        internal BrowserVerificationLifecycleStage? LastStage { get; private set; }

        public async ValueTask ObserveAsync(
            BrowserVerificationLifecycleStage stage,
            CancellationToken cancellationToken)
        {
            LastStage = stage;
            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class ReversibleTestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
