namespace Nvidea.Core.Jobs;

public sealed record PreparedNebiusResearchDispatch(
    Guid LocalJobId,
    string CheckpointStep,
    string OpaqueWorkItemId,
    DateTimeOffset PreparedAt,
    ProtectedResearchWorkItemEnvelope Envelope);

public sealed class TwoPhaseNebiusResearchDispatcher
{
    private readonly INebiusServerlessJobClient _serverless;
    private readonly IProtectedResearchWorkItemTransport _transport;
    private readonly NebiusResearchDispatchOptions _options;
    private readonly ResearchDispatchBindingPublisher? _bindingPublisher;
    private readonly DurableResearchEnvelopeCommitment? _envelopeCommitment;
    private readonly AtomicRemoteResearchDispatchReservation? _atomicReservation;

    public TwoPhaseNebiusResearchDispatcher(
        INebiusServerlessJobClient serverless,
        IProtectedResearchWorkItemTransport transport,
        NebiusResearchDispatchOptions options,
        ResearchDispatchBindingPublisher? bindingPublisher = null,
        DurableResearchEnvelopeCommitment? envelopeCommitment = null,
        AtomicRemoteResearchDispatchReservation? atomicReservation = null)
    {
        _serverless = serverless ?? throw new ArgumentNullException(nameof(serverless));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _bindingPublisher = bindingPublisher;
        _envelopeCommitment = envelopeCommitment;
        _atomicReservation = atomicReservation;
        ValidateOptions(options);
    }

    public async Task<PreparedNebiusResearchDispatch> PrepareAsync(RemoteResearchWorkItem workItem, ResearchCloudAuthorization authorization, CancellationToken cancellationToken = default)
    {
        ResearchWorkItemProtector.ValidateAuthorization(authorization, workItem);
        var envelope = ResearchWorkItemProtector.Protect(workItem, _options.WorkerPublicKeyPem);
        await _transport.PutAsync(envelope, cancellationToken).ConfigureAwait(false);
        return new PreparedNebiusResearchDispatch(workItem.LocalJobId, workItem.CheckpointStep, envelope.OpaqueWorkItemId, DateTimeOffset.UtcNow, envelope);
    }

    public async Task<NebiusResearchDispatchReceipt> StartPreparedAsync(PreparedNebiusResearchDispatch prepared, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        if (prepared.LocalJobId == Guid.Empty || string.IsNullOrWhiteSpace(prepared.CheckpointStep) || string.IsNullOrWhiteSpace(prepared.OpaqueWorkItemId)
            || !string.Equals(prepared.OpaqueWorkItemId, prepared.Envelope.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(prepared.Envelope.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Prepared remote research dispatch metadata is invalid.");
        return await CreateRemoteReceiptAsync(prepared.LocalJobId, prepared.CheckpointStep, prepared.OpaqueWorkItemId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AgentJobRecord> ResumeReservedAsync(Guid localJobId, RemoteResearchResultIngestor ingestor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingestor);
        if (_atomicReservation is null)
            throw new InvalidOperationException("Restarted remote dispatch requires the production atomic reservation coordinator.");
        var recovered = await _atomicReservation.RecoverAsync(localJobId, cancellationToken).ConfigureAwait(false);
        var receipt = await StartAtomicReservationAsync(recovered, cancellationToken).ConfigureAwait(false);
        var attached = await ingestor.AttachDispatchAsync(receipt, cancellationToken).ConfigureAwait(false);
        await PublishBindingIfConfiguredAsync(attached, cancellationToken).ConfigureAwait(false);
        return attached;
    }

    public async Task<AgentJobRecord> DispatchWithReservationAsync(RemoteResearchWorkItem workItem, ResearchCloudAuthorization authorization, RemoteResearchResultIngestor ingestor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingestor);
        ResearchWorkItemProtector.ValidateAuthorization(authorization, workItem);
        await ingestor.PreflightDispatchReservationAsync(workItem.LocalJobId, workItem.CheckpointStep, cancellationToken).ConfigureAwait(false);
        var prepared = await PrepareAsync(workItem, authorization, cancellationToken).ConfigureAwait(false);
        AgentJobRecord reserved;
        try
        {
            var reservation = new RemoteResearchDispatchReservation(prepared.LocalJobId, prepared.CheckpointStep, prepared.OpaqueWorkItemId, prepared.PreparedAt, prepared.Envelope.ExpiresAt);
            if (_atomicReservation is not null)
                reserved = await _atomicReservation.ReserveAsync(reservation, prepared.Envelope, cancellationToken).ConfigureAwait(false);
            else
            {
                reserved = await ingestor.ReserveDispatchAsync(reservation, cancellationToken).ConfigureAwait(false);
                if (_envelopeCommitment is not null)
                    reserved = await _envelopeCommitment.AttachAsync(reserved.JobId, prepared.Envelope, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (RemoteResearchDispatchReservationAuditPendingException) { throw; }
        catch
        {
            await TryDeleteAsync(prepared.OpaqueWorkItemId).ConfigureAwait(false);
            throw;
        }

        // Production atomic dispatch re-reads its durable authority immediately before Create just as
        // restart recovery does. Legacy/lower-level composition retains StartPreparedAsync behavior.
        var receipt = _atomicReservation is not null
            ? await StartAtomicReservationAsync(reserved, cancellationToken).ConfigureAwait(false)
            : await StartPreparedAsync(prepared, cancellationToken).ConfigureAwait(false);
        var attached = await ingestor.AttachDispatchAsync(receipt, cancellationToken).ConfigureAwait(false);
        await PublishBindingIfConfiguredAsync(attached, cancellationToken).ConfigureAwait(false);
        return attached;
    }

    public string GetDeterministicRemoteJobName(string opaqueWorkItemId) => ResearchDispatchBindingProtector.GetDeterministicRemoteJobName(opaqueWorkItemId);

    private async Task<NebiusResearchDispatchReceipt> StartAtomicReservationAsync(AgentJobRecord recovered, CancellationToken cancellationToken)
    {
        if (_atomicReservation is null)
            throw new InvalidOperationException("Atomic provider creation requires the production atomic reservation coordinator.");
        var current = await _atomicReservation.RevalidateCreateAuthorityAsync(recovered, cancellationToken).ConfigureAwait(false);
        var trust = ValidateProviderCreateAuthority(current);
        return await CreateRemoteReceiptAsync(current.JobId, trust.Provenance.InputCheckpointStep, trust.Provenance.OpaqueWorkItemId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<NebiusResearchDispatchReceipt> CreateRemoteReceiptAsync(Guid localJobId, string checkpointStep, string opaqueWorkItemId, CancellationToken cancellationToken)
    {
        var response = await _serverless.CreateAsync(BuildSpec(opaqueWorkItemId), cancellationToken).ConfigureAwait(false);
        var remoteJobId = response.TryGetResourceId();
        if (string.IsNullOrWhiteSpace(remoteJobId))
            throw new InvalidOperationException("Nebius accepted the Serverless create request but did not expose a job resource id. The durable DispatchReserved state and encrypted work item must be retained for reconciliation.");
        return new NebiusResearchDispatchReceipt(localJobId, checkpointStep, opaqueWorkItemId, remoteJobId, DateTimeOffset.UtcNow);
    }

    private static RemoteResearchReservationTrust ValidateProviderCreateAuthority(AgentJobRecord recovered)
    {
        RemoteResearchReservationTrustValidator.RequireAuditSettled(recovered);
        return RemoteResearchReservationTrustValidator.ValidateReserved(recovered, DateTimeOffset.UtcNow, requireUnexpired: true);
    }

    private async Task PublishBindingIfConfiguredAsync(AgentJobRecord attached, CancellationToken cancellationToken)
    {
        if (_bindingPublisher is null) return;
        var provenance = attached.RemoteResearch ?? throw new InvalidOperationException("Attached remote research job is missing provenance.");
        if (attached.ExecutionLocation != JobExecutionLocation.NebiusServerless || provenance.State != RemoteResearchProvenanceState.Dispatched || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
            throw new InvalidOperationException("Authoritative dispatch binding can only be published after durable remote-id attachment.");
        var expiresAt = provenance.WorkItemExpiresAt ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
        if (attached.RemoteWorkItemEnvelopeSha256 is { } commitment)
        {
            await _bindingPublisher.PublishEnvelopeBoundAsync(provenance.OpaqueWorkItemId, provenance.RemoteJobId, commitment, expiresAt, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }
        await _bindingPublisher.PublishAsync(provenance.OpaqueWorkItemId, provenance.RemoteJobId, expiresAt, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private NebiusServerlessJobSpec BuildSpec(string opaqueWorkItemId)
    {
        var environment = new Dictionary<string, string>(StringComparer.Ordinal) { ["NVIDEA_RESEARCH_PROTOCOL"] = ResearchWorkItemProtector.ProtocolVersion };
        if (_options.EnvironmentVariables is not null)
            foreach (var pair in _options.EnvironmentVariables) environment.Add(pair.Key, pair.Value);
        return new NebiusServerlessJobSpec(Name: GetDeterministicRemoteJobName(opaqueWorkItemId), Image: _options.WorkerImage, ContainerCommand: _options.ContainerCommand,
            Arguments: $"Nvidea.Worker.dll research --work-item-id={opaqueWorkItemId}", Platform: _options.Platform, Preset: _options.Preset, Timeout: _options.Timeout,
            SubnetId: _options.SubnetId, EnvironmentVariables: environment, Disk: _options.Disk, SecretEnvironmentVariables: _options.SecretEnvironmentVariables, Volumes: _options.Volumes);
    }

    private async Task TryDeleteAsync(string opaqueWorkItemId)
    {
        try { await _transport.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false); } catch { }
    }

    private static void ValidateOptions(NebiusResearchDispatchOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WorkerImage) || string.IsNullOrWhiteSpace(options.WorkerPublicKeyPem) || string.IsNullOrWhiteSpace(options.ContainerCommand)
            || string.IsNullOrWhiteSpace(options.Platform) || string.IsNullOrWhiteSpace(options.Preset) || string.IsNullOrWhiteSpace(options.Timeout) || string.IsNullOrWhiteSpace(options.SubnetId))
            throw new ArgumentException("Worker image/key, command, platform, preset, timeout and subnet are required.", nameof(options));
        if (options.Disk is null || string.IsNullOrWhiteSpace(options.Disk.Type) || options.Disk.SizeBytes <= 0)
            throw new ArgumentException("An explicit positive-size Serverless disk is required.", nameof(options));
        if (options.EnvironmentVariables?.ContainsKey("NVIDEA_RESEARCH_PROTOCOL") == true)
            throw new ArgumentException("NVIDEA_RESEARCH_PROTOCOL is reserved by the dispatcher.", nameof(options));
    }
}
