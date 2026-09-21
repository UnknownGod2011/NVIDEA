using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Couples durable browser-job admission/execution with the judge-evidence publication boundary.
/// This type exists to make ordering structural rather than relying on callers to remember it:
/// stale evidence is cleared before durable admission and before every execution attempt, while
/// evidence publication happens only after the orchestrator has returned its authoritative record.
/// </summary>
internal sealed class BrowserVerificationActionLifecycle
{
    private readonly BrowserVerificationPublicationBoundary _publication;

    internal BrowserVerificationActionLifecycle(BrowserVerificationPublicationBoundary publication)
    {
        _publication = publication ?? throw new ArgumentNullException(nameof(publication));
    }

    /// <summary>
    /// Clears any previous green receipt before invoking durable job creation. Clear failure is
    /// intentionally propagated because no new browser side effect has been admitted yet.
    /// </summary>
    internal async Task<AgentJobRecord> AdmitAsync(
        Func<CancellationToken, Task<AgentJobRecord>> create,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(create);

        await _publication.BeginActionAsync(cancellationToken).ConfigureAwait(false);
        return await create(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invalidates any previous green receipt before an execution attempt, then observes only the
    /// authoritative record returned by that durable boundary. Clearing here is intentionally
    /// redundant with new-job admission: it protects resumed/migrated/pre-verification jobs that may
    /// be advanced without passing through this process's admission path. Clear failure occurs before
    /// browser execution and therefore safely prevents the attempt. Once execution returns, evidence
    /// publication is observational: its failure can never replace or downgrade the authoritative
    /// record, preventing evidence-store faults from encouraging side-effect replay.
    /// </summary>
    internal async Task<BrowserVerificationPublicationResult> AdvanceAsync(
        Func<CancellationToken, Task<AgentJobRecord>> runNextStep,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runNextStep);

        await _publication.BeginActionAsync(cancellationToken).ConfigureAwait(false);
        var authoritative = await runNextStep(cancellationToken).ConfigureAwait(false);
        return await _publication
            .ObserveCommittedAsync(authoritative, cancellationToken)
            .ConfigureAwait(false);
    }
}
