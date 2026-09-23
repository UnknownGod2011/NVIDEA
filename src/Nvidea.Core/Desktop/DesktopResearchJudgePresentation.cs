using Nvidea.Core.Research;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Payload-free Windows presentation of canonical research judging authority.
/// Only a fully verified ResearchJudgeEvidence may render a positive status.
/// </summary>
public sealed record DesktopResearchJudgePresentation(
    string Status,
    string EvidenceSummary,
    string DiagnosticSummary,
    bool IsVerified)
{
    public static DesktopResearchJudgePresentation FromEvidence(ResearchJudgeEvidence? evidence)
    {
        if (evidence is null)
        {
            return new(
                "RESEARCH PROVENANCE: NOT VERIFIED",
                "No canonical research provenance projection is available for this evidence view.",
                "Unknown source IDs: none reported.",
                false);
        }

        var verified = evidence.IsVerifiedForJudging &&
                       string.Equals(evidence.Provenance, "verified", StringComparison.Ordinal) &&
                       evidence.UnknownSourceIds.Count == 0;
        var status = verified
            ? "RESEARCH PROVENANCE: VERIFIED"
            : $"RESEARCH PROVENANCE: {evidence.Provenance.ToUpperInvariant()} — NOT VERIFIED";
        var summary = $"Evidence sources: {evidence.EvidenceSourceCount} · verified citations: {evidence.VerifiedCitationCount}";
        var diagnostics = evidence.UnknownSourceIds.Count == 0
            ? "Unknown source IDs: none."
            : $"Unknown source IDs: {string.Join(", ", evidence.UnknownSourceIds)}";

        return new(status, summary, diagnostics, verified);
    }
}
