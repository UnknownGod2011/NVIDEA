using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

/// <summary>
/// Exercises the issued goal facade against the actual composition root disposal path without
/// starting Chromium or contacting a provider. The root is constructed through the assembly-
/// internal least-authority deterministic seam used only by qualification code.
/// </summary>
public sealed class NvideaCompositionRootBrowserGoalDisposalRaceTests
{
    [Fact]
    public async Task Issued_run_holds_actual_root_disposal_until_transaction_finishes()
    {
        var inference = new BlockingInferenceClient();
        var host = new CountingCrashConsistentHost();
        var fixture = await RootFixture.CreateAsync(inference, host);
        try
        {
            var agent = await fixture.Root.CreateBrowserGoalAgentAsync();
            var run = agent.RunUntilPauseAsync(BrowserGoalSession.Create("inspect page"));

            await inference.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var disposal = fixture.Root.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);

            inference.Release.TrySetResult();
            var result = await run;
            Assert.Equal(BrowserGoalStatus.Completed, result.Status);
            await disposal;

            Assert.Equal(1, host.ObserveCalls);
            Assert.Equal(1, inference.Calls);
        }
        finally
        {
            inference.Release.TrySetResult();
            await fixture.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("run")]
    [InlineData("resume")]
    [InlineData("approve")]
    [InlineData("cancel")]
    public async Task Issued_transactions_after_actual_root_disposal_fail_before_planner_or_browser_authority(string transaction)
    {
        var inference = new BlockingInferenceClient(releaseImmediately: true);
        var host = new CountingCrashConsistentHost();
        var fixture = await RootFixture.CreateAsync(inference, host);
        try
        {
            var agent = await fixture.Root.CreateBrowserGoalAgentAsync();
            var session = BrowserGoalSession.Create("inspect page");
            await fixture.Root.DisposeAsync();

            Task<BrowserGoalSession> operation = transaction switch
            {
                "run" => agent.RunUntilPauseAsync(session),
                "resume" => agent.ResumeAsync(session.SessionId),
                "approve" => agent.ApproveAndContinueAsync(session, "browser.click:#submit"),
                "cancel" => agent.CancelAsync(session),
                _ => throw new ArgumentOutOfRangeException(nameof(transaction))
            };

            await Assert.ThrowsAsync<ObjectDisposedException>(() => operation);
            Assert.Equal(0, host.TotalCalls);
            Assert.Equal(0, inference.Calls);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private sealed class RootFixture : IAsyncDisposable
    {
        private readonly string _directory;
        private bool _disposed;

        private RootFixture(NvideaCompositionRoot root, string directory)
        {
            Root = root;
            _directory = directory;
        }

        public NvideaCompositionRoot Root { get; }

        public static async Task<RootFixture> CreateAsync(IAgentInferenceClient inference, ICrashConsistentBrowserGoalHost host)
        {
            var directory = Path.Combine(Path.GetTempPath(), "nvidea-root-goal-lifetime", Guid.NewGuid().ToString("N"));
            try
            {
                Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>> hostFactory = cancellationToken =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.FromResult(host);
                };
                var root = await NvideaCompositionRoot.CreateDeterministicAsync(inference, hostFactory, directory);
                return new RootFixture(root, directory);
            }
            catch
            {
                try { Directory.Delete(directory, recursive: true); } catch { }
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await Root.DisposeAsync();
            try { Directory.Delete(_directory, recursive: true); } catch { }
        }
    }

    private sealed class BlockingInferenceClient : IAgentInferenceClient
    {
        private readonly bool _releaseImmediately;

        public BlockingInferenceClient(bool releaseImmediately = false)
        {
            _releaseImmediately = releaseImmediately;
            if (releaseImmediately) Release.TrySetResult();
        }

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls { get; private set; }

        public async Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Entered.TrySetResult();
            if (!_releaseImmediately)
                await Release.Task.WaitAsync(cancellationToken);
            return new AgentCompletion(
                "{\"decision\":\"complete\",\"reason\":\"Goal satisfied.\",\"action\":null}",
                Array.Empty<ToolCall>(),
                NebiusOptions.VerifiedNemotronSuperModel,
                "stop");
        }
    }

    private sealed class CountingCrashConsistentHost : ICrashConsistentBrowserGoalHost
    {
        public int ObserveCalls { get; private set; }
        public int TotalCalls { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserveCalls++;
            TotalCalls++;
            return Task.FromResult(new BrowserObservation(
                new Uri("https://example.com/app"),
                "Example",
                Array.Empty<BrowserElement>(),
                "Ready",
                DateTimeOffset.UtcNow,
                SnapshotId: $"snapshot-{ObserveCalls}"));
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
            => Unexpected<BrowserJobOutcome>();

        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default)
            => Unexpected<BrowserJobOutcome>();

        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Unexpected<BrowserJobOutcome>();

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Unexpected<BrowserJobOutcome?>();

        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
            => Unexpected<BrowserJobOutcome>();

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
            => Unexpected<BrowserJobOutcome>();

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Unexpected<BrowserJobOutcome>();

        private Task<T> Unexpected<T>()
        {
            TotalCalls++;
            throw new InvalidOperationException("Unexpected browser authority call in composition lifetime qualification.");
        }
    }
}
