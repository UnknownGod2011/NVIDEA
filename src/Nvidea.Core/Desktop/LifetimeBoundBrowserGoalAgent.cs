namespace Nvidea.Core.Desktop;

/// <summary>
/// Exactly-one-lease transaction boundary for an already-composed browser goal agent.
/// The wrapped agent deliberately remains unaware of composition lifetime so its internal
/// Resume/Approve -> Run delegation cannot re-enter the non-reentrant root gate.
/// </summary>
internal sealed class LifetimeBoundBrowserGoalAgent
{
    private readonly BrowserGoalAgent _inner;
    private readonly BrowserGoalTransactionLifetime _transactions = new();

    internal LifetimeBoundBrowserGoalAgent(BrowserGoalAgent inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    internal void BindCompositionLifetime(CompositionLifetimeGate lifetime)
        => _transactions.Bind(lifetime);

    internal Task<BrowserGoalSession> ResumeAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
        => _transactions.ExecuteAsync(
            token => _inner.ResumeAsync(sessionId, token),
            cancellationToken);

    internal Task<BrowserGoalSession> RunUntilPauseAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _transactions.ExecuteAsync(
            token => _inner.RunUntilPauseAsync(session, token),
            cancellationToken);
    }

    internal Task<BrowserGoalSession> ApproveAndContinueAsync(
        BrowserGoalSession session,
        string exactScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _transactions.ExecuteAsync(
            token => _inner.ApproveAndContinueAsync(session, exactScope, token),
            cancellationToken);
    }

    internal Task<BrowserGoalSession> CancelAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _transactions.ExecuteAsync(
            token => _inner.CancelAsync(session, token),
            cancellationToken);
    }
}
