using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class DurableProtectedPayloadCleanupIntentTests
{
    [Fact]
    public async Task Stage_and_clear_keep_cleanup_owed_until_completion_is_durably_recorded()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var coordinator = new DurableProtectedPayloadCleanupIntent(store);
        var job = CreateJob();
        await store.SaveAsync(job);

        var staged = coordinator.Stage(job, OpaqueId, DateTimeOffset.Parse("2026-09-14T14:00:00Z"));
        Assert.NotNull(staged.PendingProtectedPayloadCleanup);
        Assert.Equal(OpaqueId, staged.PendingProtectedPayloadCleanup!.OpaqueWorkItemId);
        Assert.True(await store.CompareExchangeAsync(job, staged));

        var cleared = await coordinator.ClearAsync(staged, staged.PendingProtectedPayloadCleanup.CleanupId);
        Assert.Null(cleared.PendingProtectedPayloadCleanup);

        var persisted = await store.GetAsync(job.JobId);
        Assert.Null(persisted?.PendingProtectedPayloadCleanup);
    }

    [Fact]
    public void Stage_reuses_existing_exact_cleanup_identity()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var coordinator = new DurableProtectedPayloadCleanupIntent(store);
        var staged = coordinator.Stage(CreateJob(), OpaqueId, DateTimeOffset.Parse("2026-09-14T14:00:00Z"));

        var reused = coordinator.Stage(staged, OpaqueId, DateTimeOffset.Parse("2026-09-14T15:00:00Z"));

        Assert.Equal(staged.PendingProtectedPayloadCleanup, reused.PendingProtectedPayloadCleanup);
    }

    [Fact]
    public async Task Compare_exchange_versions_pending_cleanup_so_stale_writer_cannot_erase_it()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var coordinator = new DurableProtectedPayloadCleanupIntent(store);
        var baseline = CreateJob();
        await store.SaveAsync(baseline);

        var staged = coordinator.Stage(baseline, OpaqueId);
        await store.SaveAsync(staged);

        var staleReplacement = baseline with { LastError = "stale writer" };
        Assert.False(await store.CompareExchangeAsync(baseline, staleReplacement));

        var persisted = await store.GetAsync(baseline.JobId);
        Assert.Equal(staged.PendingProtectedPayloadCleanup, persisted?.PendingProtectedPayloadCleanup);
        Assert.Null(persisted?.LastError);
    }

    [Fact]
    public async Task Clear_refuses_before_required_audit_is_durable()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var coordinator = new DurableProtectedPayloadCleanupIntent(store);
        var staged = coordinator.Stage(CreateJob(), OpaqueId) with
        {
            PendingAuditEvent = CreateAudit()
        };
        await store.SaveAsync(staged);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.ClearAsync(staged, staged.PendingProtectedPayloadCleanup!.CleanupId));

        Assert.Contains("audit", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull((await store.GetAsync(staged.JobId))?.PendingProtectedPayloadCleanup);
    }

    [Fact]
    public void ValidatePending_rejects_substituted_or_corrupt_cleanup_target()
    {
        using var scope = new TempScope();
        var coordinator = new DurableProtectedPayloadCleanupIntent(new JsonAgentJobStore(scope.JobPath, protector: null));
        var staged = coordinator.Stage(CreateJob(), OpaqueId);

        Assert.Throws<InvalidOperationException>(() =>
            coordinator.ValidatePending(staged, "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));

        var corrupt = staged with
        {
            PendingProtectedPayloadCleanup = staged.PendingProtectedPayloadCleanup! with
            {
                OpaqueWorkItemId = "cccccccccccccccccccccccccccccccc"
            }
        };
        Assert.Throws<InvalidOperationException>(() => coordinator.ValidatePending(corrupt, OpaqueId));
    }

    private const string OpaqueId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static AgentJobRecord CreateJob()
    {
        var now = DateTimeOffset.Parse("2026-09-14T13:00:00Z");
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission>(),
                CapabilityRiskLevel.Medium,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true),
            AgentJobState.Failed,
            JobExecutionLocation.Local,
            Attempt: 1,
            Checkpoint: null,
            ApprovalScope: null,
            LastError: "terminal",
            CreatedAt: now,
            UpdatedAt: now,
            RemoteResearch: new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                OpaqueId,
                "remote-job-123",
                "research.query",
                now.AddMinutes(-5),
                now.AddMinutes(-4),
                RemoteResearchProvenanceState.RemoteFailed,
                TerminalAt: now));
    }

    private static AuditEvent CreateAudit() =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-09-14T14:00:00Z"),
            "research.deep",
            Guid.NewGuid().ToString("N"),
            "research.remote_failed",
            CapabilityRiskLevel.Medium,
            Allowed: true,
            Approved: false,
            ApprovalScope: string.Empty,
            Summary: "Terminal research state.",
            Metadata: new Dictionary<string, string>());

    private sealed class TempScope : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "nvidea-cleanup-intent-tests-" + Guid.NewGuid().ToString("N"));
        public string JobPath => Path.Combine(_root, "jobs.json");

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
    }
}
