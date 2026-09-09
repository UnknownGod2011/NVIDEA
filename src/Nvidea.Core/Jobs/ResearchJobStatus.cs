namespace Nvidea.Core.Jobs;

public enum ResearchJobStage
{
    Requested,
    Planning,
    GatheringEvidence,
    Synthesizing,
    Interrupted,
    Completed,
    WaitingToRetry,
    Cancelled,
    Failed,
    Unknown
}

/// <summary>
/// Privacy-safe projection of a durable research job for desktop status UI.
/// Deliberately excludes checkpoint payloads, source URLs/content, query text,
/// approval grants and error details that may contain provider or user data.
/// </summary>
public sealed record ResearchJobStatus(
    Guid JobId,
    ResearchJobStage Stage,
    AgentJobState State,
    JobExecutionLocation ExecutionLocation,
    int Attempt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? NextAttemptAt,
    bool CanRunNextStep,
    bool CanRecoverInterrupted,
    bool CanCancel,
    bool IsTerminal,
    string DisplayText)
{
    public static readonly TimeSpan InterruptedRecoveryDelay = TimeSpan.FromSeconds(30);

    public static ResearchJobStatus FromRecord(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!string.Equals(record.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Only durable research jobs can be projected as research status.");

        var terminal = record.State is AgentJobState.Completed or AgentJobState.Cancelled or AgentJobState.Failed;
        var canCancel = !terminal;
        var canRun = record.State switch
        {
            AgentJobState.Pending => true,
            AgentJobState.RetryScheduled => record.NextAttemptAt is null || record.NextAttemptAt <= DateTimeOffset.UtcNow,
            _ => false
        };
        var canRecoverInterrupted = CanRecoverInterrupted(record, DateTimeOffset.UtcNow);

        var stage = ResolveStage(record);
        return new ResearchJobStatus(
            record.JobId,
            stage,
            record.State,
            record.ExecutionLocation,
            record.Attempt,
            record.UpdatedAt,
            record.NextAttemptAt,
            canRun,
            canRecoverInterrupted,
            canCancel,
            terminal,
            Display(record, stage));
    }

    internal static bool CanRecoverInterrupted(AgentJobRecord record, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(record);
        return record.State == AgentJobState.Running
            && record.ExecutionLocation == JobExecutionLocation.Local
            && record.Attempt < record.Definition.MaxAttempts
            && record.ApprovalScope is null
            && IsRecoverableCheckpoint(record.Checkpoint?.Step)
            && record.UpdatedAt <= now - InterruptedRecoveryDelay;
    }

    internal static bool IsRecoverableCheckpoint(string? step) => step is
        ResearchJobHandler.RequestedStep or
        ResearchJobHandler.PlannedStep or
        ResearchJobHandler.EvidenceStep;

    private static ResearchJobStage ResolveStage(AgentJobRecord record)
    {
        if (record.State == AgentJobState.Completed) return ResearchJobStage.Completed;
        if (record.State == AgentJobState.Cancelled) return ResearchJobStage.Cancelled;
        if (record.State == AgentJobState.Failed) return ResearchJobStage.Failed;
        if (record.State == AgentJobState.RetryScheduled) return ResearchJobStage.WaitingToRetry;
        if (record.State == AgentJobState.Running && IsRecoverableCheckpoint(record.Checkpoint?.Step))
            return ResearchJobStage.Interrupted;

        return ResolveCheckpointStage(record.Checkpoint?.Step);
    }

    private static ResearchJobStage ResolveCheckpointStage(string? step) => step switch
    {
        ResearchJobHandler.RequestedStep => ResearchJobStage.Planning,
        ResearchJobHandler.PlannedStep => ResearchJobStage.GatheringEvidence,
        ResearchJobHandler.EvidenceStep => ResearchJobStage.Synthesizing,
        ResearchJobHandler.CompletedStep => ResearchJobStage.Completed,
        null => ResearchJobStage.Requested,
        _ => ResearchJobStage.Unknown
    };

    private static string Display(AgentJobRecord record, ResearchJobStage stage)
    {
        if (stage == ResearchJobStage.Interrupted)
        {
            var interruptedStage = ResolveCheckpointStage(record.Checkpoint?.Step) switch
            {
                ResearchJobStage.Planning => "Nemotron planning",
                ResearchJobStage.GatheringEvidence => "Tavily evidence gathering",
                ResearchJobStage.Synthesizing => "Nemotron synthesis",
                _ => "research work"
            };
            return $"Interrupted during {interruptedStage} — explicit retry may repeat provider work/cost";
        }

        return (stage, record.State) switch
        {
            (ResearchJobStage.Planning, AgentJobState.Running) => "Planning with Nemotron…",
            (ResearchJobStage.Planning, _) => "Ready to plan with Nemotron",
            (ResearchJobStage.GatheringEvidence, AgentJobState.Running) => "Searching and extracting with Tavily…",
            (ResearchJobStage.GatheringEvidence, _) => "Ready to gather Tavily evidence",
            (ResearchJobStage.Synthesizing, AgentJobState.Running) => "Synthesizing verified evidence with Nemotron…",
            (ResearchJobStage.Synthesizing, _) => "Ready to synthesize saved evidence",
            (ResearchJobStage.WaitingToRetry, _) => "Paused after a recoverable error",
            (ResearchJobStage.Completed, _) => "Research complete",
            (ResearchJobStage.Cancelled, _) => "Research cancelled",
            (ResearchJobStage.Failed, _) => "Research failed",
            (ResearchJobStage.Requested, _) => "Research request saved",
            _ => "Research state needs review"
        };
    }
}
