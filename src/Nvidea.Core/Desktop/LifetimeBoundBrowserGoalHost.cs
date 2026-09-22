using Nvidea.Core.Browser;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Least-authority browser-goal host decorator that linearizes each privileged host call
/// against composition shutdown. This is intentionally internal: callers receive goal-level
/// authority, never the composition lifetime gate itself.
///
/// This boundary prevents BrowserHostRuntime from being disposed while an individual host
/// operation is in flight and makes calls attempted after composition disposal fail closed.
/// It does not by itself make a multi-call BrowserGoalAgent transaction atomic; the agent
/// must still hold one outer lifetime lease for a complete Run/Resume/Approve/Cancel operation.
/// </summary>
internal sealed class LifetimeBoundBrowserGoalHost : ICrashConsistentBrowserGoalHost
{
    private readonly ICrashConsistentBrowserGoalHost _inner;
    private readonly CompositionLifetimeGate _lifetime;

    internal LifetimeBoundBrowserGoalHost(
        ICrashConsistentBrowserGoalHost inner,
        CompositionLifetimeGate lifetime)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    public Task<BrowserObservation> ObserveAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => _inner.ObserveAsync(ct), cancellationToken);

    public Task<BrowserJobOutcome> StartActionAsync(BrowserAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ExecuteAsync(ct => _inner.StartActionAsync(action, ct), cancellationToken);
    }

    public Task<BrowserJobOutcome> CreateActionAsync(Guid jobId, BrowserAction action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ExecuteAsync(ct => _inner.CreateActionAsync(jobId, action, ct), cancellationToken);
    }

    public Task<BrowserJobOutcome> AdvanceActionAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => _inner.AdvanceActionAsync(jobId, ct), cancellationToken);

    public Task<BrowserJobOutcome?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => _inner.GetAsync(jobId, ct), cancellationToken);

    public Task<BrowserJobOutcome> RearmApprovalAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("An exact approval scope is required.", nameof(exactScope));
        return ExecuteAsync(ct => _inner.RearmApprovalAsync(jobId, exactScope, ct), cancellationToken);
    }

    public Task<BrowserJobOutcome> ApproveAndResumeAsync(Guid jobId, string exactScope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exactScope))
            throw new ArgumentException("An exact approval scope is required.", nameof(exactScope));
        return ExecuteAsync(ct => _inner.ApproveAndResumeAsync(jobId, exactScope, ct), cancellationToken);
    }

    public Task<BrowserJobOutcome> CancelAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => _inner.CancelAsync(jobId, ct), cancellationToken);

    private async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await using var lease = await _lifetime.AcquireAsync(cancellationToken).ConfigureAwait(false);
        return await operation(cancellationToken).ConfigureAwait(false);
    }
}
