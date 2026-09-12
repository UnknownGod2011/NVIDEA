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

        // Load the credential-free deployment topology before provider secrets or either local PEM
        // file. The worker image is digest-pinned and the final derived-public-key identity checks
        // still run after all shape/alignment checks that do not require key material have passed.
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
        var objectStorageBucket = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_BUCKET");

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
        var topologyPlainEnvironment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.TransportRootEnvironmentVariable] = workerTransportRoot
        };

        var topologyDispatchOptions = new NebiusResearchDispatchOptions(
            WorkerImage: workerImage,
            WorkerPublicKeyPem: string.Empty,
            ContainerCommand: "dotnet",
            Platform: platform,
            Preset: preset,
            Timeout: timeout,
            SubnetId: subnetId,
            Disk: new NebiusServerlessDiskSpec(diskType, diskSizeBytes),
            EnvironmentVariables: topologyPlainEnvironment,
            SecretEnvironmentVariables: secretEnvironment,
            Volumes: new[]
            {
                new NebiusServerlessVolumeMount(
                    transportSource,
                    workerTransportRoot,
                    "READ_WRITE",
                    transportSourcePath)
            });

        // Alignment accepts only credential-free namespace identity. This also runs the immutable
        // worker-image/topology checks before either local PEM file is opened.
        NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment(
            topologyDispatchOptions,
            new NebiusObjectStorageTransportAlignment(objectStorageBucket, objectStoragePrefix));

        // The worker envelope public key is needed only after deployment shape and storage namespace
        // alignment are known-good. Validate and canonicalize it immediately after reading so malformed,
        // weak, private, or protocol-incompatible material cannot enter deployment plumbing or cause
        // the client signing key/provider credentials to be read.
        var workerPublicKeyPem = ReadRequiredPemFile(environmentReader, "NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE");
        workerPublicKeyPem = NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(workerPublicKeyPem);
        topologyDispatchOptions = topologyDispatchOptions with
        {
            WorkerPublicKeyPem = workerPublicKeyPem
        };

        // Only now load the client signing private key. Validate its size and actual private signing
        // capability immediately, before any Serverless/Object Storage credential is accessed, then
        // use the canonical derived public identity for the worker-side dispatch binding.
        var clientPrivateKeyPem = ReadRequiredPemFile(environmentReader, "NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE");
        var clientPublicKeyPem = NebiusResearchLiveDryRunPreflight.ValidateAndDeriveClientPublicKey(clientPrivateKeyPem);

        var finalPlainEnvironment = new Dictionary<string, string>(topologyPlainEnvironment, StringComparer.Ordinal)
        {
            [NebiusResearchDeploymentPreflight.ClientPublicKeyEnvironmentVariable] = clientPublicKeyPem
        };
        var dispatchOptions = topologyDispatchOptions with
        {
            EnvironmentVariables = finalPlainEnvironment
        };

        var serverlessAccessToken = RequiredEnvironment(environmentReader, "NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN");
        var objectStorageOptions = new NebiusObjectStorageClientOptions(
            Endpoint: objectStorageEndpoint,
            Region: objectStorageRegion,
            Bucket: objectStorageBucket,
            AccessKeyId: RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID"),
            SecretAccessKey: RequiredEnvironment(environmentReader, "NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY"),
            Prefix: objectStoragePrefix,
            OperationTimeout: TimeSpan.FromSeconds(30),
            MaxRetries: 2);

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
        var trustFailure = NebiusObjectStorageEndpointTrust.Validate(endpointValue, region);
        switch (trustFailure)
        {
            case NebiusObjectStorageEndpointTrustFailure.InvalidRegion:
                throw new InvalidOperationException("NVIDEA_LIVE_OBJECT_STORAGE_REGION must be a bounded ASCII Nebius region identifier.");
            case NebiusObjectStorageEndpointTrustFailure.InvalidEndpoint:
                throw new InvalidOperationException("NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT must be the trusted HTTPS regional Nebius Object Storage origin on port 443.");
            case NebiusObjectStorageEndpointTrustFailure.RegionMismatch:
                throw new InvalidOperationException("NVIDEA_LIVE_OBJECT_STORAGE_ENDPOINT must match NVIDEA_LIVE_OBJECT_STORAGE_REGION.");
            case NebiusObjectStorageEndpointTrustFailure.None:
                return;
            default:
                throw new InvalidOperationException("Nebius Object Storage endpoint trust validation failed closed.");
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
