using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class EvidenceObservingRemoteResearchClientRuntimeTests
{
    [Fact]
    public async Task Dispatch_and_cancellation_request_never_record_background_evidence()
    {
        var ledger = new SessionEvidenceLedger();
        var record = Record(RemoteResearchProvenanceState.ResultApplied);
        var inner = new FakeRuntime { DispatchResult = record, CancellationRequestResult = record };
        var runtime = new EvidenceObservingRemoteResearchClientRuntime(inner, ledger);

        var workItem = new RemoteResearchWorkItem(record.JobId, "requested", null, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5));
        var authorization = new ResearchCloudAuthorization(record.JobId, "requested", true, ResearchWorkItemProtector.DisclosureVersion, DateTimeOffset.UtcNow);
        await runtime.DispatchAsync(workItem, authorization);
        await runtime.RequestCancellationAsync(record.JobId);

        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.NebiusBackgroundExecutionObserved));
    }

    [Theory]
    [InlineData(RemoteResearchProvenanceState.DispatchReserved, JobExecutionLocation.Local)]
    [InlineData(RemoteResearchProvenanceState.Dispatched, JobExecutionLocation.NebiusServerless)]
    [InlineData(RemoteResearchProvenanceState.CancelRequested, JobExecutionLocation.NebiusServerless)]
    [InlineData(RemoteResearchProvenanceState.Cancelled, JobExecutionLocation.Local)]
    [InlineData(RemoteResearchProvenanceState.RemoteFailed, JobExecutionLocation.Local)]
    [InlineData(RemoteResearchProvenanceState.Expired, JobExecutionLocation.Local)]
    public async Task Reconciliation_non_applied_states_never_record_background_evidence(RemoteResearchProvenanceState state, JobExecutionLocation location)
    {
        var ledger = new SessionEvidenceLedger();
        var record = Record(state, location);
        var runtime = new EvidenceObservingRemoteResearchClientRuntime(new FakeRuntime { ReconcileDispatchedResult = record }, ledger);

        await runtime.ReconcileDispatchedAsync(record.JobId);

        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.NebiusBackgroundExecutionObserved));
    }

    [Fact]
    public async Task Result_applied_with_pending_audit_remains_non_evidence_until_recovery_clears_outbox()
    {
        var ledger = new SessionEvidenceLedger();
        var pending = Record(RemoteResearchProvenanceState.ResultApplied, pendingAudit: true);
        var recovered = pending with { PendingAuditEvent = null, UpdatedAt = pending.UpdatedAt.AddSeconds(1) };
        var inner = new FakeRuntime { ReconcileDispatchedResult = pending, RecoverPendingAuditResult = recovered };
        var runtime = new EvidenceObservingRemoteResearchClientRuntime(inner, ledger);

        await runtime.ReconcileDispatchedAsync(pending.JobId);
        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.NebiusBackgroundExecutionObserved));

        await runtime.RecoverPendingAuditAsync(pending.JobId);
        Assert.True(ledger.Snapshot().Contains(SessionEvidenceKind.NebiusBackgroundExecutionObserved));
    }

    [Fact]
    public async Task Audited_result_applied_records_background_evidence_exactly_once()
    {
        var first = new DateTimeOffset(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);
        var clockCalls = 0;
        var ledger = new SessionEvidenceLedger(() => first.AddMinutes(clockCalls++));
        var record = Record(RemoteResearchProvenanceState.ResultApplied);
        var inner = new FakeRuntime
        {
            ReconcileReservedResult = record,
            ReconcileDispatchedResult = record,
            ReconcileCancellationResult = record,
            RecoverPendingAuditResult = record
        };
        var runtime = new EvidenceObservingRemoteResearchClientRuntime(inner, ledger);

        await runtime.ReconcileReservedAsync(record.JobId);
        await runtime.ReconcileDispatchedAsync(record.JobId);
        await runtime.ReconcileCancellationAsync(record.JobId);
        await runtime.RecoverPendingAuditAsync(record.JobId);

        var entries = ledger.Snapshot().Entries.Where(x => x.Kind == SessionEvidenceKind.NebiusBackgroundExecutionObserved).ToArray();
        Assert.Single(entries);
        Assert.Equal(first, entries[0].FirstObservedAt);
    }

    [Fact]
    public async Task Thrown_reconciliation_never_records_background_evidence()
    {
        var ledger = new SessionEvidenceLedger();
        var runtime = new EvidenceObservingRemoteResearchClientRuntime(new FakeRuntime { ReconcileException = new InvalidOperationException("provider reconciliation failed") }, ledger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.ReconcileDispatchedAsync(Guid.NewGuid()));

        Assert.False(ledger.Snapshot().Contains(SessionEvidenceKind.NebiusBackgroundExecutionObserved));
    }

    private static AgentJobRecord Record(
        RemoteResearchProvenanceState state,
        JobExecutionLocation? location = null,
        bool pendingAudit = false)
    {
        var now = DateTimeOffset.UtcNow;
        var executionLocation = location ?? (state == RemoteResearchProvenanceState.Dispatched || state == RemoteResearchProvenanceState.CancelRequested
            ? JobExecutionLocation.NebiusServerless
            : JobExecutionLocation.Local);
        var provenance = new RemoteResearchProvenance(
            "nvidea.research.remote.v1",
            "opaque-work-item",
            state == RemoteResearchProvenanceState.DispatchReserved ? null : "remote-job",
            "requested",
            now.AddMinutes(-2),
            now.AddMinutes(-1),
            state,
            state == RemoteResearchProvenanceState.ResultApplied ? now : null,
            now.AddHours(1),
            state is RemoteResearchProvenanceState.Cancelled or RemoteResearchProvenanceState.RemoteFailed or RemoteResearchProvenanceState.Expired ? now : null);
        var audit = pendingAudit
            ? new AuditEvent(Guid.NewGuid(), now, "research", "remote-result", "research.remote_result_applied", CapabilityRiskLevel.Low, true, true, string.Empty, "Remote result applied.")
            : null;
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition("research", "research", new HashSet<DataPermission>(), CapabilityRiskLevel.Low, false, true),
            state == RemoteResearchProvenanceState.ResultApplied ? AgentJobState.Pending : state switch
            {
                RemoteResearchProvenanceState.Cancelled => AgentJobState.Cancelled,
                RemoteResearchProvenanceState.RemoteFailed or RemoteResearchProvenanceState.Expired => AgentJobState.Failed,
                _ => AgentJobState.Running
            },
            executionLocation,
            1,
            new AgentJobCheckpoint("requested", null, now.AddMinutes(-2)),
            null,
            null,
            now.AddMinutes(-3),
            now,
            RemoteResearch: provenance,
            PendingAuditEvent: audit);
    }

    private sealed class FakeRuntime : IRemoteResearchClientRuntime
    {
        public AgentJobRecord? DispatchResult { get; init; }
        public AgentJobRecord? ReconcileReservedResult { get; init; }
        public AgentJobRecord? ReconcileDispatchedResult { get; init; }
        public AgentJobRecord? CancellationRequestResult { get; init; }
        public AgentJobRecord? ReconcileCancellationResult { get; init; }
        public AgentJobRecord? RecoverPendingAuditResult { get; init; }
        public Exception? ReconcileException { get; init; }

        public Task<AgentJobRecord> DispatchAsync(RemoteResearchWorkItem workItem, ResearchCloudAuthorization authorization, CancellationToken cancellationToken = default) => Task.FromResult(Required(DispatchResult));
        public Task<AgentJobRecord> ReconcileReservedAsync(Guid jobId, CancellationToken cancellationToken = default) => Task.FromResult(Required(ReconcileReservedResult));
        public Task<AgentJobRecord> ReconcileDispatchedAsync(Guid jobId, DateTimeOffset? now = null, CancellationToken cancellationToken = default) => ReconcileException is null ? Task.FromResult(Required(ReconcileDispatchedResult)) : Task.FromException<AgentJobRecord>(ReconcileException);
        public Task<AgentJobRecord> RequestCancellationAsync(Guid jobId, CancellationToken cancellationToken = default) => Task.FromResult(Required(CancellationRequestResult));
        public Task<AgentJobRecord> ReconcileCancellationAsync(Guid jobId, CancellationToken cancellationToken = default) => Task.FromResult(Required(ReconcileCancellationResult));
        public Task<AgentJobRecord> RecoverPendingAuditAsync(Guid jobId, CancellationToken cancellationToken = default) => Task.FromResult(Required(RecoverPendingAuditResult));

        private static AgentJobRecord Required(AgentJobRecord? record) => record ?? throw new InvalidOperationException("Fake runtime result was not configured.");
    }
}
