namespace Nvidea.Core.Capabilities;

/// <summary>
/// Trust boundary for capability/action/tool identity tokens that participate in
/// approval authority and durable audit metadata. Identity values are never
/// normalized: callers must provide an already-canonical bounded ASCII token so
/// exact approval equality cannot be changed by trimming/case folding/sanitization.
/// </summary>
public static class CapabilityIdentityTrust
{
    public const int MaxCapabilityIdLength = 96;
    public const int MaxActionIdLength = 128;
    public const int MaxToolNameLength = 96;

    public static bool IsValidCapabilityId(string? value) =>
        IsValidToken(value, MaxCapabilityIdLength);

    public static bool IsValidActionId(string? value) =>
        IsValidToken(value, MaxActionIdLength);

    public static bool IsValidToolName(string? value) =>
        IsValidToken(value, MaxToolNameLength);

    public static string RequireCapabilityId(string? value, string paramName)
    {
        if (!IsValidCapabilityId(value))
            throw new ArgumentException(
                $"Capability id must be a 1-{MaxCapabilityIdLength} character ASCII identity token.",
                paramName);
        return value!;
    }

    public static string RequireActionId(string? value, string paramName)
    {
        if (!IsValidActionId(value))
            throw new ArgumentException(
                $"Action id must be a 1-{MaxActionIdLength} character ASCII identity token.",
                paramName);
        return value!;
    }

    public static string RequireToolName(string? value, string paramName)
    {
        if (!IsValidToolName(value))
            throw new ArgumentException(
                $"Tool name must be a 1-{MaxToolNameLength} character ASCII identity token.",
                paramName);
        return value!;
    }

    private static bool IsValidToken(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength)
            return false;

        foreach (var character in value)
        {
            var allowed = character is >= 'a' and <= 'z'
                or >= 'A' and <= 'Z'
                or >= '0' and <= '9'
                or '.' or '-' or '_' or ':';
            if (!allowed)
                return false;
        }

        return true;
    }
}
