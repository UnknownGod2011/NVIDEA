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
/// Optional stronger host contract for durable parent/child orchestration. Creation and advancement
/// are deliberately separate so the parent can persist a child id before the child can execute.
/// </summary>
public interface ICrashConsistentBrowserGoalHost : IBrowserGoalHost
{
    Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default);
    Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default);
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

    internal BrowserGoalAgent(
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

        if (IsTerminal(session.Status))
            return session;

        if (session.Status == BrowserGoalStatus.WaitingForApproval)
        {
            if (_host is ICrashConsistentBrowserGoalHost crashHost && session.PendingJobId is { } waitingJobId)
            {
                var child = await crashHost.GetAsync(waitingJobId, cancellationToken).ConfigureAwait(false);
                if (child?.State == AgentJobState.Completed)
                {
                    var reconciled = AppendVerifiedStep(ClearPending(session with { Status = BrowserGoalStatus.Running }), child.VerifiedStep);
                    await PersistAsync(reconciled, cancellationToken).ConfigureAwait(false);
                    return await RunUntilPauseAsync(reconciled, cancellationToken).ConfigureAwait(false);
                }

                if (child?.State == AgentJobState.Pending && !string.IsNullOrWhiteSpace(session.PendingExactScope))
                {
                    await crashHost.RearmApprovalAsync(waitingJobId, session.PendingExactScope, cancellationToken).ConfigureAwait(false);
                    return await PersistAsync(Touch(session with
                    {
                        Detail = "Approval expired across restart before execution; explicit approval is required again."
                    }), cancellationToken).ConfigureAwait(false);
                }

                if (child?.State == AgentJobState.Running)
                {
                    return await PersistAsync(Touch(session with
                    {
                        Status = BrowserGoalStatus.Failed,
                        Detail = "Browser child was in-flight when the process stopped. It will not be replayed automatically because the side effect is ambiguous."
                    }), cancellationToken).ConfigureAwait(false);
                }
            }
            return session;
        }

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

        if (current.PendingJobId is not null && _host is ICrashConsistentBrowserGoalHost recoveryHost)
        {
            var recovery = await ReconcilePendingChildAsync(current, recoveryHost, cancellationToken).ConfigureAwait(false);
            current = recovery.Session;
            if (!recovery.ContinuePlanning)
                return current;
        }

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

            try
            {
                BrowserLegacyActionMigration.EnsureAutonomousActionUsesTypedVerification(decision.Action);
            }
            catch (InvalidOperationException ex)
            {
                return await PersistAsync(Touch(current with
                {
                    Status = BrowserGoalStatus.Stopped,
                    Detail = $"Nemotron produced an invalid autonomous verification contract: {ex.Message}"
                }), cancellationToken).ConfigureAwait(false);
            }

            BrowserJobOutcome outcome;
            if (_host is ICrashConsistentBrowserGoalHost crashHost)
            {
                var childJobId = Guid.NewGuid();
                current = Touch(current with
                {
                    ActionCount = current.ActionCount + 1,
                    PendingJobId = childJobId,
                    PendingExactScope = null,
                    PendingAction = decision.Action,
                    Detail = "Browser child job reserved; no browser action has executed yet."
                });

                await PersistAsync(current, cancellationToken).ConfigureAwait(false);
                await crashHost.CreateActionAsync(childJobId, decision.Action, cancellationToken).ConfigureAwait(false);
                outcome = await crashHost.AdvanceActionAsync(childJobId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                outcome = await _host.StartActionAsync(decision.Action, cancellationToken).ConfigureAwait(false);
                current = Touch(current with { ActionCount = current.ActionCount + 1 });
            }

            var handled = await HandleChildOutcomeAsync(current, outcome, decision.Action, cancellationToken).ConfigureAwait(false);
            current = handled.Session;
            if (!handled.ContinuePlanning)
                return current;
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
        var resumed = Touch(session with { Detail = outcome.Message });

        if (outcome.State == AgentJobState.Completed)
        {
            resumed = AppendVerifiedStep(ClearPending(resumed with { Status = BrowserGoalStatus.Running }), outcome.VerifiedStep);
            await PersistAsync(resumed, cancellationToken).ConfigureAwait(false);
            return await RunUntilPauseAsync(resumed, cancellationToken).ConfigureAwait(false);
        }
        if (outcome.State == AgentJobState.Cancelled)
            return await PersistAsync(Touch(ClearPending(resumed) with { Status = BrowserGoalStatus.Cancelled }), cancellationToken).ConfigureAwait(false);
        if (outcome.State == AgentJobState.WaitingForApproval)
            throw new InvalidOperationException("A single browser action requested a second approval after consuming the exact grant.");
        if (outcome.State == AgentJobState.Running)
        {
            return await PersistAsync(Touch(resumed with
            {
                Status = BrowserGoalStatus.Failed,
                Detail = "Browser action is in an ambiguous in-flight state and will not be replayed automatically."
            }), cancellationToken).ConfigureAwait(false);
        }

        return await PersistAsync(Touch(ClearPending(resumed) with { Status = BrowserGoalStatus.Failed }), cancellationToken).ConfigureAwait(false);
    }

    public async Task<BrowserGoalSession> CancelAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ValidateSession(session);

        if (session.PendingJobId is { } jobId)
        {
            try
            {
                await _host.CancelAsync(jobId, cancellationToken).ConfigureAwait(false);
            }
            catch (KeyNotFoundException) when (_host is ICrashConsistentBrowserGoalHost)
            {
            }
        }

        return await PersistAsync(Touch(ClearPending(session) with
        {
            Status = BrowserGoalStatus.Cancelled,
            Detail = "Browser goal cancelled by the user."
        }), cancellationToken).ConfigureAwait(false);
    }

    private async Task<(BrowserGoalSession Session, bool ContinuePlanning)> ReconcilePendingChildAsync(
        BrowserGoalSession session,
        ICrashConsistentBrowserGoalHost host,
        CancellationToken cancellationToken)
    {
        var jobId = session.PendingJobId!.Value;
        var existing = await host.GetAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var replannable = Touch(ClearPending(session) with
            {
                ActionCount = Math.Max(0, session.ActionCount - 1),
                Detail = "Recovered a reserved child id that was never created; safely re-planning."
            });
            await PersistAsync(replannable, cancellationToken).ConfigureAwait(false);
            return (replannable, true);
        }

        if (existing.State == AgentJobState.Completed)
        {
            var completed = AppendVerifiedStep(ClearPending(session), existing.VerifiedStep);
            await PersistAsync(completed, cancellationToken).ConfigureAwait(false);
            return (completed, true);
        }

        if (existing.State == AgentJobState.WaitingForApproval)
            return await HandleChildOutcomeAsync(session, existing, session.PendingAction, cancellationToken).ConfigureAwait(false);

        if (existing.State is AgentJobState.Failed or AgentJobState.Cancelled)
            return await HandleChildOutcomeAsync(session, existing, session.PendingAction, cancellationToken).ConfigureAwait(false);

        if (existing.State == AgentJobState.Running)
        {
            var ambiguous = await PersistAsync(Touch(session with
            {
                Status = BrowserGoalStatus.Failed,
                Detail = "Recovered an in-flight browser action with ambiguous side-effect state. It was not replayed automatically."
            }), cancellationToken).ConfigureAwait(false);
            return (ambiguous, false);
        }

        var advanced = await host.AdvanceActionAsync(jobId, cancellationToken).ConfigureAwait(false);
        return await HandleChildOutcomeAsync(session, advanced, session.PendingAction, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(BrowserGoalSession Session, bool ContinuePlanning)> HandleChildOutcomeAsync(
        BrowserGoalSession session,
        BrowserJobOutcome outcome,
        BrowserAction? action,
        CancellationToken cancellationToken)
    {
        if (outcome.State == AgentJobState.WaitingForApproval)
        {
            if (outcome.Approval is null || string.IsNullOrWhiteSpace(outcome.Approval.ExactScope))
            {
                var failedScope = await PersistAsync(Touch(session with
                {
                    Status = BrowserGoalStatus.Failed,
                    Detail = "Browser job requested approval without an exact approval scope."
                }), cancellationToken).ConfigureAwait(false);
                return (failedScope, false);
            }

            var waiting = await PersistAsync(Touch(session with
            {
                Status = BrowserGoalStatus.WaitingForApproval,
                Detail = outcome.Message,
                PendingJobId = outcome.JobId,
                PendingExactScope = outcome.Approval.ExactScope,
                PendingAction = action
            }), cancellationToken).ConfigureAwait(false);
            return (waiting, false);
        }

        if (outcome.State == AgentJobState.Completed)
        {
            var completed = AppendVerifiedStep(ClearPending(session with { Status = BrowserGoalStatus.Running, Detail = outcome.Message }), outcome.VerifiedStep);
            await PersistAsync(completed, cancellationToken).ConfigureAwait(false);
            return (completed, true);
        }

        if (outcome.State == AgentJobState.Cancelled)
        {
            var cancelled = await PersistAsync(Touch(ClearPending(session) with
            {
                Status = BrowserGoalStatus.Cancelled,
                Detail = outcome.Message
            }), cancellationToken).ConfigureAwait(false);
            return (cancelled, false);
        }

        if (outcome.State == AgentJobState.Running)
        {
            var ambiguous = await PersistAsync(Touch(session with
            {
                Status = BrowserGoalStatus.Failed,
                Detail = "Browser child is in an ambiguous in-flight state and was not replayed."
            }), cancellationToken).ConfigureAwait(false);
            return (ambiguous, false);
        }

        if (outcome.State is AgentJobState.Pending or AgentJobState.RetryScheduled)
        {
            var pending = await PersistAsync(Touch(session with
            {
                Status = BrowserGoalStatus.Running,
                PendingJobId = outcome.JobId,
                Detail = outcome.Message
            }), cancellationToken).ConfigureAwait(false);
            return (pending, false);
        }

        var failed = await PersistAsync(Touch(ClearPending(session) with
        {
            Status = BrowserGoalStatus.Failed,
            Detail = outcome.Message
        }), cancellationToken).ConfigureAwait(false);
        return (failed, false);
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

    private static BrowserGoalSession ClearPending(BrowserGoalSession session) => session with
    {
        PendingJobId = null,
        PendingExactScope = null,
        PendingAction = null
    };

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

    private sealed class BrowserHostAdapter : ICrashConsistentBrowserGoalHost
    {
        private readonly BrowserHostRuntime _host;

        public BrowserHostAdapter(BrowserHostRuntime host) =>
            _host = host ?? throw new ArgumentNullException(nameof(host));

        public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) =>
            _host.ObserveAsync(cancellationToken);

        public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default) =>
            _host.StartActionAsync(action, cancellationToken);

        public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default) =>
            _host.CreateActionAsync(jobId, action, cancellationToken);

        public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            _host.AdvanceActionAsync(jobId, cancellationToken);

        public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            _host.GetAsync(jobId, cancellationToken);

        public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            _host.RearmApprovalAsync(jobId, exactScope, cancellationToken);

        public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default) =>
            _host.ApproveAndResumeAsync(jobId, exactScope, cancellationToken);

        public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            _host.CancelAsync(jobId, cancellationToken);
    }
}
