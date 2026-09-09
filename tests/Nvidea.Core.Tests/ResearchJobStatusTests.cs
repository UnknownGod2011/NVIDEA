using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchJobStatusTests
{
    [Fact]
    public void Evidence_checkpoint_projects_to_synthesis_without_exposing_payload()
    {
        var record = CreateRecord(
            AgentJobState.Pending,
            new AgentJobCheckpoint(ResearchJobHandler.EvidenceStep, "{\"secret\":\"source text\"}", DateTimeOffset.UtcNow));

        var status = ResearchJobStatus.FromRecord(record);

        Assert.Equal(ResearchJobStage.Synthesizing, status.Stage);
        Assert.True(status.CanRunNextStep);
        Assert.True(status.CanCancel);
        Assert.False(status.IsTerminal);
        Assert.DoesNotContain("secret", status.DisplayText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("source text", status.DisplayText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stale_local_running_research_is_explicitly_recoverable_without_exposing_payload()
    {
        var secretPayload = "{\"question\":\"private acquisition target\",\"source\":\"https://secret.example\"}";
        var record = CreateRecord(
            AgentJobState.Running,
            new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, secretPayload, DateTimeOffset.UtcNow.AddMinutes(-2))) with
        {
            ExecutionLocation = JobExecutionLocation.Local,
            UpdatedAt = DateTimeOffset.UtcNow.Subtract(ResearchJobStatus.InterruptedRecoveryDelay).AddSeconds(-1),
            Attempt = 1
        };

        var status = ResearchJobStatus.FromRecord(record);

        Assert.Equal(ResearchJobStage.Interrupted, status.Stage);
        Assert.True(status.CanRecoverInterrupted);
        Assert.False(status.CanRunNextStep);
        Assert.True(status.CanCancel);
        Assert.Contains("Tavily evidence gathering", status.DisplayText, StringComparison.Ordinal);
        Assert.Contains("repeat provider work/cost", status.DisplayText, StringComparison.Ordinal);
        Assert.DoesNotContain("private acquisition", status.DisplayText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret.example", status.DisplayText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fresh_running_research_is_not_recoverable_during_grace_period()
    {
        var record = CreateRecord(
            AgentJobState.Running,
            new AgentJobCheckpoint(ResearchJobHandler.RequestedStep, "{}", DateTimeOffset.UtcNow)) with
        {
            ExecutionLocation = JobExecutionLocation.Local,
            UpdatedAt = DateTimeOffset.UtcNow,
            Attempt = 1
        };

        var status = ResearchJobStatus.FromRecord(record);

        Assert.Equal(ResearchJobStage.Interrupted, status.Stage);
        Assert.False(status.CanRecoverInterrupted);
        Assert.False(status.CanRunNextStep);
    }

    [Fact]
    public void Retry_is_not_resumable_before_due_time()
    {
        var record = CreateRecord(
            AgentJobState.RetryScheduled,
            new AgentJobCheckpoint(ResearchJobHandler.PlannedStep, "{}", DateTimeOffset.UtcNow)) with
        {
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        var status = ResearchJobStatus.FromRecord(record);

        Assert.Equal(ResearchJobStage.WaitingToRetry, status.Stage);
        Assert.False(status.CanRunNextStep);
        Assert.True(status.CanCancel);
    }

    [Fact]
    public void Completed_job_is_terminal_and_not_cancellable()
    {
        var record = CreateRecord(
            AgentJobState.Completed,
            new AgentJobCheckpoint(ResearchJobHandler.CompletedStep, "{\"answer\":\"private\"}", DateTimeOffset.UtcNow));

        var status = ResearchJobStatus.FromRecord(record);

        Assert.Equal(ResearchJobStage.Completed, status.Stage);
        Assert.True(status.IsTerminal);
        Assert.False(status.CanRunNextStep);
        Assert.False(status.CanCancel);
        Assert.Equal("Research complete", status.DisplayText);
    }

    [Fact]
    public void Non_research_jobs_are_rejected()
    {
        var record = CreateRecord(AgentJobState.Pending, null) with
        {
            Definition = new AgentJobDefinition(
                "browser.action",
                "browser.task",
                new HashSet<DataPermission>(),
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: false)
        };

        Assert.Throws<InvalidOperationException>(() => ResearchJobStatus.FromRecord(record));
    }

    private static AgentJobRecord CreateRecord(AgentJobState state, AgentJobCheckpoint? checkpoint)
    {
        var now = DateTimeOffset.UtcNow;
        return new AgentJobRecord(
            Guid.NewGuid(),
            new AgentJobDefinition(
                ResearchJobHandler.Type,
                "research.deep",
                new HashSet<DataPermission>(),
                CapabilityRiskLevel.Low,
                ContainsPrivateOsData: false,
                BenefitsFromBackgroundExecution: true),
            state,
            JobExecutionLocation.NebiusServerless,
            1,
            checkpoint,
            null,
            null,
            now,
            now);
    }
}
