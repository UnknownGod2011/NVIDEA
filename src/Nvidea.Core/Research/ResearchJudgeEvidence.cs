namespace Nvidea.Core.Research;

/// <summary>
/// Payload-safe, machine-readable research provenance evidence for evaluator/UI surfaces.
/// This projection deliberately contains identifiers and counts only; source bodies, prompts,
/// credentials and private context never cross this boundary.
/// </summary>
public sealed record ResearchJudgeEvidence(
    string Provenance,
    bool Verified,
    int EvidenceSourceCount,
    int VerifiedCitationCount,
    IReadOnlyList<string> UnknownSourceIds)
{
    public static ResearchJudgeEvidence FromReport(ResearchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(report.Evidence);

        var provenance = ResearchReportProvenance.FromReport(report);
        var synthesis = ResearchSynthesisProvenance.Project(
            report.AnswerMarkdown,
            report.Evidence.Citations);

        return new ResearchJudgeEvidence(
            provenance.JudgeLabel,
            provenance.IsVerifiedForJudging,
            report.Evidence.Sources.Count,
            synthesis.VerifiedCitations.Count,
            provenance.UnknownSourceIds.ToArray());
    }
}
