namespace Nvidea.Core.Research;

public enum ResearchAuthorityBasis
{
    DefaultWeb,
    FirstPartyDocumentation,
    AcademicOrInstitutional,
    GovernmentOrInternational
}

public enum ResearchFreshnessBasis
{
    Unknown,
    PublishedTimestamp,
    SearchWindow
}

public sealed record ResearchEvidenceQuality(
    double RelevanceScore,
    double AuthorityScore,
    double FreshnessScore,
    double CompositeScore,
    double DiversityPenalty,
    ResearchAuthorityBasis AuthorityBasis,
    ResearchFreshnessBasis FreshnessBasis);

public sealed record ResearchEvidenceRanking(
    ResearchBatch Batch,
    IReadOnlyDictionary<string, ResearchEvidenceQuality> QualityBySourceId);

public sealed class ResearchEvidenceRanker
{
    private readonly TimeProvider _timeProvider;

    public ResearchEvidenceRanker(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ResearchEvidenceRanking Rank(ResearchBatch batch, IReadOnlyList<ResearchPlanItem> plan)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(plan);
        if (batch.Sources.Count == 0)
            return new ResearchEvidenceRanking(batch, new Dictionary<string, ResearchEvidenceQuality>(StringComparer.OrdinalIgnoreCase));

        var now = _timeProvider.GetUtcNow();
        var planByQuery = plan
            .GroupBy(item => item.Query, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var assessed = batch.Sources
            .Select(source => new AssessedSource(
                source,
                Assess(source, planByQuery.GetValueOrDefault(source.Query), now)))
            .ToList();

        var ordered = new List<ResearchSource>(assessed.Count);
        var qualityBySourceId = new Dictionary<string, ResearchEvidenceQuality>(StringComparer.OrdinalIgnoreCase);
        var hostCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        while (assessed.Count > 0)
        {
            AssessedSource? best = null;
            double bestAdjustedScore = double.MinValue;
            foreach (var candidate in assessed)
            {
                var host = NormalizeHost(candidate.Source.Url.Host);
                var priorHostCount = hostCounts.GetValueOrDefault(host);
                var diversityPenalty = Math.Min(0.18d, priorHostCount * 0.09d);
                var adjustedScore = candidate.Quality.CompositeScore - diversityPenalty;
                if (best is null
                    || adjustedScore > bestAdjustedScore
                    || (Math.Abs(adjustedScore - bestAdjustedScore) < 0.000001d
                        && candidate.Source.ProviderScore > best.Source.ProviderScore))
                {
                    best = candidate;
                    bestAdjustedScore = adjustedScore;
                }
            }

            var selected = best!;
            assessed.Remove(selected);
            var selectedHost = NormalizeHost(selected.Source.Url.Host);
            var selectedPenalty = Math.Min(0.18d, hostCounts.GetValueOrDefault(selectedHost) * 0.09d);
            hostCounts[selectedHost] = hostCounts.GetValueOrDefault(selectedHost) + 1;
            var finalQuality = selected.Quality with { DiversityPenalty = selectedPenalty };
            ordered.Add(selected.Source);
            qualityBySourceId[selected.Source.Id] = finalQuality;
        }

        var warnings = new List<string>(batch.Warnings);
        var newsWithUnverifiedFreshness = ordered.Count(source =>
            planByQuery.TryGetValue(source.Query, out var item)
            && item.Topic == ResearchTopic.News
            && source.PublishedAt is null
            && item.StartDate is null);
        if (newsWithUnverifiedFreshness > 0)
        {
            warnings.Add($"Freshness could not be independently established for {newsWithUnverifiedFreshness} news source(s); avoid treating recency as certain without corroboration.");
        }

        var staleNewsSources = ordered.Count(source =>
            planByQuery.TryGetValue(source.Query, out var item)
            && item.Topic == ResearchTopic.News
            && source.PublishedAt is { } publishedAt
            && now - publishedAt > TimeSpan.FromDays(30));
        if (staleNewsSources > 0)
            warnings.Add($"{staleNewsSources} news source(s) are older than 30 days and may be stale for current-event claims.");

        var distinctHosts = ordered.Select(source => NormalizeHost(source.Url.Host))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (ordered.Count >= 3 && distinctHosts < 2)
            warnings.Add("Research evidence is concentrated in a single host; independent corroboration is limited.");

        var citationById = batch.Citations
            .GroupBy(citation => citation.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var citations = ordered.Select(source => citationById.TryGetValue(source.Id, out var citation)
            ? citation
            : new ResearchCitation(
                source.Id,
                source.Title,
                source.Url,
                source.CanonicalUrl,
                source.Query,
                source.RetrievedAt,
                source.PublishedAt)).ToArray();

        var rankedBatch = batch with
        {
            Sources = ordered,
            Citations = citations,
            Warnings = warnings
        };
        return new ResearchEvidenceRanking(rankedBatch, qualityBySourceId);
    }

    private static ResearchEvidenceQuality Assess(
        ResearchSource source,
        ResearchPlanItem? planItem,
        DateTimeOffset now)
    {
        var relevance = Math.Clamp(source.ProviderScore, 0d, 1d);
        var (authority, authorityBasis) = ScoreAuthority(source.Url);
        var (freshness, freshnessBasis) = ScoreFreshness(source, planItem, now);
        var composite = Math.Clamp(
            (relevance * 0.60d) + (authority * 0.22d) + (freshness * 0.18d),
            0d,
            1d);

        return new ResearchEvidenceQuality(
            relevance,
            authority,
            freshness,
            composite,
            0d,
            authorityBasis,
            freshnessBasis);
    }

    private static (double Score, ResearchAuthorityBasis Basis) ScoreAuthority(Uri url)
    {
        var host = NormalizeHost(url.Host);
        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (host.EndsWith(".gov", StringComparison.OrdinalIgnoreCase)
            || labels.Contains("gov", StringComparer.OrdinalIgnoreCase)
            || host.EndsWith(".mil", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".int", StringComparison.OrdinalIgnoreCase))
        {
            return (0.95d, ResearchAuthorityBasis.GovernmentOrInternational);
        }

        if (host.EndsWith(".edu", StringComparison.OrdinalIgnoreCase)
            || labels.Contains("edu", StringComparer.OrdinalIgnoreCase)
            || labels.Contains("ac", StringComparer.OrdinalIgnoreCase))
        {
            return (0.88d, ResearchAuthorityBasis.AcademicOrInstitutional);
        }

        if (labels.Length >= 3
            && (labels[0].Equals("docs", StringComparison.OrdinalIgnoreCase)
                || labels[0].Equals("developer", StringComparison.OrdinalIgnoreCase)
                || labels[0].Equals("developers", StringComparison.OrdinalIgnoreCase)
                || labels[0].Equals("api", StringComparison.OrdinalIgnoreCase)))
        {
            return (0.74d, ResearchAuthorityBasis.FirstPartyDocumentation);
        }

        return (0.55d, ResearchAuthorityBasis.DefaultWeb);
    }

    private static (double Score, ResearchFreshnessBasis Basis) ScoreFreshness(
        ResearchSource source,
        ResearchPlanItem? planItem,
        DateTimeOffset now)
    {
        if (source.PublishedAt is { } publishedAt)
        {
            var age = now - publishedAt;
            if (age < TimeSpan.Zero)
                return (0.20d, ResearchFreshnessBasis.PublishedTimestamp);
            if (age <= TimeSpan.FromDays(1))
                return (1.00d, ResearchFreshnessBasis.PublishedTimestamp);
            if (age <= TimeSpan.FromDays(7))
                return (0.95d, ResearchFreshnessBasis.PublishedTimestamp);
            if (age <= TimeSpan.FromDays(30))
                return (0.82d, ResearchFreshnessBasis.PublishedTimestamp);
            if (age <= TimeSpan.FromDays(180))
                return (0.65d, ResearchFreshnessBasis.PublishedTimestamp);
            if (age <= TimeSpan.FromDays(365))
                return (0.50d, ResearchFreshnessBasis.PublishedTimestamp);
            return (0.30d, ResearchFreshnessBasis.PublishedTimestamp);
        }

        if (planItem?.StartDate is { } startDate)
        {
            var start = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var windowAge = now - start;
            if (windowAge < TimeSpan.Zero)
                return (0.20d, ResearchFreshnessBasis.SearchWindow);
            if (windowAge <= TimeSpan.FromDays(7))
                return (0.90d, ResearchFreshnessBasis.SearchWindow);
            if (windowAge <= TimeSpan.FromDays(30))
                return (0.80d, ResearchFreshnessBasis.SearchWindow);
            if (windowAge <= TimeSpan.FromDays(365))
                return (0.65d, ResearchFreshnessBasis.SearchWindow);
            return (0.50d, ResearchFreshnessBasis.SearchWindow);
        }

        return planItem?.Topic == ResearchTopic.News
            ? (0.35d, ResearchFreshnessBasis.Unknown)
            : (0.50d, ResearchFreshnessBasis.Unknown);
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        return normalized.StartsWith("www.", StringComparison.Ordinal) ? normalized[4..] : normalized;
    }

    private sealed record AssessedSource(ResearchSource Source, ResearchEvidenceQuality Quality);
}
