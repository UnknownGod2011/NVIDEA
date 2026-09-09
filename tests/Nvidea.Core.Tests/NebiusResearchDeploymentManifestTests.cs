using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDeploymentManifestTests
{
    [Fact]
    public void Build_IsDeterministicAndRedactsRawInfrastructureIdentifiers()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem());
        var storage = CreateStorage();

        var first = NebiusResearchDeploymentManifestBuilder.Build(dispatch, storage);
        var second = NebiusResearchDeploymentManifestBuilder.Build(dispatch, storage);
        var json = NebiusResearchDeploymentManifestBuilder.ToJson(first);

        Assert.Equal(first.DeploymentFingerprintSha256, second.DeploymentFingerprintSha256);
        Assert.Equal(64, first.DeploymentFingerprintSha256.Length);
        Assert.Equal("sha256:" + new string('a', 64), first.WorkerImageDigest);
        Assert.Contains(first.Secrets, secret => secret.EnvironmentVariable == "NEBIUS_API_KEY" && secret.ReferenceType == "primary-version");
        Assert.Contains(first.Secrets, secret => secret.EnvironmentVariable == "TAVILY_API_KEY" && secret.ReferenceType == "version-pinned");
        Assert.DoesNotContain("mbsec-nebius-sensitive", json, StringComparison.Ordinal);
        Assert.DoesNotContain("mbsecver-tavily-sensitive", json, StringComparison.Ordinal);
        Assert.DoesNotContain("nvidea-live-bucket", json, StringComparison.Ordinal);
        Assert.DoesNotContain("subnet-sensitive", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-access-key", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ChangesFingerprintWhenPinnedSecretVersionChangesWithoutExposingVersionId()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem());
        var changedSecrets = dispatch.SecretEnvironmentVariables!.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        changedSecrets["TAVILY_API_KEY"] = new NebiusMysteryBoxSecretRef(SecretId: "mbsec-tavily", VersionId: "mbsecver-tavily-rotated");
        var changed = dispatch with { SecretEnvironmentVariables = changedSecrets };

        var originalManifest = NebiusResearchDeploymentManifestBuilder.Build(dispatch, CreateStorage());
        var changedManifest = NebiusResearchDeploymentManifestBuilder.Build(changed, CreateStorage());
        var changedJson = NebiusResearchDeploymentManifestBuilder.ToJson(changedManifest);

        Assert.NotEqual(originalManifest.DeploymentFingerprintSha256, changedManifest.DeploymentFingerprintSha256);
        Assert.DoesNotContain("mbsecver-tavily-rotated", changedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Reporter_SurfacesVersionPinCoverageWithoutReturningSecretReferences()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem());

        var report = NebiusResearchLivePreflightReporter.ValidateAndBuild(
            dispatch,
            CreateStorage(),
            "serverless-access-token",
            "project-sensitive",
            client.ExportPkcs8PrivateKeyPem());

        Assert.Equal(2, report.VersionPinnedSecretCount);
        Assert.Equal(1, report.PrimaryVersionSecretCount);
        Assert.False(report.AllWorkerSecretsVersionPinned);
        var json = NebiusResearchDeploymentManifestBuilder.ToJson(report.Manifest);
        Assert.DoesNotContain("project-sensitive", json, StringComparison.Ordinal);
        Assert.DoesNotContain("serverless-access-token", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Reporter_ReportsFullyPinnedConfigurationAndFingerprintStillRedactsVersionIds()
    {
        using var worker = RSA.Create(2048);
        using var client = RSA.Create(2048);
        var dispatch = CreateDispatch(worker.ExportSubjectPublicKeyInfoPem(), client.ExportSubjectPublicKeyInfoPem());
        var pinnedSecrets = dispatch.SecretEnvironmentVariables!.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        pinnedSecrets["NEBIUS_API_KEY"] = new NebiusMysteryBoxSecretRef(
            SecretId: "mbsec-nebius-sensitive",
            VersionId: "mbsecver-nebius-sensitive");
        var fullyPinned = dispatch with { SecretEnvironmentVariables = pinnedSecrets };

        var report = NebiusResearchLivePreflightReporter.ValidateAndBuild(
            fullyPinned,
            CreateStorage(),
            "serverless-access-token",
            "project-sensitive",
            client.ExportPkcs8PrivateKeyPem());
        var json = NebiusResearchDeploymentManifestBuilder.ToJson(report.Manifest);

        Assert.Equal(3, report.VersionPinnedSecretCount);
        Assert.Equal(0, report.PrimaryVersionSecretCount);
        Assert.True(report.AllWorkerSecretsVersionPinned);
        Assert.Equal(64, report.Manifest.DeploymentFingerprintSha256.Length);
        Assert.DoesNotContain("mbsecver-nebius-sensitive", json, StringComparison.Ordinal);
        Assert.DoesNotContain("mbsecver-tavily-sensitive", json, StringComparison.Ordinal);
        Assert.DoesNotContain("mbsecver-worker-sensitive", json, StringComparison.Ordinal);
    }

    private static NebiusResearchDispatchOptions CreateDispatch(string workerPublicKeyPem, string clientPublicKeyPem) =>
        new(
            WorkerImage: $"cr.eu-north1.nebius.cloud/nvidea/worker@sha256:{new string('a', 64)}",
            WorkerPublicKeyPem: workerPublicKeyPem,
            ContainerCommand: "dotnet",
            Platform: "cpu-d3",
            Preset: "1vcpu-4gb",
            Timeout: "3600s",
            SubnetId: "subnet-sensitive",
            Disk: new NebiusServerlessDiskSpec("NETWORK_SSD", 10L * 1024 * 1024 * 1024),
            EnvironmentVariables: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable] = "/mnt/nvidea-research",
                [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = clientPublicKeyPem
            },
            SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>(StringComparer.Ordinal)
            {
                ["NEBIUS_API_KEY"] = new(SecretId: "mbsec-nebius-sensitive"),
                ["TAVILY_API_KEY"] = new(SecretId: "mbsec-tavily", VersionId: "mbsecver-tavily-sensitive"),
                ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = new(VersionId: "mbsecver-worker-sensitive")
            },
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount("nvidea-live-bucket", "/mnt/nvidea-research", "READ_WRITE", "nvidea-research")
            });

    private static NebiusObjectStorageClientOptions CreateStorage() =>
        new(
            Endpoint: "https://storage.eu-north1.nebius.cloud",
            Region: "eu-north1",
            Bucket: "nvidea-live-bucket",
            AccessKeyId: "access-key",
            SecretAccessKey: "secret-access-key",
            Prefix: "nvidea-research",
            OperationTimeout: TimeSpan.FromSeconds(30),
            MaxRetries: 2);
}
