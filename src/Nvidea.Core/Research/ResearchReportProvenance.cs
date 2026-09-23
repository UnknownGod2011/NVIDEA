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
