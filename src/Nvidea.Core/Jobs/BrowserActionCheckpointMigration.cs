using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Jobs;

public sealed record BrowserActionCheckpointMigrationReport(
    int Examined,
    int AlreadyCurrent,
    int Migrated,
    int Quarantined);

public enum BrowserActionCheckpointRecordMigrationStatus
{
    Ignored,
    AlreadyCurrent,
    Migrated,
    Quarantined
}

public sealed record BrowserActionCheckpointRecordMigration(
    BrowserActionCheckpointRecordMigrationStatus Status,
    AgentJobRecord Record);

/// <summary>
/// Versioned serializer for durable browser-action checkpoints. Version 2 is the first
/// contract that requires typed postconditions for state-changing autonomous actions and
/// forbids free-text ExpectedState from being treated as executable verification.
/// The version marker is added to the existing top-level BrowserAction JSON shape so older
/// read-only recovery/UX readers can still deserialize the descriptive action safely.
/// </summary>
public static class BrowserActionCheckpointCodec
{
    public const int CurrentVerificationContractVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SerializeCurrent(BrowserAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ValidateCurrent(action);

        var node = JsonSerializer.SerializeToNode(action, JsonOptions) as JsonObject
            ?? throw new InvalidOperationException("Browser action did not serialize to an object.");
        node["verificationContractVersion"] = CurrentVerificationContractVersion;
        return node.ToJsonString(JsonOptions);
    }

    public static BrowserAction DeserializeCurrent(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("Browser action job requires a persisted action checkpoint payload.");

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("verificationContractVersion", out var version)
                || !version.TryGetInt32(out var parsed))
            {
                throw new InvalidOperationException(
                    "Browser action checkpoint is unversioned. Run durable browser checkpoint migration before resuming it.");
            }

            if (parsed != CurrentVerificationContractVersion)
            {
                throw new InvalidOperationException(
                    $"Browser action checkpoint verification contract v{parsed} is not executable. Run durable browser checkpoint migration before resuming it.");
            }

            var action = JsonSerializer.Deserialize<BrowserAction>(payload, JsonOptions)
                ?? throw new InvalidOperationException("Browser action checkpoint was empty after deserialization.");
            ValidateCurrent(action);
            return action;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Browser action checkpoint payload is invalid.", ex);
        }
    }

    public static bool IsCurrent(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("verificationContractVersion", out var version)
                && version.TryGetInt32(out var parsed)
                && parsed == CurrentVerificationContractVersion;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static BrowserAction DeserializeLegacyUnversioned(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("Legacy browser action checkpoint payload is empty.");

        try
        {
            return JsonSerializer.Deserialize<BrowserAction>(payload, JsonOptions)
                ?? throw new InvalidOperationException("Legacy browser action checkpoint was empty after deserialization.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Legacy browser action checkpoint payload is invalid.", ex);
        }
    }

    private static void ValidateCurrent(BrowserAction action)
    {
        if (!string.IsNullOrWhiteSpace(action.ExpectedState))
        {
            throw new InvalidOperationException(
                "Verification contract v2 forbids legacy ExpectedState. Persist typed Postconditions instead.");
        }

        BrowserLegacyActionMigration.EnsureAutonomousActionUsesTypedVerification(action);
    }
}

/// <summary>
/// One-way migration for durable browser.action checkpoints written before verification
/// contract versioning. Safe cases are rewritten to v2. Ambiguous legacy mutations are
/// quarantined as failed, with the original action payload removed so they cannot be replayed.
/// </summary>
public sealed class BrowserActionCheckpointMigrationService
{
    public const string QuarantinedStep = "browser.action.quarantined.legacy-verification";

    private readonly IAgentJobStore _store;

    public BrowserActionCheckpointMigrationService(IAgentJobStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<BrowserActionCheckpointMigrationReport> MigrateAsync(
        CancellationToken cancellationToken = default)
    {
        var jobs = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
        var examined = 0;
        var current = 0;
        var migrated = 0;
        var quarantined = 0;

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = MigrateRecord(job);
            switch (result.Status)
            {
                case BrowserActionCheckpointRecordMigrationStatus.Ignored:
                    continue;
                case BrowserActionCheckpointRecordMigrationStatus.AlreadyCurrent:
                    examined++;
                    current++;
                    continue;
                case BrowserActionCheckpointRecordMigrationStatus.Migrated:
                    examined++;
                    migrated++;
                    break;
                case BrowserActionCheckpointRecordMigrationStatus.Quarantined:
                    examined++;
                    quarantined++;
                    break;
            }

            await _store.SaveAsync(result.Record, cancellationToken).ConfigureAwait(false);
        }

        return new BrowserActionCheckpointMigrationReport(examined, current, migrated, quarantined);
    }

    /// <summary>
    /// Pure record transform used by durable stores before exposing legacy browser jobs to callers.
    /// It never executes browser code, creates approval, or invokes a model.
    /// </summary>
    public static BrowserActionCheckpointRecordMigration MigrateRecord(AgentJobRecord job)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (!string.Equals(job.Definition.JobType, BrowserActionJobHandler.Type, StringComparison.Ordinal)
            || !string.Equals(job.Checkpoint?.Step, BrowserActionJobHandler.CheckpointStep, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(job.Checkpoint.Payload))
        {
            return new BrowserActionCheckpointRecordMigration(
                BrowserActionCheckpointRecordMigrationStatus.Ignored,
                job);
        }

        if (BrowserActionCheckpointCodec.IsCurrent(job.Checkpoint.Payload))
        {
            return new BrowserActionCheckpointRecordMigration(
                BrowserActionCheckpointRecordMigrationStatus.AlreadyCurrent,
                job);
        }

        BrowserAction legacyAction;
        try
        {
            legacyAction = BrowserActionCheckpointCodec.DeserializeLegacyUnversioned(job.Checkpoint.Payload);
        }
        catch (InvalidOperationException ex)
        {
            return Quarantine(job, "Legacy browser checkpoint could not be parsed: " + ex.Message);
        }

        var result = BrowserLegacyActionMigration.Migrate(legacyAction);
        if (result.Status == BrowserLegacyMigrationStatus.RequiresHumanReview || result.Action is null)
            return Quarantine(job, result.Reason);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var rewritten = job with
            {
                Checkpoint = new AgentJobCheckpoint(
                    BrowserActionJobHandler.CheckpointStep,
                    BrowserActionCheckpointCodec.SerializeCurrent(result.Action),
                    now),
                UpdatedAt = now
            };
            return new BrowserActionCheckpointRecordMigration(
                BrowserActionCheckpointRecordMigrationStatus.Migrated,
                rewritten);
        }
        catch (InvalidOperationException ex)
        {
            return Quarantine(
                job,
                "Legacy browser checkpoint cannot satisfy verification contract v2: " + ex.Message);
        }
    }

    private static BrowserActionCheckpointRecordMigration Quarantine(AgentJobRecord job, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        var sanitized = JsonSerializer.Serialize(new
        {
            quarantined = true,
            reason,
            originalPayloadRemoved = true
        });
        var blocked = job with
        {
            State = AgentJobState.Failed,
            Checkpoint = new AgentJobCheckpoint(QuarantinedStep, sanitized, now),
            ApprovalScope = null,
            LastError = "Legacy browser action requires human review and was quarantined without execution. " + reason,
            UpdatedAt = now,
            NextAttemptAt = null
        };
        return new BrowserActionCheckpointRecordMigration(
            BrowserActionCheckpointRecordMigrationStatus.Quarantined,
            blocked);
    }
}
