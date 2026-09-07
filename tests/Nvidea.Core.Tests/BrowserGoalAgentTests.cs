using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalAgentTests
{
    [Fact]
    public async Task RunUntilPauseAsync_ReobservesAndStopsWhenNemotronMarksGoalComplete()
    {
        var inference = new SequenceInferenceClient(
            ClickDecision("e-1", "Loaded"),
            CompleteDecision("Goal satisfied."));
        var host = new FakeHost
        {
            StartResultFactory = _ => CompletedOutcome()
        };
        var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(inference));

        var result = await agent.RunUntilPauseAsync(BrowserGoalSession.Create("Open the panel"));

        Assert.Equal(BrowserGoalStatus.Completed, result.Status);
        Assert.Equal(1, result.ActionCount);
        Assert.Equal(2, host.ObserveCount);
        Assert.Single(host.StartedActions);
    }

    [Fact]
    public async Task RunUntilPauseAsync_ConsequentialActionStopsAtApprovalBoundary()
    {
        const string scope = "capability:browser.agent:job:abc:click:submit";
        var host = new FakeHost
        {
            StartResultFactory = action => new BrowserJobOutcome(
                Guid.Parse("00000000-0000-0000-0000-000000000123"),
                AgentJobState.WaitingForApproval,
                "approval required",
                new BrowserApprovalPrompt(
                    Guid.Parse("00000000-0000-0000-0000-000000000123"),
                    scope,
                    action.Kind,
                    "Submit demo",
                    "Submit",
                    DateTimeOffset.UtcNow))
        };
        var agent = new BrowserGoalAgent(
            host,
            new NemotronBrowserPlanner(new SequenceInferenceClient(ClickDecision("e-1", "Submitted"))));

        var result = await agent.RunUntilPauseAsync(BrowserGoalSession.Create("Submit the demo"));

        Assert.Equal(BrowserGoalStatus.WaitingForApproval, result.Status);
        Assert.Equal(scope, result.PendingExactScope);
        Assert.Equal(1, result.ActionCount);
        Assert.Equal(0, host.ApproveCount);
    }

    [Fact]
    public async Task ApproveAndContinueAsync_RejectsWrongScopeBeforeHostCall()
    {
        const string scope = "exact-scope";
        var host = new FakeHost();
        var agent = new BrowserGoalAgent(
            host,
            new NemotronBrowserPlanner(new SequenceInferenceClient(CompleteDecision("done"))));
        var session = BrowserGoalSession.Create("Submit") with
        {
            ActionCount = 1,
            Status = BrowserGoalStatus.WaitingForApproval,
            PendingJobId = Guid.NewGuid(),
            PendingExactScope = scope,
            PendingAction = new BrowserAction(BrowserActionKind.Click, BrowserLocator.Accessibility("e-1"))
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            agent.ApproveAndContinueAsync(session, "different-scope"));

        Assert.Equal(0, host.ApproveCount);
    }

    [Fact]
    public async Task ApproveAndContinueAsync_ConsumesApprovalThenContinuesPlanning()
    {
        const string scope = "exact-scope";
        var jobId = Guid.NewGuid();
        var host = new FakeHost
        {
            ApproveResultFactory = (_, _) => new BrowserJobOutcome(
                jobId,
                AgentJobState.Completed,
                "verified")
        };
        var agent = new BrowserGoalAgent(
            host,
            new NemotronBrowserPlanner(new SequenceInferenceClient(CompleteDecision("Finished after approval."))));
        var session = BrowserGoalSession.Create("Submit") with
        {
            ActionCount = 1,
            Status = BrowserGoalStatus.WaitingForApproval,
            PendingJobId = jobId,
            PendingExactScope = scope,
            PendingAction = new BrowserAction(BrowserActionKind.Click, BrowserLocator.Accessibility("e-1"))
        };

        var result = await agent.ApproveAndContinueAsync(session, scope);

        Assert.Equal(BrowserGoalStatus.Completed, result.Status);
        Assert.Equal(1, host.ApproveCount);
        Assert.Null(result.PendingExactScope);
        Assert.Null(result.PendingJobId);
    }

    [Fact]
    public async Task RunUntilPauseAsync_EnforcesStrictActionBudget()
    {
        var inference = new SequenceInferenceClient(
            ClickDecision("e-1", "one"),
            ClickDecision("e-1", "two"),
            ClickDecision("e-1", "three"));
        var host = new FakeHost
        {
            StartResultFactory = _ => CompletedOutcome()
        };
        var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(inference));

        var result = await agent.RunUntilPauseAsync(BrowserGoalSession.Create("Keep clicking", maxActions: 2));

        Assert.Equal(BrowserGoalStatus.BudgetExhausted, result.Status);
        Assert.Equal(2, result.ActionCount);
        Assert.Equal(2, host.StartedActions.Count);
        Assert.Equal(2, inference.CallCount);
    }

    [Fact]
    public async Task CancelAsync_CancelsPendingJobAndClearsApprovalDescription()
    {
        var host = new FakeHost();
        var agent = new BrowserGoalAgent(
            host,
            new NemotronBrowserPlanner(new SequenceInferenceClient(CompleteDecision("unused"))));
        var jobId = Guid.NewGuid();
        var session = BrowserGoalSession.Create("Submit") with
        {
            Status = BrowserGoalStatus.WaitingForApproval,
            PendingJobId = jobId,
            PendingExactScope = "exact",
            PendingAction = new BrowserAction(BrowserActionKind.Click, BrowserLocator.Accessibility("e-1"))
        };

        var result = await agent.CancelAsync(session);

        Assert.Equal(BrowserGoalStatus.Cancelled, result.Status);
        Assert.Equal(jobId, host.LastCancelledJobId);
        Assert.Null(result.PendingJobId);
        Assert.Null(result.PendingExactScope);
    }

    private static BrowserJobOutcome CompletedOutcome() => new(
        Guid.NewGuid(), AgentJobState.Completed, "verified");

    private static string CompleteDecision(string reason) => $$"""
        {"decision":"complete","reason":"{{reason}}","action":null}
        """;

    private static string ClickDecision(string reference, string expected) => $$"""
        {
          "decision":"act",
          "reason":"Advance goal",
          "action":{
            "kind":"click",
            "locator_kind":"accessibility_ref",
            "locator_value":"{{reference}}",
            "locator_name":"Continue",
            "locator_role":"button",
            "value":null,
            "destination":null,
            "expected_state":"{{expected}}",
            "rationale":"Advance the user goal"
          }
        }
        """;

    private sealed class FakeHost : IBrowserGoalHost
    {
        public int ObserveCount { get; private set; }
        public int ApproveCount { get; private set; }
        public Guid? LastCancelledJobId { get; private set; }
        public List<BrowserAction> StartedActions { get; } = new();
        public Func<BrowserAction, BrowserJobOutcome>? StartResultFactory { get; init; }
        public Func<Guid, string, BrowserJobOutcome>? ApproveResultFactory { get; init; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserveCount++;
            return Task.FromResult(new BrowserObservation(
                new Uri("https://example.com/app"),
                "Example",
                new[] { new BrowserElement("e-1", "button", "Continue", null, true, true, false) },
                "Continue",
                DateTimeOffset.UtcNow,
                SnapshotId: $"snapshot-{ObserveCount}"));
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartedActions.Add(action);
            return Task.FromResult(StartResultFactory?.Invoke(action) ?? CompletedOutcome());
        }

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApproveCount++;
            return Task.FromResult(ApproveResultFactory?.Invoke(jobId, exactScope)
                ?? new BrowserJobOutcome(jobId, AgentJobState.Completed, "verified"));
        }

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastCancelledJobId = jobId;
            return Task.FromResult(new BrowserJobOutcome(jobId, AgentJobState.Cancelled, "cancelled"));
        }
    }

    private sealed class SequenceInferenceClient : IAgentInferenceClient
    {
        private readonly Queue<string> _responses;

        public SequenceInferenceClient(params string[] responses) => _responses = new Queue<string>(responses);

        public int CallCount { get; private set; }

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            if (_responses.Count == 0)
                throw new InvalidOperationException("No fake Nemotron response remains.");
            return Task.FromResult(new AgentCompletion(
                _responses.Dequeue(),
                Array.Empty<ToolCall>(),
                NebiusOptions.VerifiedNemotronSuperModel,
                "stop"));
        }
    }
}
