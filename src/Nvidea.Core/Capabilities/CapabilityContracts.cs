namespace Nvidea.Core.Capabilities;

public enum CapabilityRiskLevel
{
    Low,
    Medium,
    High,
    Blocked
}

public enum DataPermission
{
    None,
    ScreenRead,
    ClipboardRead,
    FilesRead,
    FilesWrite,
    BrowserRead,
    BrowserWrite,
    NetworkAccess,
    EmailRead,
    EmailDraft,
    EmailSend,
    CalendarRead,
    CalendarWrite,
    MemoryRead,
    MemoryWrite,
    LocalProcessControl
}

public sealed record CapabilityDescriptor(
    string Id,
    string Version,
    string DisplayName,
    IReadOnlySet<DataPermission> Permissions,
    CapabilityRiskLevel Risk,
    bool RequiresConfirmation,
    string Description);

public sealed record CapabilityInvocation(
    string CapabilityId,
    string ActionId,
    IReadOnlySet<DataPermission> RequestedPermissions,
    CapabilityRiskLevel RequestedRisk,
    bool Consequential,
    string? UntrustedSource = null,
    string? Summary = null);

public sealed record PermissionDecision(
    bool Allowed,
    bool RequiresApproval,
    CapabilityRiskLevel EffectiveRisk,
    IReadOnlySet<DataPermission> EffectivePermissions,
    string Reason,
    string ApprovalScope);

public interface ICapabilityRegistry
{
    CapabilityDescriptor GetRequired(string capabilityId);
    IReadOnlyCollection<CapabilityDescriptor> List();
}

public interface ICapabilityPermissionPolicy
{
    PermissionDecision Evaluate(CapabilityInvocation invocation);
}
