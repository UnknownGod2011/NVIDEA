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

/// <summary>
/// Privacy-minimized verified history safe to persist and feed back to the planner.
/// It intentionally excludes typed values, page bodies, approval material and secrets.
/// </summary>
public sealed record BrowserGoalVerifiedStep(
    Guid JobId,
    BrowserActionKind ActionKind,
    Uri UrlBefore,
    Uri UrlAfter,
    string? VerificationDetail,
    DateTimeOffset CompletedAt);

public sealed record BrowserGoalSession(
    Guid SessionId,
    string Goal,
    int ActionCount,
    int MaxActions,
    BrowserGoalStatus Status,
    string? Detail = null,
    Guid? PendingJobId = null,
    string? PendingExactScope = null,
    BrowserAction? PendingAction = null,
    DateTimeOffset StartedAt = default,
    DateTimeOffset UpdatedAt = default,
    int PlannerTurnCount = 0,
    int MaxPlannerTurns = 20,
    long PlannerContextCharacters = 0,
    long MaxPlannerContextCharacters = 120_000,
    int MaxWallClockSeconds = 600,
    IReadOnlyList<BrowserGoalVerifiedStep>? VerifiedSteps = null)
{
    public static BrowserGoalSession Create(
        string goal,
        int maxActions = 12,
        int maxPlannerTurns = 20,
        long maxPlannerContextCharacters = 120_000,
        int maxWallClockSeconds = 600,
        TimeProvider? timeProvider = null)
    {
        if (string.IsNullOrWhiteSpace(goal))
            throw new ArgumentException("A browser goal is required.", nameof(goal));
        if (maxActions is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(maxActions), "Browser goal action budget must be between 1 and 30.");
        if (maxPlannerTurns is < 1 or > 60)
            throw new ArgumentOutOfRangeException(nameof(maxPlannerTurns), "Browser goal planner-turn budget must be between 1 and 60.");
        if (maxPlannerContextCharacters is < 8_000 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(maxPlannerContextCharacters));
        if (maxWallClockSeconds is < 30 or > 7_200)
            throw new ArgumentOutOfRangeException(nameof(maxWallClockSeconds));

        var boundedGoal = goal.Trim();
        if (boundedGoal.Length > 4_000)
            boundedGoal = boundedGoal[..4_000];

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
        return new BrowserGoalSession(
            Guid.NewGuid(), boundedGoal, 0, maxActions, BrowserGoalStatus.Running,
            StartedAt: now,
            UpdatedAt: now,
            MaxPlannerTurns: maxPlannerTurns,
            MaxPlannerContextCharacters: maxPlannerContextCharacters,
            MaxWallClockSeconds: maxWallClockSeconds,
            VerifiedSteps: Array.Empty<BrowserGoalVerifiedStep>());
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
/// Durable observe -> Nemotron plan -> policy-enforced job -> verify loop.
/// Consequential actions stop at exact approval boundaries. Persisted state is descriptive only;
/// BrowserHostRuntime remains the sole owner of ephemeral single-use approval grants.
/// </summary>
public sealed class BrowserGoalAgent
{
    private readonly IBrowserGoalHost _host;
    private readonly NemotronBrowserPlanner _planner;
    private readonly IBrowserGoalSessionStore? _store;
    private readonly TimeProvider _timeProvider;

    public BrowserGoalAgent(
        BrowserHostRuntime host,
        NemotronBrowserPlanner planner,
        IBrowserGoalSessionStore? store = null,
        TimeProvider? timeProvider = null)
        : this(new BrowserHostAdapter(host), planner, store, timeProvider)
    {
    }

    public BrowserGoalAgent(
        IBrowserGoalHost host,
        NemotronBrowserPlanner planner,
        IBrowserGoalSessionStore? store = null,
        TimeProvider? timeProvider = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _store = store;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<BrowserGoalSession> ResumeAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (_store is null)
            throw new InvalidOperationException("Browser goal persistence is not configured.");
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Browser goal session id is required.", nameof(sessionId));

        var session = await _store.GetAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Browser goal session '{sessionId}' was not found.");
        ValidateSession(session);

        // A restart never recreates authorization. Waiting sessions remain paused until the
        // user explicitly approves the persisted descriptive scope again.
        if (session.Status == BrowserGoalStatus.WaitingForApproval || IsTerminal(session.Status))
            return session;

        return await RunUntilPauseAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserGoalSession> RunUntilPauseAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateSession(session);

        if (session.Status == BrowserGoalStatus.WaitingForApproval || IsTerminal(session.Status))
            return await PersistAsync(session, cancellationToken).ConfigureAwait(false);

        var current = Touch(session with { Status = BrowserGoalStatus.Running, Detail = null });
        await PersistAsync(current, cancellationToken).ConfigureAwait(false);

        while (current.ActionCount < current.MaxActions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsWallClockExhausted(current))
                return await ExhaustAsync(current, "wall-clock", cancellationToken).ConfigureAwait(false);
            if (current.PlannerTurnCount >= current.MaxPlannerTurns)
                return await ExhaustAsync(current, "planner-turn", cancellationToken).ConfigureAwait(false);

            var observation = await _host.ObserveAsync(cancellationToken).ConfigureAwait(false);
            var estimatedContext = EstimatePlannerContextCharacters(current, observation);
            if (current.PlannerContextCharacters + estimatedContext > current.MaxPlannerContextCharacters)
                return await ExhaustAsync(current, "planner-context", cancellationToken).ConfigureAwait(false);

            current = Touch(current with
            {
                PlannerTurnCount = current.PlannerTurnCount + 1,
                PlannerContextCharacters = current.PlannerContextCharacters + estimatedContext
            });
            await PersistAsync(current, cancellationToken).ConfigureAwait(false);

            var decision = await _planner
                .PlanNextAsync(current.Goal, observation, ToPlannerHistory(current.VerifiedSteps), cancellationToken)
                .ConfigureAwait(false);

            if (decision.Kind == BrowserPlannerDecisionKind.Complete)
            {
                return await PersistAsync(Touch(current with
                {
                    Status = BrowserGoalStatus.Completed,
                    Detail = string.IsNullOrWhiteSpace(decision.Reason)
                        ? "Nemotron determined the browser goal is complete."
                        : decision.Reason
                }), cancellationToken).ConfigureAwait(false);
            }

            if (decision.Kind == BrowserPlannerDecisionKind.Stop || decision.Action is null)
            {
                return await PersistAsync(Touch(current with
                {
                    Status = BrowserGoalStatus.Stopped,
                    Detail = string.IsNullOrWhiteSpace(decision.Reason)
                        ? "Nemotron stopped because no safe next action was available."
                        : decision.Reason
                }), cancellationToken).ConfigureAwait(false);
            }

            var outcome = await _host.StartActionAsync(decision.Action, cancellationToken).ConfigureAwait(false);
            current = Touch(current with { ActionCount = current.ActionCount + 1 });

            if (outcome.State == AgentJobState.WaitingForApproval)
            {
                if (outcome.Approval is null || string.IsNullOrWhiteSpace(outcome.Approval.ExactScope))
                {
                    return await PersistAsync(Touch(current with
                    {
                        Status = BrowserGoalStatus.Failed,
                        Detail = "Browser job requested approval without an exact approval scope."
                    }), cancellationToken).ConfigureAwait(false);
                }

                return await PersistAsync(Touch(current with
                {
                    Status = BrowserGoalStatus.WaitingForApproval,
                    Detail = outcome.Message,
                    PendingJobId = outcome.JobId,
                    PendingExactScope = outcome.Approval.ExactScope,
                    PendingAction = decision.Action
                }), cancellationToken).ConfigureAwait(false);
            }

            if (outcome.State == AgentJobState.Completed)
            {
                current = AppendVerifiedStep(current, outcome.VerifiedStep);
                await PersistAsync(current, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (outcome.State == AgentJobState.Cancelled)
                return await PersistAsync(Touch(current with { Status = BrowserGoalStatus.Cancelled, Detail = outcome.Message }), cancellationToken).ConfigureAwait(false);

            return await PersistAsync(Touch(current with
            {
                Status = BrowserGoalStatus.Failed,
                Detail = outcome.Message
            }), cancellationToken).ConfigureAwait(false);
        }

        return await ExhaustAsync(current, "action", cancellationToken).ConfigureAwait(false);
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
        if (IsWallClockExhausted(session))
            return await ExhaustAsync(session, "wall-clock", cancellationToken).ConfigureAwait(false);

        var outcome = await _host
            .ApproveAndResumeAsync(session.PendingJobId.Value, exactScope, cancellationToken)
            .ConfigureAwait(false);
        var resumed = Touch(session with
        {
            PendingJobId = null,
            PendingExactScope = null,
            PendingAction = null,
            Detail = outcome.Message
        });

        if (outcome.State == AgentJobState.Completed)
        {
            resumed = AppendVerifiedStep(resumed, outcome.VerifiedStep);
            await PersistAsync(resumed, cancellationToken).ConfigureAwait(false);
            return await RunUntilPauseAsync(resumed with { Status = BrowserGoalStatus.Running }, cancellationToken).ConfigureAwait(false);
        }
        if (outcome.State == AgentJobState.Cancelled)
            return await PersistAsync(Touch(resumed with { Status = BrowserGoalStatus.Cancelled }), cancellationToken).ConfigureAwait(false);
        if (outcome.State == AgentJobState.WaitingForApproval)
            throw new InvalidOperationException("A single browser action requested a second approval after consuming the exact grant.");

        return await PersistAsync(Touch(resumed with { Status = BrowserGoalStatus.Failed }), cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserGoalSession> CancelAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateSession(session);

        if (session.PendingJobId is { } jobId)
            await _host.CancelAsync(jobId, cancellationToken).ConfigureAwait(false);

        return await PersistAsync(Touch(session with
        {
            Status = BrowserGoalStatus.Cancelled,
            Detail = "Browser goal cancelled by the user.",
            PendingJobId = null,
            PendingExactScope = null,
            PendingAction = null
        }), cancellationToken).ConfigureAwait(false);
    }

    private BrowserGoalSession AppendVerifiedStep(BrowserGoalSession session, BrowserGoalVerifiedStep? step)
    {
        if (step is null)
            return Touch(session);

        var history = (session.VerifiedSteps ?? Array.Empty<BrowserGoalVerifiedStep>()).ToList();
        if (!history.Any(existing => existing.JobId == step.JobId))
            history.Add(step);
        if (history.Count > 50)
            history.RemoveRange(0, history.Count - 50);
        return Touch(session with { VerifiedSteps = history });
    }

    private IReadOnlyList<BrowserActionReceipt> ToPlannerHistory(IReadOnlyList<BrowserGoalVerifiedStep>? steps)
    {
        if (steps is null || steps.Count == 0)
            return Array.Empty<BrowserActionReceipt>();

        return steps.Select(step => new BrowserActionReceipt(
            step.JobId,
            new BrowserAction(step.ActionKind),
            new BrowserActionDecision(BrowserRiskLevel.Low, false, true, "Previously verified browser step."),
            step.CompletedAt,
            step.CompletedAt,
            DriverReportedSuccess: true,
            Verified: true,
            step.VerificationDetail,
            step.UrlBefore,
            step.UrlAfter)).ToArray();
    }

    private long EstimatePlannerContextCharacters(BrowserGoalSession session, BrowserObservation observation)
    {
        long total = session.Goal.Length + observation.Title.Length + observation.VisibleText.Length + 256;
        foreach (var element in observation.Elements)
            total += element.Reference.Length + element.Role.Length + (element.Name?.Length ?? 0) + 24;
        foreach (var step in session.VerifiedSteps ?? Array.Empty<BrowserGoalVerifiedStep>())
            total += (step.VerificationDetail?.Length ?? 0) + 128;
        return Math.Min(total, 50_000);
    }

    private bool IsWallClockExhausted(BrowserGoalSession session) =>
        _timeProvider.GetUtcNow() - session.StartedAt >= TimeSpan.FromSeconds(session.MaxWallClockSeconds);

    private async Task<BrowserGoalSession> ExhaustAsync(
        BrowserGoalSession session,
        string budget,
        CancellationToken cancellationToken)
    {
        var detail = budget switch
        {
            "action" => $"Browser goal reached its strict {session.MaxActions}-action budget before completion.",
            "planner-turn" => $"Browser goal reached its strict {session.MaxPlannerTurns}-planner-turn budget before completion.",
            "planner-context" => $"Browser goal reached its strict {session.MaxPlannerContextCharacters:N0}-character planner-context budget before completion.",
            _ => $"Browser goal reached its strict {session.MaxWallClockSeconds}-second wall-clock budget before completion."
        };
        return await PersistAsync(Touch(session with { Status = BrowserGoalStatus.BudgetExhausted, Detail = detail }), cancellationToken).ConfigureAwait(false);
    }

    private BrowserGoalSession Touch(BrowserGoalSession session) => session with { UpdatedAt = _timeProvider.GetUtcNow() };

    private async Task<BrowserGoalSession> PersistAsync(BrowserGoalSession session, CancellationToken cancellationToken)
    {
        if (_store is not null)
            await _store.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        return session;
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
        if (session.MaxPlannerTurns is < 1 or > 60 || session.PlannerTurnCount < 0 || session.PlannerTurnCount > session.MaxPlannerTurns)
            throw new InvalidOperationException("Browser goal session planner-turn budget is invalid.");
        if (session.MaxPlannerContextCharacters is < 8_000 or > 1_000_000 || session.PlannerContextCharacters < 0)
            throw new InvalidOperationException("Browser goal session planner-context budget is invalid.");
        if (session.MaxWallClockSeconds is < 30 or > 7_200)
            throw new InvalidOperationException("Browser goal session wall-clock budget is invalid.");
        if (session.StartedAt == default)
            throw new InvalidOperationException("Browser goal session is missing its start time.");
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
