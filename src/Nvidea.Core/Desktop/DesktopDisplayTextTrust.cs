using System.Text;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Presentation-only trust boundary for runtime-provided text that may originate from
/// websites, tools, providers, window titles, model output, or other untrusted sources.
/// This helper never grants authority: it only produces bounded single-line display text.
/// </summary>
public static class DesktopDisplayTextTrust
{
    public const int MaxStatusCharacters = 160;
    public const int MaxApprovalSummaryCharacters = 240;
    public const int MaxApprovalTargetCharacters = 320;
    public const int MaxContextCharacters = 160;

    public static string Canonicalize(
        string? value,
        int maxCharacters,
        string fallback)
    {
        if (maxCharacters <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCharacters));
        if (string.IsNullOrWhiteSpace(fallback))
            throw new ArgumentException("A non-empty display fallback is required.", nameof(fallback));

        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var builder = new StringBuilder(Math.Min(value.Length, maxCharacters));
        var pendingSpace = false;
        foreach (var character in value)
        {
            if (char.IsControl(character) || char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace && builder.Length < maxCharacters)
                builder.Append(' ');
            pendingSpace = false;

            if (builder.Length >= maxCharacters)
                break;
            builder.Append(character);
        }

        var normalized = builder.ToString().Trim();
        return normalized.Length == 0 ? fallback : normalized;
    }

    /// <summary>
    /// Produces a privacy-reduced navigation display target. User-info, query, and fragment
    /// components are intentionally omitted because URLs frequently carry credentials/tokens.
    /// </summary>
    public static string ProjectNavigationTarget(Uri? uri, string fallback = "current browser context")
    {
        if (uri is null || !uri.IsAbsoluteUri || uri.Scheme is not ("http" or "https"))
            return fallback;

        var authorityAndPath = $"{uri.Scheme}://{uri.IdnHost}{uri.AbsolutePath}";
        return Canonicalize(authorityAndPath, MaxApprovalTargetCharacters, fallback);
    }
}
