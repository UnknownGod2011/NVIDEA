namespace Nvidea.Core.Jobs;

/// <summary>
/// Recovers worker-authoritative binding publication exclusively from protected durable client state.
/// Production V2 jobs use a durable pending/completed obligation; legacy jobs remain recoverable only
/// for lifecycle compatibility and cannot authorize the hardened V2-only worker.
/// </summary>
public sealed class ResearchDispatchBindingRecovery
{
    private readonly JsonAgentJobStore _store;
    private readonly ResearchDispatchBindingPublisher _publisher;
    private readonly DurableResearchDispatchBindingObligation? _obligation;

    public ResearchDispatchBindingRecovery(JsonAgentJobStore store, ResearchDispatchBindingPublisher publisher, DurableResearchDispatchBindingObligation? obligation = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _obligation = obligation;
    }

    public async Task<AgentJobRecord> EnsureAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("Research job id is required.", nameof(jobId));
        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Dispatch-binding recovery only accepts research jobs.");
        if (job.PendingAuditEvent is not null)
            throw new InvalidOperationException("Dispatch binding cannot be recovered before the exact pending audit is durable.");

        var provenance = job.RemoteResearch ?? throw new InvalidOperationException("Research job has no remote dispatch provenance.");
        if (job.State != AgentJobState.Running || job.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State is not (RemoteResearchProvenanceState.Dispatched or RemoteResearchProvenanceState.CancelRequested)
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
            throw new InvalidOperationException("Only an active, durably attached Nebius research stage can recover its dispatch binding.");

        if (job.RemoteWorkItemEnvelopeSha256 is not null && _obligation is not null)
            return await _obligation.EnsurePublishedAsync(jobId, cancellationToken).ConfigureAwait(false);

        var expiresAt = provenance.WorkItemExpiresAt ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
        if (job.RemoteWorkItemEnvelopeSha256 is { } commitment)
            await _publisher.PublishEnvelopeBoundAsync(provenance.OpaqueWorkItemId, provenance.RemoteJobId, commitment, expiresAt, cancellationToken: cancellationToken).ConfigureAwait(false);
        else
            await _publisher.PublishAsync(provenance.OpaqueWorkItemId, provenance.RemoteJobId, expiresAt, cancellationToken: cancellationToken).ConfigureAwait(false);
        return job;
    }
}
