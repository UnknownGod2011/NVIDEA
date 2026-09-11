using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchDispatchUiStateTests
{
    [Fact]
    public void PendingLocalCheckpoint_WithDispatchEnabled_OffersOneShotDispatch()
    {
        var status = Status(containsPrivateOsData: false, checkpointStep: "requested");

        var ui = ResearchProductUiState.Project(
            status,
            runtimeAvailable: true,
            operationInProgress: false,
            hasActiveJob: true,
            remoteLifecycleAvailable: true,
            remoteDispatchEnabled: true);

        Assert.True(ui.DispatchEnabled);
        Assert.Contains("one-shot approval", ui.CloudDisclosureText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrivateOsData_NeverOffersCloudDispatch()
    {
        var status = Status(containsPrivateOsData: true, checkpointStep: "requested");

        var ui = ResearchProductUiState.Project(status, true, false, true, true, true);

        Assert.False(ui.DispatchEnabled);
        Assert.True(ui.ResumeEnabled);
    }

    [Fact]
    public void MissingCheckpoint_NeverOffersCloudDispatch()
    {
        var status = Status(containsPrivateOsData: false, checkpointStep: null);

        var ui = ResearchProductUiState.Project(status, true, false, true, true, true);

        Assert.False(ui.DispatchEnabled);
    }

    [Fact]
    public void BusyOperation_NeverOffersSecondCloudDispatch()
    {
        var status = Status(containsPrivateOsData: false, checkpointStep: "requested");

        var ui = ResearchProductUiState.Project(status, true, true, true, true, true);

        Assert.False(ui.DispatchEnabled);
    }

    [Fact]
    public void RemoteReconciliationRequired_NeverOffersNewDispatch()
    {
        var status = Status(containsPrivateOsData: false, checkpointStep: "planned") with
        {
            RequiresRemoteReconciliation = true
        };

        var ui = ResearchProductUiState.Project(status, true, false, true, true, true);

        Assert.False(ui.DispatchEnabled);
    }

    private static ResearchJobStatus Status(bool containsPrivateOsData, string? checkpointStep) =>
        new(
            JobId: Guid.NewGuid(),
            Stage: ResearchJobStage.Planning,
            State: AgentJobState.Pending,
            ExecutionLocation: JobExecutionLocation.Local,
            Attempt: 0,
            UpdatedAt: DateTimeOffset.UtcNow,
            NextAttemptAt: null,
            CanRunNextStep: true,
            CanRecoverInterrupted: false,
            CanCancel: true,
            IsTerminal: false,
            DisplayText: "Ready to plan with Nemotron")
        {
            RequiresRemoteReconciliation = false,
            CheckpointStep = checkpointStep,
            ContainsPrivateOsData = containsPrivateOsData
        };
}
