using System.Text.RegularExpressions;

namespace Nvidea.Core.Research;

/// <summary>
/// Deterministically validates model-emitted research source markers against the
/// evidence actually supplied to synthesis. This is intentionally model-independent:
/// untrusted/model output cannot manufacture provenance by merely formatting a marker.
/// </summary>
public static class ResearchCitationIntegrity
{
    private static readonly Regex SourceMarker = new(
        @"\[src:(?<id>[A-Za-z0-9._:-]+)\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static ResearchCitationIntegrityResult Verify(
        string answerMarkdown,
        IReadOnlyCollection<ResearchCitation> availableCitations)
    {
        ArgumentNullException.ThrowIfNull(answerMarkdown);
        ArgumentNullException.ThrowIfNull(availableCitations);

        var available = availableCitations
            .GroupBy(c => c.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var referencedIds = SourceMarker.Matches(answerMarkdown)
            .Select(match => match.Groups["id"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var valid = referencedIds
            .Where(available.ContainsKey)
            .Select(id => available[id])
            .ToArray();
        var unknown = referencedIds
            .Where(id => !available.ContainsKey(id))
            .ToArray();

        return new ResearchCitationIntegrityResult(
            referencedIds,
            valid,
            unknown,
            referencedIds.Length > 0 && unknown.Length == 0);
    }
}

public sealed record ResearchCitationIntegrityResult(
    IReadOnlyList<string> ReferencedSourceIds,
    IReadOnlyList<ResearchCitation> VerifiedCitations,
    IReadOnlyList<string> UnknownSourceIds,
    bool IsFullyVerified);
