using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchCredentialFreeTopologyTests
{
    [Fact]
    public void CredentialFreeTopology_AcceptsValidShapeWithoutClientPublicKey()
    {
        var options = CreateTopologyOnlyOptions();

        NebiusResearchDeploymentPreflight.ValidateCredentialFreeTopology(options);
    }

    [Fact]
    public void CredentialFreeTopology_RejectsMutableWorkerImageBeforeKeyMaterialIsNeeded()
    {
        var options = CreateTopologyOnlyOptions() with
        {
            WorkerImage = "registry.example/nvidea-worker:latest",
            WorkerPublicKeyPem = string.Empty
        };

        var error = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchDeploymentPreflight.ValidateCredentialFreeTopology(options));

        Assert.Contains("digest-pinned", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullValidation_StillRequiresClientPublicKeyAfterTopologyPasses()
    {
        var options = CreateTopologyOnlyOptions();

        var error = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchDeploymentPreflight.Validate(options));

        Assert.Contains(
            NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable,
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Alignment_UsesCredentialFreeNamespaceContract()
    {
        var options = CreateTopologyOnlyOptions() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount(
                    "nvidea-live-bucket",
                    "/mnt/nvidea-research",
                    "READ_WRITE",
                    "nvidea-research")
            }
        };

        NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(
            options,
            new NebiusObjectStorageTransportAlignment(
                Bucket: "nvidea-live-bucket",
                Prefix: "nvidea-research"));
    }

    [Fact]
    public void Alignment_RejectsMismatchWithoutRuntimeClientOptions()
    {
        var options = CreateTopologyOnlyOptions() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount(
                    "worker-bucket",
                    "/mnt/nvidea-research",
                    "READ_WRITE",
                    "nvidea-research")
            }
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(
                options,
                new NebiusObjectStorageTransportAlignment(
                    Bucket: "client-bucket",
                    Prefix: "nvidea-research")));

        Assert.Contains("bucket", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static NebiusResearchDispatchOptions CreateTopologyOnlyOptions() =>
        new(
            WorkerImage: $"registry.example/nvidea-worker@sha256:{new string('a', 64)}",
            WorkerPublicKeyPem: "public-key-used-by-client-envelope-protection",
            ContainerCommand: "dotnet",
            Platform: "cpu-d3",
            Preset: "1vcpu-4gb",
            Timeout: "3600s",
            SubnetId: "subnet-test",
            Disk: new NebiusServerlessDiskSpec("NETWORK_SSD", 10L * 1024 * 1024 * 1024),
            EnvironmentVariables: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable] = "/mnt/nvidea-research"
            },
            SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>(StringComparer.Ordinal)
            {
                ["NEBIUS_API_KEY"] = new(SecretId: "mbsec-nebius"),
                ["TAVILY_API_KEY"] = new(SecretId: "mbsec-tavily"),
                ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = new(
                    SecretId: "mbsec-worker-key",
                    VersionId: "mbsecver-worker-key-v1")
            },
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount(
                    "bucket",
                    "/mnt/nvidea-research",
                    "READ_WRITE")
            });
}
