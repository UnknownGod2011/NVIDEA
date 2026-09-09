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

/// <summary>
/// Conservative parser for Nebius AI job resources. Unknown or malformed status values are
/// preserved as Unknown and must never be treated as proof that work started, completed, failed,
/// or was cancelled. The state mapping intentionally mirrors the current Nebius AI v1 JobStatus
/// enum rather than guessing provider states.
/// </summary>
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
        "PROVISIONING" or "STARTING" => NebiusRemoteJobState.Pending,
        "RUNNING" => NebiusRemoteJobState.Running,
        "CANCELLING" => NebiusRemoteJobState.Cancelling,
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
/// Reconciles the narrow crash window where a deterministic Nebius job may have been accepted
/// after local DispatchReserved persistence but before the remote id was attached. Reconciliation
/// never re-creates work: it only attaches one uniquely matching, recognized Nebius job resource
/// discovered across a bounded complete listing and verifies that resource again through a direct
/// GET. It also implements durable remote cancellation as a two-step CancelRequested -> Cancelled flow.
/// </summary>
public sealed class NebiusResearchLifecycleReconciler
{
    private readonly JsonAgentJobStore _store;
    private readonly INebiusServerlessJobClient _serverless;
    private readonly RemoteResearchResultIngestor _ingestor;
    private readonly IAuditTrail _auditTrail;

    public NebiusResearchLifecycleReconciler(
        JsonAgentJobStore store,
        INebiusServerlessJobClient serverless,
        RemoteResearchResultIngestor ingestor,
        IAuditTrail auditTrail)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _serverless = serverless ?? throw new ArgumentNullException(nameof(serverless));
        _ingestor = ingestor ?? throw new ArgumentNullException(nameof(ingestor));
        _auditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
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
        {
            throw new InvalidOperationException("Only an unresolved DispatchReserved research stage can be reconciled.");
        }

        var expectedName = GetDeterministicRemoteJobName(provenance.OpaqueWorkItemId);
        var jobs = await NebiusBoundedJobListReader.ReadAllAsync(_serverless, cancellationToken: cancellationToken).ConfigureAwait(false);
        var matches = jobs
            .Where(job => string.Equals(job.Name, expectedName, StringComparison.Ordinal))
            .ToArray();

        if (matches.Length == 0)
            throw new InvalidOperationException("No Nebius job exactly matches the reserved deterministic research job name; reservation remains unchanged.");
        if (matches.Length > 1)
            throw new InvalidOperationException("Multiple Nebius jobs match the reserved deterministic research job name; refusing ambiguous attachment.");

        var listMatch = matches[0];
        if (listMatch.State == NebiusRemoteJobState.Unknown)
            throw new InvalidOperationException("Matching Nebius job has an unknown lifecycle state; refusing attachment.");

        var verified = NebiusServerlessJobSnapshotParser.ParseGet(
            await _serverless.GetAsync(listMatch.Id, cancellationToken).ConfigureAwait(false));
        if (!string.Equals(verified.Id, listMatch.Id, StringComparison.Ordinal)
            || !string.Equals(verified.Name, expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Nebius direct job verification did not match the uniquely listed research resource.");
        }
        if (verified.State == NebiusRemoteJobState.Unknown)
            throw new InvalidOperationException("Verified Nebius job has an unknown lifecycle state; refusing attachment.");

        return await _ingestor.AttachDispatchAsync(
            new NebiusResearchDispatchReceipt(
                current.JobId,
                provenance.InputCheckpointStep,
                provenance.OpaqueWorkItemId,
                verified.Id,
                provenance.DispatchedAt),
            cancellationToken).ConfigureAwait(false);
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
        {
            throw new InvalidOperationException("Only an actively dispatched Nebius research stage can request remote cancellation.");
        }

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
            // Keep CancelRequested durable. A retry may safely re-issue the provider cancellation,
            // while local execution remains blocked until remote state is reconciled.
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
        {
            throw new InvalidOperationException("Only a durable CancelRequested research stage can reconcile cancellation.");
        }

        var remote = NebiusServerlessJobSnapshotParser.ParseGet(
            await _serverless.GetAsync(provenance.RemoteJobId, cancellationToken).ConfigureAwait(false));
        if (!string.Equals(remote.Id, provenance.RemoteJobId, StringComparison.Ordinal))
            throw new InvalidOperationException("Nebius cancellation status returned a substituted resource id.");
        if (remote.State == NebiusRemoteJobState.Unknown)
            throw new InvalidOperationException("Nebius cancellation status is unknown; durable cancellation remains pending.");
        if (remote.State != NebiusRemoteJobState.Cancelled)
            return current;

        var now = DateTimeOffset.UtcNow;
        var replacement = current with
        {
            State = AgentJobState.Cancelled,
            ExecutionLocation = JobExecutionLocation.Local,
            LastError = null,
            NextAttemptAt = null,
            RemoteResearch = provenance with { State = RemoteResearchProvenanceState.Cancelled },
            UpdatedAt = now
        };
        if (!await _store.CompareExchangeAsync(current, replacement, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Research state changed while remote cancellation was being finalized.");

        await AppendAuditAsync(replacement, "research.remote_cancelled", "Nebius confirmed remote research cancellation.", cancellationToken).ConfigureAwait(false);
        return replacement;
    }

    public static string GetDeterministicRemoteJobName(string opaqueWorkItemId)
    {
        if (string.IsNullOrWhiteSpace(opaqueWorkItemId) || opaqueWorkItemId.Length < 12)
            throw new ArgumentException("A valid opaque work-item id is required.", nameof(opaqueWorkItemId));
        return $"nvidea-research-{opaqueWorkItemId[..12].ToLowerInvariant()}";
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
