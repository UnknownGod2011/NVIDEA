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

public sealed record NebiusServerlessDiskSpec(
    string Type,
    long SizeBytes);

public sealed record NebiusMysteryBoxSecretRef(
    string? SecretId = null,
    string? VersionId = null);

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
    IReadOnlyDictionary<string, NebiusMysteryBoxSecretRef>? SecretEnvironmentVariables = null);

public sealed record NebiusServerlessResponse(
    HttpStatusCode StatusCode,
    string RawJson)
{
    /// <summary>
    /// Create operations currently return the created resource id in resourceId.
    /// This helper is intentionally tolerant of an operation response that has not
    /// exposed the resource yet and never guesses an id from unrelated JSON fields.
    /// </summary>
    public string? TryGetResourceId()
    {
        if (string.IsNullOrWhiteSpace(RawJson))
            return null;

        using var json = JsonDocument.Parse(RawJson);
        return json.RootElement.TryGetProperty("resourceId", out var resourceId)
            && resourceId.ValueKind == JsonValueKind.String
            ? resourceId.GetString()
            : null;
    }

    /// <summary>
    /// Returns status.state for a direct Job GET response. Unknown/missing states
    /// remain unknown rather than being mapped optimistically.
    /// </summary>
    public string? TryGetJobState()
    {
        if (string.IsNullOrWhiteSpace(RawJson))
            return null;

        using var json = JsonDocument.Parse(RawJson);
        if (!json.RootElement.TryGetProperty("status", out var status)
            || status.ValueKind != JsonValueKind.Object
            || !status.TryGetProperty("state", out var state)
            || state.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return state.GetString();
    }
}

public interface INebiusServerlessJobClient
{
    Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default);
    Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default);
    Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default);
    Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Credential-injected REST client for Nebius Serverless AI Jobs. The control plane
/// keeps raw JSON at its boundary because create/cancel may return long-running
/// operations whose representation can evolve independently of the job resource.
/// It does, however, validate the currently required job inputs and supports
/// MysteryBox secret references so API keys never need to be committed or supplied
/// as plaintext secret-like environment variables.
/// </summary>
public sealed class NebiusServerlessJobClient : INebiusServerlessJobClient
{
    private static readonly Uri DefaultBaseUri = new("https://api.nebius.cloud/");
    private static readonly string[] SensitiveNameMarkers =
    {
        "PASSWORD", "PASSWD", "SECRET", "TOKEN", "API_KEY", "APIKEY", "PRIVATE_KEY", "CREDENTIAL"
    };

    private readonly HttpClient _httpClient;
    private readonly NebiusServerlessOptions _options;
    private readonly Uri _baseUri;

    public NebiusServerlessJobClient(HttpClient httpClient, NebiusServerlessOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.AccessToken))
            throw new ArgumentException("Nebius access token is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.ProjectId))
            throw new ArgumentException("Nebius project id is required.", nameof(options));
        if (options.MaxRetries is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetries must be between 0 and 5.");

        _baseUri = options.BaseUri ?? DefaultBaseUri;
        ValidateBaseUri(_baseUri);
    }

    public Task<NebiusServerlessResponse> CreateAsync(
        NebiusServerlessJobSpec spec,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ValidateSpec(spec);

        var plaintextEnvironment = (spec.EnvironmentVariables ?? new Dictionary<string, string>())
            .Select(pair => new NebiusEnvironmentVariablePayload(pair.Key, pair.Value, null));
        var secretEnvironment = (spec.SecretEnvironmentVariables ?? new Dictionary<string, NebiusMysteryBoxSecretRef>())
            .Select(pair => new NebiusEnvironmentVariablePayload(
                pair.Key,
                null,
                new NebiusMysteryBoxSecretPayload(pair.Value.SecretId, pair.Value.VersionId)));
        var environmentVariables = plaintextEnvironment.Concat(secretEnvironment).ToArray();

        var payload = new
        {
            metadata = new
            {
                parentId = _options.ProjectId,
                name = spec.Name
            },
            spec = new
            {
                image = spec.Image,
                containerCommand = spec.ContainerCommand,
                args = spec.Arguments,
                environmentVariables,
                timeout = spec.Timeout,
                platform = spec.Platform,
                preset = spec.Preset,
                subnetId = spec.SubnetId,
                disk = new
                {
                    type = spec.Disk!.Type,
                    sizeBytes = spec.Disk.SizeBytes
                }
            }
        };

        return SendJsonAsync(HttpMethod.Post, "ai/v1/jobs", payload, cancellationToken);
    }

    public Task<NebiusServerlessResponse> GetAsync(
        string remoteJobId,
        CancellationToken cancellationToken = default)
    {
        var id = ValidateAndEscapeRemoteJobId(remoteJobId);
        return SendAsync(HttpMethod.Get, $"ai/v1/jobs/{id}", null, cancellationToken);
    }

    public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default)
    {
        var projectId = Uri.EscapeDataString(_options.ProjectId);
        return SendAsync(HttpMethod.Get, $"ai/v1/jobs?parentId={projectId}", null, cancellationToken);
    }

    public Task<NebiusServerlessResponse> CancelAsync(
        string remoteJobId,
        CancellationToken cancellationToken = default)
    {
        var id = ValidateRemoteJobId(remoteJobId);
        return SendJsonAsync(HttpMethod.Post, "ai/v1/jobs/cancel", new { id }, cancellationToken);
    }

    private Task<NebiusServerlessResponse> SendJsonAsync(
        HttpMethod method,
        string relativeUri,
        object payload,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        return SendAsync(method, relativeUri, json, cancellationToken);
    }

    private async Task<NebiusServerlessResponse> SendAsync(
        HttpMethod method,
        string relativeUri,
        string? json,
        CancellationToken cancellationToken)
    {
        Exception? lastTransient = null;
        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(_options.RequestTimeout ?? TimeSpan.FromSeconds(30));

            using var request = new HttpRequestMessage(method, new Uri(_baseUri, relativeUri));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (json is not null)
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, linked.Token).ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);

                if (IsTransient(response.StatusCode) && attempt < _options.MaxRetries)
                {
                    await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        $"Nebius Serverless request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).",
                        null,
                        response.StatusCode);

                EnsureJsonIfPresent(body);
                return new NebiusServerlessResponse(response.StatusCode, body);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < _options.MaxRetries)
            {
                lastTransient = ex;
                await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (
                attempt < _options.MaxRetries
                && (ex.StatusCode is null || IsTransient(ex.StatusCode.Value)))
            {
                lastTransient = ex;
                await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastTransient ?? new HttpRequestException("Nebius Serverless request failed after retries.");
    }

    private static void ValidateSpec(NebiusServerlessJobSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Name)
            || string.IsNullOrWhiteSpace(spec.Image)
            || string.IsNullOrWhiteSpace(spec.ContainerCommand)
            || string.IsNullOrWhiteSpace(spec.Platform)
            || string.IsNullOrWhiteSpace(spec.Preset)
            || string.IsNullOrWhiteSpace(spec.Timeout))
        {
            throw new ArgumentException("Name, image, command, platform, preset and timeout are required.", nameof(spec));
        }

        if (string.IsNullOrWhiteSpace(spec.SubnetId))
            throw new ArgumentException("SubnetId is required by the current Nebius Serverless Jobs API.", nameof(spec));

        if (spec.Disk is null
            || string.IsNullOrWhiteSpace(spec.Disk.Type)
            || spec.Disk.SizeBytes <= 0)
        {
            throw new ArgumentException("An explicit positive-size disk specification is required by the current Nebius Serverless Jobs API.", nameof(spec));
        }

        var plaintext = spec.EnvironmentVariables ?? new Dictionary<string, string>();
        var secrets = spec.SecretEnvironmentVariables ?? new Dictionary<string, NebiusMysteryBoxSecretRef>();
        var duplicate = plaintext.Keys.Intersect(secrets.Keys, StringComparer.Ordinal).FirstOrDefault();
        if (duplicate is not null)
            throw new ArgumentException($"Environment variable '{duplicate}' cannot have both a plaintext value and a MysteryBox secret reference.", nameof(spec));

        foreach (var pair in plaintext)
        {
            ValidateEnvironmentVariableName(pair.Key, nameof(spec));
            var normalized = pair.Key.Trim().ToUpperInvariant();
            if (SensitiveNameMarkers.Any(normalized.Contains))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{pair.Key}' looks secret-bearing. Use a Nebius MysteryBox secret reference rather than a plaintext job environment variable.");
            }
        }

        foreach (var pair in secrets)
        {
            ValidateEnvironmentVariableName(pair.Key, nameof(spec));
            ArgumentNullException.ThrowIfNull(pair.Value);
            var hasSecretId = !string.IsNullOrWhiteSpace(pair.Value.SecretId);
            var hasVersionId = !string.IsNullOrWhiteSpace(pair.Value.VersionId);
            if (!hasSecretId && !hasVersionId)
                throw new ArgumentException($"MysteryBox reference for '{pair.Key}' must provide secretId or versionId.", nameof(spec));
        }
    }

    private static void ValidateEnvironmentVariableName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Environment variable names cannot be blank.", parameterName);

        var trimmed = name.Trim();
        if (!(char.IsLetter(trimmed[0]) || trimmed[0] == '_')
            || !trimmed.Skip(1).All(static ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new ArgumentException($"Environment variable '{name}' is not a valid container environment-variable name.", parameterName);
        }
    }

    private static string ValidateAndEscapeRemoteJobId(string remoteJobId) =>
        Uri.EscapeDataString(ValidateRemoteJobId(remoteJobId));

    private static string ValidateRemoteJobId(string remoteJobId)
    {
        if (string.IsNullOrWhiteSpace(remoteJobId))
            throw new ArgumentException("Remote job id is required.", nameof(remoteJobId));

        var id = remoteJobId.Trim();
        if (id.Length > 256 || id.Any(char.IsControl))
            throw new ArgumentException("Remote job id is invalid.", nameof(remoteJobId));
        return id;
    }

    private static void ValidateBaseUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Nebius Serverless base URI must be absolute HTTPS.");

        if (!string.Equals(uri.Host, "api.nebius.cloud", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Nebius Serverless base URI must target api.nebius.cloud (or localhost for contract tests).", nameof(uri));
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    private static Task DelayAsync(int attempt, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(Math.Min(1000, 100 * Math.Pow(2, attempt))), cancellationToken);

    private static void EnsureJsonIfPresent(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return;

        using var _ = JsonDocument.Parse(body);
    }

    private sealed record NebiusEnvironmentVariablePayload(
        string Name,
        string? Value,
        NebiusMysteryBoxSecretPayload? MysteryboxSecret);

    private sealed record NebiusMysteryBoxSecretPayload(
        string? SecretId,
        string? VersionId);
}
