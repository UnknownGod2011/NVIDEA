namespace Nvidea.Core.Jobs;

public enum ResearchJobStage
{
    Requested,
    Planning,
    GatheringEvidence,
    Synthesizing,
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
    int Attempt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? NextAttemptAt,
    bool CanRunNextStep,
    bool CanCancel,
    bool IsTerminal,
    string DisplayText)
{
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

        var stage = ResolveStage(record);
        return new ResearchJobStatus(
            record.JobId,
            stage,
            record.State,
            record.Attempt,
            record.UpdatedAt,
            record.NextAttemptAt,
            canRun,
            canCancel,
            terminal,
            Display(stage, record.State));
    }

    private static ResearchJobStage ResolveStage(AgentJobRecord record)
    {
        if (record.State == AgentJobState.Completed) return ResearchJobStage.Completed;
        if (record.State == AgentJobState.Cancelled) return ResearchJobStage.Cancelled;
        if (record.State == AgentJobState.Failed) return ResearchJobStage.Failed;
        if (record.State == AgentJobState.RetryScheduled) return ResearchJobStage.WaitingToRetry;

        return record.Checkpoint?.Step switch
        {
            ResearchJobHandler.RequestedStep => ResearchJobStage.Planning,
            ResearchJobHandler.PlannedStep => ResearchJobStage.GatheringEvidence,
            ResearchJobHandler.EvidenceStep => ResearchJobStage.Synthesizing,
            ResearchJobHandler.CompletedStep => ResearchJobStage.Completed,
            null => ResearchJobStage.Requested,
            _ => ResearchJobStage.Unknown
        };
    }

    private static string Display(ResearchJobStage stage, AgentJobState state) => (stage, state) switch
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
