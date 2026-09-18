namespace Nvidea.Core.Browser;

/// <summary>
/// Classifies form controls whose current value must never be copied into an agent observation.
/// This policy intentionally operates only on non-secret DOM metadata (input type/autocomplete),
/// so callers can decide whether a value is observable before reading or serializing it.
/// </summary>
public static class BrowserObservedValuePrivacyPolicy
{
    private static readonly string[] SensitiveAutocompleteTokenArray =
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

    private static readonly HashSet<string> SensitiveAutocompleteTokenSet =
        new(SensitiveAutocompleteTokenArray, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Canonical non-secret autocomplete field tokens that require value suppression.
    /// Browser-side observers consume this list so classification cannot drift from this policy.
    /// </summary>
    public static IReadOnlyList<string> SensitiveAutocompleteTokens { get; } =
        Array.AsReadOnly(SensitiveAutocompleteTokenArray);

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
            if (SensitiveAutocompleteTokenSet.Contains(token))
            {
                return true;
            }
        }

        return false;
    }
}
