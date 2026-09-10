namespace Nvidea.Core.Jobs;

/// <summary>
/// Enforces the last local safety gate immediately before live provider resources are constructed.
/// Provider factories are invoked only after the exact canonical judging-artifact destinations from
/// <see cref="NebiusResearchLiveConfiguration"/> have passed distinctness and non-destructive
/// writability checks.
/// </summary>
public static class NebiusResearchLiveProviderStartup
{
    public static T CreateAfterDestinationPreflight<T>(
        NebiusResearchLiveConfiguration configuration,
        Func<T> providerFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(providerFactory);

        NebiusResearchArtifactDestinationPreflight.ValidatePaths(
            configuration.RedactedManifestPath,
            configuration.PassEvidencePath);

        return providerFactory();
    }
}
