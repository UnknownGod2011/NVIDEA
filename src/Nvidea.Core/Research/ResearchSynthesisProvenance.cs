namespace Nvidea.Core.Research;

/// <summary>
/// Projects untrusted model synthesis into the evidence-backed provenance that may be
/// shown as verified to users and judges. Unknown model-emitted source ids never become
/// citations; callers receive explicit warnings suitable for durable/report evidence.
/// </summary>
public static class ResearchSynthesisProvenance
{
    public static ResearchSynthesisProvenanceResult Project(
        string answerMarkdown,
        IReadOnlyCollection<ResearchCitation> availableCitations,
        IEnumerable<string>? existingWarnings = null)
    {
        var integrity = ResearchCitationIntegrity.Verify(answerMarkdown, availableCitations);
        var warnings = existingWarnings?.ToList() ?? [];

        if (integrity.UnknownSourceIds.Count > 0)
        {
            warnings.Add(
                $"Synthesis referenced unverified source ids: {string.Join(", ", integrity.UnknownSourceIds)}. " +
                "Those markers were excluded from verified Tavily provenance.");
        }

        if (integrity.ReferencedSourceIds.Count == 0)
            warnings.Add("Synthesis contained no machine-verifiable source markers; no Tavily provenance is verified for the answer.");

        return new ResearchSynthesisProvenanceResult(
            integrity.VerifiedCitations,
            warnings,
            integrity.IsFullyVerified,
            integrity.UnknownSourceIds);
    }
}

public sealed record ResearchSynthesisProvenanceResult(
    IReadOnlyList<ResearchCitation> VerifiedCitations,
    IReadOnlyList<string> Warnings,
    bool IsFullyVerified,
    IReadOnlyList<string> UnknownSourceIds);
