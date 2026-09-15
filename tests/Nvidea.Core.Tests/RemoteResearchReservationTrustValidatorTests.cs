using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class RemoteResearchReservationTrustValidatorTests
{
    [Fact]
    public void ValidateReserved_AcceptsExactAtomicTrustRoot()
    {
        var now = DateTimeOffset.UtcNow;
        var record = CreateReserved(now);

        var trust = RemoteResearchReservationTrustValidator.ValidateReserved(record, now, requireUnexpired: true);

        Assert.Equal(record.RemoteResearch!.OpaqueWorkItemId, trust.Provenance.OpaqueWorkItemId);
        Assert.Equal(record.Checkpoint, trust.Checkpoint);
        Assert.Equal(record.RemoteWorkItemEnvelopeSha256, trust.EnvelopeSha256);
    }

    [Fact]
    public void ValidateReserved_RejectsCheckpointTimestampSubstitution()
    {
        var now = DateTimeOffset.UtcNow;
        var record = CreateReserved(now);
        record = record with { Checkpoint = record.Checkpoint! with { SavedAt = now.AddSeconds(1) } };

        Assert.Throws<InvalidDataException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(record));
    }

    [Fact]
    public void ValidateReserved_RejectsRemoteIdBeforeProviderCreateAuthority()
    {
        var now = DateTimeOffset.UtcNow;
        var record = CreateReserved(now);
        record = record with { RemoteResearch = record.RemoteResearch! with { RemoteJobId = "substituted-remote-id" } };

        Assert.Throws<InvalidOperationException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(record));
    }

    [Fact]
    public void ValidateReserved_RejectsProtocolAndDigestSubstitution()
    {
        var now = DateTimeOffset.UtcNow;
        var protocol = CreateReserved(now) with
        {
            RemoteResearch = CreateReserved(now).RemoteResearch! with { ProtocolVersion = "nvidea.research.remote.v999" }
        };
        Assert.Throws<InvalidOperationException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(protocol));

        var digest = CreateReserved(now) with { RemoteWorkItemEnvelopeSha256 = new string('A', 64) };
        Assert.Throws<InvalidOperationException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(digest));
    }

    [Fact]
    public void ValidateReserved_RejectsExpiredOrOverlongLifetimeWhenRequired()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = CreateReserved(now.AddHours(-2));
        Assert.Throws<InvalidOperationException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(expired, now, requireUnexpired: true));

        var overlong = CreateReserved(now);
        overlong = overlong with
        {
            RemoteResearch = overlong.RemoteResearch! with
            {
                WorkItemExpiresAt = overlong.RemoteResearch.DispatchedAt + ResearchWorkItemProtector.MaxLifetime + TimeSpan.FromSeconds(1)
            }
        };
        Assert.Throws<InvalidDataException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(overlong));
    }

    [Fact]
    public void ValidateReserved_RejectsApprovalOrWrongExecutionLocation()
    {
        var now = DateTimeOffset.UtcNow;
        var approved = CreateReserved(now) with { ApprovalScope = "must-not-cross-cloud-boundary" };
        Assert.Throws<InvalidOperationException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(approved));

        var remote = CreateReserved(now) with { ExecutionLocation = JobExecutionLocation.NebiusServerless };
        Assert.Throws<InvalidOperationException>(() =>
            RemoteResearchReservationTrustValidator.ValidateReserved(remote));
    }

    private static AgentJobRecord CreateReserved(DateTimeOffset now)
    {
        var checkpoint = new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{\"plan\":true}", now);
        var provenance = new RemoteResearchProvenance(
            ResearchWorkItemProtector.ProtocolVersion,
            "opaque-work-item-test",
            null,
            checkpoint.Step,
            checkpoint.SavedAt,
            now,
            RemoteResearchProvenanceState.DispatchReserved,
            now.AddHours(1));
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Low,
                false,
                true),
            AgentJobState.Running,
            JobExecutionLocation.Local,
            0,
            checkpoint,
            null,
            null,
            now,
            now,
            RemoteResearch: provenance,
            RemoteWorkItemEnvelopeSha256: new string('a', 64));
    }
}
