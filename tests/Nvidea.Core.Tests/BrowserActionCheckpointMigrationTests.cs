using System.Text.Json;
using System.Text.Json.Serialization;
using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class BrowserActionCheckpointMigrationTests
{
    private static readonly JsonSerializerOptions LegacyJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void CurrentCheckpoint_UsesTopLevelVersionAndTypedActionShape()
    {
        var action = new BrowserAction(
            BrowserActionKind.Navigate,
            Destination: new Uri("https://example.test/done"),
            Postconditions: new[]
            {
                new BrowserPostcondition(BrowserPostconditionKind.UrlEquals, Expected: "https://example.test/done")
            });

        var payload = BrowserActionCheckpointCodec.SerializeCurrent(action);
        using var document = JsonDocument.Parse(payload);

        Assert.Equal(2, document.RootElement.GetProperty("verificationContractVersion").GetInt32());
        Assert.Equal("Navigate", document.RootElement.GetProperty("kind").GetString());
        Assert.False(document.RootElement.TryGetProperty("action", out _));
        Assert.Null(BrowserActionCheckpointCodec.DeserializeCurrent(payload).ExpectedState);
    }

    [Fact]
    public async Task LegacyNavigation_IsRewrittenToVersion2WithoutExecutionAuthority()
    {
        var store = new InMemoryJobStore();
        var legacy = new BrowserAction(
            BrowserActionKind.Navigate,
            Destination: new Uri("https://example.test/complete"),
            ExpectedState: "done");
        var job = CreateLegacyJob(legacy, AgentJobState.Pending, approvalScope: null);
        await store.SaveAsync(job);

        var report = await new BrowserActionCheckpointMigrationService(store).MigrateAsync();
        var migrated = await store.GetAsync(job.JobId);

        Assert.Equal(1, report.Migrated);
        Assert.Equal(0, report.Quarantined);
        Assert.NotNull(migrated);
        Assert.True(BrowserActionCheckpointCodec.IsCurrent(migrated!.Checkpoint!.Payload));
        var action = BrowserActionCheckpointCodec.DeserializeCurrent(migrated.Checkpoint.Payload);
        Assert.Null(action.ExpectedState);
        Assert.Single(action.Postconditions!);
        Assert.Equal(BrowserPostconditionKind.UrlEquals, action.Postconditions![0].Kind);
        Assert.Equal(AgentJobState.Pending, migrated.State);
        Assert.Null(migrated.ApprovalScope);
    }

    [Fact]
    public async Task LegacyMutation_IsQuarantinedAndOriginalPayloadRemoved()
    {
        var store = new InMemoryJobStore();
        var legacy = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Delete"),
            ExpectedState: "Deleted",
            Rationale: "Delete item");
        var job = CreateLegacyJob(legacy, AgentJobState.WaitingForApproval, "browser|dangerous|scope");
        await store.SaveAsync(job);

        var report = await new BrowserActionCheckpointMigrationService(store).MigrateAsync();
        var quarantined = await store.GetAsync(job.JobId);

        Assert.Equal(1, report.Quarantined);
        Assert.NotNull(quarantined);
        Assert.Equal(AgentJobState.Failed, quarantined!.State);
        Assert.Equal(BrowserActionCheckpointMigrationService.QuarantinedStep, quarantined.Checkpoint!.Step);
        Assert.Null(quarantined.ApprovalScope);
        Assert.Null(quarantined.NextAttemptAt);
        Assert.Contains("human review", quarantined.LastError ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Delete item", quarantined.Checkpoint.Payload ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("Deleted", quarantined.Checkpoint.Payload ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("browser|dangerous|scope", quarantined.Checkpoint.Payload ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AlreadyVersionedCheckpoint_IsIdempotent()
    {
        var store = new InMemoryJobStore();
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Next"),
            Postconditions: new[]
            {
                new BrowserPostcondition(BrowserPostconditionKind.VisibleTextContains, Expected: "Step 2")
            });
        var checkpoint = BrowserActionJobHandler.CreateCheckpoint(action);
        var job = CreateJob(checkpoint, AgentJobState.Pending, approvalScope: null);
        await store.SaveAsync(job);

        var report = await new BrowserActionCheckpointMigrationService(store).MigrateAsync();
        var unchanged = await store.GetAsync(job.JobId);

        Assert.Equal(1, report.AlreadyCurrent);
        Assert.Equal(0, report.Migrated);
        Assert.Equal(0, report.Quarantined);
        Assert.Equal(checkpoint.Payload, unchanged!.Checkpoint!.Payload);
    }

    [Fact]
    public void HandlerCodec_RejectsUnversionedTypedWriteUntilMigrationRuns()
    {
        var action = new BrowserAction(
            BrowserActionKind.Click,
            BrowserLocator.ByRole("button", "Next"),
            Postconditions: new[]
            {
                new BrowserPostcondition(BrowserPostconditionKind.VisibleTextContains, Expected: "Step 2")
            });
        var rawLegacyShape = JsonSerializer.Serialize(action, LegacyJson);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BrowserActionCheckpointCodec.DeserializeCurrent(rawLegacyShape));

        Assert.Contains("unversioned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static AgentJobRecord CreateLegacyJob(
        BrowserAction action,
        AgentJobState state,
        string? approvalScope)
    {
        var payload = JsonSerializer.Serialize(action, LegacyJson);
        return CreateJob(
            new AgentJobCheckpoint(BrowserActionJobHandler.CheckpointStep, payload, DateTimeOffset.UtcNow),
            state,
            approvalScope);
    }

    private static AgentJobRecord CreateJob(
        AgentJobCheckpoint checkpoint,
        AgentJobState state,
        string? approvalScope)
    {
        var now = DateTimeOffset.UtcNow;
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                BrowserActionJobHandler.Type,
                "browser.agent",
                new HashSet<DataPermission> { DataPermission.BrowserWrite },
                CapabilityRiskLevel.High,
                ContainsPrivateOsData: true,
                BenefitsFromBackgroundExecution: false),
            state,
            JobExecutionLocation.Local,
            Attempt: 0,
            checkpoint,
            approvalScope,
            LastError: null,
            CreatedAt: now,
            UpdatedAt: now);
    }

    private sealed class InMemoryJobStore : IAgentJobStore
    {
        private readonly Dictionary<Guid, AgentJobRecord> _jobs = new();

        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _jobs.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AgentJobRecord>>(_jobs.Values.ToArray());
        }

        public Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _jobs[record.JobId] = record;
            return Task.CompletedTask;
        }
    }
}
