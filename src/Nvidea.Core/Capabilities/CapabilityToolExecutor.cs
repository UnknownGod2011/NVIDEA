namespace Nvidea.Core.Capabilities;

public sealed record CapabilityToolRequest(
    CapabilityInvocation Invocation,
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    ApprovalGrant? Approval = null);

public sealed record CapabilityToolResult(
    bool Executed,
    bool RequiresApproval,
    bool Allowed,
    string Reason,
    string ApprovalScope,
    object? Output = null);

public interface ICapabilityToolBackend
{
    Task<object?> ExecuteAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
}

public interface ICapabilityToolExecutor
{
    Task<CapabilityToolResult> ExecuteAsync(
        CapabilityToolRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Mandatory least-privilege boundary for concrete tool calls.
/// Every invocation is re-evaluated immediately before execution; persisted job
/// state/checkpoints are never treated as authorization.
/// </summary>
public sealed class CapabilityToolExecutor : ICapabilityToolExecutor
{
    private readonly ICapabilityPermissionPolicy _policy;
    private readonly ScopedApprovalAuthorizer _approvals;
    private readonly IAuditTrail _auditTrail;
    private readonly ICapabilityToolBackend _backend;

    public CapabilityToolExecutor(
        ICapabilityPermissionPolicy policy,
        ScopedApprovalAuthorizer approvals,
        IAuditTrail auditTrail,
        ICapabilityToolBackend backend)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _approvals = approvals ?? throw new ArgumentNullException(nameof(approvals));
        _auditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    public async Task<CapabilityToolResult> ExecuteAsync(
        CapabilityToolRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Invocation);

        if (string.IsNullOrWhiteSpace(request.ToolName))
            throw new ArgumentException("Tool name is required.", nameof(request));

        // Deliberately evaluate at the last possible moment. Resumed jobs must not
        // inherit an old allow/approval decision from a checkpoint.
        var decision = _policy.Evaluate(request.Invocation);
        if (!decision.Allowed)
        {
            await AuditAsync(request, decision, "tool.denied", false, false, decision.Reason, cancellationToken)
                .ConfigureAwait(false);
            return new CapabilityToolResult(false, false, false, decision.Reason, decision.ApprovalScope);
        }

        var approved = !decision.RequiresApproval;
        if (decision.RequiresApproval)
        {
            approved = request.Approval is not null
                && _approvals.TryAuthorize(request.Approval, decision);

            if (!approved)
            {
                const string reason = "Exact single-use approval is required immediately before this tool call.";
                await AuditAsync(request, decision, "tool.awaiting_approval", true, false, reason, cancellationToken)
                    .ConfigureAwait(false);
                return new CapabilityToolResult(false, true, true, reason, decision.ApprovalScope);
            }
        }

        await AuditAsync(request, decision, "tool.execution_started", true, approved, "Tool execution started.", cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var output = await _backend.ExecuteAsync(request.ToolName, request.Arguments, cancellationToken)
                .ConfigureAwait(false);
            await AuditAsync(request, decision, "tool.execution_succeeded", true, approved, "Tool execution succeeded.", cancellationToken)
                .ConfigureAwait(false);
            return new CapabilityToolResult(true, false, true, "Executed under current capability policy.", decision.ApprovalScope, output);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await AuditAsync(request, decision, "tool.execution_cancelled", true, approved, "Tool execution cancelled.", CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            // A consumed approval is intentionally NOT restored. When a consequential
            // call fails ambiguously, a retry must obtain fresh user approval rather
            // than risk duplicating a side effect.
            await AuditAsync(request, decision, "tool.execution_failed", true, approved, ex.Message, cancellationToken)
                .ConfigureAwait(false);
            throw;
        }
    }

    private Task AuditAsync(
        CapabilityToolRequest request,
        PermissionDecision decision,
        string eventType,
        bool allowed,
        bool approved,
        string summary,
        CancellationToken cancellationToken)
    {
        return _auditTrail.AppendAsync(new AuditEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            request.Invocation.CapabilityId,
            request.Invocation.ActionId,
            eventType,
            decision.EffectiveRisk,
            allowed,
            approved,
            decision.ApprovalScope,
            summary,
            new Dictionary<string, string>
            {
                ["tool"] = request.ToolName,
                ["permissions"] = string.Join(',', decision.EffectivePermissions.OrderBy(x => x).Select(x => x.ToString())),
                ["untrustedSourcePresent"] = (!string.IsNullOrWhiteSpace(request.Invocation.UntrustedSource)).ToString()
            }), cancellationToken);
    }
}
