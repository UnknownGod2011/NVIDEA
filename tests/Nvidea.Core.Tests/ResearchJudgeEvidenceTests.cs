using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchJudgeEvidenceTests
{
    private static readonly Uri SourceUrl = new("https://example.com/source");

    [Fact]
    public void FromReport_exposes_verified_evidence_without_source_payloads()
    {
        var report = CreateReport("Grounded [src:s1].", [Citation("s1")]);

        var evidence = ResearchJudgeEvidence.FromReport(report);

        Assert.Equal("verified", evidence.Provenance);
        Assert.True(evidence.Verified);
        Assert.Equal(1, evidence.EvidenceSourceCount);
        Assert.Equal(1, evidence.VerifiedCitationCount);
        Assert.Empty(evidence.UnknownSourceIds);
    }

    [Fact]
    public void FromReport_fails_closed_when_model_mixes_real_and_fabricated_markers()
    {
        var report = CreateReport("Grounded [src:s1]. Hallucinated [src:fake].", [Citation("s1")]);

        var evidence = ResearchJudgeEvidence.FromReport(report);

        Assert.Equal("partial", evidence.Provenance);
        Assert.False(evidence.Verified);
        Assert.Equal(1, evidence.VerifiedCitationCount);
        Assert.Equal(["fake"], evidence.UnknownSourceIds);
    }

    [Fact]
    public void FromReport_fails_closed_for_uncited_answer_even_when_sources_exist()
    {
        var report = CreateReport("No citation markers.", [Citation("s1")]);

        var evidence = ResearchJudgeEvidence.FromReport(report);

        Assert.Equal("unverified", evidence.Provenance);
        Assert.False(evidence.Verified);
        Assert.Equal(1, evidence.EvidenceSourceCount);
        Assert.Equal(1, evidence.VerifiedCitationCount);
        Assert.Empty(evidence.UnknownSourceIds);
    }

    private static ResearchReport CreateReport(string answer, IReadOnlyList<ResearchCitation> citations)
    {
        var source = new ResearchSource(
            "s1", "Source", SourceUrl, SourceUrl.AbsoluteUri.TrimEnd('/'), "Sensitive fixture body", 0.9,
            "query", DateTimeOffset.UtcNow);
        var batch = new ResearchBatch([source], citations, 1, []);
        return new ResearchReport("question", answer, batch, citations, []);
    }

    private static ResearchCitation Citation(string id) =>
        new(id, "Source", SourceUrl, SourceUrl.AbsoluteUri.TrimEnd('/'), "query", DateTimeOffset.UtcNow, null);
}
