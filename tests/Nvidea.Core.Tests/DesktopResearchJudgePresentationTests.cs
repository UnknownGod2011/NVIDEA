using Nvidea.Core.Desktop;
using Nvidea.Core.Research;

namespace Nvidea.Core.Tests;

public sealed class DesktopResearchJudgePresentationTests
{
    [Fact]
    public void VerifiedEvidence_RendersVerifiedOnlyWhenCanonicalGateIsTrue()
    {
        var presentation = DesktopResearchJudgePresentation.FromEvidence(
            new ResearchJudgeEvidence("verified", true, 2, 2, Array.Empty<string>()));

        Assert.True(presentation.IsVerified);
        Assert.Equal("RESEARCH PROVENANCE: VERIFIED", presentation.Status);
        Assert.Contains("verified citations: 2", presentation.EvidenceSummary);
    }

    [Theory]
    [InlineData("partial")]
    [InlineData("unverified")]
    [InlineData("no-sources")]
    public void NonVerifiedStates_NeverRenderGreen(string provenance)
    {
        var presentation = DesktopResearchJudgePresentation.FromEvidence(
            new ResearchJudgeEvidence(provenance, false, 2, 1, Array.Empty<string>()));

        Assert.False(presentation.IsVerified);
        Assert.Contains("NOT VERIFIED", presentation.Status);
    }

    [Fact]
    public void FabricatedSourceId_ForcesFailClosedEvenIfCallerClaimsVerified()
    {
        var presentation = DesktopResearchJudgePresentation.FromEvidence(
            new ResearchJudgeEvidence("verified", true, 2, 2, new[] { "fabricated-source" }));

        Assert.False(presentation.IsVerified);
        Assert.Contains("NOT VERIFIED", presentation.Status);
        Assert.Contains("fabricated-source", presentation.DiagnosticSummary);
    }

    [Fact]
    public void MissingEvidence_IsNotVerified()
    {
        var presentation = DesktopResearchJudgePresentation.FromEvidence(null);

        Assert.False(presentation.IsVerified);
        Assert.Contains("NOT VERIFIED", presentation.Status);
    }
}
