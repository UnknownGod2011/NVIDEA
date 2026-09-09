namespace Nvidea.Core.Jobs;

public sealed record NebiusResearchLivePreflightReport(
    NebiusResearchDeploymentManifest Manifest,
    int VersionPinnedSecretCount,
    int PrimaryVersionSecretCount)
{
    public bool AllWorkerSecretsVersionPinned => PrimaryVersionSecretCount == 0;
}

/// <summary>
/// Runs the exact zero-cost live validation and returns only reproducibility-safe metadata.
/// Secret ids, version ids, access tokens, static credentials and PEM bodies are never returned.
/// </summary>
public static class NebiusResearchLivePreflightReporter
{
    public static NebiusResearchLivePreflightReport ValidateAndBuild(
        NebiusResearchDispatchOptions dispatchOptions,
        NebiusObjectStorageClientOptions objectStorageOptions,
        string serverlessAccessToken,
        string projectId,
        string clientPrivateKeyPem)
    {
        NebiusResearchLiveDryRunPreflight.Validate(
            dispatchOptions,
            objectStorageOptions,
            serverlessAccessToken,
            projectId,
            clientPrivateKeyPem);

        var manifest = NebiusResearchDeploymentManifestBuilder.Build(dispatchOptions, objectStorageOptions);
        var secretRefs = dispatchOptions.SecretEnvironmentVariables?.Values
            ?? Enumerable.Empty<NebiusMysteryBoxSecretRef>();
        var pinned = secretRefs.Count(static secret => !string.IsNullOrWhiteSpace(secret.VersionId));
        var primary = secretRefs.Count() - pinned;

        return new NebiusResearchLivePreflightReport(manifest, pinned, primary);
    }
}
