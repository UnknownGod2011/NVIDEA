using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
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

    [Fact]
    public async Task Issued_resume_holds_actual_root_disposal_until_complete_transaction_finishes()
    {
        var inference = new BlockingInferenceClient();
        var host = new CountingCrashConsistentHost();
        var fixture = await RootFixture.CreateAsync(inference, host);
        try
        {
            var seeded = BrowserGoalSession.Create("resume durable browser goal");
            await fixture.SeedGoalSessionAsync(seeded);
            var agent = await fixture.Root.CreateBrowserGoalAgentAsync();

            var resume = agent.ResumeAsync(seeded.SessionId);
            await inference.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, host.ObserveCalls);
            Assert.Equal(1, inference.Calls);

            var disposal = fixture.Root.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);

            inference.Release.TrySetResult();
            var result = await resume;
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

    [Fact]
    public async Task Issued_approval_holds_actual_root_disposal_until_browser_authority_and_continuation_finish()
    {
        var inference = new BlockingInferenceClient(releaseImmediately: true);
        var host = new BlockingAuthorityHost(blockApproval: true);
        var fixture = await RootFixture.CreateAsync(inference, host);
        try
        {
            var jobId = Guid.NewGuid();
            const string scope = "browser.click:#submit";
            var waiting = BrowserGoalSession.Create("approve a consequential browser action") with
            {
                Status = BrowserGoalStatus.WaitingForApproval,
                PendingJobId = jobId,
                PendingExactScope = scope,
                PendingAction = new BrowserAction(
                    BrowserActionKind.Click,
                    BrowserLocator.Accessibility("submit"),
                    ExpectedState: "submitted")
            };
            await fixture.SeedGoalSessionAsync(waiting);
            var agent = await fixture.Root.CreateBrowserGoalAgentAsync();

            var approval = agent.ApproveAndContinueAsync(waiting, scope);
            await host.ApprovalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, host.ApproveCalls);
            Assert.Equal(0, inference.Calls);

            var disposal = fixture.Root.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);

            host.ReleaseApproval.TrySetResult();
            var result = await approval;
            Assert.Equal(BrowserGoalStatus.Completed, result.Status);
            await disposal;

            Assert.Equal(1, host.ApproveCalls);
            Assert.Equal(1, host.ObserveCalls);
            Assert.Equal(1, inference.Calls);
        }
        finally
        {
            host.ReleaseApproval.TrySetResult();
            await fixture.DisposeAsync();
        }
    }

    [Fact]
    public async Task Issued_cancel_holds_actual_root_disposal_until_browser_cancellation_finishes()
    {
        var inference = new BlockingInferenceClient(releaseImmediately: true);
        var host = new BlockingAuthorityHost(blockCancel: true);
        var fixture = await RootFixture.CreateAsync(inference, host);
        try
        {
            var pending = BrowserGoalSession.Create("cancel a pending browser action") with
            {
                PendingJobId = Guid.NewGuid(),
                PendingAction = new BrowserAction(BrowserActionKind.Click, BrowserLocator.Accessibility("submit"))
            };
            await fixture.SeedGoalSessionAsync(pending);
            var agent = await fixture.Root.CreateBrowserGoalAgentAsync();

            var cancellation = agent.CancelAsync(pending);
            await host.CancelEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, host.CancelCalls);
            Assert.Equal(0, inference.Calls);

            var disposal = fixture.Root.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);

            host.ReleaseCancel.TrySetResult();
            var result = await cancellation;
            Assert.Equal(BrowserGoalStatus.Cancelled, result.Status);
            await disposal;

            Assert.Equal(1, host.CancelCalls);
            Assert.Equal(0, inference.Calls);
        }
        finally
        {
            host.ReleaseCancel.TrySetResult();
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

        public Task SeedGoalSessionAsync(BrowserGoalSession session, CancellationToken cancellationToken = default)
        {
            var store = new JsonBrowserGoalSessionStore(Path.Combine(_directory, "browser", "goal-sessions.json"));
            return store.SaveAsync(session, cancellationToken);
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
            return Task.FromResult(CreateObservation(ObserveCalls));
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

    private sealed class BlockingAuthorityHost : ICrashConsistentBrowserGoalHost
    {
        private readonly bool _blockApproval;
        private readonly bool _blockCancel;

        public BlockingAuthorityHost(bool blockApproval = false, bool blockCancel = false)
        {
            _blockApproval = blockApproval;
            _blockCancel = blockCancel;
        }

        public TaskCompletionSource ApprovalEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseApproval { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CancelEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseCancel { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ApproveCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public int ObserveCalls { get; private set; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserveCalls++;
            return Task.FromResult(CreateObservation(ObserveCalls));
        }

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected non-durable action start.");

        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected child creation.");

        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected child advancement.");

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected child lookup.");

        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected approval rearm.");

        public async Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
        {
            ApproveCalls++;
            ApprovalEntered.TrySetResult();
            if (_blockApproval)
                await ReleaseApproval.Task.WaitAsync(cancellationToken);
            return new BrowserJobOutcome(jobId, AgentJobState.Completed, "Approved browser action completed.");
        }

        public async Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            CancelEntered.TrySetResult();
            if (_blockCancel)
                await ReleaseCancel.Task.WaitAsync(cancellationToken);
            return new BrowserJobOutcome(jobId, AgentJobState.Cancelled, "Browser action cancelled.");
        }
    }

    private static BrowserObservation CreateObservation(int sequence) => new(
        new Uri("https://example.com/app"),
        "Example",
        Array.Empty<BrowserElement>(),
        "Ready",
        DateTimeOffset.UtcNow,
        SnapshotId: $"snapshot-{sequence}");
}
