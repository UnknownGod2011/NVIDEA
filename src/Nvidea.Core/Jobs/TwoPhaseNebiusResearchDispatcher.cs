namespace Nvidea.Core.Jobs;

/// <summary>
/// Crash-consistent Nebius research dispatch. The encrypted work item is prepared first, then the
/// exact local checkpoint/opaque id is durably reserved, and only then may Nebius job creation run.
/// A reservation without a remote job id is deliberately ambiguous and must not be auto-replayed.
/// </summary>
public sealed class TwoPhaseNebiusResearchDispatcher
{
    private readonly INebiusServerlessJobClient _serverless;
    private readonly IProtectedResearchWorkItemTransport _transport;
    private readonly NebiusResearchDispatchOptions _options;
    private readonly ResearchDispatchBindingPublisher? _bindingPublisher;

    public TwoPhaseNebiusResearchDispatcher(
        INebiusServerlessJobClient serverless,
        IProtectedResearchWorkItemTransport transport,
        NebiusResearchDispatchOptions options,
        ResearchDispatchBindingPublisher? bindingPublisher = null)
    {
        _serverless = serverless ?? throw new ArgumentNullException(nameof(serverless));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _bindingPublisher = bindingPublisher;
        ResearchWorkItemProtector.ValidatePublicKeyPem(_options.WorkerPublicKeyPem);
    }

    public async Task<AgentJobRecord> DispatchWithReservationAsync(
        RemoteResearchWorkItem item,
        ResearchCloudAuthorization authorization,
        RemoteResearchResultIngestor ingestor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(ingestor);
        ValidateAuthorization(item, authorization);

        // This preflight validates immutable job/audit fields before ciphertext upload. The actual
        // reservation repeats validation against fresh state immediately before its CAS.
        await ingestor.PreflightDispatchReservationAsync(item.LocalJobId, item.InputCheckpointStep, cancellationToken)
            .ConfigureAwait(false);

        var prepared = await PrepareAsync(item, cancellationToken).ConfigureAwait(false);
        try
        {
            await ingestor.ReserveDispatchAsync(
                new RemoteResearchDispatchReservation(
                    item.LocalJobId,
                    item.InputCheckpointStep,
                    prepared.OpaqueWorkItemId,
                    prepared.PreparedAt,
                    prepared.ExpiresAt),
                cancellationToken).ConfigureAwait(false);
        }
        catch (RemoteResearchDispatchReservationAuditPendingException)
        {
            // The reservation CAS already owns this ciphertext. Deleting it here would leave a
            // recoverable durable reservation pointing at a missing protected work item.
            throw;
        }
        catch
        {
            await TryDeleteAsync(prepared.OpaqueWorkItemId).ConfigureAwait(false);
            throw;
        }

        NebiusResearchDispatchReceipt receipt;
        try
        {
            receipt = await StartPreparedAsync(prepared, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Intentionally retain both the durable reservation and encrypted work item. The provider
            // call may have succeeded even when the response was lost, so retrying CreateAsync could
            // duplicate expensive work.
            throw;
        }

        var attached = await ingestor.AttachDispatchAsync(receipt, cancellationToken).ConfigureAwait(false);
        await PublishBindingIfConfiguredAsync(attached, cancellationToken).ConfigureAwait(false);
        return attached;
    }

    public async Task<PreparedNebiusResearchDispatch> PrepareAsync(
        RemoteResearchWorkItem item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        var encrypted = ResearchWorkItemProtector.Protect(item, _options.WorkerPublicKeyPem);
        try
        {
            await _transport.PutAsync(encrypted, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await TryDeleteAsync(encrypted.OpaqueWorkItemId).ConfigureAwait(false);
            throw;
        }

        return new PreparedNebiusResearchDispatch(
            item.LocalJobId,
            item.InputCheckpointStep,
            encrypted.OpaqueWorkItemId,
            item.CreatedAt,
            item.ExpiresAt,
            DateTimeOffset.UtcNow);
    }

    public async Task<NebiusResearchDispatchReceipt> StartPreparedAsync(
        PreparedNebiusResearchDispatch prepared,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        var spec = CreateSpec(prepared.OpaqueWorkItemId);
        var response = await _serverless.CreateAsync(spec, cancellationToken).ConfigureAwait(false);
        var remoteJobId = NebiusServerlessJobClient.ParseCreatedJobId(response);
        if (string.IsNullOrWhiteSpace(remoteJobId))
            throw new InvalidOperationException("Nebius Serverless did not return a remote research job id.");

        return new NebiusResearchDispatchReceipt(
            prepared.LocalJobId,
            prepared.InputCheckpointStep,
            prepared.OpaqueWorkItemId,
            remoteJobId,
            DateTimeOffset.UtcNow);
    }

    private NebiusServerlessJobSpec CreateSpec(string opaqueWorkItemId)
    {
        if (string.IsNullOrWhiteSpace(opaqueWorkItemId))
            throw new InvalidOperationException("Opaque work-item id is required for Nebius dispatch.");

        var args = new[]
        {
            _options.WorkerDll,
            "--work-item",
            opaqueWorkItemId
        };
        return new NebiusServerlessJobSpec(
            _options.WorkerImage,
            _options.ContainerCommand,
            args,
            _options.EnvironmentVariables,
            _options.Platform,
            _options.Preset,
            _options.Timeout,
            _options.SubnetId,
            _options.Disk,
            _options.VolumeMounts,
            _options.SharedMemorySize);
    }

    private async Task PublishBindingIfConfiguredAsync(
        AgentJobRecord attached,
        CancellationToken cancellationToken)
    {
        if (_bindingPublisher is null)
            return;

        var provenance = attached.RemoteResearch
            ?? throw new InvalidOperationException("Attached research dispatch is missing remote provenance.");
        if (provenance.State != RemoteResearchProvenanceState.Dispatched
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
        {
            throw new InvalidOperationException("Signed dispatch binding can only be published after a durable remote job id is attached.");
        }

        await _bindingPublisher.PublishAsync(
            attached.JobId,
            provenance.InputCheckpointStep,
            provenance.OpaqueWorkItemId,
            provenance.RemoteJobId,
            provenance.DispatchedAt,
            cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateAuthorization(RemoteResearchWorkItem item, ResearchCloudAuthorization authorization)
    {
        if (!authorization.Approved)
            throw new InvalidOperationException("Cloud research dispatch requires explicit user authorization.");
        if (authorization.LocalJobId != item.LocalJobId
            || !string.Equals(authorization.CheckpointStep, item.InputCheckpointStep, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cloud research authorization does not match the exact local research checkpoint.");
        }
        if (!string.Equals(authorization.DisclosureVersion, ResearchWorkItemProtector.DisclosureVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Cloud research authorization disclosure version is unsupported.");
        if (authorization.GrantedAt > item.ExpiresAt)
            throw new InvalidOperationException("Cloud research authorization was granted after the work item expired.");
    }

    private async Task TryDeleteAsync(string opaqueWorkItemId)
    {
        try
        {
            await _transport.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Encrypted envelopes are self-expiring and contain no plaintext; cleanup is best effort.
        }
    }
}

public sealed record PreparedNebiusResearchDispatch(
    Guid LocalJobId,
    string InputCheckpointStep,
    string OpaqueWorkItemId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset PreparedAt);
