namespace Nvidea.Core.Jobs;

/// <summary>
/// Encrypted preparation record for a single remote research stage. It intentionally contains no
/// plaintext research payload; only opaque/local identity plus the already-protected envelope.
/// </summary>
public sealed record PreparedNebiusResearchDispatch(
    Guid LocalJobId,
    string CheckpointStep,
    string OpaqueWorkItemId,
    DateTimeOffset PreparedAt,
    ProtectedResearchWorkItemEnvelope Envelope);

/// <summary>
/// Coordinates the crash-sensitive Serverless dispatch boundary in two phases:
/// 1) encrypt/upload the work item and durably reserve its exact opaque id/checkpoint locally;
/// 2) only after that CAS succeeds, create the Nebius job and attach the returned remote id.
///
/// This closes the previous window where Nebius could accept a job before the client had any
/// durable provenance. A crash after reservation remains explicitly visible as DispatchReserved;
/// it is never silently replayed. A crash after remote creation but before receipt attachment is
/// still ambiguous, but the durable opaque id and deterministic Nebius job name are retained for
/// later reconciliation rather than losing all provenance.
/// </summary>
public sealed class TwoPhaseNebiusResearchDispatcher
{
    private readonly INebiusServerlessJobClient _serverless;
    private readonly IProtectedResearchWorkItemTransport _transport;
    private readonly NebiusResearchDispatchOptions _options;

    public TwoPhaseNebiusResearchDispatcher(
        INebiusServerlessJobClient serverless,
        IProtectedResearchWorkItemTransport transport,
        NebiusResearchDispatchOptions options)
    {
        _serverless = serverless ?? throw new ArgumentNullException(nameof(serverless));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ValidateOptions(options);
    }

    public async Task<PreparedNebiusResearchDispatch> PrepareAsync(
        RemoteResearchWorkItem workItem,
        ResearchCloudAuthorization authorization,
        CancellationToken cancellationToken = default)
    {
        ResearchWorkItemProtector.ValidateAuthorization(authorization, workItem);
        var envelope = ResearchWorkItemProtector.Protect(workItem, _options.WorkerPublicKeyPem);
        await _transport.PutAsync(envelope, cancellationToken).ConfigureAwait(false);

        return new PreparedNebiusResearchDispatch(
            workItem.LocalJobId,
            workItem.CheckpointStep,
            envelope.OpaqueWorkItemId,
            DateTimeOffset.UtcNow,
            envelope);
    }

    public async Task<NebiusResearchDispatchReceipt> StartPreparedAsync(
        PreparedNebiusResearchDispatch prepared,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        if (prepared.LocalJobId == Guid.Empty
            || string.IsNullOrWhiteSpace(prepared.CheckpointStep)
            || string.IsNullOrWhiteSpace(prepared.OpaqueWorkItemId)
            || !string.Equals(prepared.OpaqueWorkItemId, prepared.Envelope.OpaqueWorkItemId, StringComparison.Ordinal)
            || !string.Equals(prepared.Envelope.ProtocolVersion, ResearchWorkItemProtector.ProtocolVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Prepared remote research dispatch metadata is invalid.");
        }

        try
        {
            var spec = BuildSpec(prepared.OpaqueWorkItemId);
            var response = await _serverless.CreateAsync(spec, cancellationToken).ConfigureAwait(false);
            var remoteJobId = response.TryGetResourceId();
            if (string.IsNullOrWhiteSpace(remoteJobId))
            {
                throw new InvalidOperationException(
                    "Nebius accepted the Serverless create request but did not expose a job resource id. " +
                    "The durable DispatchReserved state and encrypted work item must be retained for reconciliation.");
            }

            return new NebiusResearchDispatchReceipt(
                prepared.LocalJobId,
                prepared.CheckpointStep,
                prepared.OpaqueWorkItemId,
                remoteJobId,
                DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("did not expose a job resource id", StringComparison.Ordinal))
        {
            throw;
        }
        catch
        {
            // Do not delete the local reservation here: the caller may not be able to prove whether
            // the control plane accepted the request. The encrypted object is TTL-bounded and the
            // durable reservation prevents accidental local replay.
            throw;
        }
    }

    public async Task<AgentJobRecord> DispatchWithReservationAsync(
        RemoteResearchWorkItem workItem,
        ResearchCloudAuthorization authorization,
        RemoteResearchResultIngestor ingestor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingestor);

        var prepared = await PrepareAsync(workItem, authorization, cancellationToken).ConfigureAwait(false);
        try
        {
            await ingestor.ReserveDispatchAsync(
                new RemoteResearchDispatchReservation(
                    prepared.LocalJobId,
                    prepared.CheckpointStep,
                    prepared.OpaqueWorkItemId,
                    prepared.PreparedAt),
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await TryDeleteAsync(prepared.OpaqueWorkItemId).ConfigureAwait(false);
            throw;
        }

        var receipt = await StartPreparedAsync(prepared, cancellationToken).ConfigureAwait(false);
        return await ingestor.AttachDispatchAsync(receipt, cancellationToken).ConfigureAwait(false);
    }

    public string GetDeterministicRemoteJobName(string opaqueWorkItemId)
    {
        if (string.IsNullOrWhiteSpace(opaqueWorkItemId) || opaqueWorkItemId.Length < 12)
            throw new ArgumentException("A valid opaque work-item id is required.", nameof(opaqueWorkItemId));
        return $"nvidea-research-{opaqueWorkItemId[..12].ToLowerInvariant()}";
    }

    private NebiusServerlessJobSpec BuildSpec(string opaqueWorkItemId)
    {
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["NVIDEA_RESEARCH_PROTOCOL"] = ResearchWorkItemProtector.ProtocolVersion
        };
        if (_options.EnvironmentVariables is not null)
        {
            foreach (var pair in _options.EnvironmentVariables)
                environment.Add(pair.Key, pair.Value);
        }

        return new NebiusServerlessJobSpec(
            Name: GetDeterministicRemoteJobName(opaqueWorkItemId),
            Image: _options.WorkerImage,
            ContainerCommand: _options.ContainerCommand,
            Arguments: $"Nvidea.Worker.dll research --work-item-id={opaqueWorkItemId}",
            Platform: _options.Platform,
            Preset: _options.Preset,
            Timeout: _options.Timeout,
            SubnetId: _options.SubnetId,
            EnvironmentVariables: environment,
            Disk: _options.Disk,
            SecretEnvironmentVariables: _options.SecretEnvironmentVariables);
    }

    private async Task TryDeleteAsync(string opaqueWorkItemId)
    {
        try
        {
            await _transport.DeleteAsync(opaqueWorkItemId, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Preparation cleanup is best effort; encrypted payloads are bounded by protocol TTL.
        }
    }

    private static void ValidateOptions(NebiusResearchDispatchOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WorkerImage)
            || string.IsNullOrWhiteSpace(options.WorkerPublicKeyPem)
            || string.IsNullOrWhiteSpace(options.ContainerCommand)
            || string.IsNullOrWhiteSpace(options.Platform)
            || string.IsNullOrWhiteSpace(options.Preset)
            || string.IsNullOrWhiteSpace(options.Timeout)
            || string.IsNullOrWhiteSpace(options.SubnetId))
        {
            throw new ArgumentException("Worker image/key, command, platform, preset, timeout and subnet are required.", nameof(options));
        }
        if (options.Disk is null || string.IsNullOrWhiteSpace(options.Disk.Type) || options.Disk.SizeBytes <= 0)
            throw new ArgumentException("An explicit positive-size Serverless disk is required.", nameof(options));
        if (options.EnvironmentVariables?.ContainsKey("NVIDEA_RESEARCH_PROTOCOL") == true)
            throw new ArgumentException("NVIDEA_RESEARCH_PROTOCOL is reserved by the dispatcher.", nameof(options));
    }
}
