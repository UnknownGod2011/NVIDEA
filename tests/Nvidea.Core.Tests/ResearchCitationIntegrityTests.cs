using Nvidea.Core.Research;

namespace Nvidea.Core.Tests;

public sealed class ResearchCitationIntegrityTests
{
    [Fact]
    public void Verify_AcceptsOnlyMarkersBackedByAvailableEvidence()
    {
        var citations = new[] { Citation("src-1"), Citation("src-2") };

        var result = ResearchCitationIntegrity.Verify(
            "Claim one [src:src-1]. Claim two [src:src-2]. Again [src:SRC-1].",
            citations);

        Assert.True(result.IsFullyVerified);
        Assert.Equal(new[] { "src-1", "src-2" }, result.ReferencedSourceIds, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(2, result.VerifiedCitations.Count);
        Assert.Empty(result.UnknownSourceIds);
    }

    [Fact]
    public void Verify_FailsClosedForFabricatedSourceMarkers()
    {
        var result = ResearchCitationIntegrity.Verify(
            "Supported [src:src-1], fabricated [src:invented-99].",
            new[] { Citation("src-1") });

        Assert.False(result.IsFullyVerified);
        Assert.Single(result.VerifiedCitations);
        Assert.Equal(new[] { "invented-99" }, result.UnknownSourceIds);
    }

    [Fact]
    public void Verify_DoesNotTreatUncitedAnswerAsVerified()
    {
        var result = ResearchCitationIntegrity.Verify("An answer with no provenance marker.", new[] { Citation("src-1") });

        Assert.False(result.IsFullyVerified);
        Assert.Empty(result.ReferencedSourceIds);
        Assert.Empty(result.VerifiedCitations);
        Assert.Empty(result.UnknownSourceIds);
    }

    private static ResearchCitation Citation(string id) => new(
        id,
        $"Title {id}",
        new Uri($"https://example.com/{id}"),
        $"https://example.com/{id}",
        "query",
        DateTimeOffset.Parse("2026-09-23T00:00:00Z"),
        null);
}
