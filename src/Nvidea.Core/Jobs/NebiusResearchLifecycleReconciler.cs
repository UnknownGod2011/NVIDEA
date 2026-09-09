using System.Text.Json;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

public enum NebiusRemoteJobState
{
    Unknown,
    Pending,
    Running,
    Cancelling,
    Completed,
    Failed,
    Cancelled
}

public sealed record NebiusRemoteJobSnapshot(
    string Id,
    string Name,
    NebiusRemoteJobState State);

public static class NebiusServerlessJobSnapshotParser
{
    public static IReadOnlyList<NebiusRemoteJobSnapshot> ParseList(NebiusServerlessResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (string.IsNullOrWhiteSpace(response.RawJson))
            return Array.Empty<NebiusRemoteJobSnapshot>();

        using var document = JsonDocument.Parse(response.RawJson);
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Nebius job list response is missing the documented items array.");

        var result = new List<NebiusRemoteJobSnapshot>();
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
                continue;

            var id = TryReadString(metadata, "id");
            var name = TryReadString(metadata, "name");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                continue;

            var rawState = item.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Object
                ? TryReadString(status, "state")
                : null;
            result.Add(new NebiusRemoteJobSnapshot(id, name, ParseState(rawState)));
        }

        return result;
    }

    public static string? TryGetNextPageToken(NebiusServerlessResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (string.IsNullOrWhiteSpace(response.RawJson))
            return null;

        using var document = JsonDocument.Parse(response.RawJson);
        return TryReadString(document.RootElement, "nextPageToken");
    }

    public static NebiusRemoteJobSnapshot ParseGet(NebiusServerlessResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (string.IsNullOrWhiteSpace(response.RawJson))
            throw new InvalidOperationException("Nebius job response was empty.");

        using var document = JsonDocument.Parse(response.RawJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Nebius job response is missing metadata.");

        var id = TryReadString(metadata, "id");
        var name = TryReadString(metadata, "name");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Nebius job response is missing resource id or name.");

        var rawState = root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Object
            ? TryReadString(status, "state")
            : null;
        return new NebiusRemoteJobSnapshot(id, name, ParseState(rawState));
    }

    public static NebiusRemoteJobState ParseState(string? state) => state?.Trim().ToUpperInvariant() switch
    {
        "PROVISIONING" or "IMAGE_PULLING" or "STARTING" => NebiusRemoteJobState.Pending,
        "RUNNING" => NebiusRemoteJobState.Running,
        "CANCELLING" or "DELETING" => NebiusRemoteJobState.Cancelling,
        "COMPLETED" => NebiusRemoteJobState.Completed,
        "FAILED" or "ERROR" => NebiusRemoteJobState.Failed,
        "CANCELLED" => NebiusRemoteJobState.Cancelled,
        _ => NebiusRemoteJobState.Unknown
    };

    private static string? TryReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

/// <summary>
/// Conservative lifecycle controller for remote research. It recovers crash-window reservations,
/// reconciles explicit cancellation, and turns provider terminal states into CAS-protected local
/// terminal states without treating a temporarily missing encrypted result as failure before its
/// authenticated transport lifetime has elapsed. When a dispatch-binding publisher is configured,
/// reconciliation also idempotently guarantees that workers can resolve the authoritative Nebius id.
/// </summary>
public sealed class NebiusResearchLifecycleReconciler
{
    private readonly JsonAgentJobStore _store;
    private readonly INebiusServerlessJobClient _serverless;
    private readonly RemoteResearchResultIngestor _ingestor;
    private readonly IAuditTrail _auditTrail;
    private readonly ResearchDispatchBindingPublisher? _bindingPublisher;

    public NebiusResearchLifecycleReconciler(
        JsonAgentJobStore store,
        INebiusServerlessJobClient serverless,
        RemoteResearchResultIngestor ingestor,
        IAuditTrail auditTrail,
        ResearchDispatchBindingPublisher? bindingPublisher = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _serverless = serverless ?? throw new ArgumentNullException(nameof(serverless));
        _ingestor = ingestor ?? throw new ArgumentNullException(nameof(ingestor));
        _auditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
        _bindingPublisher = bindingPublisher;
    }

    public async Task<AgentJobRecord> ReconcileReservedAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var provenance = current.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote dispatch provenance.");
        if (current.State != AgentJobState.Running
            || current.ExecutionLocation != JobExecutionLocation.Local
            || provenance.State != RemoteResearchProvenanceState.DispatchReserved
            || !string.IsNullOrWhiteSpace(provenance.RemoteJobId))
            throw new InvalidOperationException("Only an unresolved DispatchReserved research stage can be reconciled.");

        var expectedName = GetDeterministicRemoteJobName(provenance.OpaqueWorkItemId);
        var jobs = await NebiusBoundedJobListReader.ReadAllAsync(_serverless, cancellationToken: cancellationToken).ConfigureAwait(false);
        var matches = jobs.Where(job => string.Equals(job.Name, expectedName, StringComparison.Ordinal)).ToArray();

        if (matches.Length == 0)
            throw new InvalidOperationException("No Nebius job exactly matches the reserved deterministic research job name; reservation remains unchanged.");
        if (matches.Length > 1)
            throw new InvalidOperationException("Multiple Nebius jobs match the reserved deterministic research job name; refusing ambiguous attachment.");

        var listMatch = matches[0];
        if (listMatch.State == NebiusRemoteJobState.Unknown)
            throw new InvalidOperationException("Matching Nebius job has an unknown lifecycle state; refusing attachment.");

        var verified = await GetVerifiedRemoteAsync(provenance, listMatch.Id, cancellationToken).ConfigureAwait(false);
        var attached = await _ingestor.AttachDispatchAsync(
            new NebiusResearchDispatchReceipt(
                current.JobId,
                provenance.InputCheckpointStep,
                provenance.OpaqueWorkItemId,
                verified.Id,
                provenance.DispatchedAt),
            cancellationToken).ConfigureAwait(false);
        await EnsureDispatchBindingAsync(attached, cancellationToken).ConfigureAwait(false);
        return attached;
    }

    /// <summary>
    /// Reconciles an attached remote stage. RUNNING/PENDING remain nonterminal. FAILED/ERROR and
    /// unexpected provider cancellation become durable local terminal states. COMPLETED attempts
    /// protected result ingestion; a missing result remains retryable until the persisted encrypted
    /// work-item lifetime expires, after which it becomes a truthful terminal failure.
    /// </summary>
    public async Task<AgentJobRecord> ReconcileDispatchedAsync(
        Guid jobId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var provenance = current.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote execution provenance.");
        if (current.State != AgentJobState.Running
            || current.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State != RemoteResearchProvenanceState.Dispatched
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
            throw new InvalidOperationException("Only an actively dispatched Nebius research stage can be reconciled.");

        await EnsureDispatchBindingAsync(current, cancellationToken).ConfigureAwait(false);

        var currentTime = now ?? DateTimeOffset.UtcNow;
        var remote = await GetVerifiedRemoteAsync(provenance, provenance.RemoteJobId, cancellationToken).ConfigureAwait(false);

        switch (remote.State)
        {
            case NebiusRemoteJobState.Pending:
            case NebiusRemoteJobState.Running:
            case NebiusRemoteJobState.Cancelling:
                return current;

            case NebiusRemoteJobState.Failed:
                return await FinalizeTerminalAsync(
                    current,
                    provenance,
                    AgentJobState.Failed,
                    RemoteResearchProvenanceState.RemoteFailed,
                    "Nebius remote research stage failed.",
                    "research.remote_failed",
                    "Nebius reported a terminal failure for the remote research stage.",
                    currentTime,
                    cancellationToken).ConfigureAwait(false);

            case NebiusRemoteJobState.Cancelled:
                return await FinalizeTerminalAsync(
                    current,
                    provenance,
                    AgentJobState.Cancelled,
                    RemoteResearchProvenanceState.Cancelled,
                    lastError: null,
                    "research.remote_cancelled",
                    "Nebius reported the remote research stage as cancelled.",
                    currentTime,
                    cancellationToken).ConfigureAwait(false);

            case NebiusRemoteJobState.Completed:
                try
                {
                    return await _ingestor.IngestAsync(jobId, currentTime, cancellationToken).ConfigureAwait(false);
                }
                catch (RemoteResearchResultNotAvailableException)
                {
                    var expiresAt = provenance.WorkItemExpiresAt
                        ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
                    if (currentTime < expiresAt)
                        return current;

                    return await FinalizeTerminalAsync(
                        current,
                        provenance,
                        AgentJobState.Failed,
                        RemoteResearchProvenanceState.Expired,
                        "Nebius completed the research stage, but its protected result was unavailable before the durable transport lifetime expired.",
                        "research.remote_result_expired",
                        "Nebius completed remote research but no protected result was available before expiry.",
                        currentTime,
                        cancellationToken).ConfigureAwait(false);
                }

            default:
                throw new InvalidOperationException("Nebius remote research lifecycle is unknown; refusing to mutate durable state.");
        }
    }

    public async Task<AgentJobRecord> RequestCancellationAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var provenance = current.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote execution provenance.");
        if (current.State != AgentJobState.Running
            || current.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State != RemoteResearchProvenanceState.Dispatched
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
            throw new InvalidOperationException("Only an actively dispatched Nebius research stage can request remote cancellation.");

        var replacement = current with
        {
            RemoteResearch = provenance with { State = RemoteResearchProvenanceState.CancelRequested },
            UpdatedAt = DateTimeOffset.UtcNow
        };
        if (!await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while cancellation was being reserved.");

        await AppendAuditAsync(replacement, "research.remote_cancel_requested", "Nebius research cancellation was durably requested before contacting the control plane.", cancellationToken).ConfigureAwait(false);

        try
        {
            await _serverless.CancelAsync(provenance.RemoteJobId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            throw;
        }

        return replacement;
    }

    public async Task<AgentJobRecord> ReconcileCancellationAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var current = await GetRequiredResearchAsync(jobId, cancellationToken).ConfigureAwait(false);
        var provenance = current.RemoteResearch
            ?? throw new InvalidOperationException("Research job has no remote execution provenance.");
        if (current.State != AgentJobState.Running
            || current.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State != RemoteResearchProvenanceState.CancelRequested
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
            throw new InvalidOperationException("Only a durable CancelRequested research stage can reconcile cancellation.");

        var remote = await GetVerifiedRemoteAsync(provenance, provenance.RemoteJobId, cancellationToken).ConfigureAwait(false);
        if (remote.State == NebiusRemoteJobState.Unknown)
            throw new InvalidOperationException("Nebius cancellation status is unknown; durable cancellation remains pending.");
        if (remote.State != NebiusRemoteJobState.Cancelled)
            return current;

        return await FinalizeTerminalAsync(
            current,
            provenance,
            AgentJobState.Cancelled,
            RemoteResearchProvenanceState.Cancelled,
            lastError: null,
            "research.remote_cancelled",
            "Nebius confirmed remote research cancellation.",
            DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);
    }

    public static string GetDeterministicRemoteJobName(string opaqueWorkItemId) =>
        ResearchDispatchBindingProtector.GetDeterministicRemoteJobName(opaqueWorkItemId);

    private async Task EnsureDispatchBindingAsync(AgentJobRecord job, CancellationToken cancellationToken)
    {
        if (_bindingPublisher is null)
            return;

        var provenance = job.RemoteResearch
            ?? throw new InvalidOperationException("Remote research job is missing provenance required for binding publication.");
        if (job.ExecutionLocation != JobExecutionLocation.NebiusServerless
            || provenance.State != RemoteResearchProvenanceState.Dispatched
            || string.IsNullOrWhiteSpace(provenance.RemoteJobId))
        {
            throw new InvalidOperationException("Authoritative dispatch binding can only be published for a durably attached remote stage.");
        }

        var expiresAt = provenance.WorkItemExpiresAt
            ?? provenance.DispatchedAt + ResearchWorkItemProtector.MaxLifetime;
        await _bindingPublisher.PublishAsync(
            provenance.OpaqueWorkItemId,
            provenance.RemoteJobId,
            expiresAt,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<NebiusRemoteJobSnapshot> GetVerifiedRemoteAsync(
        RemoteResearchProvenance provenance,
        string remoteJobId,
        CancellationToken cancellationToken)
    {
        var remote = NebiusServerlessJobSnapshotParser.ParseGet(
            await _serverless.GetAsync(remoteJobId, cancellationToken).ConfigureAwait(false));
        var expectedName = GetDeterministicRemoteJobName(provenance.OpaqueWorkItemId);
        if (!string.Equals(remote.Id, remoteJobId, StringComparison.Ordinal)
            || !string.Equals(remote.Name, expectedName, StringComparison.Ordinal))
            throw new InvalidOperationException("Nebius direct job verification returned substituted remote provenance.");
        if (remote.State == NebiusRemoteJobState.Unknown)
            throw new InvalidOperationException("Verified Nebius job has an unknown lifecycle state; refusing durable mutation.");
        return remote;
    }

    private async Task<AgentJobRecord> FinalizeTerminalAsync(
        AgentJobRecord current,
        RemoteResearchProvenance provenance,
        AgentJobState localState,
        RemoteResearchProvenanceState provenanceState,
        string? lastError,
        string eventType,
        string summary,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var replacement = current with
        {
            State = localState,
            ExecutionLocation = JobExecutionLocation.Local,
            LastError = lastError,
            NextAttemptAt = null,
            RemoteResearch = provenance with { State = provenanceState, TerminalAt = now },
            UpdatedAt = now
        };
        if (!await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while remote terminal state was being finalized.");

        await AppendAuditAsync(replacement, eventType, summary, cancellationToken).ConfigureAwait(false);
        await _ingestor.CleanupProtectedPayloadsAsync(provenance.OpaqueWorkItemId).ConfigureAwait(false);
        return replacement;
    }

    private async Task<AgentJobRecord> GetRequiredResearchAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Research job id is required.", nameof(jobId));
        var job = await _store.GetAsync(jobId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Research job '{jobId}' was not found.");
        if (!string.Equals(job.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Nebius research lifecycle reconciliation only accepts research jobs.");
        return job;
    }

    private Task AppendAuditAsync(AgentJobRecord job, string eventType, string summary, CancellationToken cancellationToken) =>
        _auditTrail.AppendAsync(
            new AuditEvent(
                Guid.NewGuid(), DateTimeOffset.UtcNow, job.Definition.CapabilityId, job.JobId.ToString("N"), eventType,
                job.Definition.Risk, true, false, string.Empty, summary,
                new Dictionary<string, string>
                {
                    ["jobType"] = job.Definition.JobType,
                    ["state"] = job.State.ToString(),
                    ["executionLocation"] = job.ExecutionLocation.ToString(),
                    ["attempt"] = job.Attempt.ToString()
                }),
            cancellationToken);
}