namespace Nvidea.Core.Jobs;

/// <summary>
/// Re-publishes the signed worker dispatch binding from the exact durable remote-research provenance
/// after a client crash. Recovery is deliberately local: it never queries or mutates Nebius and it
/// never invents a remote id. The durable dispatch/cancellation audit must already be settled before
/// shared binding state can be written.
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
            throw new InvalidOperationException("Dispatch-binding recovery requires the durable remote audit to be settled first.");

        var provenance = job.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote execution provenance.");
        if (job.State != AgentJobState.Running
            || job.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State is not (RemoteResearchProvenanceState.Dispatched or RemoteResearchProvenanceState.CancelRequested)
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
        {
            throw new InvalidOperationException(
                "Dispatch-binding recovery requires an active durable Dispatched or CancelRequested Nebius research stage.");
        }

        var expiresAt = provenance.WorkItemExpiresAt
            ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
        await _publisher.PublishAsync(
            provenance.OpaqueWorkItemId,
            provenance.RemoteJobId,
            expiresAt,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return job;
    }
}
