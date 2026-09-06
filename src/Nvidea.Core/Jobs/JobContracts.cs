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
    DateTimeOffset? NextAttemptAt = null);

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
