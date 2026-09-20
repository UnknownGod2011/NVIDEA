using System.Text.Json;
using System.Text.Json.Serialization;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Jobs;

/// <summary>
/// Resumable single-browser-action job. The action itself may be persisted as
/// descriptive checkpoint data, but approval is always supplied separately through
/// JobExecutionContext and is consumed only at the immediate tool boundary.
/// </summary>
public sealed class BrowserActionJobHandler : IAgentJobHandler
{
    public const string Type = "browser.action";
    public const string CheckpointStep = "browser.action.pending";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly BrowserCapabilityExecutionService _execution;

    public BrowserActionJobHandler(BrowserCapabilityExecutionService execution)
    {
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
    }

    public string JobType => Type;

    public Task<JobStepResult> ExecuteStepAsync(
        AgentJobRecord job,
        CancellationToken cancellationToken = default) =>
        ExecuteStepAsync(job, new JobExecutionContext(job.JobId, approval: null), cancellationToken);

    public async Task<JobStepResult> ExecuteStepAsync(
        AgentJobRecord job,
        JobExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(executionContext);

        if (!string.Equals(job.Definition.JobType, Type, StringComparison.Ordinal))
            throw new InvalidOperationException($"BrowserActionJobHandler cannot execute job type '{job.Definition.JobType}'.");

        var action = BrowserActionCheckpointCodec.DeserializeCurrent(job.Checkpoint?.Payload);
        var firstAttempt = await _execution
            .ExecuteAsync(action, approval: null, stableActionId: job.JobId, cancellationToken)
            .ConfigureAwait(false);

        if (firstAttempt.RequiresApproval)
        {
            var scope = firstAttempt.ApprovalScope;
            if (string.IsNullOrWhiteSpace(scope))
                throw new InvalidOperationException("Browser execution requested approval without an exact approval scope.");

            var approval = executionContext.TakeApproval(scope);
            if (approval is null)
            {
                return new JobStepResult(
                    Completed: false,
                    RequiresApproval: true,
                    ApprovalScope: scope,
                    CheckpointStep: CheckpointStep,
                    CheckpointPayload: SerializeAction(action));
            }

            var approvedAttempt = await _execution
                .ExecuteAsync(action, approval, stableActionId: job.JobId, cancellationToken)
                .ConfigureAwait(false);

            if (approvedAttempt.RequiresApproval)
            {
                return new JobStepResult(
                    Completed: false,
                    RequiresApproval: true,
                    ApprovalScope: approvedAttempt.ApprovalScope,
                    CheckpointStep: CheckpointStep,
                    CheckpointPayload: SerializeAction(action));
            }

            return ToStepResult(action, approvedAttempt);
        }

        return ToStepResult(action, firstAttempt);
    }

    public static AgentJobCheckpoint CreateCheckpoint(BrowserAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new AgentJobCheckpoint(CheckpointStep, SerializeAction(action), DateTimeOffset.UtcNow);
    }

    public static string SerializeAction(BrowserAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return BrowserActionCheckpointCodec.SerializeCurrent(action);
    }

    private static JobStepResult ToStepResult(
        BrowserAction action,
        BrowserCapabilityExecutionResult execution)
    {
        var receipt = execution.Receipt;
        if (!receipt.DriverReportedSuccess)
        {
            throw new InvalidOperationException(
                $"Browser action did not execute: {receipt.Error ?? receipt.VerificationDetail ?? receipt.Decision.Reason}");
        }

        if (!receipt.Verified)
        {
            throw new AmbiguousJobExecutionException(
                "Browser action may have executed but its post-action state was not verified.");
        }

        // Keep the existing recovery/display fields, but also commit the minimum structural facts
        // needed to derive a non-authorizing durable judge receipt *after* the orchestrator has
        // durably transitioned this job to Completed. Approval scope/token and typed values are
        // intentionally absent from this projection.
        var evidence = new DurableBrowserActionEvidence(
            receipt.ActionId,
            receipt.Action.Kind,
            receipt.Decision.Risk,
            receipt.Decision.Allowed,
            receipt.Decision.RequiresApproval,
            receipt.ApprovalGranted,
            receipt.DriverReportedSuccess,
            receipt.Verified,
            receipt.StartedAt.ToUniversalTime(),
            receipt.CompletedAt.ToUniversalTime());

        return new JobStepResult(
            Completed: true,
            CheckpointStep: "browser.action.verified",
            CheckpointPayload: JsonSerializer.Serialize(new
            {
                kind = action.Kind.ToString(),
                receipt.ActionId,
                receipt.UrlBefore,
                receipt.UrlAfter,
                receipt.VerificationDetail,
                receipt.CompletedAt,
                durableEvidence = evidence
            }, JsonOptions));
    }
}
