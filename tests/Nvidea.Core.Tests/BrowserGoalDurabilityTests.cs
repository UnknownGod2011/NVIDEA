using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalDurabilityTests
{
    [Fact]
    public async Task ResumeAsync_WaitingSessionRemainsPausedAndDoesNotRecreateApproval()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-resume-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var original = BrowserGoalSession.Create("Submit the draft") with
            {
                Status = BrowserGoalStatus.WaitingForApproval,
                PendingJobId = Guid.NewGuid(),
                PendingExactScope = "capability:browser.agent:exact-submit",
                PendingAction = new BrowserAction(BrowserActionKind.Click, BrowserLocator.Accessibility("e-9"))
            };
            await store.SaveAsync(original);

            var host = new NoExecutionHost();
            var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(new ThrowingInferenceClient()), store);
            var restored = await agent.ResumeAsync(original.SessionId);

            Assert.Equal(BrowserGoalStatus.WaitingForApproval, restored.Status);
            Assert.Equal(original.PendingJobId, restored.PendingJobId);
            Assert.Equal(original.PendingExactScope, restored.PendingExactScope);
            Assert.Null(restored.PendingAction);
            Assert.Equal(0, host.ObserveCount);
            Assert.Equal(0, host.StartCount);
            Assert.Equal(0, host.ApproveCount);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunUntilPauseAsync_ExpiredWallClockStopsBeforeObservationOrInference()
    {
        var host = new NoExecutionHost();
        var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(new ThrowingInferenceClient()));
        var session = BrowserGoalSession.Create("Inspect the page", maxWallClockSeconds: 30) with
        {
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
        };

        var result = await agent.RunUntilPauseAsync(session);

        Assert.Equal(BrowserGoalStatus.BudgetExhausted, result.Status);
        Assert.Contains("wall-clock", result.Detail!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, host.ObserveCount);
        Assert.Equal(0, host.StartCount);
    }

    [Fact]
    public async Task RunUntilPauseAsync_ContextBudgetStopsBeforeNemotronCall()
    {
        var host = new LargeObservationHost();
        var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(new ThrowingInferenceClient()));
        var session = BrowserGoalSession.Create(
            "Inspect the page",
            maxPlannerContextCharacters: 8_000);

        var result = await agent.RunUntilPauseAsync(session);

        Assert.Equal(BrowserGoalStatus.BudgetExhausted, result.Status);
        Assert.Contains("planner-context", result.Detail!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, host.ObserveCount);
        Assert.Equal(0, host.StartCount);
        Assert.Equal(0, result.PlannerTurnCount);
    }

    private sealed class NoExecutionHost : IBrowserGoalHost
    {
        public int ObserveCount { get; private set; }
        public int StartCount { get; private set; }
        public int ApproveCount { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            ObserveCount++;
            throw new InvalidOperationException("Observation should not be requested in this test.");
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            StartCount++;
            throw new InvalidOperationException("Browser action should not execute in this test.");
        }

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
        {
            ApproveCount++;
            throw new InvalidOperationException("Approval should not be recreated during resume.");
        }

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserJobOutcome(jobId, AgentJobState.Cancelled, "cancelled"));
    }

    private sealed class LargeObservationHost : IBrowserGoalHost
    {
        public int ObserveCount { get; private set; }
        public int StartCount { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserveCount++;
            return Task.FromResult(new BrowserObservation(
                new Uri("https://example.com"),
                "Large page",
                Array.Empty<BrowserElement>(),
                new string('x', 9_000),
                DateTimeOffset.UtcNow));
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            StartCount++;
            throw new InvalidOperationException("Browser action should not execute after context budget exhaustion.");
        }

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not used.");

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserJobOutcome(jobId, AgentJobState.Cancelled, "cancelled"));
    }

    private sealed class ThrowingInferenceClient : IAgentInferenceClient
    {
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Nemotron should not be called after a budget is exhausted or while approval is paused.");
    }
}
