using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchCloudExecutionCoordinatorTests
{
    [Fact]
    public async Task Dispatch_requires_exact_stage_approval_before_remote_runtime_is_invoked()
    {
        var directory = CreateDirectory();
        try
        {
            var local = CreateLocalRuntime(directory);
            var created = await local.CreateAsync("Cloud-stage safety");
            var remote = new RecordingRemoteRuntime(directory);
            var coordinator = new ResearchCloudExecutionCoordinator(directory, remote);
            var wrongApproval = new ResearchCloudAuthorization(
                created.JobId,
                ResearchJobHandler.PlannedStep,
                Approved: true,
                ResearchWorkItemProtector.DisclosureVersion,
                DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.DispatchCurrentStageAsync(created.JobId, wrongApproval));

            Assert.Equal(0, remote.DispatchCalls);
            var persisted = await new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json")).GetAsync(created.JobId);
            Assert.NotNull(persisted);
            Assert.Equal(AgentJobState.Pending, persisted!.State);
            Assert.Equal(JobExecutionLocation.Local, persisted.ExecutionLocation);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Dispatch_projects_active_nebius_stage_without_false_interrupted_status()
    {
        var directory = CreateDirectory();
        try
        {
            var local = CreateLocalRuntime(directory);
            var created = await local.CreateAsync("Run this stage remotely");
            var remote = new RecordingRemoteRuntime(directory);
            var coordinator = new ResearchCloudExecutionCoordinator(directory, remote);
            var approval = new ResearchCloudAuthorization(
                created.JobId,
                ResearchJobHandler.RequestedStep,
                Approved: true,
                ResearchWorkItemProtector.DisclosureVersion,
                DateTimeOffset.UtcNow);

            var status = await coordinator.DispatchCurrentStageAsync(created.JobId, approval);

            Assert.Equal(1, remote.DispatchCalls);
            Assert.NotNull(remote.LastWorkItem);
            Assert.Equal(created.JobId, remote.LastWorkItem!.LocalJobId);
            Assert.Equal(ResearchJobHandler.RequestedStep, remote.LastWorkItem.CheckpointStep);
            Assert.False(remote.LastWorkItem.ContainsPrivateOsData);
            Assert.Equal(JobExecutionLocation.NebiusServerless, status.ExecutionLocation);
            Assert.Equal(AgentJobState.Running, status.State);
            Assert.Equal(ResearchJobStage.Planning, status.Stage);
            Assert.False(status.CanRecoverInterrupted);
            Assert.False(status.CanRunNextStep);
            Assert.Contains("Nebius Serverless", status.DisplayText, StringComparison.Ordinal);
            Assert.DoesNotContain("Interrupted", status.DisplayText, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Private_os_data_fails_closed_before_remote_runtime_is_invoked()
    {
        var directory = CreateDirectory();
        try
        {
            var local = CreateLocalRuntime(directory);
            var created = await local.CreateAsync("Local-only stage");
            var store = new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json"));
            var record = await store.GetAsync(created.JobId) ?? throw new InvalidOperationException("Expected research job.");
            await store.SaveAsync(record with
            {
                Definition = record.Definition with { ContainsPrivateOsData = true },
                UpdatedAt = DateTimeOffset.UtcNow
            });

            var remote = new RecordingRemoteRuntime(directory);
            var coordinator = new ResearchCloudExecutionCoordinator(directory, remote);
            var approval = new ResearchCloudAuthorization(
                created.JobId,
                ResearchJobHandler.RequestedStep,
                Approved: true,
                ResearchWorkItemProtector.DisclosureVersion,
                DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.DispatchCurrentStageAsync(created.JobId, approval));

            Assert.Equal(0, remote.DispatchCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Invalid_work_item_lifetime_fails_before_remote_runtime_is_invoked()
    {
        var directory = CreateDirectory();
        try
        {
            var local = CreateLocalRuntime(directory);
            var created = await local.CreateAsync("Bounded remote stage");
            var remote = new RecordingRemoteRuntime(directory);
            var coordinator = new ResearchCloudExecutionCoordinator(directory, remote);
            var approval = new ResearchCloudAuthorization(
                created.JobId,
                ResearchJobHandler.RequestedStep,
                Approved: true,
                ResearchWorkItemProtector.DisclosureVersion,
                DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                coordinator.DispatchCurrentStageAsync(
                    created.JobId,
                    approval,
                    ResearchWorkItemProtector.MaxLifetime + TimeSpan.FromSeconds(1)));

            Assert.Equal(0, remote.DispatchCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Reconcile_routes_dispatch_reserved_state_without_local_stage_execution()
    {
        var directory = CreateDirectory();
        try
        {
            var local = CreateLocalRuntime(directory);
            var created = await local.CreateAsync("Recover ambiguous dispatch");
            var store = new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json"));
            var record = await store.GetAsync(created.JobId) ?? throw new InvalidOperationException("Expected research job.");
            var checkpoint = record.Checkpoint ?? throw new InvalidOperationException("Expected checkpoint.");
            var provenance = new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                "opaque-test-id-abcdefghijklmnop",
                null,
                checkpoint.Step,
                checkpoint.SavedAt,
                DateTimeOffset.UtcNow,
                RemoteResearchProvenanceState.DispatchReserved,
                WorkItemExpiresAt: DateTimeOffset.UtcNow.AddHours(1));
            await store.SaveAsync(record with
            {
                State = AgentJobState.Running,
                RemoteResearch = provenance,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            var remote = new RecordingRemoteRuntime(directory);
            var coordinator = new ResearchCloudExecutionCoordinator(directory, remote);
            var status = await coordinator.ReconcileAsync(created.JobId);

            Assert.Equal(1, remote.ReconcileReservedCalls);
            Assert.Equal(0, remote.DispatchCalls);
            Assert.Equal(created.JobId, status.JobId);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static ResearchJobRuntime CreateLocalRuntime(string directory) =>
        new(
            directory,
            new ResearchEngine(new NeverInferenceClient(), new NeverResearchProvider()),
            new MemoryAuditTrail());

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-cloud-coordinator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private sealed class RecordingRemoteRuntime : IRemoteResearchClientRuntime
    {
        private readonly JsonAgentJobStore _store;

        public RecordingRemoteRuntime(string directory)
        {
            _store = new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json"));
        }

        public int DispatchCalls { get; private set; }
        public int ReconcileReservedCalls { get; private set; }
        public RemoteResearchWorkItem? LastWorkItem { get; private set; }

        public async Task<AgentJobRecord> DispatchAsync(RemoteResearchWorkItem workItem, ResearchCloudAuthorization authorization, CancellationToken cancellationToken = default)
        {
            DispatchCalls++;
            LastWorkItem = workItem;
            var current = await _store.GetAsync(workItem.LocalJobId, cancellationToken) ?? throw new InvalidOperationException("Expected job.");
            var checkpoint = current.Checkpoint ?? throw new InvalidOperationException("Expected checkpoint.");
            var provenance = new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                "opaque-test-id-abcdefghijklmnop",
                "job-test-123",
                checkpoint.Step,
                checkpoint.SavedAt,
                DateTimeOffset.UtcNow,
                RemoteResearchProvenanceState.Dispatched,
                WorkItemExpiresAt: workItem.ExpiresAt);
            var updated = current with
            {
                State = AgentJobState.Running,
                ExecutionLocation = JobExecutionLocation.NebiusServerless,
                Attempt = current.Attempt + 1,
                RemoteResearch = provenance,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _store.SaveAsync(updated, cancellationToken);
            return updated;
        }

        public async Task<AgentJobRecord> ReconcileReservedAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            ReconcileReservedCalls++;
            return await _store.GetAsync(jobId, cancellationToken) ?? throw new InvalidOperationException("Expected job.");
        }

        public Task<AgentJobRecord> ReconcileDispatchedAsync(Guid jobId, DateTimeOffset? now = null, CancellationToken cancellationToken = default) =>
            GetAsync(jobId, cancellationToken);

        public Task<AgentJobRecord> RequestCancellationAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            GetAsync(jobId, cancellationToken);

        public Task<AgentJobRecord> ReconcileCancellationAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            GetAsync(jobId, cancellationToken);

        private async Task<AgentJobRecord> GetAsync(Guid jobId, CancellationToken cancellationToken) =>
            await _store.GetAsync(jobId, cancellationToken) ?? throw new InvalidOperationException("Expected job.");
    }

    private sealed class NeverInferenceClient : IAgentInferenceClient
    {
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Local inference must not run in coordinator tests.");
    }

    private sealed class NeverResearchProvider : IResearchProvider
    {
        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Local research provider must not run in coordinator tests.");
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }
}
