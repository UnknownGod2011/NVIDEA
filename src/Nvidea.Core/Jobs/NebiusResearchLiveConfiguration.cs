using System.Security.Cryptography;

namespace Nvidea.Core.Jobs;

public sealed record NebiusResearchLiveConfiguration(
    string ServerlessAccessToken,
    string ProjectId,
    string ClientPrivateKeyPem,
    NebiusResearchDispatchOptions DispatchOptions,
    NebiusObjectStorageClientOptions ObjectStorageOptions,
    NebiusResearchLivePreflightReport Report,
    string? RedactedManifestPath,
    string? PassEvidencePath,
    int PollSeconds,
    int TotalTimeoutMinutes,
    string ResearchQuestion);

/// <summary>
/// Loads and validates the complete environment/file based configuration used by the opt-in
/// Nebius Serverless research contract probe. Parsing is side-effect free apart from reading
/// explicitly referenced PEM files; no provider clients or network requests are created here.
/// </summary>
public static class NebiusResearchLiveConfigurationLoader
{
    public const int MaximumEnvironmentValueLength = 8192;
    public const int MaximumPemLength = 65536;
    public const int MaximumResearchQuestionLength = 2000;

    public static NebiusResearchLiveConfiguration LoadFromEnvironment() =>
        Load(Environment.GetEnvironmentVariable);

    public static NebiusResearchLiveConfiguration Load(Func<string, string?> environmentReader)
    {
        ArgumentNullException.ThrowIfNull(environmentReader);

        // Establish the static-credential destination boundary before reading any Object Storage
        // access-key material. This keeps a tampered endpoint/region pair from causing credential
        // variables to be touched at all.
        var objectStorageEndpoint = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT");
        var objectStorageRegion = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_REGION");
        ValidateObjectStorageEndpointAndRegion(objectStorageEndpoint, objectStorageRegion);

        var serverlessAccessToken = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN");
        var projectId = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_SERVERLESS_PROJECT_ID");
        var workerImage = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_WORKER_IMAGE");
        var subnetId = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_SUBNET_ID");
        var platform = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_PLATFORM");
        var preset = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_PRESET");
        var timeout = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_TIMEOUT");
        var diskType = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_DISK_TYPE");
        var diskSizeBytes = RequiredPositiveInt64(environmentReader, "NVIDEA_LIVE_DISK_SIZE_BYTES");
        var transportSource = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_TRANSPORT_SOURCE");
        var workerTransportRoot = OptionalEnvironment(environmentReader, "NVIDEA_LIVE_WORKER_TRANSPORT_ROOT") ?? "/mnt/nvidea-research";
        var objectStoragePrefix = OptionalEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_PREFIX") ?? "nvidea-research";
        var transportSourcePath = OptionalEnvironment(environmentReader, "NVIDEA_LIVE_TRANSPORT_SOURCE_PATH") ?? objectStoragePrefix;
        var workerPublicKeyPem = ReadRequiredPemFile(environmentReader, "NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE");
        var clientPrivateKeyPem = ReadRequiredPemFile(environmentReader, "NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE");

        var objectStorageOptions = new NebiusObjectStorageClientOptions(
            Endpoint: objectStorageEndpoint,
            Region: objectStorageRegion,
            Bucket: RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_BUCKET"),
            AccessKeyId: RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID"),
            SecretAccessKey: RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY"),
            Prefix: objectStoragePrefix,
            OperationTimeout: TimeSpan.FromSeconds(30),
            MaxRetries: 2);

        string clientPublicKeyPem;
        using (var clientRsa = RSA.Create())
        {
            try
            {
                clientRsa.ImportFromPem(clientPrivateKeyPem);
            }
            catch (Exception exception) when (exception is CryptographicException or ArgumentException)
            {
                throw new InvalidOperationException("NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE does not contain a valid RSA private key.");
            }

            clientPublicKeyPem = clientRsa.ExportSubjectPublicKeyInfoPem();
        }

        var secretEnvironment = new Dictionary<string, NebiusMysteryBoxSecretRef>(StringComparer.Ordinal)
        {
            ["NEBIUS_API_KEY"] = RequiredSecretRef(
                environmentReader,
                "NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_ID",
                "NVIDEA_LIVE_SECRET_NEBIUS_API_KEY_VERSION_ID"),
            ["TAVILY_API_KEY"] = RequiredSecretRef(
                environmentReader,
                "NVIDEA_LIVE_SECRET_TAVILY_API_KEY_ID",
                "NVIDEA_LIVE_SECRET_TAVILY_API_KEY_VERSION_ID"),
            ["NVIDEA_WORKER_PRIVATE_KEY_PEM"] = RequiredSecretRef(
                environmentReader,
                "NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_ID",
                "NVIDEA_LIVE_SECRET_WORKER_PRIVATE_KEY_VERSION_ID")
        };
        var plainEnvironment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable] = workerTransportRoot,
            [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = clientPublicKeyPem
        };

        var dispatchOptions = new NebiusResearchDispatchOptions(
            WorkerImage: workerImage,
            WorkerPublicKeyPem: workerPublicKeyPem,
            ContainerCommand: "dotnet",
            Platform: platform,
            Preset: preset,
            Timeout: timeout,
            SubnetId: subnetId,
            Disk: new NebiusServerlessDiskSpec(diskType, diskSizeBytes),
            EnvironmentVariables: plainEnvironment,
            SecretEnvironmentVariables: secretEnvironment,
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount(
                    transportSource,
                    workerTransportRoot,
                    "READ_WRITE",
                    transportSourcePath)
            });

        var report = NebiusResearchLivePreflightReporter.ValidateAndBuild(
            dispatchOptions,
            objectStorageOptions,
            serverlessAccessToken,
            projectId,
            clientPrivateKeyPem);

        var question = OptionalEnvironment(environmentReader, "NVIDEA_LIVE_RESEARCH_QUESTION")
            ?? "What are the current official capabilities of NVIDIA Nemotron models served through Nebius for agentic research? Use authoritative sources and state uncertainty.";
        if (question.Length > MaximumResearchQuestionLength)
            throw new InvalidOperationException("NVIDEA_LIVE_RESEARCH_QUESTION exceeds the live-probe limit.");

        return new NebiusResearchLiveConfiguration(
            serverlessAccessToken,
            projectId,
            clientPrivateKeyPem,
            dispatchOptions,
            objectStorageOptions,
            report,
            OptionalOutputPath(environmentReader, "NVIDEA_LIVE_REDACTED_MANIFEST_PATH"),
            OptionalOutputPath(environmentReader, "NVIDEA_LIVE_PASS_EVIDENCE_PATH"),
            BoundedInt32(environmentReader, "NVIDEA_LIVE_POLL_SECONDS", 5, 1, 30),
            BoundedInt32(environmentReader, "NVIDEA_LIVE_TOTAL_TIMEOUT_MINUTES", 20, 2, 60),
            question);
    }

    internal static void ValidateObjectStorageEndpointAndRegion(string endpointValue, string region)
    {
        if (string.IsNullOrWhiteSpace(region)
            || region.Length > 64
            || region.Any(static ch => !(char.IsLower(ch) || char.IsDigit(ch) || ch == '-'))
            || region[0] == '-'
            || region[^1] == '-')
        {
            throw new InvalidOperationException("NVIDEA_LIVE_OBJECT_STORAGE_REGION must be a bounded Nebius region identifier.");
        }

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || endpoint.Port != 443
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment)
            || endpoint.AbsolutePath != "/")
        {
            throw new InvalidOperationException("NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT must be the trusted HTTPS regional Nebius Object Storage origin on port 443.");
        }

        var expectedHost = $"storage.{region}.nebius.cloud";
        if (!string.Equals(endpoint.Host, expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT must match NVIDEA_LIVE_OBJECT_STORAGE_REGION.");
        }
    }

    public static string RequiredEnvironment(Func<string, string?> environmentReader, string name)
    {
        var value = ReadEnvironment(environmentReader, name);
        if (value is null)
            throw new InvalidOperationException($"Required live-probe configuration '{name}' is missing.");
        return value;
    }

    public static string? OptionalEnvironment(Func<string, string?> environmentReader, string name) =>
        ReadEnvironment(environmentReader, name);

    public static long RequiredPositiveInt64(Func<string, string?> environmentReader, string name)
    {
        var value = RequiredEnvironment(environmentReader, name);
        if (!long.TryParse(value, out var parsed) || parsed <= 0)
            throw new InvalidOperationException($"Required live-probe configuration '{name}' must be a positive integer.");
        return parsed;
    }

    public static int BoundedInt32(
        Func<string, string?> environmentReader,
        string name,
        int defaultValue,
        int minimum,
        int maximum)
    {
        if (minimum > maximum || defaultValue < minimum || defaultValue > maximum)
            throw new ArgumentOutOfRangeException(nameof(defaultValue));

        var value = OptionalEnvironment(environmentReader, name);
        if (value is null) return defaultValue;
        if (!int.TryParse(value, out var parsed) || parsed < minimum || parsed > maximum)
            throw new InvalidOperationException($"Live-probe configuration '{name}' must be between {minimum} and {maximum}.");
        return parsed;
    }

    public static string ReadRequiredPemFile(Func<string, string?> environmentReader, string environmentName)
    {
        var requestedPath = RequiredEnvironment(environmentReader, environmentName);
        string path;
        try
        {
            path = Path.GetFullPath(requestedPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"PEM file path referenced by '{environmentName}' is invalid.");
        }

        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length <= 0 || info.Length > MaximumPemLength)
                throw new InvalidOperationException($"PEM material referenced by '{environmentName}' is missing or oversized.");

            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text) || text.Length > MaximumPemLength)
                throw new InvalidOperationException($"PEM material referenced by '{environmentName}' is missing or oversized.");
            return text;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"PEM material referenced by '{environmentName}' could not be read.");
        }
    }

    public static string? OptionalOutputPath(Func<string, string?> environmentReader, string environmentName)
    {
        var requestedPath = OptionalEnvironment(environmentReader, environmentName);
        if (requestedPath is null) return null;

        string path;
        try
        {
            path = Path.GetFullPath(requestedPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException($"{environmentName} must point to a valid file path in an existing directory.");
        }

        return AtomicTextArtifactWriter.ValidateDestination(path, environmentName);
    }

    public static NebiusMysteryBoxSecretRef RequiredSecretRef(
        Func<string, string?> environmentReader,
        string idEnvironmentName,
        string versionEnvironmentName)
    {
        var reference = new NebiusMysteryBoxSecretRef(
            SecretId: RequiredEnvironment(environmentReader, idEnvironmentName),
            VersionId: OptionalEnvironment(environmentReader, versionEnvironmentName));

        NebiusResearchDeploymentPreflight.ValidateMysteryBoxSecretReference(
            idEnvironmentName,
            reference);
        return reference;
    }

    private static string? ReadEnvironment(Func<string, string?> environmentReader, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var value = environmentReader(name)?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Length > MaximumEnvironmentValueLength || value.Any(char.IsControl))
            throw new InvalidOperationException($"Live-probe configuration '{name}' is invalid.");
        return value;
    }
}
