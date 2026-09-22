namespace Nvidea.Core.Desktop;

/// <summary>
/// Least-authority public surface for durable browser-goal transactions.
/// Composition may enforce transaction lifetime, shutdown ordering, and other trusted
/// boundaries without exposing those authorities to callers.
/// </summary>
public interface IBrowserGoalAgent
{
    Task<BrowserGoalSession> ResumeAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<BrowserGoalSession> RunUntilPauseAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default);

    Task<BrowserGoalSession> ApproveAndContinueAsync(
        BrowserGoalSession session,
        string exactScope,
        CancellationToken cancellationToken = default);

    Task<BrowserGoalSession> CancelAsync(
        BrowserGoalSession session,
        CancellationToken cancellationToken = default);
}
