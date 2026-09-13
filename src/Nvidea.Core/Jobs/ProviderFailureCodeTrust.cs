namespace Nvidea.Core.Jobs;

/// <summary>
/// Trust boundary for provider-supplied remote failure classification codes.
/// This validates evidence shape only; it deliberately does not decide whether a code is
/// recognized, actionable, retryable, or eligible for user guidance.
/// </summary>
public static class ProviderFailureCodeTrust
{
    public const int MaxLength = 128;

    /// <summary>
    /// Canonicalizes an optional provider failure code by trimming surrounding whitespace.
    /// Null/blank values are treated as absent. Oversized or control-character-bearing values
    /// are rejected. Unknown but otherwise well-formed codes remain valid evidence.
    /// </summary>
    public static bool TryCanonicalize(string? value, out string? canonical)
    {
        canonical = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var candidate = value.Trim();
        if (candidate.Length > MaxLength || candidate.Any(char.IsControl))
            return false;

        canonical = candidate;
        return true;
    }

    /// <summary>
    /// Canonicalizes evidence or fails closed. Intended for durable-state boundaries where silently
    /// accepting malformed structured provenance would violate the persisted data invariant.
    /// </summary>
    public static string? CanonicalizeOrThrow(string? value)
    {
        if (!TryCanonicalize(value, out var canonical))
        {
            throw new InvalidDataException(
                $"Provider failure code must be at most {MaxLength} characters and contain no control characters.");
        }

        return canonical;
    }
}
