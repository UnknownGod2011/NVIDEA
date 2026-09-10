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

    /// <summary>
    /// True when durable provenance says provider-aware reconciliation is required before any
    /// local retry/recovery can be considered. This intentionally exposes only a lifecycle boolean,
    /// not provider ids, payloads, source data, approval state or errors.
    /// </summary>
    public bool RequiresRemoteReconciliation { get; init; }

    public static ResearchJobStatus FromRecord(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!string.Equals(record.Definition.JobType, ResearchJobHandler.Type, StringComparison.Ordinal))
            throw new InvalidOperationException("Only durable research jobs can be projected as research status.");

        var terminal = record.State is AgentJobState.Completed or AgentJobState.Cancelled or AgentJobState.Failed;
        var remoteState = record.RemoteResearch?.State;
        var canCancel = !terminal
            && remoteState is not RemoteResearchProvenanceState.DispatchReserved
            && remoteState is not RemoteResearchProvenanceState.CancelRequested;
        var canRun = record.State switch
        {
            AgentJobState.Pending => record.ExecutionLocation == JobExecutionLocation.Local,
            AgentJobState.RetryScheduled =>
                record.ExecutionLocation == JobExecutionLocation.Local
                && (record.NextAttemptAt is null || record.NextAttemptAt <= DateTimeOffset.UtcNow),
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
            Display(record, stage))
        {
            RequiresRemoteReconciliation = HasUnfinishedRemoteProvenance(record)
        };
    }

    internal static bool CanRecoverInterrupted(AgentJobRecord record, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(record);
        return record.State == AgentJobState.Running
            && record.ExecutionLocation == JobExecutionLocation.Local
            && !HasUnfinishedRemoteProvenance(record)
            && record.Attempt < record.Definition.MaxAttempts
            && record.ApprovalScope is null
            && IsRecoverableCheckpoint(record.Checkpoint?.Step)
            && record.UpdatedAt <= now - InterruptedRecoveryDelay;
    }

    internal static bool IsRecoverableCheckpoint(string? step) => step is
        ResearchJobHandler.RequestedStep or
        ResearchJobHandler.PlannedStep or
        ResearchJobHandler.EvidenceStep;

    internal static bool HasUnfinishedRemoteProvenance(AgentJobRecord record) =>
        record.RemoteResearch is
        {
            State: RemoteResearchProvenanceState.DispatchReserved
                or RemoteResearchProvenanceState.Dispatched
                or RemoteResearchProvenanceState.CancelRequested
        };

    private static ResearchJobStage ResolveStage(AgentJobRecord record)
    {
        if (record.State == AgentJobState.Completed) return ResearchJobStage.Completed;
        if (record.State == AgentJobState.Cancelled) return ResearchJobStage.Cancelled;
        if (record.State == AgentJobState.Failed) return ResearchJobStage.Failed;
        if (record.State == AgentJobState.RetryScheduled) return ResearchJobStage.WaitingToRetry;
        if (record.State == AgentJobState.Running
            && record.ExecutionLocation == JobExecutionLocation.Local
            && !HasUnfinishedRemoteProvenance(record)
            && IsRecoverableCheckpoint(record.Checkpoint?.Step))
        {
            return ResearchJobStage.Interrupted;
        }

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

        if (record.RemoteResearch?.State == RemoteResearchProvenanceState.DispatchReserved)
            return "Nebius dispatch outcome is ambiguous — reconcile before any retry or cancellation";

        if (record.RemoteResearch?.State == RemoteResearchProvenanceState.CancelRequested)
            return "Nebius cancellation requested — reconcile provider state";

        if (record.ExecutionLocation == JobExecutionLocation.NebiusServerless && record.State == AgentJobState.Running)
        {
            return stage switch
            {
                ResearchJobStage.Planning => "Planning with Nemotron on Nebius Serverless…",
                ResearchJobStage.GatheringEvidence => "Gathering Tavily evidence on Nebius Serverless…",
                ResearchJobStage.Synthesizing => "Synthesizing verified evidence on Nebius Serverless…",
                _ => "Nebius Serverless research is running…"
            };
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
