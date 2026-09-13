using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class BrowserGoalImmediateEvidenceTrustTests
{
    [Fact]
    public async Task RunUntilPauseAsync_ProjectsWaitingDetailWithoutChangingExactApprovalScope()
    {
        const string exactScope = "capability:browser.agent:job:abc:click:https://example.com/submit?token=KEEP-EXACT";
        var jobId = Guid.Parse("00000000-0000-0000-0000-000000000321");
        var hostileDetail = "approval\r\nrequired\t" + new string('x', 900);
        var host = new FakeHost
        {
            StartResultFactory = action => new BrowserJobOutcome(
                jobId,
                AgentJobState.WaitingForApproval,
                hostileDetail,
                new BrowserApprovalPrompt(
                    jobId,
                    exactScope,
                    action.Kind,
                    "Submit demo",
                    "https://example.com/submit?token=display-secret",
                    DateTimeOffset.UtcNow))
        };
        var agent = new BrowserGoalAgent(
            host,
            new NemotronBrowserPlanner(new SequenceInferenceClient(ClickDecision())));

        var result = await agent.RunUntilPauseAsync(BrowserGoalSession.Create("Submit the demo"));

        Assert.Equal(BrowserGoalStatus.WaitingForApproval, result.Status);
        Assert.Equal(exactScope, result.PendingExactScope);
        Assert.Equal(jobId, result.PendingJobId);
        Assert.Null(result.PendingAction);
        Assert.NotNull(result.Detail);
        Assert.True(result.Detail!.Length <= BrowserGoalEvidenceTrust.MaxSessionDetailCharacters);
        Assert.DoesNotContain('\r', result.Detail);
        Assert.DoesNotContain('\n', result.Detail);
        Assert.DoesNotContain('\t', result.Detail);
    }

    [Fact]
    public async Task RunUntilPauseAsync_ProjectsVerifiedEvidenceBeforeSameProcessPlannerContinuation()
    {
        var jobId = Guid.Parse("00000000-0000-0000-0000-000000000654");
        var verification = "verified\r\nsite evidence\t" + new string('v', 700);
        var step = new BrowserGoalVerifiedStep(
            jobId,
            BrowserActionKind.Click,
            new Uri("https://user:pass@example.com/start?token=before-secret#before"),
            new Uri("https://user:pass@example.com/after?token=after-secret#after"),
            verification,
            DateTimeOffset.UtcNow);
        var inference = new SequenceInferenceClient(
            ClickDecision(),
            CompleteDecision("Goal complete."));
        var host = new FakeHost
        {
            StartResultFactory = _ => new BrowserJobOutcome(
                jobId,
                AgentJobState.Completed,
                "child\r\ncompleted",
                VerifiedStep: step)
        };
        var agent = new BrowserGoalAgent(host, new NemotronBrowserPlanner(inference));

        var result = await agent.RunUntilPauseAsync(BrowserGoalSession.Create("Finish the workflow"));

        Assert.Equal(BrowserGoalStatus.Completed, result.Status);
        var projected = Assert.Single(result.VerifiedSteps!);
        Assert.Equal(jobId, projected.JobId);
        Assert.Equal("https://example.com/start", projected.UrlBefore.AbsoluteUri.TrimEnd('/'));
        Assert.Equal("https://example.com/after", projected.UrlAfter.AbsoluteUri.TrimEnd('/'));
        Assert.DoesNotContain("before-secret", projected.UrlBefore.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain("after-secret", projected.UrlAfter.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain("user", projected.UrlAfter.UserInfo, StringComparison.Ordinal);
        Assert.NotNull(projected.VerificationDetail);
        Assert.True(projected.VerificationDetail!.Length <= BrowserGoalEvidenceTrust.MaxVerificationDetailCharacters);
        Assert.DoesNotContain('\r', projected.VerificationDetail);
        Assert.DoesNotContain('\n', projected.VerificationDetail);
        Assert.DoesNotContain('\t', projected.VerificationDetail);
        Assert.Equal(2, inference.CallCount);
    }

    [Fact]
    public async Task RunUntilPauseAsync_ProjectsCallerSuppliedHistoryEvenWithoutPersistenceStore()
    {
        var untrusted = BrowserGoalSession.Create("Inspect status") with
        {
            VerifiedSteps = new[]
            {
                new BrowserGoalVerifiedStep(
                    Guid.NewGuid(),
                    BrowserActionKind.Navigate,
                    new Uri("file:///C:/Users/private/secret.txt"),
                    new Uri("https://example.com/result?api_key=secret#fragment"),
                    "legacy\r\ncontext",
                    DateTimeOffset.UtcNow)
            }
        };
        var agent = new BrowserGoalAgent(
            new FakeHost(),
            new NemotronBrowserPlanner(new SequenceInferenceClient(CompleteDecision("done"))));

        var result = await agent.RunUntilPauseAsync(untrusted);

        var projected = Assert.Single(result.VerifiedSteps!);
        Assert.Equal("about:blank", projected.UrlBefore.AbsoluteUri);
        Assert.Equal("https://example.com/result", projected.UrlAfter.AbsoluteUri.TrimEnd('/'));
        Assert.DoesNotContain("api_key", projected.UrlAfter.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain('#', projected.UrlAfter.AbsoluteUri);
        Assert.DoesNotContain('\r', projected.VerificationDetail!);
        Assert.DoesNotContain('\n', projected.VerificationDetail!);
    }

    private static string CompleteDecision(string reason) => $$"""
        {"decision":"complete","reason":"{{reason}}","action":null}
        """;

    private static string ClickDecision() => """
        {
          "decision":"act",
          "reason":"Advance goal",
          "action":{
            "kind":"click",
            "locator_kind":"accessibility_ref",
            "locator_value":"e-1",
            "locator_name":"Continue",
            "locator_role":"button",
            "value":null,
            "destination":null,
            "expected_state":"Loaded",
            "rationale":"Advance the user goal"
          }
        }
        """;

    private sealed class FakeHost : IBrowserGoalHost
    {
        public Func<BrowserAction, BrowserJobOutcome>? StartResultFactory { get; init; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new BrowserObservation(
                new Uri("https://example.com/app"),
                "Example",
                new[] { new BrowserElement("e-1", "button", "Continue", null, true, true, false) },
                "Continue",
                DateTimeOffset.UtcNow,
                SnapshotId: Guid.NewGuid().ToString("N")));
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(StartResultFactory?.Invoke(action)
                ?? new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Completed, "verified"));
        }

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserJobOutcome(jobId, AgentJobState.Completed, "verified"));

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserJobOutcome(jobId, AgentJobState.Cancelled, "cancelled"));
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
