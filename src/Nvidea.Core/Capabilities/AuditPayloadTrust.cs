namespace Nvidea.Core.Capabilities;

/// <summary>
/// Reject-only trust boundary for descriptive audit payload fields. Audit evidence must never be
/// silently truncated or normalized because doing so could change forensic meaning; callers must
/// supply bounded, single-line, purpose-appropriate data before persistence.
/// </summary>
public static class AuditPayloadTrust
{
    public const int MaxEventTypeLength = 96;
    public const int MaxApprovalScopeLength = 512;
    public const int MaxSummaryLength = 1_024;
    public const int MaxMetadataEntries = 16;
    public const int MaxMetadataKeyLength = 64;
    public const int MaxMetadataValueLength = 512;
    public const int MaxMetadataCharacters = 4_096;

    private static readonly string[] SensitiveMetadataKeyFragments =
    {
        "password",
        "passwd",
        "secret",
        "token",
        "authorization",
        "cookie",
        "credential",
        "apikey",
        "api_key",
        "privatekey",
        "private_key"
    };

    public static void ValidateForPersistence(AuditEvent auditEvent, string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        RequireEventType(auditEvent.EventType, paramName);
        RequireSingleLineBoundedText(auditEvent.ApprovalScope, MaxApprovalScopeLength, "approval scope", paramName, allowEmpty: true);
        RequireSingleLineBoundedText(auditEvent.Summary, MaxSummaryLength, "summary", paramName, allowEmpty: true);
        RequireMetadata(auditEvent.Metadata, paramName);
    }

    public static void RequireEventType(string? value, string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Audit event type is required.", paramName);
        if (value.Length > MaxEventTypeLength)
            throw new ArgumentException($"Audit event type cannot exceed {MaxEventTypeLength} characters.", paramName);

        foreach (var character in value)
        {
            var valid = character is >= 'a' and <= 'z'
                or >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '.'
                or '_'
                or '-';
            if (!valid)
                throw new ArgumentException("Audit event type must be a canonical ASCII token.", paramName);
        }
    }

    private static void RequireMetadata(IReadOnlyDictionary<string, string>? metadata, string? paramName)
    {
        if (metadata is null)
            return;
        if (metadata.Count > MaxMetadataEntries)
            throw new ArgumentException($"Audit metadata cannot contain more than {MaxMetadataEntries} entries.", paramName);

        var totalCharacters = 0;
        foreach (var pair in metadata)
        {
            RequireMetadataKey(pair.Key, paramName);
            RequireSingleLineBoundedText(pair.Value, MaxMetadataValueLength, "metadata value", paramName, allowEmpty: true);

            totalCharacters = checked(totalCharacters + pair.Key.Length + pair.Value.Length);
            if (totalCharacters > MaxMetadataCharacters)
                throw new ArgumentException($"Audit metadata cannot exceed {MaxMetadataCharacters} total characters.", paramName);
        }
    }

    private static void RequireMetadataKey(string? key, string? paramName)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Audit metadata keys must be non-empty canonical tokens.", paramName);
        if (key.Length > MaxMetadataKeyLength)
            throw new ArgumentException($"Audit metadata keys cannot exceed {MaxMetadataKeyLength} characters.", paramName);

        foreach (var character in key)
        {
            var valid = character is >= 'a' and <= 'z'
                or >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '.'
                or '_'
                or '-';
            if (!valid)
                throw new ArgumentException("Audit metadata keys must be canonical ASCII tokens.", paramName);
        }

        var normalized = key.ToLowerInvariant();
        if (SensitiveMetadataKeyFragments.Any(normalized.Contains))
        {
            throw new ArgumentException(
                "Audit metadata keys must not designate secrets or authentication material.",
                paramName);
        }
    }

    private static void RequireSingleLineBoundedText(
        string? value,
        int maxLength,
        string fieldName,
        string? paramName,
        bool allowEmpty)
    {
        if (value is null)
            throw new ArgumentException($"Audit {fieldName} cannot be null.", paramName);
        if (!allowEmpty && value.Length == 0)
            throw new ArgumentException($"Audit {fieldName} is required.", paramName);
        if (value.Length > maxLength)
            throw new ArgumentException($"Audit {fieldName} cannot exceed {maxLength} characters.", paramName);

        foreach (var character in value)
        {
            if (char.IsControl(character))
                throw new ArgumentException($"Audit {fieldName} must be single-line text without control characters.", paramName);
        }
    }
}
