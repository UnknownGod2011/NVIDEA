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

public sealed record NebiusServerlessJobSpec(
    string Name,
    string Image,
    string ContainerCommand,
    string Arguments,
    string Platform,
    string Preset,
    string Timeout,
    string? SubnetId = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null);

public sealed record NebiusServerlessResponse(
    HttpStatusCode StatusCode,
    string RawJson);

public interface INebiusServerlessJobClient
{
    Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default);
    Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default);
    Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal credential-injected REST client for the documented Nebius Serverless AI
/// Jobs API. It deliberately returns raw JSON because the public API can surface
/// long-running-operation/job representations that should not be guessed locally.
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

        var env = (spec.EnvironmentVariables ?? new Dictionary<string, string>())
            .Select(pair => new { name = pair.Key, value = pair.Value })
            .ToArray();

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
                environmentVariables = env,
                timeout = spec.Timeout,
                platform = spec.Platform,
                preset = spec.Preset,
                subnetId = spec.SubnetId
            }
        };

        return SendJsonAsync(HttpMethod.Post, "ai/v1/jobs", payload, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(remoteJobId))
            throw new ArgumentException("Remote job id is required.", nameof(remoteJobId));

        return SendJsonAsync(HttpMethod.Post, "ai/v1/jobs/cancel", new { id = remoteJobId.Trim() }, cancellationToken);
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
            catch (HttpRequestException ex) when (attempt < _options.MaxRetries)
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

        foreach (var pair in spec.EnvironmentVariables ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                throw new ArgumentException("Environment variable names cannot be blank.", nameof(spec));

            var normalized = pair.Key.Trim().ToUpperInvariant();
            if (SensitiveNameMarkers.Any(normalized.Contains))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{pair.Key}' looks secret-bearing. Use Nebius SecretStash/env-secret rather than plaintext job environment variables.");
            }
        }
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
}
