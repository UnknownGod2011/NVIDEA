using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class WorkerResultSigningDeploymentPreflightTests
{
    [Fact]
    public void Validate_RequiresDedicatedWorkerResultSigningSecretReference()
    {
        var valid = CreateValidOptions();
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(valid.SecretEnvironmentVariables!, StringComparer.Ordinal);
        secrets.Remove(NebiusResearchDeploymentPreflight.WorkerResultSigningPrivateKeyEnvironmentVariable);

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { SecretEnvironmentVariables = secrets }));

        Assert.Contains(NebiusResearchDeploymentPreflight.WorkerResultSigningPrivateKeyEnvironmentVariable, error.Message, StringComparison.Ordinal);
        Assert.Contains("MysteryBox", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsWorkerResultSigningPrivateKeyInPlaintextConfiguration()
    {
        var valid = CreateValidOptions();
        var environment = new Dictionary<string, string>(valid.EnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.WorkerResultSigningPrivateKeyEnvironmentVariable] = "private-key-must-never-be-plaintext"
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { EnvironmentVariables = environment }));

        Assert.Contains("must never be supplied as plaintext", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_AcceptsVersionPinnedWorkerResultSigningSecretReference()
    {
        NebiusResearchDeploymentPreflight.Validate(CreateValidOptions());
    }

    private static NebiusResearchDispatchOptions CreateValidOptions()
    {
        using var client = RSA.Create(2048);
        using var resultClient = RSA.Create(2048);
        return new NebiusResearchDispatchOptions(
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
                [NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable] = "/mnt/nvidea-research",
                [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = client.ExportSubjectPublicKeyInfoPem(),
                [NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable] = resultClient.ExportSubjectPublicKeyInfoPem()
            },
            SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>(StringComparer.Ordinal)
            {
                ["NEBIUS_API_KEY"] = new(SecretId: "mbsec-nebius"),
                ["TAVILY_API_KEY"] = new(SecretId: "mbsec-tavily"),
                ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = new(SecretId: "mbsec-worker-key", VersionId: "mbsecver-worker-key-v1"),
                [NebiusResearchDeploymentPreflight.WorkerResultSigningPrivateKeyEnvironmentVariable] =
                    new(SecretId: "mbsec-worker-result-signing", VersionId: "mbsecver-worker-result-signing-v1")
            },
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount("bucket", "/mnt/nvidea-research", "READ_WRITE")
            });
    }
}
