using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Browser;

public interface IBrowserCapabilityGuard
{
    BrowserActionDecision Evaluate(
        Guid actionId,
        BrowserAction action,
        BrowserObservation observation,
        BrowserActionDecision browserDecision);
}

public sealed class BrowserCapabilityGuard : IBrowserCapabilityGuard
{
    private readonly string _capabilityId;
    private readonly ICapabilityPermissionPolicy _policy;

    public BrowserCapabilityGuard(string capabilityId, ICapabilityPermissionPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("Capability id is required.", nameof(capabilityId));

        _capabilityId = capabilityId;
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public BrowserActionDecision Evaluate(
        Guid actionId,
        BrowserAction action,
        BrowserObservation observation,
        BrowserActionDecision browserDecision)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(browserDecision);

        if (!browserDecision.Allowed || browserDecision.Risk == BrowserRiskLevel.Blocked)
            return browserDecision;

        var permissions = PermissionsFor(action.Kind);
        var invocation = new CapabilityInvocation(
            _capabilityId,
            actionId.ToString("N"),
            permissions,
            MapRisk(browserDecision.Risk),
            Consequential: browserDecision.RequiresApproval,
            UntrustedSource: observation.ContainsUntrustedInstructions ? observation.Url.AbsoluteUri : null,
            Summary: action.Rationale);

        var capability = _policy.Evaluate(invocation);
        if (!capability.Allowed || capability.EffectiveRisk == CapabilityRiskLevel.Blocked)
        {
            return new BrowserActionDecision(
                BrowserRiskLevel.Blocked,
                RequiresApproval: false,
                Allowed: false,
                $"Blocked by capability policy: {capability.Reason}");
        }

        var effectiveRisk = Max(browserDecision.Risk, MapRisk(capability.EffectiveRisk));
        return new BrowserActionDecision(
            effectiveRisk,
            browserDecision.RequiresApproval || capability.RequiresApproval,
            Allowed: true,
            $"{browserDecision.Reason} Capability policy: {capability.Reason}");
    }

    private static IReadOnlySet<DataPermission> PermissionsFor(BrowserActionKind kind) => kind switch
    {
        BrowserActionKind.Read => new HashSet<DataPermission> { DataPermission.BrowserRead },
        BrowserActionKind.Download => new HashSet<DataPermission> { DataPermission.BrowserRead, DataPermission.FilesWrite },
        BrowserActionKind.Upload => new HashSet<DataPermission> { DataPermission.BrowserWrite, DataPermission.FilesRead },
        BrowserActionKind.Navigate or BrowserActionKind.Click or BrowserActionKind.Type or BrowserActionKind.Select
            or BrowserActionKind.Back or BrowserActionKind.Refresh =>
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
        _ => new HashSet<DataPermission> { DataPermission.BrowserWrite }
    };

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
