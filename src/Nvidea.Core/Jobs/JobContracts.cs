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

/// <summary>Durable non-secret intent for an external side effect whose delivery may be ambiguous.</summary>
public sealed record PendingExternalAction(
    Guid ActionId,
    DurableExternalActionKind Kind,
    string TargetId,
    Guid AuditEventId,
    DateTimeOffset CreatedAt);

/// <summary>Durable proof that protected remote-research transport artifacts still require deletion.</summary>
public sealed record PendingProtectedPayloadCleanup(
    Guid CleanupId,
    string OpaqueWorkItemId,
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
    PendingExternalAction? PendingExternalAction = null,
    PendingProtectedPayloadCleanup? PendingProtectedPayloadCleanup = null,
    string? RemoteWorkItemEnvelopeSha256 = null,
    PendingResearchDispatchBinding? PendingResearchDispatchBinding = null);

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
    Task<JobStepResult> ExecuteStepAsync(AgentJobRecord job, JobExecutionContext executionContext, CancellationToken cancellationToken = default) => ExecuteStepAsync(job, cancellationToken);
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
        if (definition.ContainsPrivateOsData) return JobExecutionLocation.Local;
        return definition.BenefitsFromBackgroundExecution ? JobExecutionLocation.NebiusServerless : JobExecutionLocation.Local;
    }
}
