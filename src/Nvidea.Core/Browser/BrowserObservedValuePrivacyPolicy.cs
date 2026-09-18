namespace Nvidea.Core.Browser;

/// <summary>
/// Classifies form controls whose current value must never be copied into an agent observation.
/// This policy intentionally operates only on non-secret DOM metadata (input type/autocomplete),
/// so callers can decide whether a value is observable before reading or serializing it.
/// </summary>
public static class BrowserObservedValuePrivacyPolicy
{
    private static readonly HashSet<string> SensitiveAutocompleteTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "current-password",
        "new-password",
        "one-time-code",
        "cc-number",
        "cc-csc",
        "cc-exp",
        "cc-exp-month",
        "cc-exp-year"
    };

    public static bool ShouldSuppressValue(string? inputType, string? autoComplete)
    {
        if (string.Equals(inputType?.Trim(), "password", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(autoComplete))
        {
            return false;
        }

        // HTML autocomplete may contain section-* and shipping/billing qualifiers before the
        // field token. Token matching avoids both false negatives and substring false positives.
        foreach (var token in autoComplete.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (SensitiveAutocompleteTokens.Contains(token))
            {
                return true;
            }
        }

        return false;
    }
}
