using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class AtomicRemoteResearchDispatchReservationTests
{
    [Fact]
    public async Task ReserveAsync_CommitsProvenanceAuditAndEnvelopeDigestTogether()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nvidea-atomic-reservation-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var audit = new RecordingAuditTrail();
            var job = CreatePendingResearchJob();
            await store.SaveAsync(job);
            var envelope = CreateEnvelope();
            var reservation = new RemoteResearchDispatchReservation(
                job.JobId,
                job.Checkpoint!.Step,
                envelope.OpaqueWorkItemId,
                envelope.CreatedAt,
                envelope.ExpiresAt);

            var atomic = new AtomicRemoteResearchDispatchReservation(store, audit);
            var committed = await atomic.ReserveAsync(reservation, envelope);

            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, committed.RemoteResearch!.State);
            Assert.Equal(envelope.OpaqueWorkItemId, committed.RemoteResearch.OpaqueWorkItemId);
            Assert.Equal(
                ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope),
                committed.RemoteWorkItemEnvelopeSha256);
            Assert.Null(committed.PendingAuditEvent);
            Assert.Single(audit.Events);
            Assert.Equal("research.remote_dispatch_reserved", audit.Events[0].EventType);
        }
        finally
        {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task ReserveAsync_WhenAuditAppendFails_LeavesExactTrustRootDurable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nvidea-atomic-reservation-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var audit = new RecordingAuditTrail { FailAppend = true };
            var job = CreatePendingResearchJob();
            await store.SaveAsync(job);
            var envelope = CreateEnvelope();
            var reservation = new RemoteResearchDispatchReservation(
                job.JobId,
                job.Checkpoint!.Step,
                envelope.OpaqueWorkItemId,
                envelope.CreatedAt,
                envelope.ExpiresAt);

            var atomic = new AtomicRemoteResearchDispatchReservation(store, audit);
            await Assert.ThrowsAsync<RemoteResearchDispatchReservationAuditPendingException>(() =>
                atomic.ReserveAsync(reservation, envelope));

            var durable = await store.GetAsync(job.JobId);
            Assert.NotNull(durable);
            Assert.Equal(RemoteResearchProvenanceState.DispatchReserved, durable!.RemoteResearch!.State);
            Assert.Equal(envelope.OpaqueWorkItemId, durable.RemoteResearch.OpaqueWorkItemId);
            Assert.Equal(
                ResearchWorkItemEnvelopeCommitment.ComputeSha256(envelope),
                durable.RemoteWorkItemEnvelopeSha256);
            Assert.NotNull(durable.PendingAuditEvent);
            Assert.Equal("research.remote_dispatch_reserved", durable.PendingAuditEvent!.EventType);
        }
        finally
        {
            DeleteStore(path);
        }
    }

    [Fact]
    public async Task ReserveAsync_RejectsEnvelopeLifetimeSubstitutionWithoutMutatingJob()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nvidea-atomic-reservation-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var audit = new RecordingAuditTrail();
            var job = CreatePendingResearchJob();
            await store.SaveAsync(job);
            var envelope = CreateEnvelope();
            var reservation = new RemoteResearchDispatchReservation(
                job.JobId,
                job.Checkpoint!.Step,
                envelope.OpaqueWorkItemId,
                envelope.CreatedAt,
                envelope.ExpiresAt.AddMinutes(-1));

            var atomic = new AtomicRemoteResearchDispatchReservation(store, audit);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                atomic.ReserveAsync(reservation, envelope));

            var durable = await store.GetAsync(job.JobId);
            Assert.Equal(AgentJobState.Pending, durable!.State);
            Assert.Null(durable.RemoteResearch);
            Assert.Null(durable.RemoteWorkItemEnvelopeSha256);
            Assert.Null(durable.PendingAuditEvent);
            Assert.Empty(audit.Events);
        }
        finally
        {
            DeleteStore(path);
        }
    }

    private static AgentJobRecord CreatePendingResearchJob()
    {
        var now = DateTimeOffset.UtcNow;
        return new AgentJobRecord(
            JobId: Guid.NewGuid(),
            Definition: new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true),
            State: AgentJobState.Pending,
            ExecutionLocation: JobExecutionLocation.Local,
            Attempt: 0,
            Checkpoint: new AgentJobCheckpoint("research.plan", "{}", now),
            ApprovalScope: null,
            LastError: null,
            CreatedAt: now,
            UpdatedAt: now);
    }

    private static ProtectedResearchWorkItemEnvelope CreateEnvelope()
    {
        var now = DateTimeOffset.UtcNow;
        return new ProtectedResearchWorkItemEnvelope(
            ResearchWorkItemProtector.ProtocolVersion,
            "abcdefghijklmnopqrstuvwx12345678",
            Convert.ToBase64String("wrapped-key"u8.ToArray()),
            Convert.ToBase64String("nonce-123456"u8.ToArray()),
            Convert.ToBase64String("ciphertext"u8.ToArray()),
            Convert.ToBase64String("auth-tag-1234567"u8.ToArray()),
            now,
            now.AddMinutes(30));
    }

    private static void DeleteStore(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    }

    private sealed class RecordingAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();
        public bool FailAppend { get; init; }

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailAppend)
                throw new IOException("simulated audit append failure");
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<AuditEvent>>(Events.ToArray());
        }
    }
}
