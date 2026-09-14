using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Jobs;

public enum AgentJobState
{
    Pending,
    Running,
    WaitingForApproval,
    RetryScheduled,
    Completed,
    Failed,
    Cancelled
}

public enum JobExecutionLocation
{
    Local,
    NebiusServerless
}

public enum DurableExternalActionKind
{
    NebiusCancelRemoteResearch
}

/// <summary>
/// Durable, non-secret intent for an external side effect whose delivery may become ambiguous across
/// process failure. The intent is deliberately separate from success state: its presence means the
/// action may still require reconciliation, never that the provider accepted or completed it.
/// AuditEventId binds the intent to the exact durable audit authority prepared for this attempt.
/// </summary>
public sealed record PendingExternalAction(
    Guid ActionId,
    DurableExternalActionKind Kind,
    string TargetId,
    Guid AuditEventId,
    DateTimeOffset CreatedAt);

public sealed record AgentJobDefinition(
    string JobType,
    string CapabilityId,
    IReadOnlySet<DataPermission> RequiredPermissions,
    CapabilityRiskLevel Risk,
    bool ContainsPrivateOsData,
    bool BenefitsFromBackgroundExecution,
    int MaxAttempts = 3);

public sealed record AgentJobCheckpoint(
    string Step,
    string? Payload,
    DateTimeOffset SavedAt);

public sealed record AgentJobRecord(
    Guid JobId,
    AgentJobDefinition Definition,
    AgentJobState State,
    JobExecutionLocation ExecutionLocation,
    int Attempt,
    AgentJobCheckpoint? Checkpoint,
    string? ApprovalScope,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? NextAttemptAt = null,
    RemoteResearchProvenance? RemoteResearch = null,
    AuditEvent? PendingAuditEvent = null,
    PendingExternalAction? PendingExternalAction = null);

public sealed record JobStepResult(
    bool Completed,
    bool RequiresApproval = false,
    string? ApprovalScope = null,
    string? CheckpointStep = null,
    string? CheckpointPayload = null);

public interface IAgentJobStore
{
    Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default);
}

public interface IAgentJobHandler
{
    string JobType { get; }

    Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Context-aware execution hook. Existing handlers remain source-compatible via
    /// the default implementation; consequential handlers should override this form
    /// and take the exact ephemeral approval only at the immediate tool call.
    /// </summary>
    Task<JobStepResult> ExecuteStepAsync(
        AgentJobRecord job,
        JobExecutionContext executionContext,
        CancellationToken cancellationToken = default) =>
        ExecuteStepAsync(job, cancellationToken);
}

public interface IJobExecutionPolicy
{
    JobExecutionLocation Choose(AgentJobDefinition definition);
}

public sealed class ConservativeJobExecutionPolicy : IJobExecutionPolicy
{
    public JobExecutionLocation Choose(AgentJobDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.ContainsPrivateOsData)
            return JobExecutionLocation.Local;

        return definition.BenefitsFromBackgroundExecution
            ? JobExecutionLocation.NebiusServerless
            : JobExecutionLocation.Local;
    }
}
