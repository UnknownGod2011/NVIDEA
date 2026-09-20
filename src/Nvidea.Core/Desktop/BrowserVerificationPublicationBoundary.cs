using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Keeps judge-evidence persistence strictly downstream of authoritative browser-job completion.
/// Evidence publication is observational: once the durable job is Completed, publication failure
/// must never be converted into an action failure that could encourage replay of a side effect.
/// </summary>
internal sealed class BrowserVerificationPublicationBoundary
{
    private readonly DurableBrowserVerificationPublisher _publisher;

    public BrowserVerificationPublicationBoundary(DurableBrowserVerificationPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <summary>
    /// Must be called before admitting a new browser action. Failure is intentionally propagated,
    /// because no browser side effect has occurred yet and stale green evidence must not survive.
    /// </summary>
    public Task BeginActionAsync(CancellationToken cancellationToken = default) =>
        _publisher.BeginActionAsync(cancellationToken);

    /// <summary>
    /// Attempts to publish least-authority evidence after the orchestrator has durably committed the
    /// returned record. The authoritative job record is always returned unchanged. Publication errors
    /// are collapsed to a fail-closed status so callers cannot mistake an evidence-store problem for a
    /// browser-action failure and retry a completed side effect.
    /// </summary>
    public async Task<BrowserVerificationPublicationResult> ObserveCommittedAsync(
        AgentJobRecord authoritativeJob,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authoritativeJob);

        if (authoritativeJob.State != AgentJobState.Completed)
            return new BrowserVerificationPublicationResult(authoritativeJob, Published: false, PublicationFailed: false);

        try
        {
            var published = await _publisher
                .TryPublishCompletedAsync(authoritativeJob, cancellationToken)
                .ConfigureAwait(false);
            return new BrowserVerificationPublicationResult(authoritativeJob, published, PublicationFailed: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser job is already durably complete. Cancellation of this observational write
            // must not rewrite history into a replayable action failure.
            return new BrowserVerificationPublicationResult(authoritativeJob, Published: false, PublicationFailed: true);
        }
        catch (Exception) when (authoritativeJob.State == AgentJobState.Completed)
        {
            return new BrowserVerificationPublicationResult(authoritativeJob, Published: false, PublicationFailed: true);
        }
    }
}

internal sealed record BrowserVerificationPublicationResult(
    AgentJobRecord AuthoritativeJob,
    bool Published,
    bool PublicationFailed)
{
    public bool EvidenceVerified => Published && !PublicationFailed;
}
