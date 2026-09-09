using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchDispatchBindingCleanupTests
{
    [Fact]
    public async Task ResultApplied_LocalStage_DeletesBinding()
    {
        var transport = new RecordingBindingTransport();
        var cleanup = new ResearchDispatchBindingCleanup(transport);
        var job = CreateJob(AgentJobState.Pending, JobExecutionLocation.Local, RemoteResearchProvenanceState.ResultApplied);

        await cleanup.TryCleanupIfTerminalAsync(job);

        Assert.Equal(job.RemoteResearch!.OpaqueWorkItemId, Assert.Single(transport.Deleted));
    }

    [Theory]
    [InlineData(RemoteResearchProvenanceState.Cancelled, AgentJobState.Cancelled)]
    [InlineData(RemoteResearchProvenanceState.RemoteFailed, AgentJobState.Failed)]
    [InlineData(RemoteResearchProvenanceState.Expired, AgentJobState.Failed)]
    public async Task TerminalProviderState_DeletesBinding(
        RemoteResearchProvenanceState provenanceState,
        AgentJobState jobState)
    {
        var transport = new RecordingBindingTransport();
        var cleanup = new ResearchDispatchBindingCleanup(transport);

        await cleanup.TryCleanupIfTerminalAsync(CreateJob(jobState, JobExecutionLocation.Local, provenanceState));

        Assert.Single(transport.Deleted);
    }

    [Theory]
    [InlineData(RemoteResearchProvenanceState.DispatchReserved)]
    [InlineData(RemoteResearchProvenanceState.Dispatched)]
    [InlineData(RemoteResearchProvenanceState.CancelRequested)]
    public async Task NonTerminalRemoteState_NeverDeletesBinding(RemoteResearchProvenanceState provenanceState)
    {
        var transport = new RecordingBindingTransport();
        var cleanup = new ResearchDispatchBindingCleanup(transport);
        var location = provenanceState == RemoteResearchProvenanceState.DispatchReserved
            ? JobExecutionLocation.Local
            : JobExecutionLocation.NebiusServerless;

        await cleanup.TryCleanupIfTerminalAsync(CreateJob(AgentJobState.Running, location, provenanceState));

        Assert.Empty(transport.Deleted);
    }

    [Fact]
    public async Task ResultApplied_ButStillRemote_NeverDeletesBinding()
    {
        var transport = new RecordingBindingTransport();
        var cleanup = new ResearchDispatchBindingCleanup(transport);
        var job = CreateJob(AgentJobState.Pending, JobExecutionLocation.NebiusServerless, RemoteResearchProvenanceState.ResultApplied);

        await cleanup.TryCleanupIfTerminalAsync(job);

        Assert.Empty(transport.Deleted);
    }

    [Fact]
    public async Task CleanupFailure_DoesNotInvalidateDurableTerminalState()
    {
        var cleanup = new ResearchDispatchBindingCleanup(new ThrowingBindingTransport());
        var job = CreateJob(AgentJobState.Completed, JobExecutionLocation.Local, RemoteResearchProvenanceState.ResultApplied);

        await cleanup.TryCleanupIfTerminalAsync(job);

        Assert.Equal(AgentJobState.Completed, job.State);
        Assert.Equal(RemoteResearchProvenanceState.ResultApplied, job.RemoteResearch!.State);
    }

    private static AgentJobRecord CreateJob(
        AgentJobState state,
        JobExecutionLocation location,
        RemoteResearchProvenanceState provenanceState)
    {
        var now = DateTimeOffset.UtcNow;
        var provenance = new RemoteResearchProvenance(
            ResearchWorkItemProtector.ProtocolVersion,
            "abcdefghijklmnopqrstuvwx12345678",
            provenanceState == RemoteResearchProvenanceState.DispatchReserved ? null : "job-123",
            "retrieved",
            now.AddMinutes(-2),
            now.AddMinutes(-1),
            provenanceState,
            provenanceState == RemoteResearchProvenanceState.ResultApplied ? now : null,
            now.AddHours(1),
            provenanceState is RemoteResearchProvenanceState.Cancelled or RemoteResearchProvenanceState.RemoteFailed or RemoteResearchProvenanceState.Expired ? now : null);

        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Medium,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true),
            state,
            location,
            1,
            new AgentJobCheckpoint("retrieved", "{}", now.AddMinutes(-2)),
            ApprovalScope: null,
            LastError: null,
            now.AddMinutes(-5),
            now,
            RemoteResearch: provenance);
    }

    private sealed class RecordingBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        public List<string> Deleted { get; } = new();

        public Task PutAsync(ProtectedResearchDispatchBinding binding, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ProtectedResearchDispatchBinding?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchDispatchBinding?>(null);

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default)
        {
            Deleted.Add(opaqueWorkItemId);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingBindingTransport : IProtectedResearchDispatchBindingTransport
    {
        public Task PutAsync(ProtectedResearchDispatchBinding binding, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ProtectedResearchDispatchBinding?> GetAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProtectedResearchDispatchBinding?>(null);

        public Task DeleteAsync(string opaqueWorkItemId, CancellationToken cancellationToken = default) =>
            throw new IOException("simulated shared transport cleanup failure");
    }
}
