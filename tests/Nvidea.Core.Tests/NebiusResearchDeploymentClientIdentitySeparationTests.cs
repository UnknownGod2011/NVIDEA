using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDeploymentClientIdentitySeparationTests
{
    [Fact]
    public void Validate_AcceptsDistinctSigningAndResultEncryptionIdentities()
    {
        var options = CreateValidOptions(out _, out _);

        NebiusResearchDeploymentPreflight.Validate(options);
    }

    [Fact]
    public void Validate_RequiresResultEncryptionPublicIdentity()
    {
        var options = CreateValidOptions(out _, out _);
        var environment = new Dictionary<string, string>(options.EnvironmentVariables!, StringComparer.Ordinal);
        environment.Remove(NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable);

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(options with { EnvironmentVariables = environment }));

        Assert.Contains(NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsReusedSigningAndResultEncryptionIdentity()
    {
        var options = CreateValidOptions(out var signingPublicKey, out _);
        var environment = new Dictionary<string, string>(options.EnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable] = signingPublicKey
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(options with { EnvironmentVariables = environment }));

        Assert.Contains("distinct RSA key pairs", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsPrivateMaterialInResultEncryptionPublicIdentity()
    {
        using var result = RSA.Create(2048);
        var options = CreateValidOptions(out _, out _);
        var environment = new Dictionary<string, string>(options.EnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable] = result.ExportPkcs8PrivateKeyPem()
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(options with { EnvironmentVariables = environment }));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsSecretBackedResultEncryptionIdentity()
    {
        var options = CreateValidOptions(out _, out _);
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(options.SecretEnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable] =
                new(SecretId: "mbsec-client-result-public")
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(options with { SecretEnvironmentVariables = secrets }));

        Assert.Contains("public result-encryption identity", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not as a secret reference", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static NebiusResearchDispatchOptions CreateValidOptions(
        out string signingPublicKey,
        out string resultPublicKey)
    {
        using var signing = RSA.Create(2048);
        using var result = RSA.Create(2048);
        signingPublicKey = signing.ExportSubjectPublicKeyInfoPem();
        resultPublicKey = result.ExportSubjectPublicKeyInfoPem();

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
                [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = signingPublicKey,
                [NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable] = resultPublicKey
            },
            SecretEnvironmentVariables: new Dictionary<string, NebiusMysteryBoxSecretRef>(StringComparer.Ordinal)
            {
                ["NEBIUS_API_KEY"] = new(SecretId: "mbsec-nebius"),
                ["TAVILY_API_KEY"] = new(SecretId: "mbsec-tavily"),
                ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = new(SecretId: "mbsec-worker-key", VersionId: "mbsecver-worker-key-v1")
            },
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount("bucket", "/mnt/nvidea-research", "READ_WRITE")
            });
    }
}
