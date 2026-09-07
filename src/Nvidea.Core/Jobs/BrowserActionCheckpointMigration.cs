using System.Text.Json;
using System.Text.Json.Serialization;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Jobs;

public sealed record BrowserActionCheckpointEnvelope(
    int VerificationContractVersion,
    BrowserAction Action);

public sealed record BrowserActionCheckpointMigrationReport(
    int Examined,
    int AlreadyCurrent,
    int Migrated,
    int Quarantined);

/// <summary>
/// Versioned serializer for durable browser-action checkpoints. Version 2 is the first
/// contract that requires typed postconditions for state-changing autonomous actions and
/// forbids free-text ExpectedState from being treated as executable verification.
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
        return JsonSerializer.Serialize(
            new BrowserActionCheckpointEnvelope(CurrentVerificationContractVersion, action),
            JsonOptions);
    }

    public static BrowserAction DeserializeCurrent(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("Browser action job requires a persisted action checkpoint payload.");

        BrowserActionCheckpointEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<BrowserActionCheckpointEnvelope>(payload, JsonOptions)
                ?? throw new InvalidOperationException("Browser action checkpoint envelope was empty after deserialization.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Browser action checkpoint payload is invalid or unversioned.", ex);
        }

        if (envelope.VerificationContractVersion != CurrentVerificationContractVersion)
        {
            throw new InvalidOperationException(
                $"Browser action checkpoint verification contract v{envelope.VerificationContractVersion} is not executable. Run durable browser checkpoint migration before resuming it.");
        }

        ValidateCurrent(envelope.Action);
        return envelope.Action;
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
            if (!string.Equals(job.Definition.JobType, BrowserActionJobHandler.Type, StringComparison.Ordinal)
                || !string.Equals(job.Checkpoint?.Step, BrowserActionJobHandler.CheckpointStep, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(job.Checkpoint.Payload))
            {
                continue;
            }

            examined++;
            if (BrowserActionCheckpointCodec.IsCurrent(job.Checkpoint.Payload))
            {
                current++;
                continue;
            }

            BrowserAction legacyAction;
            try
            {
                legacyAction = BrowserActionCheckpointCodec.DeserializeLegacyUnversioned(job.Checkpoint.Payload);
            }
            catch (InvalidOperationException ex)
            {
                await QuarantineAsync(job, "Legacy browser checkpoint could not be parsed: " + ex.Message, cancellationToken)
                    .ConfigureAwait(false);
                quarantined++;
                continue;
            }

            var result = BrowserLegacyActionMigration.Migrate(legacyAction);
            if (result.Status == BrowserLegacyMigrationStatus.RequiresHumanReview || result.Action is null)
            {
                await QuarantineAsync(job, result.Reason, cancellationToken).ConfigureAwait(false);
                quarantined++;
                continue;
            }

            try
            {
                var payload = BrowserActionCheckpointCodec.SerializeCurrent(result.Action);
                var rewritten = job with
                {
                    Checkpoint = new AgentJobCheckpoint(
                        BrowserActionJobHandler.CheckpointStep,
                        payload,
                        DateTimeOffset.UtcNow),
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await _store.SaveAsync(rewritten, cancellationToken).ConfigureAwait(false);
                migrated++;
            }
            catch (InvalidOperationException ex)
            {
                await QuarantineAsync(job, "Legacy browser checkpoint cannot satisfy verification contract v2: " + ex.Message, cancellationToken)
                    .ConfigureAwait(false);
                quarantined++;
            }
        }

        return new BrowserActionCheckpointMigrationReport(examined, current, migrated, quarantined);
    }

    private async Task QuarantineAsync(
        AgentJobRecord job,
        string reason,
        CancellationToken cancellationToken)
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
        await _store.SaveAsync(blocked, cancellationToken).ConfigureAwait(false);
    }
}
