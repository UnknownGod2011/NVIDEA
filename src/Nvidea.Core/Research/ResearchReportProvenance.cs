namespace Nvidea.Core.Research;

/// <summary>
/// Machine-readable provenance state for a research answer. This is derived only from
/// deterministic evidence verification; model text and human-readable warnings are never
/// treated as provenance authority.
/// </summary>
public enum ResearchProvenanceStatus
{
    Unverified = 0,
    Partial = 1,
    Verified = 2,
    NoSources = 3
}

public sealed record ResearchReportProvenance(
    ResearchProvenanceStatus Status,
    IReadOnlyList<string> UnknownSourceIds)
{
    /// <summary>
    /// Projects a completed report into a stable machine-readable status without parsing warning
    /// text. No-source reports are intentionally distinct from synthesized-but-uncited answers.
    /// </summary>
    public static ResearchReportProvenance FromReport(ResearchReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(report.Evidence);

        if (report.Evidence.Sources.Count == 0)
            return NoSources;

        var provenance = ResearchSynthesisProvenance.Project(
            report.AnswerMarkdown,
            report.Evidence.Citations);

        return FromSynthesis(provenance);
    }

    public static ResearchReportProvenance FromSynthesis(ResearchSynthesisProvenanceResult provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);

        var status = provenance.IsFullyVerified && provenance.VerifiedCitations.Count > 0
            ? ResearchProvenanceStatus.Verified
            : provenance.VerifiedCitations.Count > 0
                ? ResearchProvenanceStatus.Partial
                : ResearchProvenanceStatus.Unverified;

        return new ResearchReportProvenance(status, provenance.UnknownSourceIds.ToArray());
    }

    public static ResearchReportProvenance NoSources { get; } =
        new(ResearchProvenanceStatus.NoSources, []);

    public static ResearchReportProvenance Unverified { get; } =
        new(ResearchProvenanceStatus.Unverified, []);
}
