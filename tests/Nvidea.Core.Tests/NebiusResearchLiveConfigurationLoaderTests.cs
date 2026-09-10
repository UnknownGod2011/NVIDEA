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
    public void Load_UsesDocumentedRuntimeDefaults()
    {
        using var fixture = LiveConfigurationFixture.Create();

        var configuration = NebiusResearchLiveConfigurationLoader.Load(fixture.Read);

        Assert.Equal(5, configuration.PollSeconds);
        Assert.Equal(20, configuration.TotalTimeoutMinutes);
        Assert.False(string.IsNullOrWhiteSpace(configuration.ResearchQuestion));
    }

    [Theory]
    [InlineData("1", "2", 1, 2)]
    [InlineData("30", "60", 30, 60)]
    public void Load_AcceptsInclusiveRuntimeBounds(
        string pollSeconds,
        string timeoutMinutes,
        int expectedPollSeconds,
        int expectedTimeoutMinutes)
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_POLL_SECONDS"] = pollSeconds;
        fixture.Environment["NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES"] = timeoutMinutes;

        var configuration = NebiusResearchLiveConfigurationLoader.Load(fixture.Read);

        Assert.Equal(expectedPollSeconds, configuration.PollSeconds);
        Assert.Equal(expectedTimeoutMinutes, configuration.TotalTimeoutMinutes);
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

    [Theory]
    [InlineData("1")]
    [InlineData("61")]
    [InlineData("not-a-number")]
    public void Load_RejectsInvalidTotalTimeoutBounds(string value)
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES"] = value;

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        Assert.Contains("NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_AcceptsMaximumResearchQuestionAndRejectsOverflow()
    {
        using var fixture = LiveConfigurationFixture.Create();
        fixture.Environment["NVIDEA_LIVE_RESEARCH_QUESTION"] = new string('q', NebiusResearchLiveConfigurationLoader.MaximumResearchQuestionLength);

        var accepted = NebiusResearchLiveConfigurationLoader.Load(fixture.Read);
        Assert.Equal(NebiusResearchLiveConfigurationLoader.MaximumResearchQuestionLength, accepted.ResearchQuestion.Length);

        fixture.Environment["NVIDEA_LIVE_RESEARCH_QUESTION"] = new string('q', NebiusResearchLiveConfigurationLoader.MaximumResearchQuestionLength + 1);
        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));
        Assert.Contains("NVIDEA_LIVE_RESEARCH_QUESTION", error.Message, StringComparison.Ordinal);
        Assert.Contains("limit", error.Message, StringComparison.OrdinalIgnoreCase);
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
    public void Load_RejectsMalformedClientPrivateKeyWithoutLeakingItsPath()
    {
        using var fixture = LiveConfigurationFixture.Create();
        var malformedPath = Path.Combine(fixture.Root, "malformed-client-private.pem");
        File.WriteAllText(malformedPath, "not a pem key");
        fixture.Environment["NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE"] = malformedPath;

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        Assert.Contains("NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE", error.Message, StringComparison.Ordinal);
        Assert.Contains("valid RSA private key", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.Root, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RejectsPublicOnlyClientSigningKeyBeforeAnyProviderUse()
    {
        using var fixture = LiveConfigurationFixture.Create();
        using var rsa = RSA.Create(2048);
        var publicOnlyPath = Path.Combine(fixture.Root, "client-public-only.pem");
        File.WriteAllText(publicOnlyPath, rsa.ExportSubjectPublicKeyInfoPem());
        fixture.Environment["NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE"] = publicOnlyPath;

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        Assert.Contains("private key material", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.Root, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RejectsPrivatePemWhereWorkerPublicKeyIsRequired()
    {
        using var fixture = LiveConfigurationFixture.Create();
        using var rsa = RSA.Create(2048);
        var privatePath = Path.Combine(fixture.Root, "worker-private-in-public-slot.pem");
        File.WriteAllText(privatePath, rsa.ExportPkcs8PrivateKeyPem());
        fixture.Environment["NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE"] = privatePath;

        var error = Assert.Throws<InvalidOperationException>(() =>
            NebiusResearchLiveConfigurationLoader.Load(fixture.Read));

        Assert.Contains("public-only", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fixture.Root, error.Message, StringComparison.Ordinal);
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
