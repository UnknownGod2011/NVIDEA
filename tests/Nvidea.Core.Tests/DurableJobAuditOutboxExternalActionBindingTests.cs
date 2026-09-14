using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class DurableJobAuditOutboxExternalActionBindingTests
{
    [Fact]
    public async Task FlushAsync_RejectsMismatchedExternalActionAuditBindingBeforeAuditAppendOrMutation()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-audit-action-binding-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new JsonAgentJobStore(Path.Combine(root, "jobs.json"), protector: null);
            var audit = new MemoryAuditTrail();
            var outbox = new DurableJobAuditOutbox(store, audit);
            var now = DateTimeOffset.Parse("2026-09-14T12:00:00Z");
            var job = new AgentJobRecord(
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
            var pendingAudit = new AuditEvent(
                Guid.NewGuid(),
                now,
                job.Definition.CapabilityId,
                job.JobId.ToString("N"),
                "research.remote_cancel_redriven",
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
            var corrupt = job with
            {
                PendingAuditEvent = pendingAudit,
                PendingExternalAction = new PendingExternalAction(
                    Guid.NewGuid(),
                    DurableExternalActionKind.NebiusCancelRemoteResearch,
                    "job-123",
                    Guid.NewGuid(),
                    now)
            };
            await store.SaveAsync(corrupt);

            await Assert.ThrowsAsync<InvalidDataException>(() => outbox.FlushAsync(corrupt));

            Assert.Empty(audit.Events);
            var persisted = await store.GetAsync(job.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(corrupt.PendingAuditEvent, persisted!.PendingAuditEvent);
            Assert.Equal(corrupt.PendingExternalAction, persisted.PendingExternalAction);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Events.ToArray());
    }
}
