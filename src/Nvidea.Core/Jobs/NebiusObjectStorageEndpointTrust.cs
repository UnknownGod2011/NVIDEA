namespace Nvidea.Core.Jobs;

internal enum NebiusObjectStorageEndpointTrustFailure
{
    None = 0,
    InvalidRegion,
    InvalidEndpoint,
    RegionMismatch
}

/// <summary>
/// Credential-free trust policy for Nebius Object Storage origins. Keep every static-key path
/// behind this single predicate so configuration preflight and the runtime S3 client cannot drift.
/// </summary>
internal static class NebiusObjectStorageEndpointTrust
{
    internal const int MaximumRegionLength = 64;

    internal static NebiusObjectStorageEndpointTrustFailure Validate(string? endpointValue, string? region)
    {
        if (!IsValidRegion(region))
            return NebiusObjectStorageEndpointTrustFailure.InvalidRegion;

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || endpoint.Port != 443
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment)
            || endpoint.AbsolutePath != "/")
        {
            return NebiusObjectStorageEndpointTrustFailure.InvalidEndpoint;
        }

        var expectedHost = $"storage.{region}.nebius.cloud";
        return string.Equals(endpoint.Host, expectedHost, StringComparison.OrdinalIgnoreCase)
            ? NebiusObjectStorageEndpointTrustFailure.None
            : NebiusObjectStorageEndpointTrustFailure.RegionMismatch;
    }

    internal static bool IsValidRegion(string? region)
    {
        if (string.IsNullOrWhiteSpace(region)
            || region.Length > MaximumRegionLength
            || region[0] == '-'
            || region[^1] == '-')
        {
            return false;
        }

        foreach (var ch in region)
        {
            var isAsciiLower = ch is >= 'a' and <= 'z';
            var isAsciiDigit = ch is >= '0' and <= '9';
            if (!isAsciiLower && !isAsciiDigit && ch != '-')
                return false;
        }

        return true;
    }
}
