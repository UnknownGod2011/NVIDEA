using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchLifecycleOnlyRuntimeTests
{
    [Fact]
    public async Task Lifecycle_only_runtime_reconciles_reserved_remote_work_without_local_research()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(RemoteResearchProvenanceState.DispatchReserved, AgentJobState.Running, JobExecutionLocation.Local);
            await SaveAsync(directory, record);
            var cloud = new RecordingCloudCoordinator(record);
            var product = new ResearchProductRuntime(directory, local: null, cloud: cloud);

            var status = await product.ReconcileRemoteAsync(record.JobId);

            Assert.False(product.LocalExecutionAvailable);
            Assert.True(product.RemoteLifecycleAvailable);
            Assert.False(product.RemoteDispatchEnabled);
            Assert.Equal(record.JobId, status.JobId);
            Assert.Equal(1, cloud.ReconcileCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Lifecycle_only_runtime_requests_provider_cancellation_without_local_research()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(RemoteResearchProvenanceState.Dispatched, AgentJobState.Running, JobExecutionLocation.NebiusServerless);
            await SaveAsync(directory, record);
            var cloud = new RecordingCloudCoordinator(record);
            var product = new ResearchProductRuntime(directory, local: null, cloud: cloud);

            await product.CancelAsync(record.JobId);

            Assert.Equal(1, cloud.CancelCalls);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task Lifecycle_only_runtime_blocks_local_execution_and_local_cancellation()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(remoteState: null, AgentJobState.Pending, JobExecutionLocation.Local);
            await SaveAsync(directory, record);
            var product = new ResearchProductRuntime(
                directory,
                local: null,
                cloud: new RecordingCloudCoordinator(record));

            var runError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                product.RunNextLocalStepAsync(record.JobId));
            var cancelError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                product.CancelAsync(record.JobId));
            var createError = Assert.Throws<InvalidOperationException>(() => product.CreateAsync("question"));

            Assert.Contains("TAVILY_API_KEY", runError.Message, StringComparison.Ordinal);
            Assert.Contains("TAVILY_API_KEY", cancelError.Message, StringComparison.Ordinal);
            Assert.Contains("TAVILY_API_KEY", createError.Message, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void New_remote_dispatch_cannot_be_enabled_without_local_research()
    {
        var directory = CreateDirectory();
        try
        {
            var record = CreateRecord(remoteState: null, AgentJobState.Pending, JobExecutionLocation.Local);
            var cloud = new RecordingCloudCoordinator(record);

            var error = Assert.Throws<ArgumentException>(() =>
                new ResearchProductRuntime(directory, local: null, cloud: cloud, remoteDispatchEnabled: true));

            Assert.Contains("local research runtime", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void Ui_projection_keeps_remote_recovery_enabled_but_local_controls_locked()
    {
        var record = CreateRecord(RemoteResearchProvenanceState.Dispatched, AgentJobState.Running, JobExecutionLocation.NebiusServerless);
        var status = ResearchJobStatus.FromRecord(record) with { RequiresRemoteReconciliation = true };

        var ui = ResearchProductUiState.Project(
            status,
            runtimeAvailable: false,
            operationInProgress: false,
            hasActiveJob: true,
            remoteLifecycleAvailable: true,
            remoteDispatchEnabled: false);

        Assert.False(ui.StartEnabled);
        Assert.False(ui.ResumeEnabled);
        Assert.False(ui.DispatchEnabled);
        Assert.True(ui.ReconcileEnabled);
        Assert.True(ui.CancelEnabled);
        Assert.Contains("without local Tavily credentials", ui.CloudDisclosureText, StringComparison.OrdinalIgnoreCase);
    }

    private static AgentJobRecord CreateRecord(
        RemoteResearchProvenanceState? remoteState,
        AgentJobState state,
        JobExecutionLocation location)
    {
        var now = DateTimeOffset.UtcNow;
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
            Guid.NewGuid(),
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
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-lifecycle-only-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private sealed class RecordingCloudCoordinator : IResearchCloudExecutionCoordinator
    {
        private readonly AgentJobRecord _record;

        public RecordingCloudCoordinator(AgentJobRecord record) => _record = record;

        public int ReconcileCalls { get; private set; }
        public int CancelCalls { get; private set; }

        public Task<ResearchJobStatus> DispatchCurrentStageAsync(
            Guid jobId,
            ResearchCloudAuthorization authorization,
            TimeSpan? workItemLifetime = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ResearchJobStatus.FromRecord(_record));

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
