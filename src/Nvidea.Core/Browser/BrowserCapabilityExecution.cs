using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Browser;

public sealed record BrowserCapabilityExecutionResult(
    BrowserActionReceipt Receipt,
    bool RequiresApproval,
    string? ApprovalScope = null);

/// <summary>
/// Concrete capability backend for browser actions. Authorization is deliberately
/// performed outside this class by CapabilityToolExecutor immediately before this
/// backend is invoked.
/// </summary>
public sealed class BrowserCapabilityBackend : ICapabilityToolBackend
{
    public const string ExecuteToolName = "browser.execute";
    public const string ActionArgumentName = "action";

    private readonly IBrowserDriver _driver;

    public BrowserCapabilityBackend(IBrowserDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public async Task<object?> ExecuteAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(toolName, ExecuteToolName, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported browser capability tool '{toolName}'.");

        if (!arguments.TryGetValue(ActionArgumentName, out var rawAction) || rawAction is not BrowserAction action)
            throw new InvalidOperationException("Browser capability execution requires a typed BrowserAction argument.");

        await _driver.ExecuteAsync(action, cancellationToken).ConfigureAwait(false);
        return action;
    }
}

/// <summary>
/// End-to-end browser action boundary:
/// fresh observation -> browser safety -> capability policy preflight -> exact
/// approval -> last-mile CapabilityToolExecutor -> driver -> fresh observation ->
/// verification. Browser safety can never be weakened by capability policy.
/// </summary>
public sealed class BrowserCapabilityExecutionService
{
    private readonly string _capabilityId;
    private readonly IBrowserDriver _driver;
    private readonly BrowserSafetyPolicy _browserSafety;
    private readonly ICapabilityPermissionPolicy _capabilityPolicy;
    private readonly ICapabilityToolExecutor _toolExecutor;
    private readonly IBrowserActionVerifier _verifier;

    public BrowserCapabilityExecutionService(
        string capabilityId,
        IBrowserDriver driver,
        BrowserSafetyPolicy browserSafety,
        ICapabilityPermissionPolicy capabilityPolicy,
        ICapabilityToolExecutor toolExecutor,
        IBrowserActionVerifier verifier)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("Capability id is required.", nameof(capabilityId));

        _capabilityId = capabilityId;
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _browserSafety = browserSafety ?? throw new ArgumentNullException(nameof(browserSafety));
        _capabilityPolicy = capabilityPolicy ?? throw new ArgumentNullException(nameof(capabilityPolicy));
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
        _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));
    }

    public async Task<BrowserCapabilityExecutionResult> ExecuteAsync(
        BrowserAction action,
        ApprovalGrant? approval = null,
        Guid? stableActionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();

        var before = await _driver.ObserveAsync(cancellationToken).ConfigureAwait(false);
        var startedAt = DateTimeOffset.UtcNow;
        var actionId = stableActionId ?? Guid.NewGuid();
        var browserDecision = _browserSafety.Evaluate(action, before);

        if (!browserDecision.Allowed || browserDecision.Risk == BrowserRiskLevel.Blocked)
        {
            return new BrowserCapabilityExecutionResult(
                BlockedReceipt(actionId, action, browserDecision, startedAt, before, browserDecision.Reason),
                RequiresApproval: false);
        }

        var invocation = BuildInvocation(actionId, action, before, browserDecision);
        var capabilityDecision = _capabilityPolicy.Evaluate(invocation);
        if (!capabilityDecision.Allowed || capabilityDecision.EffectiveRisk == CapabilityRiskLevel.Blocked)
        {
            var blockedDecision = new BrowserActionDecision(
                BrowserRiskLevel.Blocked,
                RequiresApproval: false,
                Allowed: false,
                $"Blocked by capability policy: {capabilityDecision.Reason}");

            return new BrowserCapabilityExecutionResult(
                BlockedReceipt(actionId, action, blockedDecision, startedAt, before, blockedDecision.Reason),
                RequiresApproval: false);
        }

        var effectiveDecision = new BrowserActionDecision(
            Max(browserDecision.Risk, MapRisk(capabilityDecision.EffectiveRisk)),
            browserDecision.RequiresApproval || capabilityDecision.RequiresApproval,
            Allowed: true,
            $"{browserDecision.Reason} Capability policy: {capabilityDecision.Reason}");

        var toolResult = await _toolExecutor.ExecuteAsync(
            new CapabilityToolRequest(
                invocation,
                BrowserCapabilityBackend.ExecuteToolName,
                new Dictionary<string, object?>
                {
                    [BrowserCapabilityBackend.ActionArgumentName] = action
                },
                approval),
            cancellationToken).ConfigureAwait(false);

        if (!toolResult.Executed)
        {
            var detail = toolResult.RequiresApproval
                ? "Exact user approval is required before browser execution."
                : "Browser action was denied by the capability boundary.";

            var receipt = new BrowserActionReceipt(
                actionId,
                action,
                effectiveDecision,
                startedAt,
                DateTimeOffset.UtcNow,
                DriverReportedSuccess: false,
                Verified: false,
                VerificationDetail: detail,
                before.Url,
                before.Url,
                toolResult.Reason);

            return new BrowserCapabilityExecutionResult(
                receipt,
                toolResult.RequiresApproval,
                toolResult.RequiresApproval ? toolResult.ApprovalScope : null);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var after = await _driver.ObserveAsync(cancellationToken).ConfigureAwait(false);
            var verification = await _verifier
                .VerifyAsync(action, before, after, cancellationToken)
                .ConfigureAwait(false);

            return new BrowserCapabilityExecutionResult(
                new BrowserActionReceipt(
                    actionId,
                    action,
                    effectiveDecision,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    DriverReportedSuccess: true,
                    Verified: verification.Verified,
                    VerificationDetail: verification.Detail,
                    before.Url,
                    after.Url),
                RequiresApproval: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new BrowserCapabilityExecutionResult(
                new BrowserActionReceipt(
                    actionId,
                    action,
                    effectiveDecision,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    DriverReportedSuccess: true,
                    Verified: false,
                    VerificationDetail: "Browser action executed but post-action observation or verification failed.",
                    before.Url,
                    before.Url,
                    ex.Message),
                RequiresApproval: false);
        }
    }

    private CapabilityInvocation BuildInvocation(
        Guid actionId,
        BrowserAction action,
        BrowserObservation observation,
        BrowserActionDecision browserDecision) =>
        new(
            _capabilityId,
            actionId.ToString("N"),
            PermissionsFor(action.Kind),
            MapRisk(browserDecision.Risk),
            Consequential: browserDecision.RequiresApproval,
            UntrustedSource: observation.ContainsUntrustedInstructions ? observation.Url.AbsoluteUri : null,
            Summary: action.Rationale);

    internal static IReadOnlySet<DataPermission> PermissionsFor(BrowserActionKind kind) => kind switch
    {
        BrowserActionKind.Read => new HashSet<DataPermission> { DataPermission.BrowserRead },
        BrowserActionKind.Download => new HashSet<DataPermission> { DataPermission.BrowserRead, DataPermission.FilesWrite },
        BrowserActionKind.Upload => new HashSet<DataPermission> { DataPermission.BrowserWrite, DataPermission.FilesRead },
        BrowserActionKind.Navigate or BrowserActionKind.Click or BrowserActionKind.Type or BrowserActionKind.Select
            or BrowserActionKind.Back or BrowserActionKind.Refresh => new HashSet<DataPermission> { DataPermission.BrowserWrite },
        _ => new HashSet<DataPermission> { DataPermission.BrowserWrite }
    };

    private static BrowserActionReceipt BlockedReceipt(
        Guid actionId,
        BrowserAction action,
        BrowserActionDecision decision,
        DateTimeOffset startedAt,
        BrowserObservation observation,
        string error) =>
        new(
            actionId,
            action,
            decision,
            startedAt,
            DateTimeOffset.UtcNow,
            DriverReportedSuccess: false,
            Verified: false,
            VerificationDetail: "Blocked before browser execution.",
            observation.Url,
            observation.Url,
            error);

    private static CapabilityRiskLevel MapRisk(BrowserRiskLevel risk) => risk switch
    {
        BrowserRiskLevel.Low => CapabilityRiskLevel.Low,
        BrowserRiskLevel.Medium => CapabilityRiskLevel.Medium,
        BrowserRiskLevel.High => CapabilityRiskLevel.High,
        _ => CapabilityRiskLevel.Blocked
    };

    private static BrowserRiskLevel MapRisk(CapabilityRiskLevel risk) => risk switch
    {
        CapabilityRiskLevel.Low => BrowserRiskLevel.Low,
        CapabilityRiskLevel.Medium => BrowserRiskLevel.Medium,
        CapabilityRiskLevel.High => BrowserRiskLevel.High,
        _ => BrowserRiskLevel.Blocked
    };

    private static BrowserRiskLevel Max(BrowserRiskLevel left, BrowserRiskLevel right) =>
        (BrowserRiskLevel)Math.Max((int)left, (int)right);
}
