using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Payload-free observation decorator for the trusted remote-research client boundary.
/// Dispatch alone never counts as background-execution proof: delivery may be ambiguous and
/// successful Create does not prove that a worker actually ran. Evidence is recorded only after
/// a reconciliation/recovery call returns durable provenance showing that a remotely produced,
/// cryptographically verified result has been applied locally and its durable audit transition
/// has no unresolved outbox obligation.
/// </summary>
internal sealed class EvidenceObservingRemoteResearchClientRuntime : IRemoteResearchClientRuntime
{
    private readonly IRemoteResearchClientRuntime _inner;
    private readonly SessionEvidenceLedger _evidence;

    internal EvidenceObservingRemoteResearchClientRuntime(
        IRemoteResearchClientRuntime inner,
        SessionEvidenceLedger? evidence = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _evidence = evidence ?? SessionEvidenceLedger.ProcessLocal;
    }

    public Task<AgentJobRecord> DispatchAsync(
        RemoteResearchWorkItem workItem,
        ResearchCloudAuthorization authorization,
        CancellationToken cancellationToken = default) =>
        _inner.DispatchAsync(workItem, authorization, cancellationToken);

    public async Task<AgentJobRecord> ReconcileReservedAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        Observe(await _inner.ReconcileReservedAsync(jobId, cancellationToken).ConfigureAwait(false));

    public async Task<AgentJobRecord> ReconcileDispatchedAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default) =>
        Observe(await _inner.ReconcileDispatchedAsync(jobId, now, cancellationToken).ConfigureAwait(false));

    public Task<AgentJobRecord> RequestCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        _inner.RequestCancellationAsync(jobId, cancellationToken);

    public async Task<AgentJobRecord> ReconcileCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        Observe(await _inner.ReconcileCancellationAsync(jobId, cancellationToken).ConfigureAwait(false));

    public async Task<AgentJobRecord> RecoverPendingAuditAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        Observe(await _inner.RecoverPendingAuditAsync(jobId, cancellationToken).ConfigureAwait(false));

    private AgentJobRecord Observe(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        // ResultApplied is set only by the trusted result-ingestion path after the signed result
        // envelope is verified and durably CAS-applied to the local job. The job has moved back to
        // Local by then. Also require the durable audit outbox to be clear: a crash after result CAS
        // but before its audit append must not become judge-visible proof until audit recovery has
        // completed. This keeps the evidence boundary downstream of both authenticated ingestion and
        // its durable accountability record.
        if (record.ExecutionLocation == JobExecutionLocation.Local
            && record.RemoteResearch is { State: RemoteResearchProvenanceState.ResultApplied }
            && record.PendingAuditEvent is null)
        {
            _evidence.Record(SessionEvidenceKind.NebiusBackgroundExecutionObserved);
        }

        return record;
    }
}
