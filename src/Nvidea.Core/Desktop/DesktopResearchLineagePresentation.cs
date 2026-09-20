namespace Nvidea.Core.Desktop;

/// <summary>
/// Closed, judge-safe presentation of durable research lineage. The model intentionally contains
/// only fixed product copy and structural counts; commitments and research payloads never cross
/// this UI boundary.
/// </summary>
public sealed record DesktopResearchLineagePresentation(
    bool Verified,
    string Status,
    string Plan,
    string Evidence,
    string Synthesis,
    string RestartLineage)
{
    public IEnumerable<string> RenderedFields => new[] { Status, Plan, Evidence, Synthesis, RestartLineage };
}

public static class DesktopResearchLineagePresentationProjector
{
    private const string Unavailable = "No completed durable research receipt is available for the current job.";

    public static DesktopResearchLineagePresentation Project(DesktopDurableResearchReceipt? receipt)
    {
        if (!IsClosedVerifiedReceipt(receipt))
        {
            return new DesktopResearchLineagePresentation(
                Verified: false,
                Status: Unavailable,
                Plan: "Nemotron plan: NOT VERIFIED",
                Evidence: "Tavily evidence: NOT VERIFIED",
                Synthesis: "Cited synthesis: NOT VERIFIED",
                RestartLineage: "Restart lineage: NOT VERIFIED");
        }

        return new DesktopResearchLineagePresentation(
            Verified: true,
            Status: "Durable research lineage: VERIFIED",
            Plan: receipt!.MultiQueryPlan
                ? $"Nemotron plan: VERIFIED · {receipt.PlannedQueryCount} queries"
                : $"Nemotron plan: VERIFIED · {receipt.PlannedQueryCount} query",
            Evidence: $"Tavily evidence: BOUND · {receipt.EvidenceSourceCount} sources",
            Synthesis: $"Cited synthesis: VERIFIED · {receipt.ValidatedCitationCount} validated citations",
            RestartLineage: "Restart lineage: BOUND");
    }

    private static bool IsClosedVerifiedReceipt(DesktopDurableResearchReceipt? receipt)
    {
        if (receipt is null || !receipt.RestartStable || !receipt.HasMachineVerifiableCitations)
            return false;

        if (receipt.PlannedQueryCount <= 0 || receipt.EvidenceSourceCount <= 0 || receipt.ValidatedCitationCount <= 0)
            return false;

        // The commitments are deliberately never rendered, but malformed/missing commitments must
        // not produce a green judge surface even if a caller constructs this projection directly.
        return IsSha256(receipt.PlanSha256)
            && IsSha256(receipt.EvidenceSha256)
            && IsSha256(receipt.SynthesisSha256);
    }

    private static bool IsSha256(string value)
        => value.Length == 64 && value.All(static c => char.IsAsciiHexDigit(c));
}
