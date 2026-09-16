using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class EvidenceObservingBrowserGoalHostTests
{
    [Fact]
    public async Task Advance_verified_completion_records_browser_proof()
    {
        var ledger = new SessionEvidenceLedger();
        var verified = VerifiedOutcome();
        var host = new EvidenceObservingBrowserGoalHost(new FakeHost { AdvanceOutcome = verified }, ledger);

        var returned = await host.AdvanceActionAsync(verified.JobId);

        Assert.Same(verified, returned);
        Assert.True(ledger.Snapshot().Contains(SessionEvidenceKind.BrowserPostStateVerified));
        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.ConsequentialApprovalGateExercised));
    }

    [Theory]
    [InlineData(AgentJobState.Pending)]
    [InlineData(AgentJobState.Running)]
    [InlineData(AgentJobState.WaitingForApproval)]
    [InlineData(AgentJobState.RetryScheduled)]
    [InlineData(AgentJobState.Failed)]
    [InlineData(AgentJobState.Cancelled)]
    public async Task Advance_nonterminal_or_failed_outcomes_never_record_browser_proof(AgentJobState state)
    {
        var ledger = new SessionEvidenceLedger();
        var malformed = new BrowserJobOutcome(Guid.NewGuid(), state, "not verified", VerifiedStep: VerifiedStep(Guid.NewGuid()));
        var host = new EvidenceObservingBrowserGoalHost(new FakeHost { AdvanceOutcome = malformed }, ledger);

        await host.AdvanceActionAsync(malformed.JobId);

        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.BrowserPostStateVerified));
    }

    [Fact]
    public async Task Completed_without_verified_step_never_records_browser_proof()
    {
        var ledger = new SessionEvidenceLedger();
        var outcome = new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Completed, "driver success only");
        var host = new EvidenceObservingBrowserGoalHost(new FakeHost { AdvanceOutcome = outcome }, ledger);

        await host.AdvanceActionAsync(outcome.JobId);

        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.BrowserPostStateVerified));
    }

    [Fact]
    public async Task Accepted_approval_records_gate_and_independent_verified_outcome()
    {
        var ledger = new SessionEvidenceLedger();
        var verified = VerifiedOutcome();
        var host = new EvidenceObservingBrowserGoalHost(new FakeHost { ApprovalOutcome = verified }, ledger);

        await host.ApproveAndResumeAsync(verified.JobId, "exact-scope");

        var snapshot = ledger.Snapshot();
        Assert.True(snapshot.Contains(SessionEvidenceKind.ConsequentialApprovalGateExercised));
        Assert.True(snapshot.Contains(SessionEvidenceKind.BrowserPostStateVerified));
    }

    [Fact]
    public async Task Rejected_approval_exception_records_no_evidence()
    {
        var ledger = new SessionEvidenceLedger();
        var host = new EvidenceObservingBrowserGoalHost(new FakeHost { ApprovalException = new InvalidOperationException("scope rejected") }, ledger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.ApproveAndResumeAsync(Guid.NewGuid(), "wrong-scope"));

        Assert.Empty(ledger.Snapshot().Entries);
    }

    [Fact]
    public async Task Read_create_get_rearm_and_cancel_are_evidence_free()
    {
        var ledger = new SessionEvidenceLedger();
        var neutral = new BrowserJobOutcome(Guid.NewGuid(), AgentJobState.Pending, "pending");
        var host = new EvidenceObservingBrowserGoalHost(new FakeHost
        {
            CreateOutcome = neutral,
            GetOutcome = neutral,
            RearmOutcome = neutral,
            CancelOutcome = new BrowserJobOutcome(neutral.JobId, AgentJobState.Cancelled, "cancelled")
        }, ledger);

        await host.CreateActionAsync(neutral.JobId, new BrowserAction(BrowserActionKind.Navigate));
        await host.GetAsync(neutral.JobId);
        await host.RearmApprovalAsync(neutral.JobId, "scope");
        await host.CancelAsync(neutral.JobId);

        Assert.Empty(ledger.Snapshot().Entries);
    }

    private static BrowserJobOutcome VerifiedOutcome()
    {
        var id = Guid.NewGuid();
        return new BrowserJobOutcome(id, AgentJobState.Completed, "verified", VerifiedStep: VerifiedStep(id));
    }

    private static BrowserGoalVerifiedStep VerifiedStep(Guid jobId) => new(
        jobId,
        BrowserActionKind.Navigate,
        new Uri("https://example.com/before"),
        new Uri("https://example.com/after"),
        "trusted postcondition",
        DateTimeOffset.UtcNow);

    private sealed class FakeHost : ICrashConsistentBrowserGoalHost
    {
        public BrowserJobOutcome? AdvanceOutcome { get; init; }
        public BrowserJobOutcome? ApprovalOutcome { get; init; }
        public Exception? ApprovalException { get; init; }
        public BrowserJobOutcome? CreateOutcome { get; init; }
        public BrowserJobOutcome? GetOutcome { get; init; }
        public BrowserJobOutcome? RearmOutcome { get; init; }
        public BrowserJobOutcome? CancelOutcome { get; init; }

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult(AdvanceOutcome ?? throw new InvalidOperationException("No start outcome configured."));

        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateOutcome ?? new BrowserJobOutcome(jobId, AgentJobState.Pending, "created"));

        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AdvanceOutcome ?? throw new InvalidOperationException("No advance outcome configured."));

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetOutcome);

        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            Task.FromResult(RearmOutcome ?? new BrowserJobOutcome(jobId, AgentJobState.Pending, "rearmed"));

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
        {
            if (ApprovalException is not null) return Task.FromException<BrowserJobOutcome>(ApprovalException);
            return Task.FromResult(ApprovalOutcome ?? throw new InvalidOperationException("No approval outcome configured."));
        }

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(CancelOutcome ?? new BrowserJobOutcome(jobId, AgentJobState.Cancelled, "cancelled"));
    }
}
