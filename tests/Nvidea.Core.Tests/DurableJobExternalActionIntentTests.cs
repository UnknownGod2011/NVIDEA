using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class DurableJobExternalActionIntentTests
{
    [Fact]
    public async Task Stage_flush_and_clear_keep_external_intent_until_reconciliation()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var audit = new InMemoryAuditTrail();
        var coordinator = new DurableJobExternalActionIntent(store, audit);
        var job = CreateJob();
        await store.SaveAsync(job);

        var evt = CreateAudit(job, "research.remote_cancel_redriven");
        var staged = coordinator.Stage(
            job,
            evt,
            DurableExternalActionKind.NebiusCancelRemoteResearch,
            "remote-job-123",
            DateTimeOffset.Parse("2026-09-14T10:00:00Z"));

        Assert.NotNull(staged.PendingAuditEvent);
        Assert.NotNull(staged.PendingExternalAction);
        Assert.Equal(evt.EventId, staged.PendingExternalAction!.AuditEventId);
        Assert.Equal("remote-job-123", staged.PendingExternalAction.TargetId);

        Assert.True(await store.CompareExchangeAsync(job, staged));

        var audited = await coordinator.FlushAuditAsync(staged);
        Assert.Null(audited.PendingAuditEvent);
        Assert.NotNull(audited.PendingExternalAction);
        Assert.Single(await audit.ReadAllAsync());

        var cleared = await coordinator.ClearAsync(audited, audited.PendingExternalAction!.ActionId);
        Assert.Null(cleared.PendingAuditEvent);
        Assert.Null(cleared.PendingExternalAction);
        Assert.Single(await audit.ReadAllAsync());
    }

    [Fact]
    public async Task Clear_refuses_to_remove_action_before_bound_audit_is_durable()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var audit = new InMemoryAuditTrail();
        var coordinator = new DurableJobExternalActionIntent(store, audit);
        var job = CreateJob();
        var evt = CreateAudit(job, "research.remote_cancel_redriven");
        var staged = coordinator.Stage(
            job,
            evt,
            DurableExternalActionKind.NebiusCancelRemoteResearch,
            "remote-job-123");

        await store.SaveAsync(staged);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.ClearAsync(staged, staged.PendingExternalAction!.ActionId));

        Assert.Contains("audit", error.Message, StringComparison.OrdinalIgnoreCase);
        var persisted = await store.GetAsync(job.JobId);
        Assert.NotNull(persisted?.PendingExternalAction);
        Assert.NotNull(persisted?.PendingAuditEvent);
    }

    [Fact]
    public async Task Compare_exchange_versions_pending_external_action()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var baseline = CreateJob();
        await store.SaveAsync(baseline);

        var action = new PendingExternalAction(
            Guid.NewGuid(),
            DurableExternalActionKind.NebiusCancelRemoteResearch,
            "remote-job-123",
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-09-14T10:00:00Z"));
        var actual = baseline with { PendingExternalAction = action };
        await store.SaveAsync(actual);

        var staleReplacement = baseline with { LastError = "stale writer" };
        Assert.False(await store.CompareExchangeAsync(baseline, staleReplacement));

        var persisted = await store.GetAsync(baseline.JobId);
        Assert.Equal(action, persisted?.PendingExternalAction);
        Assert.Null(persisted?.LastError);
    }

    [Fact]
    public void Stage_rejects_control_characters_in_provider_target()
    {
        using var scope = new TempScope();
        var store = new JsonAgentJobStore(scope.JobPath, protector: null);
        var coordinator = new DurableJobExternalActionIntent(store, new InMemoryAuditTrail());
        var job = CreateJob();
        var evt = CreateAudit(job, "research.remote_cancel_redriven");

        Assert.Throws<ArgumentException>(() => coordinator.Stage(
            job,
            evt,
            DurableExternalActionKind.NebiusCancelRemoteResearch,
            "remote\nsecret"));
    }

    private static AgentJobRecord CreateJob()
    {
        var now = DateTimeOffset.Parse("2026-09-14T09:00:00Z");
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission>(),
                CapabilityRiskLevel.Medium,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true),
            AgentJobState.Running,
            JobExecutionLocation.NebiusServerless,
            Attempt: 1,
            Checkpoint: null,
            ApprovalScope: null,
            LastError: null,
            CreatedAt: now,
            UpdatedAt: now);
    }

    private static AuditEvent CreateAudit(AgentJobRecord job, string eventType) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-09-14T10:00:00Z"),
            job.Definition.CapabilityId,
            job.JobId.ToString("N"),
            eventType,
            job.Definition.Risk,
            Allowed: true,
            Approved: false,
            ApprovalScope: string.Empty,
            Summary: "Durable cancellation redrive intent.",
            Metadata: new Dictionary<string, string>
            {
                ["jobType"] = job.Definition.JobType,
                ["state"] = job.State.ToString(),
                ["executionLocation"] = job.ExecutionLocation.ToString(),
                ["attempt"] = job.Attempt.ToString()
            });

    private sealed class InMemoryAuditTrail : IAuditTrail
    {
        private readonly List<AuditEvent> _events = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (_events.Any(existing => existing.EventId == auditEvent.EventId))
                throw new InvalidOperationException("duplicate audit id");
            _events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(_events.ToArray());
    }

    private sealed class TempScope : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "nvidea-external-action-tests-" + Guid.NewGuid().ToString("N"));
        public string JobPath => Path.Combine(_root, "jobs.json");

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
    }
}
