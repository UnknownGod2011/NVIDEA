using Nvidea.Core.Browser;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

public enum BrowserGoalStatus
{
    Running,
    WaitingForApproval,
    Completed,
    Stopped,
    Failed,
    BudgetExhausted,
    Cancelled
}

public sealed record BrowserGoalSession(
    Guid SessionId,
    string Goal,
    int ActionCount,
    int MaxActions,
    BrowserGoalStatus Status,
    string? Detail = null,
    Guid? PendingJobId = null,
    string? PendingExactScope = null,
    BrowserAction? PendingAction = null)
{
    public static BrowserGoalSession Create(string goal, int maxActions = 12)
    {
        if (string.IsNullOrWhiteSpace(goal))
            throw new ArgumentException("A browser goal is required.", nameof(goal));
        if (maxActions is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(maxActions), "Browser goal action budget must be between 1 and 30.");

        var boundedGoal = goal.Trim();
        if (boundedGoal.Length > 4_000)
            boundedGoal = boundedGoal[..4_000];

        return new BrowserGoalSession(
            Guid.NewGuid(), boundedGoal, 0, maxActions, BrowserGoalStatus.Running);
    }
}

/// <summary>
/// Minimal host contract used by the goal loop. The implementation remains responsible for
/// hard browser safety, exact approval grants, capability enforcement and post-action verification.
/// </summary>
public interface IBrowserGoalHost
{
    Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default);
    Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default);
    Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default);
    Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Observe -> Nemotron plan -> policy-enforced job -> verify loop with a strict action budget.
/// It deliberately stops at consequential approval boundaries. User approval is handed only to
/// BrowserHostRuntime, which owns the ephemeral single-use grant path.
/// </summary>
public sealed class BrowserGoalAgent
{
    private readonly IBrowserGoalHost _host;
    private readonly NemotronBrowserPlanner _planner;

    public BrowserGoalAgent(BrowserHostRuntime host, NemotronBrowserPlanner planner)
        : this(new BrowserHostAdapter(host), planner)
    {
    }

    public BrowserGoalAgent(IBrowserGoalHost host, NemotronBrowserPlanner planner)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    public async Task<BrowserGoalSession> RunUntilPauseAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateSession(session);

        if (session.Status == BrowserGoalStatus.WaitingForApproval)
            return session;
        if (IsTerminal(session.Status))
            return session;

        var current = session with { Status = BrowserGoalStatus.Running, Detail = null };
        while (current.ActionCount < current.MaxActions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var observation = await _host.ObserveAsync(cancellationToken).ConfigureAwait(false);
            var decision = await _planner
                .PlanNextAsync(current.Goal, observation, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (decision.Kind == BrowserPlannerDecisionKind.Complete)
            {
                return current with
                {
                    Status = BrowserGoalStatus.Completed,
                    Detail = string.IsNullOrWhiteSpace(decision.Reason)
                        ? "Nemotron determined the browser goal is complete."
                        : decision.Reason
                };
            }

            if (decision.Kind == BrowserPlannerDecisionKind.Stop || decision.Action is null)
            {
                return current with
                {
                    Status = BrowserGoalStatus.Stopped,
                    Detail = string.IsNullOrWhiteSpace(decision.Reason)
                        ? "Nemotron stopped because no safe next action was available."
                        : decision.Reason
                };
            }

            var outcome = await _host.StartActionAsync(decision.Action, cancellationToken).ConfigureAwait(false);
            current = current with { ActionCount = current.ActionCount + 1 };

            if (outcome.State == AgentJobState.WaitingForApproval)
            {
                if (outcome.Approval is null || string.IsNullOrWhiteSpace(outcome.Approval.ExactScope))
                {
                    return current with
                    {
                        Status = BrowserGoalStatus.Failed,
                        Detail = "Browser job requested approval without an exact approval scope."
                    };
                }

                return current with
                {
                    Status = BrowserGoalStatus.WaitingForApproval,
                    Detail = outcome.Message,
                    PendingJobId = outcome.JobId,
                    PendingExactScope = outcome.Approval.ExactScope,
                    PendingAction = decision.Action
                };
            }

            if (outcome.State == AgentJobState.Completed)
                continue;

            if (outcome.State == AgentJobState.Cancelled)
                return current with { Status = BrowserGoalStatus.Cancelled, Detail = outcome.Message };

            return current with
            {
                Status = BrowserGoalStatus.Failed,
                Detail = outcome.Message
            };
        }

        return current with
        {
            Status = BrowserGoalStatus.BudgetExhausted,
            Detail = $"Browser goal reached its strict {current.MaxActions}-action budget before completion."
        };
    }

    public async Task<BrowserGoalSession> ApproveAndContinueAsync(
        BrowserGoalSession session,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateSession(session);
        if (session.Status != BrowserGoalStatus.WaitingForApproval
            || session.PendingJobId is null
            || string.IsNullOrWhiteSpace(session.PendingExactScope))
        {
            throw new InvalidOperationException("Browser goal is not waiting for an approval.");
        }
        if (!string.Equals(session.PendingExactScope, exactScope, StringComparison.Ordinal))
            throw new InvalidOperationException("Approval scope does not exactly match the paused browser action.");

        var outcome = await _host
            .ApproveAndResumeAsync(session.PendingJobId.Value, exactScope, cancellationToken)
            .ConfigureAwait(false);
        var resumed = session with
        {
            PendingJobId = null,
            PendingExactScope = null,
            PendingAction = null,
            Detail = outcome.Message
        };

        if (outcome.State == AgentJobState.Completed)
            return await RunUntilPauseAsync(resumed with { Status = BrowserGoalStatus.Running }, cancellationToken).ConfigureAwait(false);
        if (outcome.State == AgentJobState.Cancelled)
            return resumed with { Status = BrowserGoalStatus.Cancelled };
        if (outcome.State == AgentJobState.WaitingForApproval)
            throw new InvalidOperationException("A single browser action requested a second approval after consuming the exact grant.");

        return resumed with { Status = BrowserGoalStatus.Failed };
    }

    public async Task<BrowserGoalSession> CancelAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateSession(session);

        if (session.PendingJobId is { } jobId)
            await _host.CancelAsync(jobId, cancellationToken).ConfigureAwait(false);

        return session with
        {
            Status = BrowserGoalStatus.Cancelled,
            Detail = "Browser goal cancelled by the user.",
            PendingJobId = null,
            PendingExactScope = null,
            PendingAction = null
        };
    }

    private static bool IsTerminal(BrowserGoalStatus status) => status is
        BrowserGoalStatus.Completed
        or BrowserGoalStatus.Stopped
        or BrowserGoalStatus.Failed
        or BrowserGoalStatus.BudgetExhausted
        or BrowserGoalStatus.Cancelled;

    private static void ValidateSession(BrowserGoalSession session)
    {
        if (session.SessionId == Guid.Empty)
            throw new InvalidOperationException("Browser goal session id is required.");
        if (string.IsNullOrWhiteSpace(session.Goal))
            throw new InvalidOperationException("Browser goal session is missing its goal.");
        if (session.MaxActions is < 1 or > 30)
            throw new InvalidOperationException("Browser goal session has an invalid action budget.");
        if (session.ActionCount < 0 || session.ActionCount > session.MaxActions)
            throw new InvalidOperationException("Browser goal session action count is invalid.");
    }

    private sealed class BrowserHostAdapter : IBrowserGoalHost
    {
        private readonly BrowserHostRuntime _host;

        public BrowserHostAdapter(BrowserHostRuntime host) =>
            _host = host ?? throw new ArgumentNullException(nameof(host));

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) =>
            _host.ObserveAsync(cancellationToken);

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) =>
            _host.StartActionAsync(action, cancellationToken);

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            _host.ApproveAndResumeAsync(jobId, exactScope, cancellationToken);

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            _host.CancelAsync(jobId, cancellationToken);
    }
}
