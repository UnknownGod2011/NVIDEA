using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class LifetimeBoundBrowserGoalAgentTests
{
    [Fact]
    public async Task Unbound_boundary_fails_closed_before_inner_agent_executes()
    {
        var host = new CountingHost();
        var boundary = CreateBoundary(host);
        var session = BrowserGoalSession.Create("inspect page");

        await Assert.ThrowsAsync<InvalidOperationException>(() => boundary.RunUntilPauseAsync(session));
        Assert.Equal(0, host.ObserveCalls);
    }

    [Fact]
    public async Task Post_disposal_transaction_fails_before_browser_authority_executes()
    {
        var host = new CountingHost();
        var boundary = CreateBoundary(host);
        var lifetime = new CompositionLifetimeGate();
        boundary.BindCompositionLifetime(lifetime);

        var disposal = await lifetime.BeginDisposeAsync();
        Assert.NotNull(disposal);
        await disposal!.DisposeAsync();

        var session = BrowserGoalSession.Create("inspect page");
        await Assert.ThrowsAsync<ObjectDisposedException>(() => boundary.RunUntilPauseAsync(session));
        Assert.Equal(0, host.ObserveCalls);
    }

    [Fact]
    public void Binding_twice_is_rejected()
    {
        var boundary = CreateBoundary(new CountingHost());
        boundary.BindCompositionLifetime(new CompositionLifetimeGate());

        Assert.Throws<InvalidOperationException>(() =>
            boundary.BindCompositionLifetime(new CompositionLifetimeGate()));
    }

    private static LifetimeBoundBrowserGoalAgent CreateBoundary(IBrowserGoalHost host)
    {
        var inference = new FakeInferenceClient();
        var planner = new NemotronBrowserPlanner(inference);
        return new LifetimeBoundBrowserGoalAgent(new BrowserGoalAgent(host, planner));
    }

    private sealed class CountingHost : IBrowserGoalHost
    {
        public int ObserveCalls { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            ObserveCalls++;
            throw new InvalidOperationException("The test should fail at the lifetime boundary before observation.");
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeInferenceClient : INebiusInferenceClient
    {
        public Task<NebiusChatResponse> CompleteAsync(NebiusChatRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Inference should not execute in lifetime-boundary tests.");
    }
}
