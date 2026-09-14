namespace Nvidea.Core.Jobs;

/// <summary>
/// Best-effort cleanup boundary for signed dispatch bindings. A binding is deleted only after the
/// durable local job record proves that the remote stage is no longer executable: the verified
/// result was applied locally, or the provider reached a terminal failure/cancellation/expiry state.
/// Cleanup is intentionally post-CAS and non-authoritative; failures leave a cryptographically
/// bounded binding behind for bucket lifecycle cleanup rather than corrupting durable job state.
/// </summary>
public sealed class ResearchDispatchBindingCleanup
{
    private readonly IProtectedResearchDispatchBindingTransport _transport;

    public ResearchDispatchBindingCleanup(IProtectedResearchDispatchBindingTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public async Task TryCleanupIfTerminalAsync(AgentJobRecord job)
    {
        ArgumentNullException.ThrowIfNull(job);
        var provenance = job.RemoteResearch;
        if (provenance is null || !CanDeleteBinding(job, provenance))
            return;

        try
        {
            await _transport.DeleteAsync(provenance.OpaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Best effort by design. The signed binding carries no research payload and is TTL-bounded.
            // Durable job state must never be rolled back merely because shared-storage cleanup failed.
        }
    }

    public static bool CanDeleteBinding(AgentJobRecord job, RemoteResearchProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(provenance);

        if (job.ExecutionLocation != JobExecutionLocation.Local)
            return false;

        return provenance.State switch
        {
            RemoteResearchProvenanceState.ResultApplied =>
                job.State is AgentJobState.Pending or AgentJobState.Completed,
            RemoteResearchProvenanceState.Cancelled =>
                job.State == AgentJobState.Cancelled,
            RemoteResearchProvenanceState.RemoteFailed or RemoteResearchProvenanceState.Expired =>
                job.State == AgentJobState.Failed,
            _ => false
        };
    }
}

/// <summary>
/// Client-only composition boundary for Nebius remote research. New dispatches atomically persist
/// reservation provenance, the exact protected-envelope commitment and the reservation audit intent
/// before Serverless Create, then publish V2 signed bindings from that durable trust root. Crash
/// recovery likewise signs only the protected durable digest; it never re-hashes mutable shared
/// transport state. Client private keys never cross this boundary into Serverless job configuration.
///
/// The optional result-envelope private key preserves compatibility for lower-level fixtures. Live
/// production composition always supplies a distinct key so RSA-PSS dispatch signing and OAEP-SHA256
/// result decryption do not share one cryptographic identity.
/// </summary>
public sealed class NebiusResearchClientRuntime : IRemoteResearchClientRuntime
{
    private readonly ResearchDispatchBindingCleanup _bindingCleanup;
    private readonly ResearchDispatchBindingRecovery _bindingRecovery;

    private NebiusResearchClientRuntime(
        TwoPhaseNebiusResearchDispatcher dispatcher,
        NebiusResearchLifecycleReconciler reconciler,
        RemoteResearchResultIngestor ingestor,
        ResearchDispatchBindingPublisher bindingPublisher,
        ResearchDispatchBindingRecovery bindingRecovery,
        ResearchDispatchBindingCleanup bindingCleanup)
    {
        Dispatcher = dispatcher;
        Reconciler = reconciler;
        Ingestor = ingestor;
        BindingPublisher = bindingPublisher;
        _bindingRecovery = bindingRecovery;
        _bindingCleanup = bindingCleanup;
    }

    public TwoPhaseNebiusResearchDispatcher Dispatcher { get; }
    public NebiusResearchLifecycleReconciler Reconciler { get; }
    public RemoteResearchResultIngestor Ingestor { get; }
    public ResearchDispatchBindingPublisher BindingPublisher { get; }

    public static NebiusResearchClientRuntime Create(
        JsonAgentJobStore store,
        INebiusServerlessJobClient serverless,
        IProtectedResearchWorkItemTransport workItems,
        IProtectedResearchResultTransport results,
        IProtectedResearchDispatchBindingTransport bindings,
        NebiusResearchDispatchOptions options,
        string clientPrivateKeyPem,
        Nvidea.Core.Capabilities.IAuditTrail auditTrail,
        string? clientResultPrivateKeyPem = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(serverless);
        ArgumentNullException.ThrowIfNull(workItems);
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(auditTrail);
        if (string.IsNullOrWhiteSpace(clientPrivateKeyPem))
            throw new ArgumentException("Client private key is required.", nameof(clientPrivateKeyPem));

        var resultPrivateKeyPem = string.IsNullOrWhiteSpace(clientResultPrivateKeyPem)
            ? clientPrivateKeyPem
            : clientResultPrivateKeyPem;

        var publisher = new ResearchDispatchBindingPublisher(bindings, clientPrivateKeyPem);
        var envelopeCommitment = new DurableResearchEnvelopeCommitment(store);
        var atomicReservation = new AtomicRemoteResearchDispatchReservation(store, auditTrail);
        var ingestor = new RemoteResearchResultIngestor(store, results, resultPrivateKeyPem, auditTrail, workItems);
        var dispatcher = new TwoPhaseNebiusResearchDispatcher(
            serverless,
            workItems,
            options,
            bindingPublisher: publisher,
            envelopeCommitment: envelopeCommitment,
            atomicReservation: atomicReservation);

        // Binding publication is centralized in the V2-aware dispatcher/recovery layer. The
        // reconciler deliberately receives no legacy publisher so it cannot create an unbound V1
        // binding on a production recovery path.
        var reconciler = new NebiusResearchLifecycleReconciler(store, serverless, ingestor, auditTrail);
        var recovery = new ResearchDispatchBindingRecovery(store, publisher);
        var cleanup = new ResearchDispatchBindingCleanup(bindings);
        return new NebiusResearchClientRuntime(dispatcher, reconciler, ingestor, publisher, recovery, cleanup);
    }

    public Task<AgentJobRecord> DispatchAsync(
        RemoteResearchWorkItem workItem,
        ResearchCloudAuthorization authorization,
        CancellationToken cancellationToken = default) =>
        Dispatcher.DispatchWithReservationAsync(workItem, authorization, Ingestor, cancellationToken);

    public async Task<AgentJobRecord> ReconcileReservedAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var attached = await Reconciler.ReconcileReservedAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (attached.ExecutionLocation == JobExecutionLocation.NebiusServerless
            && attached.RemoteResearch is { State: RemoteResearchProvenanceState.Dispatched })
        {
            await _bindingRecovery.EnsureAsync(jobId, cancellationToken).ConfigureAwait(false);
        }
        return attached;
    }

    public async Task<AgentJobRecord> ReconcileDispatchedAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var recovered = await TryRecoverAppliedResultAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (recovered is not null)
            return recovered;

        await _bindingRecovery.EnsureAsync(jobId, cancellationToken).ConfigureAwait(false);

        var result = await Reconciler.ReconcileDispatchedAsync(jobId, now, cancellationToken).ConfigureAwait(false);
        await _bindingCleanup.TryCleanupIfTerminalAsync(result).ConfigureAwait(false);
        return result;
    }

    public async Task<AgentJobRecord> RequestCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        await Ingestor.RecoverPendingAuditAsync(jobId, cancellationToken).ConfigureAwait(false);
        await _bindingRecovery.EnsureAsync(jobId, cancellationToken).ConfigureAwait(false);
        return await Reconciler.RequestCancellationAsync(jobId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AgentJobRecord> ReconcileCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var recovered = await TryRecoverAppliedResultAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (recovered is not null)
            return recovered;

        await _bindingRecovery.EnsureAsync(jobId, cancellationToken).ConfigureAwait(false);

        var result = await Reconciler.ReconcileCancellationAsync(jobId, cancellationToken).ConfigureAwait(false);
        await _bindingCleanup.TryCleanupIfTerminalAsync(result).ConfigureAwait(false);
        return result;
    }

    public async Task<AgentJobRecord> RecoverPendingAuditAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var result = await Ingestor.RecoverPendingAuditAsync(jobId, cancellationToken).ConfigureAwait(false);
        await _bindingCleanup.TryCleanupIfTerminalAsync(result).ConfigureAwait(false);
        return result;
    }

    public async Task<AgentJobRecord> IngestAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Ingestor.IngestAsync(jobId, now, cancellationToken).ConfigureAwait(false);
        await _bindingCleanup.TryCleanupIfTerminalAsync(result).ConfigureAwait(false);
        return result;
    }

    private async Task<AgentJobRecord?> TryRecoverAppliedResultAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var recovered = await Ingestor.RecoverPendingAuditAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (recovered.ExecutionLocation != JobExecutionLocation.Local
            || recovered.RemoteResearch is not { State: RemoteResearchProvenanceState.ResultApplied })
        {
            return null;
        }

        await _bindingCleanup.TryCleanupIfTerminalAsync(recovered).ConfigureAwait(false);
        return recovered;
    }
}
