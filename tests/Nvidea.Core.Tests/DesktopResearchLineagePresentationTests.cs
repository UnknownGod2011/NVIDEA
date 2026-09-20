using Nvidea.Core.Desktop;

namespace Nvidea.Core.Tests;

public sealed class DesktopResearchLineagePresentationTests
{
    private const string PrivateMarker = "PRIVATE-QUESTION-MARKER-DO-NOT-RENDER";
    private const string Sha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Project_ValidReceipt_ProducesClosedVerifiedPresentationWithoutCommitments()
    {
        var receipt = Receipt();

        var presentation = DesktopResearchLineagePresentationProjector.Project(receipt);
        var rendered = string.Join("\n", presentation.RenderedFields);

        Assert.True(presentation.Verified);
        Assert.Contains("Nemotron plan: VERIFIED · 3 queries", rendered, StringComparison.Ordinal);
        Assert.Contains("Tavily evidence: BOUND · 5 sources", rendered, StringComparison.Ordinal);
        Assert.Contains("Cited synthesis: VERIFIED · 2 validated citations", rendered, StringComparison.Ordinal);
        Assert.Contains("Restart lineage: BOUND", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain(Sha, rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PrivateMarker, rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Project_IncompleteOrLegacyReceipt_NeverBecomesGreen()
    {
        AssertClosedFailure(null);
        AssertClosedFailure(Receipt() with { RestartStable = false });
        AssertClosedFailure(Receipt() with { HasMachineVerifiableCitations = false });
        AssertClosedFailure(Receipt() with { ValidatedCitationCount = 0 });
        AssertClosedFailure(Receipt() with { EvidenceSourceCount = 0 });
        AssertClosedFailure(Receipt() with { PlannedQueryCount = 0 });
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-sha")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Project_MalformedOrTamperedCommitmentShape_NeverBecomesGreen(string commitment)
    {
        AssertClosedFailure(Receipt() with { EvidenceSha256 = commitment });
    }

    [Fact]
    public void RenderedFields_AreClosedCopyAndCannotCarryPrivateMarkerStrings()
    {
        // The desktop receipt type has no research-payload fields. Even deliberately using a marker
        // as a malformed commitment must fail closed, and commitments are never rendered.
        var receipt = Receipt() with { PlanSha256 = PrivateMarker };

        var presentation = DesktopResearchLineagePresentationProjector.Project(receipt);
        var rendered = string.Join("\n", presentation.RenderedFields);

        Assert.False(presentation.Verified);
        Assert.DoesNotContain(PrivateMarker, rendered, StringComparison.Ordinal);
        Assert.All(presentation.RenderedFields, field => Assert.DoesNotContain("http", field, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertClosedFailure(DesktopDurableResearchReceipt? receipt)
    {
        var presentation = DesktopResearchLineagePresentationProjector.Project(receipt);
        var rendered = string.Join("\n", presentation.RenderedFields);
        Assert.False(presentation.Verified);
        Assert.DoesNotContain(" · ", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("BOUND", rendered, StringComparison.Ordinal);
        Assert.Contains("NOT VERIFIED", rendered, StringComparison.Ordinal);
    }

    private static DesktopDurableResearchReceipt Receipt() => new(
        PlanSha256: Sha,
        EvidenceSha256: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        SynthesisSha256: "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
        PlannedQueryCount: 3,
        EvidenceSourceCount: 5,
        ValidatedCitationCount: 2,
        MultiQueryPlan: true,
        HasMachineVerifiableCitations: true,
        RestartStable: true);
}
