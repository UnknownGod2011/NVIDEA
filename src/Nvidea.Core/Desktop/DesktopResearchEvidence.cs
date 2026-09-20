using Nvidea.Core.Research;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Payload-free product evidence derived from a completed research report. This is safe to project
/// into judge/readiness surfaces: it contains counts and closed quality signals, never queries,
/// URLs, titles, snippets, answer text, or provider payloads.
/// </summary>
public sealed record DesktopResearchEvidence(
    int SourceCount,
    int UsedCitationCount,
    int UniqueQueryCount,
    int UniqueHostCount,
    int UnknownFreshnessCount,
    int WarningCount,
    int ProviderCreditsUsed,
    bool MultiQueryEvidence,
    bool CanonicalSourcesUnique,
    bool HasMachineVerifiableCitations);

public static class DesktopResearchEvidenceProjector
{
    public static DesktopResearchEvidence Project(ResearchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(report.Evidence);

        var sources = report.Evidence.Sources;
        var uniqueQueries = sources
            .Select(source => source.Query)
            .Where(query => !string.IsNullOrWhiteSpace(query))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var uniqueHosts = sources
            .Select(source => source.Url.Host)
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var canonicalCount = sources
            .Select(source => source.CanonicalUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var unknownFreshness = report.QualityBySourceId?.Values
            .Count(quality => quality.FreshnessBasis == ResearchFreshnessBasis.Unknown) ?? 0;

        return new DesktopResearchEvidence(
            sources.Count,
            report.UsedCitations.Count,
            uniqueQueries,
            uniqueHosts,
            unknownFreshness,
            report.Warnings.Count,
            report.Evidence.ProviderCreditsUsed,
            uniqueQueries >= 2,
            canonicalCount == sources.Count,
            report.UsedCitations.Count > 0);
    }
}
