namespace Nvidea.Core.Jobs;

/// <summary>
/// Canonicalizes the small amount of provider failure metadata that NVIDEA allows into durable
/// remote provenance. This is deliberately narrow: only codes already recognized by the fixed
/// local remediation allowlist are migrated from legacy LastError evidence. Provider messages are
/// never copied, parsed for authority, or persisted into structured provenance.
/// </summary>
public static class RemoteResearchFailureProvenanceMigration
{
    public static AgentJobRecord Migrate(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var provenance = record.RemoteResearch;
        if (provenance is null
            || provenance.State != RemoteResearchProvenanceState.RemoteFailed
            || !string.IsNullOrWhiteSpace(provenance.ProviderFailureCode))
        {
            return record;
        }

        var remediation = NebiusFailureRemediationPolicy.ClassifyPersistedFailureEvidence(record.LastError);
        if (remediation is null)
            return record;

        return record with
        {
            RemoteResearch = provenance with
            {
                ProviderFailureCode = remediation.ProviderCode
            }
        };
    }
}
