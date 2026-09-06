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

        var action = DeserializeAction(job.Checkpoint?.Payload);

        // Use the job id as the stable capability action id. It is descriptive audit
        // scope only, not authorization, and stays constant across approval resume.
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
                // Fail closed if the exact policy scope changed between preflight and
                // execution. Never reuse or translate approval to a new scope.
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
        return JsonSerializer.Serialize(action, JsonOptions);
    }

    private static BrowserAction DeserializeAction(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("Browser action job requires a persisted action checkpoint payload.");

        try
        {
            return JsonSerializer.Deserialize<BrowserAction>(payload, JsonOptions)
                ?? throw new InvalidOperationException("Browser action checkpoint was empty after deserialization.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Browser action checkpoint payload is invalid.", ex);
        }
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
            throw new InvalidOperationException(
                $"Browser action executed but verification failed: {receipt.VerificationDetail ?? "no verification detail"}");
        }

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
                receipt.CompletedAt
            }, JsonOptions));
    }
}
