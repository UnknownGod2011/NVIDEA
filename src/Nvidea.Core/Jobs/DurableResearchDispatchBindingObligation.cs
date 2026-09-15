namespace Nvidea.Core.Jobs;

/// <summary>
/// Durable local obligation to publish the exact envelope-bound worker dispatch binding.
/// Presence means publication is still owed; absence never substitutes for worker verification.
/// The obligation contains only non-secret provenance already protected by the local job store.
/// </summary>
public sealed record PendingResearchDispatchBinding(
    Guid ObligationId,
    string OpaqueWorkItemId,
    string RemoteJobId,
    string EnvelopeSha256,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// Persists binding publication intent before touching shared binding transport and clears it only
/// after successful idempotent publication. Restart recovery therefore replays an exact protected
/// obligation rather than reconstructing authority from mutable shared storage.
/// </summary>
public sealed class DurableResearchDispatchBindingObligation
{
    private readonly JsonAgentJobStore _store;
    private readonly ResearchDispatchBindingPublisher _publisher;

    public DurableResearchDispatchBindingObligation(JsonAgentJobStore store, ResearchDispatchBindingPublisher publisher)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task<AgentJobRecord> EnsurePublishedAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (jobId == Guid.Empty) throw new ArgumentException("Research job id is required.", nameof(jobId));

        var current = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        ValidatePublishable(current);

        if (current.PendingResearchDispatchBinding is null)
        {
            var provenance = current.RemoteResearch!;
            var commitment = ResearchWorkItemEnvelopeCommitment.ValidateCanonicalSha256(current.RemoteWorkItemEnvelopeSha256!, nameof(current.RemoteWorkItemEnvelopeSha256));
            var expiresAt = provenance.WorkItemExpiresAt ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
            var obligation = new PendingResearchDispatchBinding(Guid.NewGuid(), provenance.OpaqueWorkItemId, provenance.RemoteJobId!, commitment, expiresAt, DateTimeOffset.UtcNow);
            var staged = current with { PendingResearchDispatchBinding = obligation, UpdatedAt = DateTimeOffset.UtcNow };
            if (!await _store.CompareExchangeAsync(current, staged, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("Research state changed while dispatch-binding publication was being staged.");
            current = staged;
        }

        var pending = current.PendingResearchDispatchBinding!;
        ValidateObligation(current, pending);
        await _publisher.PublishEnvelopeBoundAsync(pending.OpaqueWorkItemId, pending.RemoteJobId, pending.EnvelopeSha256, pending.ExpiresAt, cancellationToken: cancellationToken).ConfigureAwait(false);

        var latest = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' disappeared while dispatch binding was being published.");
        if (latest.PendingResearchDispatchBinding?.ObligationId != pending.ObligationId)
            throw new InvalidOperationException("Dispatch-binding obligation changed during publication; refusing to clear it.");
        ValidateObligation(latest, pending);
        var completed = latest with { PendingResearchDispatchBinding = null, UpdatedAt = DateTimeOffset.UtcNow };
        if (!await _store.CompareExchangeAsync(latest, completed, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while dispatch-binding publication was being completed.");
        return completed;
    }

    private static void ValidatePublishable(AgentJobRecord job)
    {
        var provenance = job.RemoteResearch ?? throw new InvalidOperationException("Research job has no remote dispatch provenance.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal)
            || job.State != AgentJobState.Running
            || job.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State is not (RemoteResearchProvenanceState.Dispatched or RemoteResearchProvenanceState.CancelRequested)
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId)
            || string.IsNullOrWhiteSpace(job.RemoteWorkItemEnvelopeSha256)
            || job.PendingAuditEvent is not null)
            throw new InvalidOperationException("Only an active audit-settled envelope-bound Nebius stage can publish its dispatch binding.");
    }

    private static void ValidateObligation(AgentJobRecord job, PendingResearchDispatchBinding pending)
    {
        ValidatePublishable(job);
        var provenance = job.RemoteResearch!;
        if (!string.Equals(pending.OpaqueWorkItemId, provenance.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(pending.RemoteJobId, provenance.RemoteJobId, StringComparison.Ordinal)
            || !ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(pending.EnvelopeSha256, job.RemoteWorkItemEnvelopeSha256!))
            throw new InvalidOperationException("Durable dispatch-binding obligation no longer matches protected remote provenance.");
        var expectedExpiry = provenance.WorkItemExpiresAt ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
        if (pending.ExpiresAt != expectedExpiry)
            throw new InvalidOperationException("Durable dispatch-binding obligation lifetime no longer matches protected remote provenance.");
    }
}
