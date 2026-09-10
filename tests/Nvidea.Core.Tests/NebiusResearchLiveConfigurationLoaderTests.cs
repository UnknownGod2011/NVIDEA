using System.Security.Cryptography;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusResearchLiveConfigurationLoaderTests
{
    [Fact]
    public void Load_AcceptsValidConfigurationAndAppliesBoundedRuntimeOptions()
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_POLL_SECONDS"] = "7";
        fixture.Environment["NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES"] = "25";
        fixture.Environment["NVIDEA_LIVE_RESEARCH_QUESTION"] = "  Verify current Nemotron research capabilities.  ";
        fixture.Environment["NVIDEA_LIVE_SECRET_TAVILY_API_KEY_VERSION_ID"] = "mbsecver-tavily-v4";

        var configuration = NebiusResearchLiveConfigurationLoader.Load(fixture.Read);

        Assert.Equal(7, configuration.PollSeconds);
        Assert.Equal(25, configuration.TotalTimeoutMinutes);
        Assert.Equal("Verify current Nemotron research capabilities.", configuration.ResearchQuestion);
        Assert.Equal("mbsecver-tavily-v4", configuration.DispatchOptions.SecretEnvironmentVariables!["TAVILY_API_KEY"].VersionId);
        Assert.Equal(configuration.ObjectStorageOptions.Prefix, configuration.DispatchOptions.Volumes!.Single().SourcePath);
        Assert.Equal(64, configuration.Report.Manifest.DeploymentFingerprintSha256.Length);
    }

    [Fact]
    public void Load_RejectsMissingRequiredEnvironmentBeforeProviderConstruction()
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment.Remove("NVIDEA_LIVE_SERVERLESS_PROJECT_ID");

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        Assert.Contains("NVIDEA_LIVE_SERVERLESS_PROJECT_ID", error.Message, StringComparison.Ordinal);
        Assert.Contains("missing", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_RejectsOversizedOrControlCharacterEnvironmentValues()
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_PLATFORM"] = new string('x', NebiusResearchLiveConfigurationLoader.MaximumEnvironmentValueLength + 1);

        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        fixture.Environment["NVIDEA_LIVE_PLATFORM"] = "cpu\nmalformed";
        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("31")]
    [InlineData("not-a-number")]
    public void Load_RejectsInvalidPollBounds(string value)
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_POLL_SECONDS"] = value;

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        Assert.Contains("NVIDEA_LIVE_POLL_SECONDS", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RejectsMalformedPinnedMysteryBoxVersion()
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_VERSION_ID"] = "not-a-version-id";

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        Assert.Contains("NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID", error.Message, StringComparison.Ordinal);
        Assert.Contains("version id", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_RejectsMissingOrOversizedPemBeforeCryptographicUse()
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE"] = Path.Combine(fixture.Root, "missing.pem");

        var missing = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));
        Assert.Contains("NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE", missing.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Root, missing.Message, StringComparison.Ordinal);

        var oversizedPath = Path.Combine(fixture.Root, "oversized.pem");
        File.WriteAllText(oversizedPath, new string('x', NebiusResearchLiveConfigurationLoader.MaximumPemLength + 1));
        fixture.Environment["NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE"] = oversizedPath;

        var oversized = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));
        Assert.Contains("oversized", oversized.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ValidatesOutputPathsAndReturnsCanonicalPaths()
    {
        using var fixture = LiveConfigurationFixture.Create();
        var manifestPath = Path.Combine(fixture.Root, "manifest.json");
        var passPath = Path.Combine(fixture.Root, "pass.json");
        fixture.Environment["NVIDEA_LIVE_REDACTED_MANIFEST_PATH"] = manifestPath;
        fixture.Environment["NVIDEA_LIVE_PASS_EVIDENCE_PATH"] = passPath;

        var configuration = NebiusResearchLiveConfigurationLoader.Load(fixture.Read);

        Assert.Equal(Path.GetFullPath(manifestPath), configuration.RedactedManifestPath);
        Assert.Equal(Path.GetFullPath(passPath), configuration.PassEvidencePath);

        fixture.Environment["NVIDEA_LIVE_PASS_EVIDENCE_PATH"] = Path.Combine(fixture.Root, "missing-parent", "pass.json");
        Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));
    }

    private sealed class LiveConfigurationFixture : IDisposable
    {
        private LiveConfigurationFixture(string root, Dictionary<string, string> environment)
        {
            Root = root;
            Environment = environment;
        }

        public string Root { get; }
        public Dictionary<string, string> Environment { get; }

        public string? Read(string name) => Environment.TryGetValue(name, out var value) ? value : null;

        public static LiveConfigurationFixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "nvidea-live-config-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            using var workerRsa = RSA.Create(2048);
            using var clientRsa = RSA.Create(2048);
            var workerPublicPath = Path.Combine(root, "worker-public.pem");
            var clientPrivatePath = Path.Combine(root, "client-private.pem");
            File.WriteAllText(workerPublicPath, workerRsa.ExportSubjectPublicKeyInfoPem());
            File.WriteAllText(clientPrivatePath, clientRsa.ExportPkcs8PrivateKeyPem());

            var environment = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN"] = "serverless-access-token-test",
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
                ["NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE"] = workerPublicPath,
                ["NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE"] = clientPrivatePath,
                ["NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT"] = "https://storage.eu-north1.nebius.cloud",
                ["NVIDEA_LIVE_OBJECT_STORAGE_REGION"] = "eu-north1",
                ["NVIDEA_LIVE_OBJECT_STORAGE_BUCKET"] = "nvidea-live-bucket",
                ["NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID"] = "object-storage-access-key-test",
                ["NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY"] = "object-storage-secret-key-test",
                ["NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID"] = "mbsec-nebius-api-key",
                ["NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID"] = "mbsec-tavily-api-key",
                ["NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID"] = "mbsec-worker-private-key"
            };

            return new LiveConfigurationFixture(root, environment);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
            }
            catch
            {
                // Test cleanup only.
            }
        }
    }
}
