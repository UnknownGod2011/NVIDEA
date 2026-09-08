using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchEvidenceRankerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rank_prefers_institutional_authority_when_relevance_is_close()
    {
        var plan = new[] { new ResearchPlanItem("policy", ResearchTopic.General, 5, null, null) };
        var generic = Source("generic", "https://example.com/article", 0.84, "policy");
        var government = Source("gov", "https://agency.gov/report", 0.80, "policy");

        var ranking = Rank([generic, government], plan);

        Assert.Equal("gov", ranking.Batch.Sources[0].Id);
        Assert.Equal(ResearchAuthorityBasis.GovernmentOrInternational, ranking.QualityBySourceId["gov"].AuthorityBasis);
        Assert.True(ranking.QualityBySourceId["gov"].AuthorityScore > ranking.QualityBySourceId["generic"].AuthorityScore);
    }

    [Fact]
    public void Rank_does_not_treat_authority_words_in_arbitrary_subdomains_as_institutional_suffixes()
    {
        var plan = new[] { new ResearchPlanItem("policy", ResearchTopic.General, 5, null, null) };
        var spoof = Source("spoof", "https://gov.example.com/report", 0.90, "policy");

        var ranking = Rank([spoof], plan);

        Assert.Equal(ResearchAuthorityBasis.DefaultWeb, ranking.QualityBySourceId["spoof"].AuthorityBasis);
    }

    [Fact]
    public void Rank_uses_explicit_search_window_as_bounded_freshness_evidence_without_inventing_publish_time()
    {
        var plan = new[]
        {
            new ResearchPlanItem("latest", ResearchTopic.News, 5, new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 9))
        };
        var source = Source("recent-window", "https://news.example.com/story", 0.90, "latest");

        var ranking = Rank([source], plan);

        var quality = ranking.QualityBySourceId["recent-window"];
        Assert.Equal(ResearchFreshnessBasis.SearchWindow, quality.FreshnessBasis);
        Assert.True(quality.FreshnessScore >= 0.80);
        Assert.DoesNotContain(ranking.Batch.Warnings, warning => warning.Contains("could not be independently established", StringComparison.OrdinalIgnoreCase));
        Assert.Null(ranking.Batch.Sources[0].PublishedAt);
    }

    [Fact]
    public void Rank_warns_when_news_freshness_is_unknown_or_stale()
    {
        var plan = new[] { new ResearchPlanItem("latest", ResearchTopic.News, 5, null, null) };
        var unknown = Source("unknown", "https://news.example.com/unknown", 0.92, "latest");
        var stale = Source("stale", "https://other.example.net/stale", 0.91, "latest", Now - TimeSpan.FromDays(45));

        var ranking = Rank([unknown, stale], plan);

        Assert.Contains(ranking.Batch.Warnings, warning => warning.Contains("Freshness could not", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ranking.Batch.Warnings, warning => warning.Contains("older than 30 days", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(ResearchFreshnessBasis.PublishedTimestamp, ranking.QualityBySourceId["stale"].FreshnessBasis);
        Assert.True(ranking.QualityBySourceId["stale"].FreshnessScore < ranking.QualityBySourceId["unknown"].RelevanceScore);
    }

    [Fact]
    public void Rank_applies_host_diversity_penalty_so_one_domain_does_not_monopolize_top_evidence()
    {
        var plan = new[] { new ResearchPlanItem("topic", ResearchTopic.General, 5, null, null) };
        var first = Source("a", "https://example.com/a", 0.95, "topic");
        var secondSameHost = Source("b", "https://example.com/b", 0.94, "topic");
        var independent = Source("c", "https://independent.net/c", 0.90, "topic");

        var ranking = Rank([first, secondSameHost, independent], plan);

        Assert.Equal(new[] { "a", "c", "b" }, ranking.Batch.Sources.Select(source => source.Id).ToArray());
        Assert.Equal(0d, ranking.QualityBySourceId["a"].DiversityPenalty);
        Assert.True(ranking.QualityBySourceId["b"].DiversityPenalty > 0d);
        Assert.Equal(ranking.Batch.Sources.Select(source => source.Id), ranking.Batch.Citations.Select(citation => citation.SourceId));
    }

    private static ResearchEvidenceRanking Rank(
        IReadOnlyList<ResearchSource> sources,
        IReadOnlyList<ResearchPlanItem> plan)
    {
        var batch = new ResearchBatch(
            sources,
            sources.Select(source => new ResearchCitation(
                source.Id,
                source.Title,
                source.Url,
                source.CanonicalUrl,
                source.Query,
                source.RetrievedAt,
                source.PublishedAt)).ToArray(),
            1,
            []);

        return new ResearchEvidenceRanker(new FixedTimeProvider(Now)).Rank(batch, plan);
    }

    private static ResearchSource Source(
        string id,
        string url,
        double providerScore,
        string query,
        DateTimeOffset? publishedAt = null)
    {
        var uri = new Uri(url);
        return new ResearchSource(
            id,
            id,
            uri,
            TavilyResearchClient.Canonicalize(uri),
            $"Evidence from {id}",
            providerScore,
            query,
            Now,
            publishedAt);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
