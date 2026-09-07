using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class BrowserAmbiguousRecoveryTests
{
    [Fact]
    public void TryVerify_Navigate_RequiresCurrentUrlToMatchDestination()
    {
        var action = new BrowserAction(
            BrowserActionKind.Navigate,
            Destination: new Uri("https://example.com/finished#fragment"));
        var matching = Observation("https://example.com/finished");
        var different = Observation("https://example.com/other");

        Assert.True(BrowserAmbiguousStateReconciler.TryVerify(action, matching).Verified);
        Assert.False(BrowserAmbiguousStateReconciler.TryVerify(action, different).Verified);
    }

    [Fact]
    public void TryVerify_Click_WithExpectedState_UsesFreshVisibleEvidence()
    {
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Submit"),
            ExpectedState: "Saved successfully");
        var matching = Observation("https://example.com/form", visibleText: "Saved successfully");
        var different = Observation("https://example.com/form", visibleText: "Ready");

        Assert.True(BrowserAmbiguousStateReconciler.TryVerify(action, matching).Verified);
        Assert.False(BrowserAmbiguousStateReconciler.TryVerify(action, different).Verified);
    }

    [Theory]
    [InlineData(BrowserActionKind.Upload)]
    [InlineData(BrowserActionKind.Download)]
    public void TryVerify_FileTransfer_NeverAutoReconcilesFromDomEvidence(BrowserActionKind kind)
    {
        var action = new BrowserAction(kind, ExpectedState: "Done");

        var result = BrowserAmbiguousStateReconciler.TryVerify(
            action,
            Observation("https://example.com/files", visibleText: "Done"));

        Assert.False(result.Verified);
        Assert.Contains("human", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecoverAsync_PositiveEvidence_RestoresParentWithoutReplay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-ambiguous-recovery-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var childId = Guid.NewGuid();
            var pendingAction = new BrowserAction(
                BrowserActionKind.Click,
                BrowserLocator.ByRole("button", "Submit"),
                ExpectedState: "Saved successfully");
            var session = BrowserGoalSession.Create("Submit the form") with
            {
                ActionCount = 1,
                PendingJobId = childId,
                PendingAction = pendingAction,
                Status = BrowserGoalStatus.Failed,
                Detail = "ambiguous"
            };
            await store.SaveAsync(session);

            var verified = new BrowserGoalVerifiedStep(
                childId,
                BrowserActionKind.Click,
                new Uri("https://example.com/form"),
                new Uri("https://example.com/form"),
                "Crash reconciliation observed the intended expected state: Saved successfully",
                DateTimeOffset.UtcNow);
            var host = new FakeRecoveryHost(
                new BrowserJobOutcome(childId, AgentJobState.Running, "ambiguous"),
                new BrowserAmbiguousRecoveryResult(
                    BrowserAmbiguousRecoveryStatus.Reconciled,
                    childId,
                    "Expected state is present.",
                    new BrowserJobOutcome(childId, AgentJobState.Completed, "reconciled", VerifiedStep: verified),
                    Observation("https://example.com/form", visibleText: "Saved successfully")));
            var service = new BrowserAmbiguousRecoveryService(host, store);

            var (recovered, result) = await service.RecoverAsync(session.SessionId);

            Assert.Equal(BrowserAmbiguousRecoveryStatus.Reconciled, result.Status);
            Assert.Equal(BrowserGoalStatus.Running, recovered.Status);
            Assert.Null(recovered.PendingJobId);
            Assert.Null(recovered.PendingAction);
            Assert.Contains(recovered.VerifiedSteps!, step => step.JobId == childId);
            Assert.Equal(1, host.ReconcileCount);
            Assert.Equal(0, host.ExecuteCount);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RecoverAsync_InconclusiveEvidence_PreservesChildForHumanResolution()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-ambiguous-human-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var childId = Guid.NewGuid();
            var pendingAction = new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("button", "Submit"));
            var session = BrowserGoalSession.Create("Submit the form") with
            {
                ActionCount = 1,
                PendingJobId = childId,
                PendingAction = pendingAction,
                Status = BrowserGoalStatus.Failed,
                Detail = "ambiguous"
            };
            await store.SaveAsync(session);

            var running = new BrowserJobOutcome(childId, AgentJobState.Running, "ambiguous");
            var host = new FakeRecoveryHost(
                running,
                new BrowserAmbiguousRecoveryResult(
                    BrowserAmbiguousRecoveryStatus.NeedsHumanResolution,
                    childId,
                    "No deterministic expected-state assertion is available.",
                    running,
                    Observation("https://example.com/form")));
            var service = new BrowserAmbiguousRecoveryService(host, store);

            var (recovered, result) = await service.RecoverAsync(session.SessionId);

            Assert.Equal(BrowserAmbiguousRecoveryStatus.NeedsHumanResolution, result.Status);
            Assert.Equal(BrowserGoalStatus.Failed, recovered.Status);
            Assert.Equal(childId, recovered.PendingJobId);
            Assert.Equal(pendingAction, recovered.PendingAction);
            Assert.Contains("human resolution", recovered.Detail!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, host.ReconcileCount);
            Assert.Equal(0, host.ExecuteCount);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static BrowserObservation Observation(string url, string visibleText = "ready") => new(
        new Uri(url),
        "Example",
        Array.Empty<BrowserElement>(),
        visibleText,
        DateTimeOffset.UtcNow,
        SnapshotId: Guid.NewGuid().ToString("N"));

    private sealed class FakeRecoveryHost : IBrowserAmbiguousRecoveryHost
    {
        private readonly BrowserJobOutcome _current;
        private readonly BrowserAmbiguousRecoveryResult _recovery;

        public FakeRecoveryHost(BrowserJobOutcome current, BrowserAmbiguousRecoveryResult recovery)
        {
            _current = current;
            _recovery = recovery;
        }

        public int ReconcileCount { get; private set; }
        public int ExecuteCount { get; private set; }

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<BrowserJobOutcome?>(_current);
        }

        public Task<BrowserAmbiguousRecoveryResult> TryReconcileAmbiguousAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReconcileCount++;
            return Task.FromResult(_recovery);
        }
    }
}
