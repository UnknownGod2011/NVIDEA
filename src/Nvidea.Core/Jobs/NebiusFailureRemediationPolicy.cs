namespace Nvidea.Core.Jobs;

/// <summary>
/// Fixed local recovery guidance for a small allowlist of provider failure codes.
/// Provider messages are intentionally not accepted by this policy: they remain untrusted
/// display/audit evidence and can never select retries, resource changes, or other actions.
/// </summary>
public sealed record NebiusFailureRemediation(
    string ProviderCode,
    string Category,
    string Guidance);

public static class NebiusFailureRemediationPolicy
{
    private const string FailureEvidencePrefix =
        "Nebius remote research stage failed. Provider diagnostic (untrusted): code=";

    private static readonly NebiusFailureRemediation CapacityUnavailable = new(
        "NotEnoughResources",
        "capacity",
        "Nebius capacity is unavailable for the requested Serverless shape. Review the approved platform/preset and cost impact, then retry manually later or choose another approved capacity target.");

    private static readonly NebiusFailureRemediation QuotaInsufficient = new(
        "Quota",
        "quota",
        "Nebius project quota appears insufficient for this Serverless request. Review the project quota and cost limits, then request quota or manually choose an approved project/resource shape.");

    /// <summary>
    /// Classifies only an exact normalized provider code. Unknown or malformed values produce no
    /// guidance. Evidence validation is delegated to ProviderFailureCodeTrust and remains separate
    /// from this policy's much narrower action/guidance allowlist.
    /// </summary>
    public static NebiusFailureRemediation? Classify(string? providerCode)
    {
        if (!ProviderFailureCodeTrust.TryCanonicalize(providerCode, out var normalized)
            || normalized is null)
        {
            return null;
        }

        if (string.Equals(normalized, CapacityUnavailable.ProviderCode, StringComparison.OrdinalIgnoreCase))
            return CapacityUnavailable;
        if (string.Equals(normalized, QuotaInsufficient.ProviderCode, StringComparison.OrdinalIgnoreCase))
            return QuotaInsufficient;

        return null;
    }

    /// <summary>
    /// Rehydrates only a fixed remediation classification from NVIDEA's own persisted remote-failure
    /// evidence format. The provider message is never read or copied into the result. This keeps the
    /// existing durable LastError format useful without promoting provider-controlled text into UI policy.
    /// </summary>
    public static NebiusFailureRemediation? ClassifyPersistedFailureEvidence(string? lastError)
    {
        if (string.IsNullOrWhiteSpace(lastError)
            || !lastError.StartsWith(FailureEvidencePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var codeStart = FailureEvidencePrefix.Length;
        var codeEnd = lastError.IndexOfAny(new[] { ';', '.' }, codeStart);
        if (codeEnd < 0)
            codeEnd = lastError.Length;
        if (codeEnd <= codeStart)
            return null;

        return Classify(lastError[codeStart..codeEnd]);
    }
}
