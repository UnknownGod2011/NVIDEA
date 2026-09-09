using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nvidea.Core.Jobs;

public sealed record NebiusServerlessOptions(
    string AccessToken,
    string ProjectId,
    Uri? BaseUri = null,
    TimeSpan? RequestTimeout = null,
    int MaxRetries = 2);

public sealed record NebiusServerlessDiskSpec(string Type, long SizeBytes);
public sealed record NebiusMysteryBoxSecretRef(string? SecretId = null, string? VersionId = null);

/// <summary>
/// A Nebius Serverless container volume mount. Source is a Nebius bucket/filesystem name or id,
/// or an s3:// source supported by Nebius. Mode uses the provider enum names READ_WRITE/READ_ONLY.
/// </summary>
public sealed record NebiusServerlessVolumeMount(
    string Source,
    string ContainerPath,
    string Mode = "READ_WRITE",
    string? SourcePath = null);

public sealed record NebiusServerlessJobSpec(
    string Name,
    string Image,
    string ContainerCommand,
    string Arguments,
    string Platform,
    string Preset,
    string Timeout,
    string? SubnetId = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    NebiusServerlessDiskSpec? Disk = null,
    IReadOnlyDictionary<string, NebiusMysteryBoxSecretRef>? SecretEnvironmentVariables = null,
    IReadOnlyList<NebiusServerlessVolumeMount>? Volumes = null);

public sealed record NebiusServerlessResponse(HttpStatusCode StatusCode, string RawJson)
{
    public string? TryGetResourceId()
    {
        if (string.IsNullOrWhiteSpace(RawJson)) return null;
        using var json = JsonDocument.Parse(RawJson);
        return json.RootElement.TryGetProperty("resourceId", out var resourceId) && resourceId.ValueKind == JsonValueKind.String
            ? resourceId.GetString() : null;
    }

    public string? TryGetJobState()
    {
        if (string.IsNullOrWhiteSpace(RawJson)) return null;
        using var json = JsonDocument.Parse(RawJson);
        if (!json.RootElement.TryGetProperty("status", out var status)
            || status.ValueKind != JsonValueKind.Object
            || !status.TryGetProperty("state", out var state)
            || state.ValueKind != JsonValueKind.String) return null;
        return state.GetString();
    }
}

public interface INebiusServerlessJobClient
{
    Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default);
    Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default);
    Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns one explicit provider page. Implementations that cannot page fail closed for non-empty tokens.</summary>
    Task<NebiusServerlessResponse> ListAsync(string? pageToken, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(pageToken)
            ? ListAsync(cancellationToken)
            : throw new NotSupportedException("This Nebius client does not support continuation-page listing.");

    Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default);
}

public sealed class NebiusServerlessJobClient : INebiusServerlessJobClient
{
    private static readonly Uri DefaultBaseUri = new("https://api.nebius.cloud/");
    private static readonly string[] SensitiveNameMarkers = { "PASSWORD", "PASSWD", "SECRET", "TOKEN", "API_KEY", "APIKEY", "PRIVATE_KEY", "CREDENTIAL" };
    private readonly HttpClient _httpClient;
    private readonly NebiusServerlessOptions _options;
    private readonly Uri _baseUri;

    public NebiusServerlessJobClient(HttpClient httpClient, NebiusServerlessOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.AccessToken)) throw new ArgumentException("Nebius access token is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.ProjectId)) throw new ArgumentException("Nebius project id is required.", nameof(options));
        if (options.MaxRetries is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(options), "MaxRetries must be between 0 and 5.");
        _baseUri = options.BaseUri ?? DefaultBaseUri;
        ValidateBaseUri(_baseUri);
    }

    public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ValidateSpec(spec);
        var plaintextEnvironment = (spec.EnvironmentVariables ?? new Dictionary<string, string>()).Select(pair => new NebiusEnvironmentVariablePayload(pair.Key, pair.Value, null));
        var secretEnvironment = (spec.SecretEnvironmentVariables ?? new Dictionary<string, NebiusMysteryBoxSecretRef>()).Select(pair => new NebiusEnvironmentVariablePayload(pair.Key, null, new NebiusMysteryBoxSecretPayload(pair.Value.SecretId, pair.Value.VersionId)));
        var environmentVariables = plaintextEnvironment.Concat(secretEnvironment).ToArray();
        var volumes = spec.Volumes?
            .Select(volume => new NebiusVolumeMountPayload(volume.Source, volume.SourcePath, volume.ContainerPath, volume.Mode))
            .ToArray();
        var payload = new
        {
            metadata = new { parentId = _options.ProjectId, name = spec.Name },
            spec = new
            {
                image = spec.Image,
                containerCommand = spec.ContainerCommand,
                args = spec.Arguments,
                environmentVariables,
                volumes,
                timeout = spec.Timeout,
                platform = spec.Platform,
                preset = spec.Preset,
                subnetId = spec.SubnetId,
                disk = new { type = spec.Disk!.Type, sizeBytes = spec.Disk.SizeBytes }
            }
        };
        return SendJsonAsync(HttpMethod.Post, "ai/v1/jobs", payload, cancellationToken);
    }

    public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default)
    {
        var id = ValidateAndEscapeRemoteJobId(remoteJobId);
        return SendAsync(HttpMethod.Get, $"ai/v1/jobs/{id}", null, cancellationToken);
    }

    public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) => ListAsync(null, cancellationToken);

    public Task<NebiusServerlessResponse> ListAsync(string? pageToken, CancellationToken cancellationToken = default)
    {
        var projectId = Uri.EscapeDataString(_options.ProjectId);
        var relativeUri = $"ai/v1/jobs?parentId={projectId}";
        if (!string.IsNullOrWhiteSpace(pageToken))
        {
            var token = pageToken.Trim();
            if (token.Length > 4096 || token.Any(char.IsControl))
                throw new ArgumentException("Nebius page token is invalid.", nameof(pageToken));
            relativeUri += $"&pageToken={Uri.EscapeDataString(token)}";
        }
        return SendAsync(HttpMethod.Get, relativeUri, null, cancellationToken);
    }

    public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default)
    {
        var id = ValidateRemoteJobId(remoteJobId);
        return SendJsonAsync(HttpMethod.Post, "ai/v1/jobs/cancel", new { id }, cancellationToken);
    }

    private Task<NebiusServerlessResponse> SendJsonAsync(HttpMethod method, string relativeUri, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        return SendAsync(method, relativeUri, json, cancellationToken);
    }

    private async Task<NebiusServerlessResponse> SendAsync(HttpMethod method, string relativeUri, string? json, CancellationToken cancellationToken)
    {
        Exception? lastTransient = null;
        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(_options.RequestTimeout ?? TimeSpan.FromSeconds(30));
            using var request = new HttpRequestMessage(method, new Uri(_baseUri, relativeUri));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (json is not null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                using var response = await _httpClient.SendAsync(request, linked.Token).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);
                if (IsTransient(response.StatusCode) && attempt < _options.MaxRetries) { await DelayAsync(attempt, cancellationToken).ConfigureAwait(false); continue; }
                if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Nebius Serverless request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).", null, response.StatusCode);
                EnsureJsonIfPresent(body);
                return new NebiusServerlessResponse(response.StatusCode, body);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < _options.MaxRetries) { lastTransient = ex; await DelayAsync(attempt, cancellationToken).ConfigureAwait(false); }
            catch (HttpRequestException ex) when (attempt < _options.MaxRetries && (ex.StatusCode is null || IsTransient(ex.StatusCode.Value))) { lastTransient = ex; await DelayAsync(attempt, cancellationToken).ConfigureAwait(false); }
        }
        throw lastTransient ?? new HttpRequestException("Nebius Serverless request failed after retries.");
    }

    private static void ValidateSpec(NebiusServerlessJobSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Name) || string.IsNullOrWhiteSpace(spec.Image) || string.IsNullOrWhiteSpace(spec.ContainerCommand) || string.IsNullOrWhiteSpace(spec.Platform) || string.IsNullOrWhiteSpace(spec.Preset) || string.IsNullOrWhiteSpace(spec.Timeout)) throw new ArgumentException("Name, image, command, platform, preset and timeout are required.", nameof(spec));
        if (string.IsNullOrWhiteSpace(spec.SubnetId)) throw new ArgumentException("SubnetId is required by the current Nebius Serverless Jobs API.", nameof(spec));
        if (spec.Disk is null || string.IsNullOrWhiteSpace(spec.Disk.Type) || spec.Disk.SizeBytes <= 0) throw new ArgumentException("An explicit positive-size disk specification is required by the current Nebius Serverless Jobs API.", nameof(spec));
        var plaintext = spec.EnvironmentVariables ?? new Dictionary<string, string>();
        var secrets = spec.SecretEnvironmentVariables ?? new Dictionary<string, NebiusMysteryBoxSecretRef>();
        var duplicate = plaintext.Keys.Intersect(secrets.Keys, StringComparer.Ordinal).FirstOrDefault();
        if (duplicate is not null) throw new ArgumentException($"Environment variable '{duplicate}' cannot have both a plaintext value and a MysteryBox secret reference.", nameof(spec));
        foreach (var pair in plaintext)
        {
            ValidateEnvironmentVariableName(pair.Key, nameof(spec));
            var normalized = pair.Key.Trim().ToUpperInvariant();
            if (SensitiveNameMarkers.Any(normalized.Contains)) throw new InvalidOperationException($"Environment variable '{pair.Key}' looks secret-bearing. Use a Nebius MysteryBox secret reference rather than a plaintext job environment variable.");
        }
        foreach (var pair in secrets)
        {
            ValidateEnvironmentVariableName(pair.Key, nameof(spec));
            ArgumentNullException.ThrowIfNull(pair.Value);
            if (string.IsNullOrWhiteSpace(pair.Value.SecretId) && string.IsNullOrWhiteSpace(pair.Value.VersionId)) throw new ArgumentException($"MysteryBox reference for '{pair.Key}' must provide secretId or versionId.", nameof(spec));
        }

        var volumes = spec.Volumes ?? Array.Empty<NebiusServerlessVolumeMount>();
        var seenContainerPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var volume in volumes)
        {
            ArgumentNullException.ThrowIfNull(volume);
            if (string.IsNullOrWhiteSpace(volume.Source) || volume.Source.Length > 1024 || volume.Source.Any(char.IsControl))
                throw new ArgumentException("Nebius volume source must be a bounded non-empty bucket/filesystem source.", nameof(spec));
            if (string.IsNullOrWhiteSpace(volume.ContainerPath)
                || volume.ContainerPath.Length > 1024
                || volume.ContainerPath.Any(char.IsControl)
                || !volume.ContainerPath.StartsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Nebius volume container path must be a bounded absolute Linux path.", nameof(spec));
            }
            if (!seenContainerPaths.Add(volume.ContainerPath))
                throw new ArgumentException($"Nebius volume container path '{volume.ContainerPath}' is mounted more than once.", nameof(spec));
            if (!string.Equals(volume.Mode, "READ_WRITE", StringComparison.Ordinal)
                && !string.Equals(volume.Mode, "READ_ONLY", StringComparison.Ordinal))
            {
                throw new ArgumentException("Nebius volume mode must be READ_WRITE or READ_ONLY.", nameof(spec));
            }
            if (volume.SourcePath is { } sourcePath
                && (sourcePath.Length > 1024 || sourcePath.Any(char.IsControl)))
            {
                throw new ArgumentException("Nebius volume source path is invalid.", nameof(spec));
            }
        }
    }

    private static void ValidateEnvironmentVariableName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Environment variable names cannot be blank.", parameterName);
        var trimmed = name.Trim();
        if (!(char.IsLetter(trimmed[0]) || trimmed[0] == '_') || !trimmed.Skip(1).All(static ch => char.IsLetterOrDigit(ch) || ch == '_')) throw new ArgumentException($"Environment variable '{name}' is not a valid container environment-variable name.", parameterName);
    }

    private static string ValidateAndEscapeRemoteJobId(string remoteJobId) => Uri.EscapeDataString(ValidateRemoteJobId(remoteJobId));
    private static string ValidateRemoteJobId(string remoteJobId)
    {
        if (string.IsNullOrWhiteSpace(remoteJobId)) throw new ArgumentException("Remote job id is required.", nameof(remoteJobId));
        var id = remoteJobId.Trim();
        if (id.Length > 256 || id.Any(char.IsControl)) throw new ArgumentException("Remote job id is invalid.", nameof(remoteJobId));
        return id;
    }

    private static void ValidateBaseUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Nebius Serverless base URI must be absolute HTTPS.");
        if (!string.Equals(uri.Host, "api.nebius.cloud", StringComparison.OrdinalIgnoreCase) && !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) && !string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Nebius Serverless base URI must target api.nebius.cloud (or localhost for contract tests).", nameof(uri));
    }

    private static bool IsTransient(HttpStatusCode statusCode) => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
    private static Task DelayAsync(int attempt, CancellationToken cancellationToken) => Task.Delay(TimeSpan.FromMilliseconds(Math.Min(1000, 100 * Math.Pow(2, attempt))), cancellationToken);
    private static void EnsureJsonIfPresent(string body) { if (!string.IsNullOrWhiteSpace(body)) using var _ = JsonDocument.Parse(body); }
    private sealed record NebiusEnvironmentVariablePayload(string Name, string? Value, NebiusMysteryBoxSecretPayload? MysteryboxSecret);
    private sealed record NebiusMysteryBoxSecretPayload(string? SecretId, string? VersionId);
    private sealed record NebiusVolumeMountPayload(string Source, string? SourcePath, string ContainerPath, string Mode);
}
