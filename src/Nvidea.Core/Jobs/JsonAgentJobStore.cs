using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Trusted durable job-state infrastructure. Product/plugin callers must not construct this
/// store directly because <see cref="SaveAsync"/> is intentionally a low-level persistence
/// primitive and does not enforce lifecycle, approval, or provider-authority invariants.
/// Construction is owned by Nvidea.Core composition/recovery paths; external code should use the
/// constrained product runtimes and coordinator contracts instead.
/// </summary>
public sealed class JsonAgentJobStore : IAgentJobStore
{
    private const string ProtectionPurpose = "agent-jobs-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _path;
    private readonly ILocalStateProtector? _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);

    internal JsonAgentJobStore(string path, ILocalStateProtector? protector = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Job store path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _protector = protector ?? (OperatingSystem.IsWindows() ? new WindowsDpapiLocalStateProtector() : null);
    }

    public async Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var all = await LoadAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(x => x.JobId == jobId);
    }

    public async Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.JobId == Guid.Empty)
            throw new ArgumentException("Jobs require a non-empty id.", nameof(record));

        record = RemoteResearchFailureProvenanceMigration.Migrate(record);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = records.FindIndex(x => x.JobId == record.JobId);
            if (index >= 0)
                records[index] = record;
            else
                records.Add(record);

            records.Sort(static (a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Atomically replaces one job only while its durable identity/version still matches the
    /// caller's previously-read record. This is intentionally stronger than a blind SaveAsync
    /// and is used when ingesting remote results so stale or duplicate cloud work cannot overwrite
    /// a newer local checkpoint.
    /// </summary>
    public async Task<bool> CompareExchangeAsync(
        AgentJobRecord expected,
        AgentJobRecord replacement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(replacement);
        if (expected.JobId == Guid.Empty || replacement.JobId == Guid.Empty || expected.JobId != replacement.JobId)
            throw new ArgumentException("Compare-exchange requires the same non-empty job id.");

        replacement = RemoteResearchFailureProvenanceMigration.Migrate(replacement);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = records.FindIndex(x => x.JobId == expected.JobId);
            if (index < 0 || !VersionEquivalent(records[index], expected))
                return false;

            records[index] = replacement;
            records.Sort(static (a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<AgentJobRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<AgentJobRecord>> LoadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return Array.Empty<AgentJobRecord>();

        var persisted = await File.ReadAllBytesAsync(_path, cancellationToken).ConfigureAwait(false);
        LocalStatePayload payload;
        if (_protector is not null)
            payload = LocalStateEnvelope.Decode(persisted, _protector, ProtectionPurpose);
        else if (LocalStateEnvelope.HasProtectedHeader(persisted))
            throw new InvalidDataException("Job store is protected but no local-state protector was configured.");
        else
            payload = new LocalStatePayload(persisted, false);

        List<AgentJobRecord> records;
        try
        {
            records = JsonSerializer.Deserialize<List<AgentJobRecord>>(payload.Plaintext, JsonOptions) ?? new List<AgentJobRecord>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Job store contains invalid data.", ex);
        }

        var changed = _protector is not null && !payload.WasProtected;
        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var remoteMigrated = RemoteResearchFailureProvenanceMigration.Migrate(record);
            if (!ReferenceEquals(remoteMigrated, record))
            {
                records[i] = remoteMigrated;
                record = remoteMigrated;
                changed = true;
            }

            var browserMigrated = BrowserActionCheckpointMigrationService.MigrateRecord(record);
            if (browserMigrated.Status is BrowserActionCheckpointRecordMigrationStatus.Migrated
                or BrowserActionCheckpointRecordMigrationStatus.Quarantined)
            {
                records[i] = browserMigrated.Record;
                changed = true;
            }
        }

        if (changed)
        {
            records.Sort(static (a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
            await PersistUnlockedAsync(records, cancellationToken).ConfigureAwait(false);
        }

        return records;
    }

    private async Task PersistUnlockedAsync(
        IReadOnlyList<AgentJobRecord> records,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(records, JsonOptions);
        var persisted = _protector is null
            ? plaintext
            : LocalStateEnvelope.Encode(plaintext, _protector, ProtectionPurpose);

        var temp = _path + ".tmp";
        await File.WriteAllBytesAsync(temp, persisted, cancellationToken).ConfigureAwait(false);
        File.Move(temp, _path, true);
    }

    private static bool VersionEquivalent(AgentJobRecord actual, AgentJobRecord expected)
    {
        if (actual.JobId != expected.JobId
            || actual.State != expected.State
            || actual.ExecutionLocation != expected.ExecutionLocation
            || actual.Attempt != expected.Attempt
            || actual.UpdatedAt != expected.UpdatedAt
            || actual.NextAttemptAt != expected.NextAttemptAt
            || !string.Equals(actual.ApprovalScope, expected.ApprovalScope, StringComparison.Ordinal)
            || !string.Equals(actual.LastError, expected.LastError, StringComparison.Ordinal)
            || !string.Equals(actual.RemoteWorkItemEnvelopeSha256, expected.RemoteWorkItemEnvelopeSha256, StringComparison.Ordinal)
            || !DefinitionEquivalent(actual.Definition, expected.Definition)
            || !CheckpointVersionEquivalent(actual.Checkpoint, expected.Checkpoint)
            || !AuditEventEquivalent(actual.PendingAuditEvent, expected.PendingAuditEvent)
            || !ExternalActionEquivalent(actual.PendingExternalAction, expected.PendingExternalAction)
            || !ProtectedPayloadCleanupEquivalent(actual.PendingProtectedPayloadCleanup, expected.PendingProtectedPayloadCleanup))
        {
            return false;
        }

        var a = actual.RemoteResearch;
        var e = expected.RemoteResearch;
        if (ReferenceEquals(a, e)) return true;
        if (a is null || e is null) return false;
        return string.Equals(a.ProtocolVersion, e.ProtocolVersion, StringComparison.Ordinal)
            && string.Equals(a.OpaqueWorkItemId, e.OpaqueWorkItemId, StringComparison.Ordinal)
            && string.Equals(a.RemoteJobId, e.RemoteJobId, StringComparison.Ordinal)
            && string.Equals(a.InputCheckpointStep, e.InputCheckpointStep, StringComparison.Ordinal)
            && a.InputCheckpointSavedAt == e.InputCheckpointSavedAt
            && a.DispatchedAt == e.DispatchedAt
            && a.State == e.State
            && a.ResultAppliedAt == e.ResultAppliedAt
            && a.WorkItemExpiresAt == e.WorkItemExpiresAt
            && a.TerminalAt == e.TerminalAt
            && string.Equals(a.ProviderFailureCode, e.ProviderFailureCode, StringComparison.Ordinal);
    }

    internal static bool AuditEventEquivalent(AuditEvent? actual, AuditEvent? expected)
    {
        if (ReferenceEquals(actual, expected))
            return true;
        if (actual is null || expected is null)
            return false;
        if (actual.EventId != expected.EventId
            || actual.OccurredAt != expected.OccurredAt
            || !string.Equals(actual.CapabilityId, expected.CapabilityId, StringComparison.Ordinal)
            || !string.Equals(actual.ActionId, expected.ActionId, StringComparison.Ordinal)
            || !string.Equals(actual.EventType, expected.EventType, StringComparison.Ordinal)
            || actual.Risk != expected.Risk
            || actual.Allowed != expected.Allowed
            || actual.Approved != expected.Approved
            || !string.Equals(actual.ApprovalScope, expected.ApprovalScope, StringComparison.Ordinal)
            || !string.Equals(actual.Summary, expected.Summary, StringComparison.Ordinal))
        {
            return false;
        }

        if (ReferenceEquals(actual.Metadata, expected.Metadata))
            return true;
        if (actual.Metadata is null || expected.Metadata is null || actual.Metadata.Count != expected.Metadata.Count)
            return false;

        foreach (var pair in actual.Metadata)
        {
            if (!expected.Metadata.TryGetValue(pair.Key, out var expectedValue)
                || !string.Equals(pair.Value, expectedValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ExternalActionEquivalent(PendingExternalAction? actual, PendingExternalAction? expected)
    {
        if (ReferenceEquals(actual, expected))
            return true;
        if (actual is null || expected is null)
            return false;

        return actual.ActionId == expected.ActionId
            && actual.Kind == expected.Kind
            && string.Equals(actual.TargetId, expected.TargetId, StringComparison.Ordinal)
            && actual.AuditEventId == expected.AuditEventId
            && actual.CreatedAt == expected.CreatedAt;
    }

    internal static bool ProtectedPayloadCleanupEquivalent(
        PendingProtectedPayloadCleanup? actual,
        PendingProtectedPayloadCleanup? expected)
    {
        if (ReferenceEquals(actual, expected))
            return true;
        if (actual is null || expected is null)
            return false;

        return actual.CleanupId == expected.CleanupId
            && string.Equals(actual.OpaqueWorkItemId, expected.OpaqueWorkItemId, StringComparison.Ordinal)
            && actual.CreatedAt == expected.CreatedAt;
    }

    private static bool DefinitionEquivalent(AgentJobDefinition actual, AgentJobDefinition expected) =>
        string.Equals(actual.JobType, expected.JobType, StringComparison.Ordinal)
        && string.Equals(actual.CapabilityId, expected.CapabilityId, StringComparison.Ordinal)
        && actual.Risk == expected.Risk
        && actual.ContainsPrivateOsData == expected.ContainsPrivateOsData
        && actual.BenefitsFromBackgroundExecution == expected.BenefitsFromBackgroundExecution
        && actual.MaxAttempts == expected.MaxAttempts
        && actual.RequiredPermissions.SetEquals(expected.RequiredPermissions);

    private static bool CheckpointVersionEquivalent(AgentJobCheckpoint? actual, AgentJobCheckpoint? expected)
    {
        if (ReferenceEquals(actual, expected)) return true;
        if (actual is null || expected is null) return false;
        return string.Equals(actual.Step, expected.Step, StringComparison.Ordinal)
            && string.Equals(actual.Payload, expected.Payload, StringComparison.Ordinal)
            && actual.SavedAt == expected.SavedAt;
    }
}
