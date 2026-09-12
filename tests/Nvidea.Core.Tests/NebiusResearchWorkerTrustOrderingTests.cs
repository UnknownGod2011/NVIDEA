using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchWorkerTrustOrderingTests
{
    [Fact]
    public void SharedDigestPinnedWorkerImagePolicy_AcceptsPinnedAndRejectsMutableReferences()
    {
        var pinned = $"registry.example/nvidea-worker@sha256:{new string('a', 64)}";

        NebiusResearchDeploymentPreflight.ValidateDigestPinnedWorkerImage(pinned);

        var mutable = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchDeploymentPreflight.ValidateDigestPinnedWorkerImage("registry.example/nvidea-worker:latest"));
        Assert.Contains("digest-pinned", mutable.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Throws<InvalidOperationException>(
            () => NebiusResearchDeploymentPreflight.ValidateDigestPinnedWorkerImage(
                $"registry.example/nvidea-worker@sha256:{new string('z', 64)}"));
    }

    [Fact]
    public void WorkerPublicKeyPolicy_RejectsPrivateAndWeakRsaMaterial()
    {
        using var strongPrivateRsa = RSA.Create(2048);
        var privatePem = strongPrivateRsa.ExportPkcs8PrivateKeyPem();
        var privateException = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(privatePem));
        Assert.Contains("public-only", privateException.Message, StringComparison.OrdinalIgnoreCase);

        using var weakRsa = RSA.Create(1024);
        var weakPublicPem = weakRsa.ExportSubjectPublicKeyInfoPem();
        var weakException = Assert.Throws<InvalidOperationException>(
            () => NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(weakPublicPem));
        Assert.Contains("2048", weakException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedWorkerPublicKey_IsRejectedBeforeClientKeyOrProviderCredentialsAreRead()
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-worker-trust-ordering", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var workerPublicPath = Path.Combine(root, "worker-public.pem");
            File.WriteAllText(workerPublicPath, "-----BEGIN PUBLIC KEY-----\nnot-a-valid-rsa-key\n-----END PUBLIC KEY-----\n");

            var reads = new List<string>();
            var environment = CreateTopologyEnvironment(workerPublicPath);

            string? Reader(string name)
            {
                reads.Add(name);
                return environment.GetValueOrDefault(name);
            }

            var exception = Assert.Throws<InvalidOperationException>(
                () => NebiusResearchLiveConfigurationLoader.Load(Reader));

            Assert.Contains("worker envelope public key", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE", reads);
            Assert.DoesNotContain("NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE", reads);
            Assert.DoesNotContain("NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN", reads);
            Assert.DoesNotContain("NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID", reads);
            Assert.DoesNotContain("NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY", reads);
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

    private static Dictionary<string, string?> CreateTopologyEnvironment(string workerPublicPath) =>
        new(StringComparer.Ordinal)
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
            ["NVIDEA_LIVE_TRANSPORT_SOURCE"] = "nvidea-live-bucket",
            ["NVIDEA_LIVE_WORKER_TRANSPORT_ROOT"] = "/mnt/nvidea-research",
            ["NVIDEA_LIVE_OBJECT_STORAGE_PREFIX"] = "nvidea-research",
            ["NVIDEA_LIVE_TRANSPORT_SOURCE_PATH"] = "nvidea-research",
            ["NVIDEA_LIVE_OBJECT_STORAGE_BUCKET"] = "nvidea-live-bucket",
            ["NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID"] = "mbsec-nebius-api-key",
            ["NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID"] = "mbsec-tavily-api-key",
            ["NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID"] = "mbsec-worker-private-key",
            ["NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE"] = workerPublicPath,
            ["NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE"] = "must-not-be-read",
            ["NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN"] = "must-not-be-read",
            ["NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID"] = "must-not-be-read",
            ["NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY"] = "must-not-be-read"
        };
}
