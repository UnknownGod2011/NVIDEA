namespace Nvidea.Core.Capabilities;

/// <summary>
/// Shared reject-only trust contract for audit events before they can influence durable state
/// ordering or enter any audit sink. Producers should validate prospective events before committing
/// the state transition that the event is intended to describe; sinks enforce the same contract as
/// defense in depth.
/// </summary>
public static class AuditEventTrust
{
    public static void ValidateForPersistence(AuditEvent auditEvent, string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        if (auditEvent.EventId == Guid.Empty)
            throw new ArgumentException("Audit events require a non-empty event id.", paramName);

        CapabilityIdentityTrust.RequireCapabilityId(auditEvent.CapabilityId, paramName ?? nameof(auditEvent));
        CapabilityIdentityTrust.RequireActionId(auditEvent.ActionId, paramName ?? nameof(auditEvent));
        AuditPayloadTrust.ValidateForPersistence(auditEvent, paramName);
    }
}
