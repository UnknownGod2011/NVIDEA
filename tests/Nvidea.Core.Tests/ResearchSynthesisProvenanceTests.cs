using Nvidea.Core.Research;

namespace Nvidea.Core.Tests;

public sealed class ResearchSynthesisProvenanceTests
{
    private static readonly ResearchCitation Citation = new(
        "tavily-1", "Official source", new Uri("https://example.com/source"),
        "https://example.com/source", "query", DateTimeOffset.UnixEpoch, null);

    [Fact]
    public void Project_ExcludesFabricatedMarkersFromVerifiedProvenance()
    {
        var result = ResearchSynthesisProvenance.Project(
            "Supported [src:tavily-1], fabricated [src:invented-99].",
            [Citation],
            ["provider warning"]);

        Assert.False(result.IsFullyVerified);
        Assert.Single(result.VerifiedCitations);
        Assert.Equal("tavily-1", result.VerifiedCitations[0].SourceId);
        Assert.Equal(["invented-99"], result.UnknownSourceIds);
        Assert.Contains("provider warning", result.Warnings);
        Assert.Contains(result.Warnings, warning => warning.Contains("excluded from verified Tavily provenance", StringComparison.Ordinal));
    }

    [Fact]
    public void Project_UncitedAnswerHasNoVerifiedProvenance()
    {
        var result = ResearchSynthesisProvenance.Project("An answer without evidence markers.", [Citation]);

        Assert.False(result.IsFullyVerified);
        Assert.Empty(result.VerifiedCitations);
        Assert.Empty(result.UnknownSourceIds);
        Assert.Contains(result.Warnings, warning => warning.Contains("no Tavily provenance is verified", StringComparison.Ordinal));
    }

    [Fact]
    public void Project_AllEvidenceBackedMarkersAreFullyVerified()
    {
        var result = ResearchSynthesisProvenance.Project("Supported [src:TAVILY-1].", [Citation]);

        Assert.True(result.IsFullyVerified);
        Assert.Single(result.VerifiedCitations);
        Assert.Empty(result.UnknownSourceIds);
        Assert.Empty(result.Warnings);
    }
}
