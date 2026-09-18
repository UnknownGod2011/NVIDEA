using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserCancellationBoundaryTests
{
    [Fact]
    public async Task ExecutePlan_CancellationAfterVerifiedAction_PreventsQueuedConsequentialAction()
    {
        using var cancellation = new CancellationTokenSource();
        var before = Observation("https://example.com/inbox", "Inbox", "1");
        var after = Observation("https://example.com/message/1", "Message", "2");
        var driver = new RecordingDriver(before, after);
        var approval = new RecordingApprovalGate();
        var verifier = new CancelAfterVerificationVerifier(cancellation);
        var executor = new BrowserAgentExecutor(
            driver,
            new BrowserSafetyPolicy(),
            approval,
            verifier);

        var plan = new[]
        {
            new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("link", "Open message")),
            // This is deliberately consequential: BrowserSafetyPolicy requires approval for Send.
            // Cancellation must be observed before this queued action can even reach approval.
            new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("button", "Send"))
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.ExecutePlanAsync(plan, cancellationToken: cancellation.Token));

        Assert.Equal(1, driver.ExecuteCount);
        Assert.Single(driver.ExecutedActions);
        Assert.Equal("Open message", driver.ExecutedActions[0].Locator?.Name);
        Assert.Equal(1, verifier.VerifyCount);
        Assert.Equal(0, approval.RequestCount);
    }

    private static BrowserObservation Observation(string url, string title, string snapshot) =>
        new(
            new Uri(url),
            title,
            Array.Empty<BrowserElement>(),
            string.Empty,
            DateTimeOffset.UtcNow,
            SnapshotId: snapshot);

    private sealed class RecordingDriver : IBrowserDriver
    {
        private readonly BrowserObservation _before;
        private readonly BrowserObservation _after;
        private int _observeCount;

        public RecordingDriver(BrowserObservation before, BrowserObservation after)
        {
            _before = before;
            _after = after;
        }

        public int ExecuteCount { get; private set; }
        public List<BrowserAction> ExecutedActions { get; } = new();

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _observeCount++;
            return Task.FromResult(_observeCount == 1 ? _before : _after);
        }

        public Task ExecuteAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExecuteCount++;
            ExecutedActions.Add(action);
            return Task.CompletedTask;
        }
    }

    private sealed class CancelAfterVerificationVerifier : IBrowserActionVerifier
    {
        private readonly CancellationTokenSource _cancellation;

        public CancelAfterVerificationVerifier(CancellationTokenSource cancellation) =>
            _cancellation = cancellation;

        public int VerifyCount { get; private set; }

        public Task<(bool Verified, string Detail)> VerifyAsync(
            BrowserAction action,
            BrowserObservation before,
            BrowserObservation after,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VerifyCount++;
            _cancellation.Cancel();
            return Task.FromResult((true, "First action verified; synthetic emergency stop requested."));
        }
    }

    private sealed class RecordingApprovalGate : IBrowserApprovalGate
    {
        public int RequestCount { get; private set; }

        public Task<bool> RequestApprovalAsync(
            BrowserAction action,
            BrowserActionDecision decision,
            BrowserObservation observation,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            return Task.FromResult(true);
        }
    }
}
