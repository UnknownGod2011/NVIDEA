using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalCrashConsistencyTests
{
    [Fact]
    public async Task ResumeAsync_ReservedChildMissing_ReplansWithoutAdvancingPhantomJob()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-missing-child-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var reservedId = Guid.NewGuid();
            var session = BrowserGoalSession.Create("Inspect the page") with
            {
                ActionCount = 1,
                PendingJobId = reservedId,
                Status = BrowserGoalStatus.Running
            };
            await store.SaveAsync(session);

            var host = new CrashHost();
            var agent = new BrowserGoalAgent(
                host,
                new NemotronBrowserPlanner(new SequenceInferenceClient(CompleteDecision("Recovered safely."))),
                store);

            var result = await agent.ResumeAsync(session.SessionId);

            Assert.Equal(BrowserGoalStatus.Completed, result.Status);
            Assert.Equal(0, result.ActionCount);
            Assert.Null(result.PendingJobId);
            Assert.Equal(0, host.AdvanceCount);
            Assert.Equal(0, host.CreateCount);
            Assert.Equal(1, host.ObserveCount);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_CreatedPendingChild_AdvancesExactChildWithoutCreatingReplacement()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-created-child-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var childId = Guid.NewGuid();
            var session = BrowserGoalSession.Create("Continue") with
            {
                ActionCount = 1,
                PendingJobId = childId,
                Status = BrowserGoalStatus.Running
            };
            await store.SaveAsync(session);

            var host = new CrashHost();
            host.Children[childId] = new BrowserJobOutcome(childId, AgentJobState.Pending, "created");
            host.AdvanceResults[childId] = new BrowserJobOutcome(childId, AgentJobState.Completed, "verified", VerifiedStep: Verified(childId));
            var agent = new BrowserGoalAgent(
                host,
                new NemotronBrowserPlanner(new SequenceInferenceClient(CompleteDecision("Done."))),
                store);

            var result = await agent.ResumeAsync(session.SessionId);

            Assert.Equal(BrowserGoalStatus.Completed, result.Status);
            Assert.Equal(1, host.AdvanceCount);
            Assert.Equal(0, host.CreateCount);
            Assert.Null(result.PendingJobId);
            Assert.Contains(result.VerifiedSteps!, step => step.JobId == childId);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_WaitingChild_DoesNotAdvanceOrRecreateApproval()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-waiting-child-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var childId = Guid.NewGuid();
            const string scope = "capability:browser.agent:exact-submit";
            var session = BrowserGoalSession.Create("Submit") with
            {
                ActionCount = 1,
                PendingJobId = childId,
                PendingExactScope = scope,
                Status = BrowserGoalStatus.WaitingForApproval
            };
            await store.SaveAsync(session);

            var host = new CrashHost();
            host.Children[childId] = Waiting(childId, scope);
            var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(new ThrowingInferenceClient()), store);

            var result = await agent.ResumeAsync(session.SessionId);

            Assert.Equal(BrowserGoalStatus.WaitingForApproval, result.Status);
            Assert.Equal(scope, result.PendingExactScope);
            Assert.Equal(0, host.AdvanceCount);
            Assert.Equal(0, host.CreateCount);
            Assert.Equal(0, host.ApproveCount);
            Assert.Equal(0, host.RearmCount);
            Assert.Equal(0, host.ObserveCount);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_ApprovalGrantLostBeforeExecution_RearmsWaitWithoutMintingApproval()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-lost-grant-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var childId = Guid.NewGuid();
            const string scope = "capability:browser.agent:exact-submit";
            var session = BrowserGoalSession.Create("Submit") with
            {
                ActionCount = 1,
                PendingJobId = childId,
                PendingExactScope = scope,
                Status = BrowserGoalStatus.WaitingForApproval
            };
            await store.SaveAsync(session);

            var host = new CrashHost();
            host.Children[childId] = new BrowserJobOutcome(childId, AgentJobState.Pending, "approval grant was lost on restart");
            var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(new ThrowingInferenceClient()), store);

            var result = await agent.ResumeAsync(session.SessionId);

            Assert.Equal(BrowserGoalStatus.WaitingForApproval, result.Status);
            Assert.Equal(1, host.RearmCount);
            Assert.Equal(0, host.AdvanceCount);
            Assert.Equal(0, host.ApproveCount);
            Assert.Equal(0, host.ObserveCount);
            Assert.Equal(AgentJobState.WaitingForApproval, host.Children[childId].State);
            Assert.Equal(scope, host.Children[childId].Approval!.ExactScope);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_CompletedChildBeforeParentUpdate_ReconcilesVerifiedHistoryWithoutReplay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-completed-child-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var childId = Guid.NewGuid();
            var session = BrowserGoalSession.Create("Submit") with
            {
                ActionCount = 1,
                PendingJobId = childId,
                Status = BrowserGoalStatus.Running
            };
            await store.SaveAsync(session);

            var host = new CrashHost();
            host.Children[childId] = new BrowserJobOutcome(childId, AgentJobState.Completed, "verified", VerifiedStep: Verified(childId));
            var agent = new BrowserGoalAgent(
                host,
                new NemotronBrowserPlanner(new SequenceInferenceClient(CompleteDecision("Already done."))),
                store);

            var result = await agent.ResumeAsync(session.SessionId);

            Assert.Equal(BrowserGoalStatus.Completed, result.Status);
            Assert.Null(result.PendingJobId);
            Assert.Contains(result.VerifiedSteps!, step => step.JobId == childId);
            Assert.Equal(0, host.AdvanceCount);
            Assert.Equal(0, host.CreateCount);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_RunningChild_FailsClosedWithoutReplay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-goal-running-child-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(directory, "sessions.json"));
            var childId = Guid.NewGuid();
            var session = BrowserGoalSession.Create("Submit") with
            {
                ActionCount = 1,
                PendingJobId = childId,
                Status = BrowserGoalStatus.Running
            };
            await store.SaveAsync(session);

            var host = new CrashHost();
            host.Children[childId] = new BrowserJobOutcome(childId, AgentJobState.Running, "ambiguous in-flight");
            var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(new ThrowingInferenceClient()), store);

            var result = await agent.ResumeAsync(session.SessionId);

            Assert.Equal(BrowserGoalStatus.Failed, result.Status);
            Assert.Contains("ambiguous", result.Detail!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, host.AdvanceCount);
            Assert.Equal(0, host.CreateCount);
            Assert.Equal(0, host.ObserveCount);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static BrowserGoalVerifiedStep Verified(Guid jobId) => new(
        jobId,
        BrowserActionKind.Click,
        new Uri("https://example.com/before"),
        new Uri("https://example.com/after"),
        "state changed",
        DateTimeOffset.UtcNow);

    private static BrowserJobOutcome Waiting(Guid jobId, string scope) => new(
        jobId,
        AgentJobState.WaitingForApproval,
        "approval required",
        new BrowserApprovalPrompt(jobId, scope, BrowserActionKind.Click, "Submit", "Submit", DateTimeOffset.UtcNow));

    private static string CompleteDecision(string reason) => $$"""
        {"decision":"complete","reason":"{{reason}}","action":null}
        """;

    private sealed class CrashHost : ICrashConsistentBrowserGoalHost
    {
        public Dictionary<Guid, BrowserJobOutcome> Children { get; } = new();
        public Dictionary<Guid, BrowserJobOutcome> AdvanceResults { get; } = new();
        public int ObserveCount { get; private set; }
        public int CreateCount { get; private set; }
        public int AdvanceCount { get; private set; }
        public int ApproveCount { get; private set; }
        public int RearmCount { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserveCount++;
            return Task.FromResult(new BrowserObservation(
                new Uri("https://example.com/app"),
                "Example",
                Array.Empty<BrowserElement>(),
                "ready",
                DateTimeOffset.UtcNow));
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Crash-consistent agent should not use combined create+advance.");

        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCount++;
            var created = new BrowserJobOutcome(jobId, AgentJobState.Pending, "created");
            Children[jobId] = created;
            return Task.FromResult(created);
        }

        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AdvanceCount++;
            var outcome = AdvanceResults.TryGetValue(jobId, out var configured) ? configured : Children[jobId];
            Children[jobId] = outcome;
            return Task.FromResult(outcome);
        }

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Children.TryGetValue(jobId, out var value) ? value : null);
        }

        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RearmCount++;
            var waiting = Waiting(jobId, exactScope);
            Children[jobId] = waiting;
            return Task.FromResult(waiting);
        }

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApproveCount++;
            throw new InvalidOperationException("Approval should not be recreated by recovery.");
        }

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = new BrowserJobOutcome(jobId, AgentJobState.Cancelled, "cancelled");
            Children[jobId] = outcome;
            return Task.FromResult(outcome);
        }
    }

    private sealed class SequenceInferenceClient : IAgentInferenceClient
    {
        private readonly Queue<string> _responses;

        public SequenceInferenceClient(params string[] responses) => _responses = new Queue<string>(responses);

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new AgentCompletion(
                _responses.Dequeue(),
                Array.Empty<ToolCall>(),
                NebiusOptions.VerifiedNemotronSuperModel,
                "stop"));
        }
    }

    private sealed class ThrowingInferenceClient : IAgentInferenceClient
    {
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Nemotron must not run during crash recovery boundaries in this test.");
    }
}
