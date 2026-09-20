using Nvidea.Core.Desktop;
using Nvidea.Core.Research;

namespace Nvidea.Core.Tests;

public sealed class DesktopResearchEvidenceTests
{
    [Fact]
    public void Project_EmitsOnlyClosedEvidenceMetrics()
    {
        var now = DateTimeOffset.Parse("2026-09-20T00:00:00Z");
        var first = Source("src-1", "https://docs.nvidia.com/a", "https://docs.nvidia.com/a", "nemotron current docs", now, now.AddDays(-1));
        var second = Source("src-2", "https://docs.tavily.com/b", "https://docs.tavily.com/b", "tavily research api", now, null);
        var batch = new ResearchBatch(
            [first, second],
            [Citation(first), Citation(second)],
            ProviderCreditsUsed: 3,
            Warnings: ["freshness unknown for src-2"]);
        var report = new ResearchReport("private question", "private answer [src:src-1]", batch, [Citation(first)], batch.Warnings);

        var evidence = DesktopResearchEvidenceProjector.Project(report);

        Assert.Equal(2, evidence.SourceCount);
        Assert.Equal(1, evidence.UsedCitationCount);
        Assert.Equal(2, evidence.UniqueQueryCount);
        Assert.Equal(2, evidence.UniqueHostCount);
        Assert.Equal(1, evidence.UnknownFreshnessCount);
        Assert.Equal(1, evidence.WarningCount);
        Assert.Equal(3, evidence.ProviderCreditsUsed);
        Assert.True(evidence.MultiQueryEvidence);
        Assert.True(evidence.CanonicalSourcesUnique);
        Assert.True(evidence.HasMachineVerifiableCitations);
        Assert.DoesNotContain("private", evidence.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nvidia.com", evidence.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_DetectsDuplicateCanonicalSourcesAndMissingCitations()
    {
        var now = DateTimeOffset.Parse("2026-09-20T00:00:00Z");
        var first = Source("src-1", "https://example.com/a?x=1", "https://example.com/a", "one", now, null);
        var duplicate = Source("src-2", "https://example.com/a?x=2", "https://example.com/a", "one", now, null);
        var batch = new ResearchBatch([first, duplicate], [Citation(first), Citation(duplicate)], 2, []);
        var report = new ResearchReport("q", "insufficient evidence", batch, [], []);

        var evidence = DesktopResearchEvidenceProjector.Project(report);

        Assert.False(evidence.MultiQueryEvidence);
        Assert.False(evidence.CanonicalSourcesUnique);
        Assert.False(evidence.HasMachineVerifiableCitations);
        Assert.Equal(2, evidence.UnknownFreshnessCount);
    }

    [Fact]
    public void ResearchEvidence_ProjectsThroughWindowsFacingInvocationResult()
    {
        var now = DateTimeOffset.Parse("2026-09-20T00:00:00Z");
        var source = Source("src-1", "https://example.com/a", "https://example.com/a", "one", now, now);
        var citation = Citation(source);
        var report = new ResearchReport("q", "answer [src:src-1]", new ResearchBatch([source], [citation], 1, []), [citation], []);
        var result = new DesktopInvocationResult("answer", null, DesktopInvocationMode.Research, [], report);

        var evidence = result.ResearchEvidence();

        Assert.NotNull(evidence);
        Assert.Equal(1, evidence!.SourceCount);
        Assert.True(evidence.HasMachineVerifiableCitations);
    }

    private static ResearchSource Source(string id, string url, string canonical, string query, DateTimeOffset retrieved, DateTimeOffset? published) =>
        new(id, "title", new Uri(url), canonical, "content", 0.9, query, retrieved, published);

    private static ResearchCitation Citation(ResearchSource source) =>
        new(source.Id, source.Title, source.Url, source.CanonicalUrl, source.Query, source.RetrievedAt, source.PublishedAt);
}
