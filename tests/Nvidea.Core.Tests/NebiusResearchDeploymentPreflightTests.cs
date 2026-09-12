using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchDeploymentPreflightTests
{
    [Fact]
    public void Validate_AcceptsMountedReadWriteTransportAndSecretBackedWorkerCredentials()
    {
        var options = CreateValidOptions();

        NebiusResearchDeploymentPreflight.Validate(options);
    }

    [Fact]
    public void Validate_RejectsTransportRootWithoutExactMountedVolume()
    {
        var options = CreateValidOptions() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount("bucket", "/mnt/other", "READ_WRITE")
            }
        };

        var error = Assert.Throws<InvalidOperationException>(() => NebiusResearchDeploymentPreflight.Validate(options));
        Assert.Contains("exactly match one configured", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsReadOnlyResearchTransport()
    {
        var options = CreateValidOptions() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount("bucket", "/mnt/nvidea-research", "READ_ONLY")
            }
        };

        var error = Assert.Throws<InvalidOperationException>(() => NebiusResearchDeploymentPreflight.Validate(options));
        Assert.Contains("READ_WRITE", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("NEBIUS_API_KEY")]
    [InlineData("TAVILY_API_KEY")]
    [InlineData("NVIDEA_WORKER_PRIVATE_KEY_PEM")]
    public void Validate_RejectsMissingRequiredWorkerSecretReference(string secretName)
    {
        var valid = CreateValidOptions();
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(valid.SecretEnvironmentVariables!, StringComparer.Ordinal);
        secrets.Remove(secretName);
        var options = valid with { SecretEnvironmentVariables = secrets };

        var error = Assert.Throws<InvalidOperationException>(() => NebiusResearchDeploymentPreflight.Validate(options));
        Assert.Contains(secretName, error.Message, StringComparison.Ordinal);
        Assert.Contains("MysteryBox", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsMalformedMysteryBoxSecretId()
    {
        var valid = CreateValidOptions();
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(valid.SecretEnvironmentVariables!, StringComparer.Ordinal)
        {
            ["TAVILY_API_KEY"] = new(SecretId: "not-a-nebius-secret")
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { SecretEnvironmentVariables = secrets }));

        Assert.Contains("TAVILY_API_KEY", error.Message, StringComparison.Ordinal);
        Assert.Contains("secret id", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsMalformedMysteryBoxVersionId()
    {
        var valid = CreateValidOptions();
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(valid.SecretEnvironmentVariables!, StringComparer.Ordinal)
        {
            ["TAVILY_API_KEY"] = new(SecretId: "mbsec-tavily", VersionId: "version-without-nebius-prefix")
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { SecretEnvironmentVariables = secrets }));

        Assert.Contains("TAVILY_API_KEY", error.Message, StringComparison.Ordinal);
        Assert.Contains("version id", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsVersionPinWithoutOwningSecretId()
    {
        var valid = CreateValidOptions();
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(valid.SecretEnvironmentVariables!, StringComparer.Ordinal)
        {
            ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = new(VersionId: "mbsecver-worker-key-v1")
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { SecretEnvironmentVariables = secrets }));

        Assert.Contains("must include its MysteryBox secret id", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_AcceptsExplicitlyVersionPinnedMysteryBoxSecret()
    {
        var valid = CreateValidOptions();
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(valid.SecretEnvironmentVariables!, StringComparer.Ordinal)
        {
            ["TAVILY_API_KEY"] = new(SecretId: "mbsec-tavily", VersionId: "mbsecver-tavily-v2")
        };

        NebiusResearchDeploymentPreflight.Validate(valid with { SecretEnvironmentVariables = secrets });
    }

    [Fact]
    public void Validate_RejectsPlaintextWorkerCredentialEvenWhenSecretReferenceAlsoExists()
    {
        var valid = CreateValidOptions();
        var environment = new Dictionary<string, string>(valid.EnvironmentVariables!, StringComparer.Ordinal)
        {
            ["TAVILY_API_KEY"] = "plaintext-must-not-be-used"
        };
        var options = valid with { EnvironmentVariables = environment };

        var error = Assert.Throws<InvalidOperationException>(() => NebiusResearchDeploymentPreflight.Validate(options));
        Assert.Contains("must never be supplied as plaintext", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RequiresClientPublicKeyForBindingVerification()
    {
        var valid = CreateValidOptions();
        var environment = new Dictionary<string, string>(valid.EnvironmentVariables!, StringComparer.Ordinal);
        environment.Remove(NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable);
        var options = valid with { EnvironmentVariables = environment };

        var error = Assert.Throws<InvalidOperationException>(() => NebiusResearchDeploymentPreflight.Validate(options));
        Assert.Contains(NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsPrivateClientKeyInPlaintextVerificationVariable()
    {
        using var client = RSA.Create(2048);
        var valid = CreateValidOptions();
        var environment = new Dictionary<string, string>(valid.EnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = client.ExportPkcs8PrivateKeyPem()
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { EnvironmentVariables = environment }));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsWeakClientVerificationKey()
    {
        using var client = RSA.Create(1024);
        var valid = CreateValidOptions();
        var environment = new Dictionary<string, string>(valid.EnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = client.ExportSubjectPublicKeyInfoPem()
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { EnvironmentVariables = environment }));

        Assert.Contains("at least 2048", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsMalformedClientVerificationKey()
    {
        var valid = CreateValidOptions();
        var environment = new Dictionary<string, string>(valid.EnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] =
                "-----BEGIN PUBLIC KEY-----\nnot-base64\n-----END PUBLIC KEY-----"
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { EnvironmentVariables = environment }));

        Assert.Contains("valid RSA public key", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsSecretBackedClientVerificationIdentity()
    {
        var valid = CreateValidOptions();
        var secrets = new Dictionary<string, NebiusMysteryBoxSecretRef>(valid.SecretEnvironmentVariables!, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = new(SecretId: "mbsec-client-verification")
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.Validate(valid with { SecretEnvironmentVariables = secrets }));

        Assert.Contains("public verification identity", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not as a secret reference", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateClientVerificationPublicKey_ReturnsCanonicalPublicOnlyIdentity()
    {
        using var client = RSA.Create(2048);
        var publicPem = client.ExportSubjectPublicKeyInfoPem();

        var canonical = NebiusResearchDeploymentPreflight.ValidateClientVerificationPublicKey(publicPem);

        Assert.Contains("BEGIN PUBLIC KEY", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE KEY", canonical, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateObjectStorageAlignment_AcceptsExactBucketAndPrefixMapping()
    {
        var dispatch = CreateValidOptions() with
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
        var storage = CreateObjectStorageOptions("nvidea-live-bucket", "nvidea-research");

        NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(dispatch, storage);
    }

    [Fact]
    public void ValidateObjectStorageAlignment_AcceptsBucketRootWhenBothPrefixesAreEmpty()
    {
        var dispatch = CreateValidOptions() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount(
                    "nvidea-live-bucket",
                    "/mnt/nvidea-research",
                    "READ_WRITE",
                    null)
            }
        };
        var storage = CreateObjectStorageOptions("nvidea-live-bucket", string.Empty);

        NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(dispatch, storage);
    }

    [Fact]
    public void ValidateObjectStorageAlignment_RejectsBucketMismatch()
    {
        var dispatch = CreateValidOptions() with
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
        var storage = CreateObjectStorageOptions("client-bucket", "nvidea-research");

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(dispatch, storage));
        Assert.Contains("bucket", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("volume source", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateObjectStorageAlignment_RejectsPrefixMismatch()
    {
        var dispatch = CreateValidOptions() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount(
                    "nvidea-live-bucket",
                    "/mnt/nvidea-research",
                    "READ_WRITE",
                    "worker-prefix")
            }
        };
        var storage = CreateObjectStorageOptions("nvidea-live-bucket", "client-prefix");

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(dispatch, storage));
        Assert.Contains("prefix", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SourcePath", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateObjectStorageAlignment_RejectsPathLikeVolumeSource()
    {
        var dispatch = CreateValidOptions() with
        {
            Volumes = new[]
            {
                new NebiusServerlessVolumeMount(
                    "s3://nvidea-live-bucket",
                    "/mnt/nvidea-research",
                    "READ_WRITE",
                    "nvidea-research")
            }
        };
        var storage = CreateObjectStorageOptions("nvidea-live-bucket", "nvidea-research");

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(dispatch, storage));
        Assert.Contains("bucket/source", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static NebiusObjectStorageClientOptions CreateObjectStorageOptions(string bucket, string prefix) =>
        new(
            Endpoint: "https://storage.eu-north1.nebius.cloud",
            Region: "eu-north1",
            Bucket: bucket,
            AccessKeyId: "test-access-key",
            SecretAccessKey: "test-secret-key",
            Prefix: prefix);

    private static NebiusResearchDispatchOptions CreateValidOptions()
    {
        using var client = RSA.Create(2048);
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
                [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = client.ExportSubjectPublicKeyInfoPem()
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
