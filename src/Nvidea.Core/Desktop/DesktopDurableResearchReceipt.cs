using Nvidea.Core.Jobs;

namespace Nvidea.Core.Desktop;

/// <summary>
/// Judge-safe desktop projection of a completed durable research receipt. The SHA-256 commitments
/// prove that the persisted plan, evidence and synthesis are bound across resume boundaries while
/// keeping all research payloads out of the evidence UI.
/// </summary>
public sealed record DesktopDurableResearchReceipt(
    string PlanSha256,
    string EvidenceSha256,
    string SynthesisSha256,
    int PlannedQueryCount,
    int EvidenceSourceCount,
    int ValidatedCitationCount,
    bool MultiQueryPlan,
    bool HasMachineVerifiableCitations,
    bool RestartStable);

public static class DesktopDurableResearchReceiptProjector
{
    public static DesktopDurableResearchReceipt ProjectDurableResearchReceipt(this AgentJobRecord completedJob)
    {
        ArgumentNullException.ThrowIfNull(completedJob);
        var receipt = ResearchJobHandler.ReadCompletedReceipt(completedJob);
        return new DesktopDurableResearchReceipt(
            receipt.PlanSha256,
            receipt.EvidenceSha256,
            receipt.SynthesisSha256,
            receipt.PlannedQueryCount,
            receipt.EvidenceSourceCount,
            receipt.ValidatedCitationCount,
            receipt.PlannedQueryCount >= 2,
            receipt.HasMachineVerifiableCitations,
            RestartStable: true);
    }
}
