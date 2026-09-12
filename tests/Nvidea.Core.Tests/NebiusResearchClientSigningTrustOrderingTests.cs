using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchClientSigningTrustOrderingTests
{
    private const string ClientPrivateKeyVariable = "NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE";
    private const string ServerlessTokenVariable = "NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN";
    private const string AccessKeyVariable = "NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID";
    private const string SecretKeyVariable = "NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY";

    [Fact]
    public void StrongPrivateKey_DerivesCanonicalPublicIdentity()
    {
        using var rsa = RSA.Create(2048);
        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var expectedPublicPem = rsa.ExportSubjectPublicKeyInfoPem();

        var actualPublicPem = NebiusResearchLiveDryRunPreflight.ValidateAndDeriveClientPublicKey(privatePem);

        using var expected = RSA.Create();
        using var actual = RSA.Create();
        expected.ImportFromPem(expectedPublicPem);
        actual.ImportFromPem(actualPublicPem);
        Assert.Equal(expected.ExportSubjectPublicKeyInfo(), actual.ExportSubjectPublicKeyInfo());
    }

    [Fact]
    public void PublicOnlyClientPem_IsRejectedBeforeProviderCredentialsAreRead()
    {
        using var clientRsa = RSA.Create(2048);
        AssertRejectedBeforeProviderCredentials(
            clientRsa.ExportSubjectPublicKeyInfoPem(),
            expectedMessage: "private key material");
    }

    [Fact]
    public void WeakClientPrivateKey_IsRejectedBeforeProviderCredentialsAreRead()
    {
        using var clientRsa = RSA.Create(1024);
        AssertRejectedBeforeProviderCredentials(
            clientRsa.ExportPkcs8PrivateKeyPem(),
            expectedMessage: "2048");
    }

    [Fact]
    public void MalformedClientPrivateKey_IsRejectedBeforeProviderCredentialsAreRead()
    {
        AssertRejectedBeforeProviderCredentials(
            "-----BEGIN PRIVATE KEY-----\nnot-valid-key-material\n-----END PRIVATE KEY-----\n",
            expectedMessage: "valid RSA private key");
    }

    private static void AssertRejectedBeforeProviderCredentials(string clientPem, string expectedMessage)
    {
        var root = Path.Combine(Path.GetTempPath(), "nvidea-client-signing-ordering", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var workerRsa = RSA.Create(2048);
            var workerPublicPath = Path.Combine(root, "worker-public.pem");
            var clientPrivatePath = Path.Combine(root, "client-private.pem");
            File.WriteAllText(workerPublicPath, workerRsa.ExportSubjectPublicKeyInfoPem());
            File.WriteAllText(clientPrivatePath, clientPem);

            var reads = new List<string>();
            var environment = CreateEnvironment(workerPublicPath, clientPrivatePath);

            string? Reader(string name)
            {
                reads.Add(name);
                return environment.GetValueOrDefault(name);
            }

            var exception = Assert.Throws<InvalidOperationException>(
                () => NebiusResearchLiveConfigurationLoader.Load(Reader));

            Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(ClientPrivateKeyVariable, reads);
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

    private static Dictionary<string, string?> CreateEnvironment(string workerPublicPath, string clientPrivatePath) =>
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
            [ClientPrivateKeyVariable] = clientPrivatePath,
            [ServerlessTokenVariable] = "must-not-be-read",
            [AccessKeyVariable] = "must-not-be-read",
            [SecretKeyVariable] = "must-not-be-read"
        };
}
