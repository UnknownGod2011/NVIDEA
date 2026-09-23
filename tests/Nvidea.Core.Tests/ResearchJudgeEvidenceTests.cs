using System.Text.Json;
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
        Assert.Equal(0, evidence.VerifiedCitationCount);
        Assert.Empty(evidence.UnknownSourceIds);
    }

    [Fact]
    public void Serialized_evidence_never_contains_research_payload_question_or_source_metadata()
    {
        const string secretQuestion = "PRIVATE_QUESTION_FIXTURE";
        const string secretAnswer = "PRIVATE_ANSWER_FIXTURE [src:s1].";
        const string secretSourceBody = "PRIVATE_SOURCE_BODY_FIXTURE";
        const string secretQuery = "PRIVATE_QUERY_FIXTURE";

        var citation = new ResearchCitation(
            "s1",
            "PRIVATE_SOURCE_TITLE_FIXTURE",
            SourceUrl,
            SourceUrl.AbsoluteUri.TrimEnd('/'),
            secretQuery,
            DateTimeOffset.UtcNow,
            null);
        var source = new ResearchSource(
            "s1",
            "PRIVATE_SOURCE_TITLE_FIXTURE",
            SourceUrl,
            SourceUrl.AbsoluteUri.TrimEnd('/'),
            secretSourceBody,
            0.9,
            secretQuery,
            DateTimeOffset.UtcNow);
        var report = new ResearchReport(
            secretQuestion,
            secretAnswer,
            new ResearchBatch([source], [citation], 1, []),
            [citation],
            []);

        var json = JsonSerializer.Serialize(ResearchJudgeEvidence.FromReport(report));

        Assert.Contains("verified", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(secretQuestion, json, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE_ANSWER_FIXTURE", json, StringComparison.Ordinal);
        Assert.DoesNotContain(secretSourceBody, json, StringComparison.Ordinal);
        Assert.DoesNotContain(secretQuery, json, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE_SOURCE_TITLE_FIXTURE", json, StringComparison.Ordinal);
        Assert.DoesNotContain(SourceUrl.AbsoluteUri, json, StringComparison.Ordinal);
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
