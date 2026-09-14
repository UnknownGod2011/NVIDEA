namespace Nvidea.Core.Jobs;

/// <summary>
/// Reconstructs the worker-authoritative signed dispatch binding exclusively from protected durable
/// client provenance. When a work-item envelope commitment is present, recovery signs that exact
/// original digest; it never re-hashes mutable shared transport state after a crash. Legacy jobs with
/// no commitment may still recover a V1 binding for lifecycle/cancellation compatibility, but the
/// hardened worker requires V2 before execution.
/// </summary>
public sealed class ResearchDispatchBindingRecovery
{
    private readonly JsonAgentJobStore _store;
    private readonly ResearchDispatchBindingPublisher _publisher;

    public ResearchDispatchBindingRecovery(
        JsonAgentJobStore store,
        ResearchDispatchBindingPublisher publisher)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task<AgentJobRecord> EnsureAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));

        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Dispatch-binding recovery only accepts research jobs.");
        if (job.PendingAuditEvent is not null)
            throw new InvalidOperationException("Dispatch binding cannot be recovered before the exact pending audit is durable.");

        var provenance = job.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote dispatch provenance.");
        if (job.State != AgentJobState.Running
            || job.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State is not (RemoteResearchProvenanceState.Dispatched or RemoteResearchProvenanceState.CancelRequested)
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
        {
            throw new InvalidOperationException("Only an active, durably attached Nebius research stage can recover its dispatch binding.");
        }

        var expiresAt = provenance.WorkItemExpiresAt
            ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
        if (job.RemoteWorkItemEnvelopeSha256 is { } commitment)
        {
            await _publisher.PublishEnvelopeBoundAsync(
                provenance.OpaqueWorkItemId,
                provenance.RemoteJobId,
                commitment,
                expiresAt,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Transitional compatibility for jobs created before envelope commitments existed. This
            // keeps cancellation/lifecycle recovery possible, but does not authorize hardened V2
            // worker execution because the worker rejects unbound V1 bindings.
            await _publisher.PublishAsync(
                provenance.OpaqueWorkItemId,
                provenance.RemoteJobId,
                expiresAt,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return job;
    }
}
