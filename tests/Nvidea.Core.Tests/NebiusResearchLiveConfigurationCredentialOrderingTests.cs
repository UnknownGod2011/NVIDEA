using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchLiveConfigurationCredentialOrderingTests
{
    private const string ServerlessTokenVariable = "NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN";
    private const string AccessKeyVariable = "NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID";
    private const string SecretKeyVariable = "NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY";
    private const string ClientPrivateKeyVariable = "NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE";

    [Fact]
    public void InvalidObjectStorageEndpoint_IsRejectedBeforeStaticCredentialsAreRead()
    {
        var reads = new List<string>();
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT"] = "https://evil.example",
            ["NVIDEA_LIVE_OBJECT_STORAGE_REGION"] = "eu-north1",
            [AccessKeyVariable] = "must-not-be-read",
            [SecretKeyVariable] = "must-not-be-read"
        };

        string? Reader(string name)
        {
            reads.Add(name);
            return environment.GetValueOrDefault(name);
        }

        var exception = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchLiveConfigurationLoader.Load(Reader));

        Assert.Contains("OBJECT_STORAGE_ENDPOINT", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessKeyVariable, reads);
        Assert.DoesNotContain(SecretKeyVariable, reads);
        Assert.Equal(
            new[]
            {
                "NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT",
                "NVIDEA_LIVE_OBJECT_STORAGE_REGION"
            },
            reads);
    }

    [Fact]
    public void RegionMismatch_IsRejectedBeforeStaticCredentialsAreRead()
    {
        var reads = new List<string>();
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT"] = "https://storage.eu-west1.nebius.cloud",
            ["NVIDEA_LIVE_OBJECT_STORAGE_REGION"] = "eu-north1",
            [AccessKeyVariable] = "must-not-be-read",
            [SecretKeyVariable] = "must-not-be-read"
        };

        string? Reader(string name)
        {
            reads.Add(name);
            return environment.GetValueOrDefault(name);
        }

        Assert.Throws<InvalidOperationException>(
            () => NebiusResearchLiveConfigurationLoader.Load(Reader));

        Assert.DoesNotContain(AccessKeyVariable, reads);
        Assert.DoesNotContain(SecretKeyVariable, reads);
    }

    [Fact]
    public void UnicodeConfusableRegion_IsRejectedBeforeStaticCredentialsAreRead()
    {
        var reads = new List<string>();
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT"] = "https://storage.eu-north1.nebius.cloud",
            ["NVIDEA_LIVE_OBJECT_STORAGE_REGION"] = "eu-nоrth1", // Cyrillic small o, U+043E.
            [AccessKeyVariable] = "must-not-be-read",
            [SecretKeyVariable] = "must-not-be-read"
        };

        string? Reader(string name)
        {
            reads.Add(name);
            return environment.GetValueOrDefault(name);
        }

        var exception = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchLiveConfigurationLoader.Load(Reader));

        Assert.Contains("OBJECT_STORAGE_REGION", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessKeyVariable, reads);
        Assert.DoesNotContain(SecretKeyVariable, reads);
        Assert.Equal(
            new[]
            {
                "NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT",
                "NVIDEA_LIVE_OBJECT_STORAGE_REGION"
            },
            reads);
    }

    [Fact]
    public void InvalidTransportBucketAlignment_IsRejectedBeforeProviderCredentialsOrClientPrivateKeyAreRead()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-live-ordering-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var workerPublicPath = Path.Combine(root, "worker-public.pem");
            var clientPrivatePath = Path.Combine(root, "client-private.pem");
            File.WriteAllText(workerPublicPath, workerRsa.ExportSubjectPublicKeyInfoPem());
            File.WriteAllText(clientPrivatePath, clientRsa.ExportPkcs8PrivateKeyPem());

            var reads = new List<string>();
            var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT"] = "https://storage.eu-north1.nebius.cloud",
                ["NVIDEA_LIVE_OBJECT_STORAGE_REGION"] = "eu-north1",
                ["NVIDEA_LIVE_SERVERLESS_PROJECT_ID"] = "project-test",
                ["NVIDEA_LIVE_WORKER_IMAGE"] = $"registry.example/nvidea-worker@sha256:{new string('a', 64)}",
                ["NVIDEA_LIVE_SUBNET_ID"] = "subnet-test",
                ["NVIDEA_LIVE_PLATFORM"] = "cpu-d3",
                ["NVIDEA_LIVE_PRESET"] = "1vcpu-4gb",
                ["NVIDEA_LIVE_TIMEOUT"] = "3600s",
                ["NVIDEA_LIVE_DISK_TYPE"] = "NETWORK_SSD",
                ["NVIDEA_LIVE_DISK_SIZE_BYTES"] = (10L * 1024 * 1024 * 1024).ToString(),
                ["NVIDEA_LIVE_TRANSPORT_SOURCE"] = "wrong-bucket",
                ["NVIDEA_LIVE_WORKER_TRANSPORT_ROOT"] = "/mnt/nvidea-research",
                ["NVIDEA_LIVE_OBJECT_STORAGE_PREFIX"] = "nvidea-research",
                ["NVIDEA_LIVE_TRANSPORT_SOURCE_PATH"] = "nvidea-research",
                ["NVIDEA_LIVE_OBJECT_STORAGE_BUCKET"] = "nvidea-live-bucket",
                ["NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE"] = workerPublicPath,
                [ClientPrivateKeyVariable] = clientPrivatePath,
                ["NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID"] = "mbsec-nebius-api-key",
                ["NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID"] = "mbsec-tavily-api-key",
                ["NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID"] = "mbsec-worker-private-key",
                [ServerlessTokenVariable] = "must-not-be-read",
                [AccessKeyVariable] = "must-not-be-read",
                [SecretKeyVariable] = "must-not-be-read"
            };

            string? Reader(string name)
            {
                reads.Add(name);
                return environment.GetValueOrDefault(name);
            }

            var exception = Assert.Throws<InvalidOperationException>(
                () => NebiusResearchLiveConfigurationLoader.Load(Reader));

            Assert.Contains("bucket", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(ClientPrivateKeyVariable, reads);
            Assert.DoesNotContain(ServerlessTokenVariable, reads);
            Assert.DoesNotContain(AccessKeyVariable, reads);
            Assert.DoesNotContain(SecretKeyVariable, reads);
        }
        finally
        {
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Test cleanup only.
            }
        }
    }

    [Theory]
    [InlineData("https://storage.eu-north1.nebius.cloud", "eu-north1")]
    [InlineData("https://storage.us-central1.nebius.cloud/", "us-central1")]
    public void TrustedRegionalOrigin_PassesEarlyBoundary(string endpoint, string region)
    {
        NebiusResearchLiveConfigurationLoader.ValidateObjectStorageEndpointAndRegion(endpoint, region);
    }

    [Theory]
    [InlineData("http://storage.eu-north1.nebius.cloud", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud:444", "eu-north1")]
    [InlineData("https://user:pass@storage.eu-north1.nebius.cloud", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud.evil.example", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud/path", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud/?x=1", "eu-north1")]
    [InlineData("https://storage.eu-north1.nebius.cloud", "EU-NORTH1")]
    [InlineData("https://storage.eu-north1.nebius.cloud", "eu-nоrth1")]
    [InlineData("https://storage.eu-north1.nebius.cloud", "eu-north١")]
    public void UntrustedEarlyBoundary_IsRejected(string endpoint, string region)
    {
        Assert.Throws<InvalidOperationException>(
            () => NebiusResearchLiveConfigurationLoader.ValidateObjectStorageEndpointAndRegion(endpoint, region));
    }
}
