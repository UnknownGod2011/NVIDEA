using Nvidea.Core.Browser;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

public enum BrowserAmbiguousRecoveryStatus
{
    Reconciled,
    NeedsHumanResolution,
    NotAmbiguous
}

public sealed record BrowserAmbiguousRecoveryResult(
    BrowserAmbiguousRecoveryStatus Status,
    Guid JobId,
    string Detail,
    BrowserJobOutcome ChildOutcome,
    BrowserObservation? Evidence = null);

/// <summary>
/// Host boundary used only for crash reconciliation. Implementations may inspect fresh browser
/// evidence and mark an already-running durable job completed only when the intended end state can
/// be proven without executing the action again.
/// </summary>
public interface IBrowserAmbiguousRecoveryHost
{
    Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<BrowserAmbiguousRecoveryResult> TryReconcileAmbiguousAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Conservative, deterministic reconciliation rules for a browser action that may have crossed an
/// external side-effect boundary immediately before a process crash. This code never executes an
/// action and never asks a model whether an action probably succeeded.
/// </summary>
public static class BrowserAmbiguousStateReconciler
{
    public static (bool Verified, string Detail) TryVerify(
        BrowserAction action,
        BrowserObservation current)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(current);

        if (action.Kind == BrowserActionKind.Read)
            return (true, "Fresh read-only browser observation recovered the interrupted read step.");

        if (action.Kind == BrowserActionKind.Navigate && action.Destination is not null)
        {
            var expected = Normalize(action.Destination);
            var actual = Normalize(current.Url);
            return expected == actual
                ? (true, $"Crash reconciliation proved the navigation destination from the current URL: {actual}.")
                : (false, $"Current URL {actual} does not prove the intended navigation destination {expected}.");
        }

        // Download completion cannot be established from DOM/URL evidence, and upload success may
        // have an external effect beyond the page. Never auto-resolve either after a crash.
        if (action.Kind is BrowserActionKind.Download or BrowserActionKind.Upload)
            return (false, $"{action.Kind} completion requires human resolution after an ambiguous crash boundary.");

        if (string.IsNullOrWhiteSpace(action.ExpectedState))
            return (false, "The interrupted action has no deterministic expected-state assertion, so it cannot be auto-reconciled safely.");

        var expectedState = action.ExpectedState.Trim();
        var visible = current.VisibleText.Contains(expectedState, StringComparison.OrdinalIgnoreCase)
            || current.Title.Contains(expectedState, StringComparison.OrdinalIgnoreCase)
            || current.Elements.Any(element =>
                (element.Name?.Contains(expectedState, StringComparison.OrdinalIgnoreCase) ?? false)
                || (element.Value?.Contains(expectedState, StringComparison.OrdinalIgnoreCase) ?? false));

        return visible
            ? (true, $"Crash reconciliation observed the intended expected state: {expectedState}")
            : (false, $"Fresh browser evidence does not contain the intended expected state: {expectedState}");
    }

    private static string Normalize(Uri uri)
    {
        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }
}

/// <summary>
/// Explicit recovery service for a parent browser-goal session whose child was left durably
/// Running by a process failure. Positive browser evidence may restore the parent to Running;
/// inconclusive evidence leaves the session untouched for human resolution and never replays it.
/// </summary>
public sealed class BrowserAmbiguousRecoveryService
{
    private readonly IBrowserAmbiguousRecoveryHost _host;
    private readonly IBrowserGoalSessionStore _store;
    private readonly TimeProvider _timeProvider;

    public BrowserAmbiguousRecoveryService(
        IBrowserAmbiguousRecoveryHost host,
        IBrowserGoalSessionStore store,
        TimeProvider? timeProvider = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<(BrowserGoalSession Session, BrowserAmbiguousRecoveryResult Recovery)> RecoverAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("Browser goal session id is required.", nameof(sessionId));

        var session = await _store.GetAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Browser goal session '{sessionId}' was not found.");
        if (session.PendingJobId is not { } jobId)
            throw new InvalidOperationException("Browser goal session has no pending child to reconcile.");

        var child = await _host.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Browser child job '{jobId}' was not found.");
        if (child.State != AgentJobState.Running)
        {
            return (session, new BrowserAmbiguousRecoveryResult(
                BrowserAmbiguousRecoveryStatus.NotAmbiguous,
                jobId,
                $"Child job is {child.State}; ambiguous-running reconciliation is not applicable.",
                child));
        }

        var recovery = await _host.TryReconcileAmbiguousAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (recovery.Status != BrowserAmbiguousRecoveryStatus.Reconciled
            || recovery.ChildOutcome.State != AgentJobState.Completed
            || recovery.ChildOutcome.VerifiedStep is null)
        {
            var unresolved = session with
            {
                Status = BrowserGoalStatus.Failed,
                Detail = $"Ambiguous browser action requires human resolution. {recovery.Detail}",
                UpdatedAt = _timeProvider.GetUtcNow()
            };
            await _store.SaveAsync(unresolved, cancellationToken).ConfigureAwait(false);
            return (unresolved, recovery);
        }

        var history = (session.VerifiedSteps ?? Array.Empty<BrowserGoalVerifiedStep>()).ToList();
        if (!history.Any(step => step.JobId == jobId))
            history.Add(recovery.ChildOutcome.VerifiedStep);
        if (history.Count > 50)
            history.RemoveRange(0, history.Count - 50);

        var reconciled = session with
        {
            Status = BrowserGoalStatus.Running,
            Detail = recovery.Detail,
            PendingJobId = null,
            PendingExactScope = null,
            PendingAction = null,
            VerifiedSteps = history,
            UpdatedAt = _timeProvider.GetUtcNow()
        };
        await _store.SaveAsync(reconciled, cancellationToken).ConfigureAwait(false);
        return (reconciled, recovery);
    }
}
