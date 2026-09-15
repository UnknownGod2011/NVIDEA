using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchCreateAuthorityRevalidationTests
{
    [Theory]
    [InlineData("opaque-id")]
    [InlineData("protocol")]
    [InlineData("checkpoint-step")]
    [InlineData("checkpoint-time")]
    [InlineData("expiry")]
    [InlineData("envelope-digest")]
    [InlineData("execution-location")]
    [InlineData("approval")]
    public async Task RevalidateCreateAuthorityAsync_TrustRootMutation_FailsClosed(string mutation)
    {
        var path = Path.Combine(Path.GetTempPath(), $"nvidea-create-authority-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var audit = new MemoryAuditTrail();
            var job = CreatePendingResearchJob();
            await store.SaveAsync(job);
            var envelope = CreateEnvelope();
            var atomic = new AtomicRemoteResearchDispatchReservation(store, audit);
            var reserved = await atomic.ReserveAsync(
                new RemoteResearchDispatchReservation(job.JobId, job.Checkpoint!.Step, envelope.OpaqueWorkItemId, envelope.CreatedAt, envelope.ExpiresAt),
                envelope);

            var changed = Mutate(reserved, mutation);
            await store.SaveAsync(changed);

            await Assert.ThrowsAnyAsync<Exception>(() => atomic.RevalidateCreateAuthorityAsync(reserved));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        }
    }

    [Fact]
    public async Task RevalidateCreateAuthorityAsync_UnchangedReservation_ReturnsCurrentAuthority()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nvidea-create-authority-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonAgentJobStore(path);
            var audit = new MemoryAuditTrail();
            var job = CreatePendingResearchJob();
            await store.SaveAsync(job);
            var envelope = CreateEnvelope();
            var atomic = new AtomicRemoteResearchDispatchReservation(store, audit);
            var reserved = await atomic.ReserveAsync(
                new RemoteResearchDispatchReservation(job.JobId, job.Checkpoint!.Step, envelope.OpaqueWorkItemId, envelope.CreatedAt, envelope.ExpiresAt),
                envelope);

            var current = await atomic.RevalidateCreateAuthorityAsync(reserved);

            Assert.Equal(reserved.JobId, current.JobId);
            Assert.Equal(reserved.RemoteResearch, current.RemoteResearch);
            Assert.Equal(reserved.RemoteWorkItemEnvelopeSha256, current.RemoteWorkItemEnvelopeSha256);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
        }
    }

    private static AgentJobRecord Mutate(AgentJobRecord source, string mutation)
    {
        var provenance = source.RemoteResearch!;
        var checkpoint = source.Checkpoint!;
        return mutation switch
        {
            "opaque-id" => source with { RemoteResearch = provenance with { OpaqueWorkItemId = provenance.OpaqueWorkItemId + "x" } },
            "protocol" => source with { RemoteResearch = provenance with { ProtocolVersion = provenance.ProtocolVersion + ".substituted" } },
            "checkpoint-step" => source with { Checkpoint = checkpoint with { Step = checkpoint.Step + ".changed" } },
            "checkpoint-time" => source with { Checkpoint = checkpoint with { SavedAt = checkpoint.SavedAt.AddSeconds(1) } },
            "expiry" => source with { RemoteResearch = provenance with { WorkItemExpiresAt = provenance.WorkItemExpiresAt!.Value.AddSeconds(-1) } },
            "envelope-digest" => source with { RemoteWorkItemEnvelopeSha256 = new string('0', ResearchWorkItemEnvelopeCommitment.HexLength) },
            "execution-location" => source with { ExecutionLocation = JobExecutionLocation.NebiusServerless },
            "approval" => source with { ApprovalScope = "substituted-approval" },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null)
        };
    }

    private static AgentJobRecord CreatePendingResearchJob()
    {
        var now = DateTimeOffset.UtcNow;
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(ResearchJobHandler.Type, "research.deep", new HashSet<DataPermission> { DataPermission.NetworkAccess }, CapabilityRiskLevel.Low, false, true),
            AgentJobState.Pending,
            JobExecutionLocation.Local,
            0,
            new AgentJobCheckpoint("research.plan", "{}", now),
            null,
            null,
            now,
            now);
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

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>());
    }
}
