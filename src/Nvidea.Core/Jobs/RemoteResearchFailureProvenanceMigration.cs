namespace Nvidea.Core.Jobs;

/// <summary>
/// Canonicalizes the small amount of provider failure metadata that NVIDEA allows into durable
/// remote provenance. Structured provider codes pass through the shared evidence trust boundary,
/// while legacy LastError migration remains deliberately narrower and allowlist-only. Provider
/// messages are never copied, parsed for authority, or persisted into structured provenance.
/// </summary>
public static class RemoteResearchFailureProvenanceMigration
{
    public static AgentJobRecord Migrate(AgentJobRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var provenance = record.RemoteResearch;
        if (provenance is null)
            return record;

        var canonicalCode = ProviderFailureCodeTrust.CanonicalizeOrThrow(provenance.ProviderFailureCode);
        if (!string.Equals(canonicalCode, provenance.ProviderFailureCode, StringComparison.Ordinal))
        {
            record = record with
            {
                RemoteResearch = provenance with
                {
                    ProviderFailureCode = canonicalCode
                }
            };
            provenance = record.RemoteResearch;
        }

        if (provenance is null
            || provenance.State != RemoteResearchProvenanceState.RemoteFailed
            || provenance.ProviderFailureCode is not null)
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
