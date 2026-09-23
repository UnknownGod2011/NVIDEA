using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

/// <summary>
/// Qualifies the least-authority goal facade after root shutdown has linearized.
/// Every public transaction must fail at the composition lifetime boundary before
/// planner or browser authority can execute.
/// </summary>
public sealed class BrowserGoalTransactionPostDisposalTests
{
    [Fact]
    public async Task Run_after_disposal_fails_before_inner_authority()
    {
        var fixture = await DisposedFixture.CreateAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            fixture.Agent.RunUntilPauseAsync(BrowserGoalSession.Create("inspect page")));

        fixture.AssertNoInnerAuthority();
    }

    [Fact]
    public async Task Resume_after_disposal_fails_before_inner_authority()
    {
        var fixture = await DisposedFixture.CreateAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            fixture.Agent.ResumeAsync(Guid.NewGuid()));

        fixture.AssertNoInnerAuthority();
    }

    [Fact]
    public async Task Approve_after_disposal_fails_before_inner_authority()
    {
        var fixture = await DisposedFixture.CreateAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            fixture.Agent.ApproveAndContinueAsync(
                BrowserGoalSession.Create("submit form"),
                "submit:example"));

        fixture.AssertNoInnerAuthority();
    }

    [Fact]
    public async Task Cancel_after_disposal_fails_before_inner_authority()
    {
        var fixture = await DisposedFixture.CreateAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            fixture.Agent.CancelAsync(BrowserGoalSession.Create("inspect page")));

        fixture.AssertNoInnerAuthority();
    }

    private sealed class DisposedFixture
    {
        private DisposedFixture(
            LifetimeBoundBrowserGoalAgent agent,
            CountingHost host,
            CountingInferenceClient inference)
        {
            Agent = agent;
            Host = host;
            Inference = inference;
        }

        public LifetimeBoundBrowserGoalAgent Agent { get; }
        private CountingHost Host { get; }
        private CountingInferenceClient Inference { get; }

        public static async Task<DisposedFixture> CreateAsync()
        {
            var host = new CountingHost();
            var inference = new CountingInferenceClient();
            var inner = new BrowserGoalAgent(host, new NemotronBrowserPlanner(inference));
            var agent = new LifetimeBoundBrowserGoalAgent(inner);
            var lifetime = new CompositionLifetimeGate();
            agent.BindCompositionLifetime(lifetime);

            var disposal = await lifetime.BeginDisposeAsync();
            Assert.NotNull(disposal);
            await disposal!.DisposeAsync();

            return new DisposedFixture(agent, host, inference);
        }

        public void AssertNoInnerAuthority()
        {
            Assert.Equal(0, Host.TotalCalls);
            Assert.Equal(0, Inference.Calls);
        }
    }

    private sealed class CountingHost : IBrowserGoalHost
    {
        public int TotalCalls { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            TotalCalls++;
            throw new InvalidOperationException("Browser authority must not execute after disposal.");
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
        {
            TotalCalls++;
            throw new InvalidOperationException("Browser authority must not execute after disposal.");
        }

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
        {
            TotalCalls++;
            throw new InvalidOperationException("Browser authority must not execute after disposal.");
        }

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            TotalCalls++;
            throw new InvalidOperationException("Browser authority must not execute after disposal.");
        }
    }

    private sealed class CountingInferenceClient : IAgentInferenceClient
    {
        public int Calls { get; private set; }

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("Planner authority must not execute after disposal.");
        }
    }
}
