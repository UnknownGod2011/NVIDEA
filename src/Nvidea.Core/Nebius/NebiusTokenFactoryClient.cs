using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nvidea.Core.Nebius;

public enum WorkloadKind
{
    Fast,
    Standard,
    Deep
}

public sealed record ChatMessage(string Role, string Content);

public sealed record ToolDefinition(
    string Name,
    string Description,
    JsonObject Parameters);

public sealed record AgentRequest(
    IReadOnlyList<ChatMessage> Messages,
    WorkloadKind Workload = WorkloadKind.Standard,
    IReadOnlyList<ToolDefinition>? Tools = null,
    string? ResponseJsonSchema = null,
    double Temperature = 1.0,
    double TopP = 0.95);

public sealed record ToolCall(string Id, string Name, string ArgumentsJson);

public sealed record AgentCompletion(
    string? Content,
    IReadOnlyList<ToolCall> ToolCalls,
    string Model,
    string? FinishReason);

public sealed class NebiusOptions
{
    // Current Token Factory identifiers verified against the official Nebius
    // Token Factory cookbook. Keep environment overrides available because
    // provider catalogs can evolve independently of this binary.
    public const string VerifiedNemotronNanoModel = "nvidia/nvidia-nemotron-3-nano-30b-a3b";
    public const string VerifiedNemotronSuperModel = "nvidia/nemotron-3-super-120b-a12b";
    public const string VerifiedNemotronUltraModel = "nvidia/Nemotron-3-Ultra-550b-a55b";

    public Uri BaseUri { get; init; } = new("https://api.tokenfactory.us-central1.nebius.com/v1/");
    public required string ApiKey { get; init; }
    public string StandardModel { get; init; } = VerifiedNemotronSuperModel;
    public string? FastModel { get; init; } = VerifiedNemotronNanoModel;
    public string? DeepModel { get; init; } = VerifiedNemotronUltraModel;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(90);
    public int MaxAttempts { get; init; } = 3;

    public static NebiusOptions FromEnvironment()
    {
        var baseUriText = Environment.GetEnvironmentVariable("NVIDEA_NEBIUS_BASE_URL");
        var baseUri = string.IsNullOrWhiteSpace(baseUriText)
            ? new Uri("https://api.tokenfactory.us-central1.nebius.com/v1/")
            : new Uri(baseUriText, UriKind.Absolute);

        // Validate the destination before reading the bearer credential. This keeps a
        // tampered endpoint override from ever reaching credential-loading/request code.
        ValidateTrustedBaseUri(baseUri);

        var apiKey = Environment.GetEnvironmentVariable("NEBIUS_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("NEBIUS_API_KEY is required.");

        return new NebiusOptions
        {
            ApiKey = apiKey,
            BaseUri = baseUri,
            StandardModel = EnvironmentOrDefault("NVIDEA_MODEL_STANDARD", VerifiedNemotronSuperModel),
            FastModel = EnvironmentOrDefault("NVIDEA_MODEL_FAST", VerifiedNemotronNanoModel),
            DeepModel = EnvironmentOrDefault("NVIDEA_MODEL_DEEP", VerifiedNemotronUltraModel)
        };
    }

    public void Validate()
    {
        ValidateTrustedBaseUri(BaseUri);
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("Nebius API key cannot be empty.");
        if (string.IsNullOrWhiteSpace(StandardModel))
            throw new InvalidOperationException("A standard Nemotron model is required.");
        if (MaxAttempts is < 1 or > 6)
            throw new InvalidOperationException("MaxAttempts must be between 1 and 6.");
        if (RequestTimeout <= TimeSpan.Zero || RequestTimeout > TimeSpan.FromMinutes(10))
            throw new InvalidOperationException("RequestTimeout is outside the supported range.");
    }

    internal static void ValidateTrustedBaseUri(Uri baseUri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        if (!baseUri.IsAbsoluteUri || !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nebius endpoint must use absolute HTTPS.");
        if (baseUri.UserInfo.Length > 0)
            throw new InvalidOperationException("Nebius endpoint cannot contain URI user-info.");
        if (baseUri.Port != 443)
            throw new InvalidOperationException("Nebius endpoint must use the standard HTTPS port 443.");

        var host = baseUri.Host;
        if (!host.Equals("nebius.com", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".nebius.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Nebius endpoint must be hosted on nebius.com or a true subdomain of nebius.com.");
        }
    }

    private static string EnvironmentOrDefault(string variableName, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

public sealed class NemotronModelRouter
{
    private readonly NebiusOptions _options;

    public NemotronModelRouter(NebiusOptions options)
    {
        options.Validate();
        _options = options;
    }

    public string Resolve(WorkloadKind workload) => workload switch
    {
        WorkloadKind.Fast when !string.IsNullOrWhiteSpace(_options.FastModel) => _options.FastModel!,
        WorkloadKind.Deep when !string.IsNullOrWhiteSpace(_options.DeepModel) => _options.DeepModel!,
        _ => _options.StandardModel
    };
}

public interface IAgentInferenceClient
{
    Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

public sealed class NebiusTokenFactoryClient : IAgentInferenceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly NebiusOptions _options;
    private readonly NemotronModelRouter _router;

    public NebiusTokenFactoryClient(HttpClient httpClient, NebiusOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _router = new NemotronModelRouter(_options);
    }

    public async Task<AgentCompletion> CompleteAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Messages is null || request.Messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(request));

        var model = _router.Resolve(request.Workload);
        var payload = BuildPayload(request, model);
        var payloadJson = payload.ToJsonString(JsonOptions);

        Exception? lastError = null;
        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(_options.RequestTimeout);

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, "chat/completions"));
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpRequest.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    linkedCts.Token).ConfigureAwait(false);

                var responseText = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                    return ParseCompletion(responseText, model);

                if (!IsRetryable(response.StatusCode) || attempt == _options.MaxAttempts)
                    throw NebiusApiException.FromResponse(response.StatusCode, responseText);

                lastError = NebiusApiException.FromResponse(response.StatusCode, responseText);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < _options.MaxAttempts)
            {
                lastError = new TimeoutException("Nebius request timed out.");
            }
            catch (HttpRequestException ex) when (attempt < _options.MaxAttempts)
            {
                lastError = ex;
            }

            var delay = TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        throw lastError ?? new InvalidOperationException("Nebius request failed without an error response.");
    }

    private static JsonObject BuildPayload(AgentRequest request, string model)
    {
        var messages = new JsonArray();
        foreach (var message in request.Messages)
        {
            messages.Add(new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = message.Content
            });
        }

        var payload = new JsonObject
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["top_p"] = request.TopP
        };

        if (request.Tools is { Count: > 0 })
        {
            var tools = new JsonArray();
            foreach (var tool in request.Tools)
            {
                tools.Add(new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        ["parameters"] = tool.Parameters.DeepClone()
                    }
                });
            }
            payload["tools"] = tools;
            payload["tool_choice"] = "auto";
        }

        if (!string.IsNullOrWhiteSpace(request.ResponseJsonSchema))
        {
            var schema = JsonNode.Parse(request.ResponseJsonSchema)
                ?? throw new ArgumentException("ResponseJsonSchema must contain valid JSON.", nameof(request));
            payload["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = "nvidea_response",
                    ["strict"] = true,
                    ["schema"] = schema
                }
            };
        }

        return payload;
    }

    private static AgentCompletion ParseCompletion(string json, string requestedModel)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new InvalidDataException("Nebius response did not contain a completion choice.");

        var choice = choices[0];
        if (!choice.TryGetProperty("message", out var message))
            throw new InvalidDataException("Nebius response did not contain a message.");

        string? content = null;
        if (message.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.String)
        {
            content = contentElement.GetString();
        }

        var toolCalls = new List<ToolCall>();
        if (message.TryGetProperty("tool_calls", out var toolCallsElement)
            && toolCallsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in toolCallsElement.EnumerateArray())
            {
                if (!item.TryGetProperty("function", out var function))
                    continue;

                var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
                var name = function.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
                var arguments = function.TryGetProperty("arguments", out var argsElement) ? argsElement.GetString() ?? "{}" : "{}";
                if (!string.IsNullOrWhiteSpace(name))
                    toolCalls.Add(new ToolCall(id, name, arguments));
            }
        }

        var model = root.TryGetProperty("model", out var modelElement)
            ? modelElement.GetString() ?? requestedModel
            : requestedModel;
        var finishReason = choice.TryGetProperty("finish_reason", out var finishElement)
            ? finishElement.GetString()
            : null;

        return new AgentCompletion(content, toolCalls, model, finishReason);
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout
        || statusCode == (HttpStatusCode)429
        || (int)statusCode >= 500;
}

public sealed class NebiusApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseExcerpt { get; }

    private NebiusApiException(HttpStatusCode statusCode, string responseExcerpt)
        : base($"Nebius Token Factory returned HTTP {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseExcerpt = responseExcerpt;
    }

    public static NebiusApiException FromResponse(HttpStatusCode statusCode, string body)
    {
        var sanitized = (body ?? string.Empty).ReplaceLineEndings(" ").Trim();
        if (sanitized.Length > 2000)
            sanitized = sanitized[..2000];
        return new NebiusApiException(statusCode, sanitized);
    }
}
