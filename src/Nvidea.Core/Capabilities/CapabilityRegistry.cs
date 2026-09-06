namespace Nvidea.Core.Capabilities;

public sealed class CapabilityRegistry : ICapabilityRegistry
{
    private readonly Dictionary<string, CapabilityDescriptor> _descriptors;

    public CapabilityRegistry(IEnumerable<CapabilityDescriptor> descriptors)
    {
        _descriptors = descriptors?.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase)
            ?? throw new ArgumentNullException(nameof(descriptors));

        if (_descriptors.Count == 0)
            throw new ArgumentException("At least one capability must be registered.", nameof(descriptors));
    }

    public CapabilityDescriptor GetRequired(string capabilityId)
    {
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("Capability id is required.", nameof(capabilityId));

        return _descriptors.TryGetValue(capabilityId, out var descriptor)
            ? descriptor
            : throw new KeyNotFoundException($"Capability '{capabilityId}' is not registered.");
    }

    public IReadOnlyCollection<CapabilityDescriptor> List() => _descriptors.Values.ToArray();
}

public sealed class CapabilityPermissionPolicy : ICapabilityPermissionPolicy
{
    private static readonly HashSet<DataPermission> ConsequentialPermissions = new()
    {
        DataPermission.EmailSend,
        DataPermission.CalendarWrite,
        DataPermission.FilesWrite,
        DataPermission.BrowserWrite,
        DataPermission.LocalProcessControl
    };

    private readonly ICapabilityRegistry _registry;

    public CapabilityPermissionPolicy(ICapabilityRegistry registry) =>
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public PermissionDecision Evaluate(CapabilityInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        var descriptor = _registry.GetRequired(invocation.CapabilityId);

        if (string.IsNullOrWhiteSpace(invocation.ActionId))
            return Deny("Every invocation needs a stable action id for approval/audit scoping.");

        if (!invocation.RequestedPermissions.IsSubsetOf(descriptor.Permissions))
            return Deny("Invocation requested permissions not declared by the registered capability.");

        var effectiveRisk = Max(descriptor.Risk, invocation.RequestedRisk);
        if (effectiveRisk == CapabilityRiskLevel.Blocked)
            return Deny("Capability or invocation is blocked by policy.", CapabilityRiskLevel.Blocked);

        var consequential = invocation.Consequential
            || invocation.RequestedPermissions.Any(ConsequentialPermissions.Contains);

        var requiresApproval = descriptor.RequiresConfirmation
            || effectiveRisk == CapabilityRiskLevel.High
            || consequential;

        if (!string.IsNullOrWhiteSpace(invocation.UntrustedSource)
            && invocation.RequestedPermissions.Any(p => p is DataPermission.EmailSend
                or DataPermission.FilesWrite
                or DataPermission.CalendarWrite
                or DataPermission.LocalProcessControl))
        {
            requiresApproval = true;
            effectiveRisk = Max(effectiveRisk, CapabilityRiskLevel.High);
        }

        var permissions = invocation.RequestedPermissions.ToHashSet();
        var reason = requiresApproval
            ? "Allowed only with approval scoped to this exact capability/action/permission set."
            : "Allowed under the capability's declared least-privilege envelope.";

        return new PermissionDecision(
            Allowed: true,
            RequiresApproval: requiresApproval,
            EffectiveRisk: effectiveRisk,
            EffectivePermissions: permissions,
            Reason: reason,
            ApprovalScope: BuildScope(invocation, permissions));
    }

    private static PermissionDecision Deny(string reason, CapabilityRiskLevel risk = CapabilityRiskLevel.Blocked) =>
        new(false, false, risk, new HashSet<DataPermission>(), reason, string.Empty);

    private static CapabilityRiskLevel Max(CapabilityRiskLevel left, CapabilityRiskLevel right) =>
        (CapabilityRiskLevel)Math.Max((int)left, (int)right);

    private static string BuildScope(CapabilityInvocation invocation, IEnumerable<DataPermission> permissions) =>
        string.Join("|", new[]
        {
            invocation.CapabilityId,
            invocation.ActionId,
            string.Join(',', permissions.OrderBy(x => x).Select(x => x.ToString()))
        });
}
