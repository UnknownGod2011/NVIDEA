using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Payload-free lifecycle stages exposed only to trusted internal/test composition. The observer is
/// intentionally notified after protected evidence has been invalidated but receives no job, receipt,
/// browser, URL, locator, approval, or typed-value material.
/// </summary>
internal enum BrowserVerificationLifecycleStage
{
    AdmissionEvidenceCleared,
    ExecutionEvidenceCleared,
}

/// <summary>
/// Narrow observation seam for deterministic lifecycle/concurrency qualification. Production
/// composition does not install an observer; implementations must not be given browser or receipt
/// authority through this contract.
/// </summary>
internal interface IBrowserVerificationLifecycleObserver
{
    ValueTask ObserveAsync(
        BrowserVerificationLifecycleStage stage,
        CancellationToken cancellationToken);
}

/// <summary>
/// Couples durable browser-job admission/execution with the judge-evidence publication boundary.
/// This type exists to make ordering structural rather than relying on callers to remember it:
/// stale evidence is cleared before durable admission and before every execution attempt, while
/// evidence publication happens only after the orchestrator has returned its authoritative record.
/// </summary>
internal sealed class BrowserVerificationActionLifecycle
{
    private readonly BrowserVerificationPublicationBoundary _publication;
    private readonly IBrowserVerificationLifecycleObserver? _observer;

    internal BrowserVerificationActionLifecycle(
        BrowserVerificationPublicationBoundary publication,
        IBrowserVerificationLifecycleObserver? observer = null)
    {
        _publication = publication ?? throw new ArgumentNullException(nameof(publication));
        _observer = observer;
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
        await ObserveAsync(BrowserVerificationLifecycleStage.AdmissionEvidenceCleared, cancellationToken)
            .ConfigureAwait(false);
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
        await ObserveAsync(BrowserVerificationLifecycleStage.ExecutionEvidenceCleared, cancellationToken)
            .ConfigureAwait(false);
        var authoritative = await runNextStep(cancellationToken).ConfigureAwait(false);
        return await _publication
            .ObserveCommittedAsync(authoritative, cancellationToken)
            .ConfigureAwait(false);
    }

    private ValueTask ObserveAsync(
        BrowserVerificationLifecycleStage stage,
        CancellationToken cancellationToken) =>
        _observer?.ObserveAsync(stage, cancellationToken) ?? ValueTask.CompletedTask;
}
