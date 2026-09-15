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
/// Internal deterministic seam used only by tests to pause/fault the narrow interval after the
/// binding is externally visible but before its protected durable obligation is completed.
/// Production construction never supplies an observer.
/// </summary>
internal interface IResearchDispatchBindingCompletionObserver
{
    Task AfterPublishedAsync(Guid jobId, PendingResearchDispatchBinding obligation, CancellationToken cancellationToken);
}

/// <summary>
/// Persists binding publication intent before touching shared binding transport and clears it only
/// after successful idempotent publication. Restart recovery therefore replays an exact protected
/// obligation rather than reconstructing authority from mutable shared storage.
/// </summary>
public sealed class DurableResearchDispatchBindingObligation
{
    private const int CompletionAttempts = 4;
    private readonly JsonAgentJobStore _store;
    private readonly ResearchDispatchBindingPublisher _publisher;
    private readonly IResearchDispatchBindingCompletionObserver? _completionObserver;

    public DurableResearchDispatchBindingObligation(JsonAgentJobStore store, ResearchDispatchBindingPublisher publisher)
        : this(store, publisher, null)
    {
    }

    internal DurableResearchDispatchBindingObligation(
        JsonAgentJobStore store,
        ResearchDispatchBindingPublisher publisher,
        IResearchDispatchBindingCompletionObserver? completionObserver)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _completionObserver = completionObserver;
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
        ValidateObligation(current, pending, requireAuditSettled: true);
        await _publisher.PublishEnvelopeBoundAsync(pending.OpaqueWorkItemId, pending.RemoteJobId, pending.EnvelopeSha256, pending.ExpiresAt, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (_completionObserver is not null)
            await _completionObserver.AfterPublishedAsync(jobId, pending, cancellationToken).ConfigureAwait(false);

        // Publication is already externally visible. Completion is bookkeeping, not authority for
        // another external side effect. Legitimate lifecycle/result transitions may therefore finish
        // while this method is paused, including transitions to local terminal/result-applied state.
        // We clear only if the exact obligation and every authority-bearing provenance field remain
        // unchanged and the resulting state/provenance pair is one produced by the research lifecycle.
        for (var attempt = 0; attempt < CompletionAttempts; attempt++)
        {
            var latest = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Research job '{jobId}' disappeared while dispatch binding was being published.");
            if (latest.PendingResearchDispatchBinding is null)
                return latest;
            if (latest.PendingResearchDispatchBinding.ObligationId != pending.ObligationId)
                throw new InvalidOperationException("Dispatch-binding obligation changed during publication; refusing to clear it.");

            ValidateObligation(latest, pending, requireAuditSettled: false);
            var completed = latest with { PendingResearchDispatchBinding = null, UpdatedAt = DateTimeOffset.UtcNow };
            if (await _store.CompareExchangeAsync(latest, completed, cancellationToken).ConfigureAwait(false))
                return completed;
        }

        throw new InvalidOperationException("Research state kept changing while dispatch-binding publication was being completed; the exact durable obligation remains pending for restart recovery.");
    }

    private static void ValidatePublishable(AgentJobRecord job)
    {
        ValidateObligationShape(job, requireAuditSettled: true);
    }

    private static void ValidateObligation(AgentJobRecord job, PendingResearchDispatchBinding pending, bool requireAuditSettled)
    {
        ValidateObligationShape(job, requireAuditSettled);
        var provenance = job.RemoteResearch!;
        if (!string.Equals(pending.OpaqueWorkItemId, provenance.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(pending.RemoteJobId, provenance.RemoteJobId, StringComparison.Ordinal)
            || !ResearchWorkItemEnvelopeCommitment.FixedTimeEquals(pending.EnvelopeSha256, job.RemoteWorkItemEnvelopeSha256!))
            throw new InvalidOperationException("Durable dispatch-binding obligation no longer matches protected remote provenance.");
        var expectedExpiry = provenance.WorkItemExpiresAt ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
        if (pending.ExpiresAt != expectedExpiry)
            throw new InvalidOperationException("Durable dispatch-binding obligation lifetime no longer matches protected remote provenance.");
    }

    private static void ValidateObligationShape(AgentJobRecord job, bool requireAuditSettled)
    {
        var provenance = job.RemoteResearch ?? throw new InvalidOperationException("Research job has no remote dispatch provenance.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId)
            || string.IsNullOrWhiteSpace(job.RemoteWorkItemEnvelopeSha256))
            throw new InvalidOperationException("Dispatch-binding completion requires envelope-bound research provenance.");

        if (requireAuditSettled)
        {
            if (job.State != AgentJobState.Running
                || job.ExecutionLocation != JobExecutionLocation.NebiusServerless
                || provenance.State is not (RemoteResearchProvenanceState.Dispatched or RemoteResearchProvenanceState.CancelRequested)
                || job.PendingAuditEvent is not null)
                throw new InvalidOperationException("Only an active audit-settled Nebius stage can publish its dispatch binding.");
            return;
        }

        var validPostPublicationState = (job.State, job.ExecutionLocation, provenance.State) switch
        {
            (AgentJobState.Running, JobExecutionLocation.NebiusServerless, RemoteResearchProvenanceState.Dispatched) => true,
            (AgentJobState.Running, JobExecutionLocation.NebiusServerless, RemoteResearchProvenanceState.CancelRequested) => true,
            (AgentJobState.Pending, JobExecutionLocation.Local, RemoteResearchProvenanceState.ResultApplied) => true,
            (AgentJobState.Completed, JobExecutionLocation.Local, RemoteResearchProvenanceState.ResultApplied) => true,
            (AgentJobState.Cancelled, JobExecutionLocation.Local, RemoteResearchProvenanceState.Cancelled) => true,
            (AgentJobState.Failed, JobExecutionLocation.Local, RemoteResearchProvenanceState.RemoteFailed) => true,
            (AgentJobState.Failed, JobExecutionLocation.Local, RemoteResearchProvenanceState.Expired) => true,
            _ => false
        };
        if (!validPostPublicationState)
            throw new InvalidOperationException("Dispatch-binding obligation encountered an inconsistent lifecycle state after publication; refusing to clear it.");
    }
}
