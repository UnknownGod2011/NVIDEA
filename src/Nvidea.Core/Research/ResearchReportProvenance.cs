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
    /// True only when every source marker emitted by synthesis resolves to supplied research
    /// evidence and at least one evidence-backed citation was used. Judge/demo surfaces should
    /// use this property rather than interpreting enum ordering, warnings, or model prose.
    /// </summary>
    public bool IsVerifiedForJudging => Status == ResearchProvenanceStatus.Verified;

    /// <summary>
    /// Stable, non-model-authored label suitable for evidence JSON and UI presentation.
    /// </summary>
    public string JudgeLabel => Status switch
    {
        ResearchProvenanceStatus.Verified => "verified",
        ResearchProvenanceStatus.Partial => "partial",
        ResearchProvenanceStatus.NoSources => "no-sources",
        _ => "unverified"
    };

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
