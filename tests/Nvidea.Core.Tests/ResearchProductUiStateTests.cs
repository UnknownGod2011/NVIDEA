using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class ResearchProductUiStateTests
{
    [Fact]
    public void LocalPending_EnablesOnlyValidLocalActions()
    {
        var status = Status(
            stage: ResearchJobStage.Planning,
            state: AgentJobState.Pending,
            location: JobExecutionLocation.Local,
            canRun: true,
            canRecover: false,
            canCancel: true,
            requiresRemoteReconciliation: false);

        var ui = ResearchProductUiState.Project(
            status,
            runtimeAvailable: true,
            operationInProgress: false,
            hasActiveJob: true,
            remoteLifecycleAvailable: false,
            remoteDispatchEnabled: false);

        Assert.True(ui.StartEnabled);
        Assert.True(ui.ResumeEnabled);
        Assert.Equal(ResearchProductUiState.ResumeNextStageLabel, ui.ResumeLabel);
        Assert.False(ui.ReconcileEnabled);
        Assert.True(ui.CancelEnabled);
        Assert.Contains("Cloud execution is locked", ui.CloudDisclosureText, StringComparison.Ordinal);
    }

    [Fact]
    public void InterruptedLocal_UsesExplicitRearmLabel()
    {
        var status = Status(
            stage: ResearchJobStage.Interrupted,
            state: AgentJobState.Running,
            location: JobExecutionLocation.Local,
            canRun: false,
            canRecover: true,
            canCancel: true,
            requiresRemoteReconciliation: false);

        var ui = ResearchProductUiState.Project(status, true, false, true, false, false);

        Assert.True(ui.ResumeEnabled);
        Assert.Equal(ResearchProductUiState.RearmInterruptedStageLabel, ui.ResumeLabel);
        Assert.False(ui.ReconcileEnabled);
    }

    [Fact]
    public void DispatchReserved_WithoutLifecycle_BlocksReplayReconcileAndCancel()
    {
        var status = Status(
            stage: ResearchJobStage.Planning,
            state: AgentJobState.Running,
            location: JobExecutionLocation.Local,
            canRun: false,
            canRecover: false,
            canCancel: false,
            requiresRemoteReconciliation: true);

        var ui = ResearchProductUiState.Project(status, true, false, true, false, false);

        Assert.False(ui.ResumeEnabled);
        Assert.False(ui.ReconcileEnabled);
        Assert.False(ui.CancelEnabled);
    }

    [Fact]
    public void Dispatched_WithLifecycle_AllowsReconcileAndProviderCancellationButNeverLocalResume()
    {
        var status = Status(
            stage: ResearchJobStage.GatheringEvidence,
            state: AgentJobState.Running,
            location: JobExecutionLocation.NebiusServerless,
            canRun: false,
            canRecover: false,
            canCancel: true,
            requiresRemoteReconciliation: true);

        var ui = ResearchProductUiState.Project(status, true, false, true, true, false);

        Assert.False(ui.ResumeEnabled);
        Assert.True(ui.ReconcileEnabled);
        Assert.True(ui.CancelEnabled);
        Assert.Contains("new Serverless dispatch remains locked", ui.CloudDisclosureText, StringComparison.Ordinal);
    }

    [Fact]
    public void CancelRequested_WithLifecycle_AllowsOnlyReconciliation()
    {
        var status = Status(
            stage: ResearchJobStage.Synthesizing,
            state: AgentJobState.Running,
            location: JobExecutionLocation.NebiusServerless,
            canRun: false,
            canRecover: false,
            canCancel: false,
            requiresRemoteReconciliation: true);

        var ui = ResearchProductUiState.Project(status, true, false, true, true, false);

        Assert.False(ui.ResumeEnabled);
        Assert.True(ui.ReconcileEnabled);
        Assert.False(ui.CancelEnabled);
    }

    [Fact]
    public void TerminalJob_DisablesLifecycleMutations()
    {
        var status = Status(
            stage: ResearchJobStage.Completed,
            state: AgentJobState.Completed,
            location: JobExecutionLocation.Local,
            canRun: false,
            canRecover: false,
            canCancel: false,
            requiresRemoteReconciliation: false,
            terminal: true);

        var ui = ResearchProductUiState.Project(status, true, false, true, true, false);

        Assert.True(ui.StartEnabled);
        Assert.False(ui.ResumeEnabled);
        Assert.False(ui.ReconcileEnabled);
        Assert.False(ui.CancelEnabled);
    }

    [Fact]
    public void BusyState_DisablesMutationStarts_AndOnlyKeepsActiveJobCancellation()
    {
        var active = ResearchProductUiState.Project(
            status: null,
            runtimeAvailable: true,
            operationInProgress: true,
            hasActiveJob: true,
            remoteLifecycleAvailable: false,
            remoteDispatchEnabled: false);
        var noJob = ResearchProductUiState.Project(
            status: null,
            runtimeAvailable: true,
            operationInProgress: true,
            hasActiveJob: false,
            remoteLifecycleAvailable: false,
            remoteDispatchEnabled: false);

        Assert.False(active.StartEnabled);
        Assert.False(active.ResumeEnabled);
        Assert.False(active.ReconcileEnabled);
        Assert.True(active.CancelEnabled);
        Assert.False(noJob.CancelEnabled);
    }

    [Fact]
    public void DispatchEnabledWithoutLifecycle_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => ResearchProductUiState.Project(
            status: null,
            runtimeAvailable: true,
            operationInProgress: false,
            hasActiveJob: false,
            remoteLifecycleAvailable: false,
            remoteDispatchEnabled: true));
    }

    private static ResearchJobStatus Status(
        ResearchJobStage stage,
        AgentJobState state,
        JobExecutionLocation location,
        bool canRun,
        bool canRecover,
        bool canCancel,
        bool requiresRemoteReconciliation,
        bool terminal = false) =>
        new(
            Guid.NewGuid(),
            stage,
            state,
            location,
            Attempt: 1,
            UpdatedAt: DateTimeOffset.UtcNow,
            NextAttemptAt: null,
            CanRunNextStep: canRun,
            CanRecoverInterrupted: canRecover,
            CanCancel: canCancel,
            IsTerminal: terminal,
            DisplayText: "test")
        {
            RequiresRemoteReconciliation = requiresRemoteReconciliation
        };
}
