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
/// Client-only composition boundary for Nebius remote research. It deliberately constructs the
/// dispatcher and lifecycle reconciler from one ResearchDispatchBindingPublisher instance so normal
/// dispatch, crash recovery, and repeated reconciliation cannot accidentally use different signing
/// identities. Client private keys never cross this boundary into Serverless job configuration.
///
/// The optional result-envelope private key preserves compatibility for lower-level fixtures. Live
/// production composition always supplies a distinct key so RSA-PSS dispatch signing and OAEP-SHA256
/// result decryption do not share one cryptographic identity.
///
/// Serverless execution should be exposed to product UX only after this runtime is backed by a live,
/// authenticated shared transport and the narrow Nebius contract probe succeeds.
/// </summary>
public sealed class NebiusResearchClientRuntime : IRemoteResearchClientRuntime
{
    private readonly ResearchDispatchBindingCleanup _bindingCleanup;

    private NebiusResearchClientRuntime(
        TwoPhaseNebiusResearchDispatcher dispatcher,
        NebiusResearchLifecycleReconciler reconciler,
        RemoteResearchResultIngestor ingestor,
        ResearchDispatchBindingPublisher bindingPublisher,
        ResearchDispatchBindingCleanup bindingCleanup)
    {
        Dispatcher = dispatcher;
        Reconciler = reconciler;
        Ingestor = ingestor;
        BindingPublisher = bindingPublisher;
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
        var ingestor = new RemoteResearchResultIngestor(store, results, resultPrivateKeyPem, auditTrail, workItems);
        var dispatcher = new TwoPhaseNebiusResearchDispatcher(serverless, workItems, options, publisher);
        var reconciler = new NebiusResearchLifecycleReconciler(store, serverless, ingestor, auditTrail, publisher);
        var cleanup = new ResearchDispatchBindingCleanup(bindings);
        return new NebiusResearchClientRuntime(dispatcher, reconciler, ingestor, publisher, cleanup);
    }

    public Task<AgentJobRecord> DispatchAsync(
        RemoteResearchWorkItem workItem,
        ResearchCloudAuthorization authorization,
        CancellationToken cancellationToken = default) =>
        Dispatcher.DispatchWithReservationAsync(workItem, authorization, Ingestor, cancellationToken);

    public Task<AgentJobRecord> ReconcileReservedAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        Reconciler.ReconcileReservedAsync(jobId, cancellationToken);

    public async Task<AgentJobRecord> ReconcileDispatchedAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Reconciler.ReconcileDispatchedAsync(jobId, now, cancellationToken).ConfigureAwait(false);
        await _bindingCleanup.TryCleanupIfTerminalAsync(result).ConfigureAwait(false);
        return result;
    }

    public Task<AgentJobRecord> RequestCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        Reconciler.RequestCancellationAsync(jobId, cancellationToken);

    public async Task<AgentJobRecord> ReconcileCancellationAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var result = await Reconciler.ReconcileCancellationAsync(jobId, cancellationToken).ConfigureAwait(false);
        await _bindingCleanup.TryCleanupIfTerminalAsync(result).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Direct exact-once ingestion entry point for callers that already have authoritative provider
    /// completion evidence. The signed binding is cleaned only after IngestAsync has durably applied
    /// the protected result and returned a local ResultApplied state.
    /// </summary>
    public async Task<AgentJobRecord> IngestAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Ingestor.IngestAsync(jobId, now, cancellationToken).ConfigureAwait(false);
        await _bindingCleanup.TryCleanupIfTerminalAsync(result).ConfigureAwait(false);
        return result;
    }
}
