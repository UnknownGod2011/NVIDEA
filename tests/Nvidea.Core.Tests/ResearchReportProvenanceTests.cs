using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchReportProvenanceTests
{
    private static readonly Uri SourceUrl = new("https://example.com/source");

    [Fact]
    public void FromReport_marks_fully_evidence_backed_synthesis_verified()
    {
        var report = CreateReport("Supported [src:s1].", [Citation("s1")]);

        var provenance = ResearchReportProvenance.FromReport(report);

        Assert.Equal(ResearchProvenanceStatus.Verified, provenance.Status);
        Assert.Empty(provenance.UnknownSourceIds);
    }

    [Fact]
    public void FromReport_marks_mixed_verified_and_fabricated_markers_partial()
    {
        var report = CreateReport("Supported [src:s1]. Fabricated [src:fake].", [Citation("s1")]);

        var provenance = ResearchReportProvenance.FromReport(report);

        Assert.Equal(ResearchProvenanceStatus.Partial, provenance.Status);
        Assert.Equal(["fake"], provenance.UnknownSourceIds);
    }

    [Fact]
    public void FromReport_marks_uncited_synthesis_unverified()
    {
        var report = CreateReport("An answer with no source marker.", [Citation("s1")]);

        var provenance = ResearchReportProvenance.FromReport(report);

        Assert.Equal(ResearchProvenanceStatus.Unverified, provenance.Status);
        Assert.Empty(provenance.UnknownSourceIds);
    }

    [Fact]
    public void FromReport_distinguishes_no_sources_from_uncited_synthesis()
    {
        var batch = new ResearchBatch([], [], 1, []);
        var report = new ResearchReport("question", "No reliable evidence.", batch, [], ["No research sources were returned."]);

        var provenance = ResearchReportProvenance.FromReport(report);

        Assert.Equal(ResearchProvenanceStatus.NoSources, provenance.Status);
        Assert.Empty(provenance.UnknownSourceIds);
    }

    private static ResearchReport CreateReport(string answer, IReadOnlyList<ResearchCitation> citations)
    {
        var source = new ResearchSource(
            "s1", "Source", SourceUrl, SourceUrl.AbsoluteUri.TrimEnd('/'), "Evidence", 0.9,
            "query", DateTimeOffset.UtcNow);
        var batch = new ResearchBatch([source], citations, 1, []);
        return new ResearchReport("question", answer, batch, citations, []);
    }

    private static ResearchCitation Citation(string id) =>
        new(id, "Source", SourceUrl, SourceUrl.AbsoluteUri.TrimEnd('/'), "query", DateTimeOffset.UtcNow, null);
}
