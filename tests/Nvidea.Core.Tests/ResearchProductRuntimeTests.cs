using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchProductRuntimeTests
{
    [Fact]
    public async Task Remote_dispatch_is_feature_gated_before_cloud_invocation()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(RemoteResearchProvenanceState.ResultApplied, AgentJobState.Pending, JobExecutionLocation.Local);
            await SaveAsync(directory, record);
            var local = new RecordingLocalRuntime(record);
            var cloud = new RecordingCloudCoordinator(record);
            var product = new ResearchProductRuntime(directory, local, cloud, remoteDispatchEnabled: false);
            var authorization = new ResearchCloudAuthorization(
                record.JobId,
                record.Checkpoint!.Step,
                Approved: true,
                ResearchWorkItemProtector.DisclosureVersion,
                DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                product.DispatchCurrentStageAsync(record.JobId, authorization));

            Assert.Equal(0, cloud.DispatchCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Ambiguous_remote_reservation_cannot_route_to_local_execution()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(RemoteResearchProvenanceState.DispatchReserved, AgentJobState.Running, JobExecutionLocation.Local);
            await SaveAsync(directory, record);
            var local = new RecordingLocalRuntime(record);
            var cloud = new RecordingCloudCoordinator(record);
            var product = new ResearchProductRuntime(directory, local, cloud);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                product.RunNextLocalStepAsync(record.JobId));

            Assert.Equal(0, local.RunCalls);
            Assert.Equal(0, cloud.ReconcileCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Ambiguous_remote_reservation_routes_only_to_reconciliation()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(RemoteResearchProvenanceState.DispatchReserved, AgentJobState.Running, JobExecutionLocation.Local);
            await SaveAsync(directory, record);
            var local = new RecordingLocalRuntime(record);
            var cloud = new RecordingCloudCoordinator(record);
            var product = new ResearchProductRuntime(directory, local, cloud);

            var status = await product.ReconcileRemoteAsync(record.JobId);

            Assert.Equal(record.JobId, status.JobId);
            Assert.Equal(1, cloud.ReconcileCalls);
            Assert.Equal(0, local.RunCalls);
            Assert.Equal(0, local.CancelCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Dispatched_remote_research_uses_provider_aware_cancellation()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(RemoteResearchProvenanceState.Dispatched, AgentJobState.Running, JobExecutionLocation.NebiusServerless);
            await SaveAsync(directory, record);
            var local = new RecordingLocalRuntime(record);
            var cloud = new RecordingCloudCoordinator(record);
            var product = new ResearchProductRuntime(directory, local, cloud);

            await product.CancelAsync(record.JobId);

            Assert.Equal(1, cloud.CancelCalls);
            Assert.Equal(0, local.CancelCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Local_research_uses_local_cancellation()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(remoteState: null, AgentJobState.Pending, JobExecutionLocation.Local);
            await SaveAsync(directory, record);
            var local = new RecordingLocalRuntime(record);
            var cloud = new RecordingCloudCoordinator(record);
            var product = new ResearchProductRuntime(directory, local, cloud);

            await product.CancelAsync(record.JobId);

            Assert.Equal(1, local.CancelCalls);
            Assert.Equal(0, cloud.CancelCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Status_reads_remote_record_without_calling_local_only_status_path()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(RemoteResearchProvenanceState.Dispatched, AgentJobState.Running, JobExecutionLocation.NebiusServerless);
            await SaveAsync(directory, record);
            var local = new RecordingLocalRuntime(record);
            var product = new ResearchProductRuntime(directory, local, new RecordingCloudCoordinator(record));

            var status = await product.GetStatusAsync(record.JobId);

            Assert.Equal(JobExecutionLocation.NebiusServerless, status.ExecutionLocation);
            Assert.Equal(0, local.GetStatusCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static AgentJobRecord CreateRecord(
        RemoteResearchProvenanceState? remoteState,
        AgentJobState state,
        JobExecutionLocation location)
    {
        var now = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid();
        var checkpoint = new AgentJobCheckpoint(ResearchJobHandler.RequestedStep, "{}", now);
        RemoteResearchProvenance? provenance = remoteState is null
            ? null
            : new RemoteResearchProvenance(
                ResearchWorkItemProtector.ProtocolVersion,
                "opaque-work-item-id-abcdefghijklmnop",
                remoteState == RemoteResearchProvenanceState.DispatchReserved ? null : "job-e00example",
                checkpoint.Step,
                checkpoint.SavedAt,
                now,
                remoteState.Value,
                WorkItemExpiresAt: now.AddHours(1));

        return new AgentJobRecord(
            jobId,
            new AgentJobDefinition(
                ResearchJobHandler.Type,
                ResearchJobRuntime.CapabilityId,
                new HashSet<DataPermission> { DataPermission.NetworkAccess },
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: false,
                MaxAttempts: 3),
            state,
            location,
            Attempt: 0,
            checkpoint,
            ApprovalScope: null,
            LastError: null,
            CreatedAt: now,
            UpdatedAt: now,
            RemoteResearch: provenance);
    }

    private static Task SaveAsync(string directory, AgentJobRecord record) =>
        new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json")).SaveAsync(record);

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-product-research-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private sealed class RecordingLocalRuntime : ILocalResearchRuntime
    {
        private readonly AgentJobRecord _record;

        public RecordingLocalRuntime(AgentJobRecord record) => _record = record;

        public int RunCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public int GetStatusCalls { get; private set; }

        public Task<ResearchJobStatus> CreateAsync(string question, CancellationToken cancellationToken = default) =>
            Task.FromResult(ResearchJobStatus.FromRecord(_record));

        public Task<IReadOnlyList<ResearchJobStatus>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ResearchJobStatus>>(new[] { ResearchJobStatus.FromRecord(_record) });

        public Task<ResearchJobStatus> RunNextStepAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            RunCalls++;
            return Task.FromResult(ResearchJobStatus.FromRecord(_record));
        }

        public Task<ResearchJobStatus> RecoverInterruptedAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ResearchJobStatus.FromRecord(_record));

        public Task<ResearchJobStatus> CancelAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            return Task.FromResult(ResearchJobStatus.FromRecord(_record));
        }

        public Task<ResearchReport> ReadCompletedReportAsync(Guid jobId, CancellationToken cancellationToken = default) =>
            Task.FromException<ResearchReport>(new InvalidOperationException("Not used by this test."));

        public Task<ResearchJobStatus> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            GetStatusCalls++;
            return Task.FromResult(ResearchJobStatus.FromRecord(_record));
        }
    }

    private sealed class RecordingCloudCoordinator : IResearchCloudExecutionCoordinator
    {
        private readonly AgentJobRecord _record;

        public RecordingCloudCoordinator(AgentJobRecord record) => _record = record;

        public int DispatchCalls { get; private set; }
        public int ReconcileCalls { get; private set; }
        public int CancelCalls { get; private set; }

        public Task<ResearchJobStatus> DispatchCurrentStageAsync(
            Guid jobId,
            ResearchCloudAuthorization authorization,
            TimeSpan? workItemLifetime = null,
            CancellationToken cancellationToken = default)
        {
            DispatchCalls++;
            return Task.FromResult(ResearchJobStatus.FromRecord(_record));
        }

        public Task<ResearchJobStatus> ReconcileAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            ReconcileCalls++;
            return Task.FromResult(ResearchJobStatus.FromRecord(_record));
        }

        public Task<ResearchJobStatus> RequestCancellationAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            CancelCalls++;
            return Task.FromResult(ResearchJobStatus.FromRecord(_record));
        }
    }
}
