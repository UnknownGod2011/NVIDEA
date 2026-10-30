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

public sealed record ChatMessage(
    string Role,
    string Content,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    string? ToolCallId = null,
    string? Name = null);

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

    public static NebiusOptions FromEnvironment() => FromEnvironment(KeyboardWtfProviderSettings.Defaults(), Environment.GetEnvironmentVariable)
        ?? throw new InvalidOperationException("A Nebius API key is required.");

    public static NebiusOptions? FromEnvironment(KeyboardWtfProviderSettings savedSettings) =>
        FromEnvironment(savedSettings, Environment.GetEnvironmentVariable);

    public static NebiusOptions? FromEnvironment(Func<KeyboardWtfProviderSettings> loadSavedSettings) =>
        FromEnvironment(loadSavedSettings, Environment.GetEnvironmentVariable);

    public static NebiusOptions? FromEnvironment(
        KeyboardWtfProviderSettings? savedSettings,
        Func<string, string?> environmentReader)
    {
        ArgumentNullException.ThrowIfNull(environmentReader);
        var baseUri = ReadAndValidateBaseUri(environmentReader);
        savedSettings ??= KeyboardWtfProviderSettings.Defaults();
        return CreateFromValidatedBaseUri(savedSettings, baseUri, environmentReader);
    }

    public static NebiusOptions? FromEnvironment(
        Func<KeyboardWtfProviderSettings> loadSavedSettings,
        Func<string, string?> environmentReader)
    {
        ArgumentNullException.ThrowIfNull(loadSavedSettings);
        ArgumentNullException.ThrowIfNull(environmentReader);
        var baseUri = ReadAndValidateBaseUri(environmentReader);
        var savedSettings = loadSavedSettings() ?? KeyboardWtfProviderSettings.Defaults();
        return CreateFromValidatedBaseUri(savedSettings, baseUri, environmentReader);
    }

    private static NebiusOptions? CreateFromValidatedBaseUri(
        KeyboardWtfProviderSettings savedSettings,
        Uri baseUri,
        Func<string, string?> environmentReader)
    {
        if (!savedSettings.TryValidate(out var settingsError))
            throw new InvalidOperationException(settingsError);

        var environmentApiKey = environmentReader("NEBIUS_API_KEY");
        var apiKey = string.IsNullOrWhiteSpace(environmentApiKey) ? savedSettings.NebiusApiKey : environmentApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        return new NebiusOptions
        {
            ApiKey = apiKey,
            BaseUri = baseUri,
            StandardModel = EnvironmentOrDefault(environmentReader("NVIDEA_MODEL_STANDARD"), savedSettings.StandardModel),
            FastModel = EnvironmentOrDefault(environmentReader("NVIDEA_MODEL_FAST"), savedSettings.FastModel),
            DeepModel = EnvironmentOrDefault(environmentReader("NVIDEA_MODEL_DEEP"), savedSettings.DeepModel)
        };
    }

    private static Uri ReadAndValidateBaseUri(Func<string, string?> environmentReader)
    {
        var baseUriText = environmentReader("NVIDEA_NEBIUS_BASE_URL");
        var baseUri = string.IsNullOrWhiteSpace(baseUriText)
            ? new Uri("https://api.tokenfactory.us-central1.nebius.com/v1/")
            : new Uri(baseUriText, UriKind.Absolute);

        // Validate the destination before loading persisted or environment credentials.
        ValidateTrustedBaseUri(baseUri);
        return baseUri;
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

    private static string EnvironmentOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
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

/// <summary>Optional text streaming boundary. Callers still receive the verified final completion and typed tools.</summary>
public interface IAgentStreamingInferenceClient
{
    Task<AgentCompletion> CompleteStreamingAsync(
        AgentRequest request,
        Func<string, ValueTask> onTextDelta,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Keeps local-first desktop features available when cloud reasoning is intentionally unconfigured.
/// It contains no credentials and fails only at the attempted cloud-inference boundary.
/// </summary>
public sealed class UnconfiguredInferenceClient : IAgentInferenceClient
{
    public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
        Task.FromException<AgentCompletion>(new InvalidOperationException(
            "NVIDIA Nemotron reasoning is not configured. Add a Nebius Token Factory key in Settings or set NEBIUS_API_KEY to use Smart Dictation or Jarvis."));
}

public sealed class NebiusTokenFactoryClient : IAgentInferenceClient, IAgentStreamingInferenceClient
{
    private const string QuarantinedTransportDiagnostic = "Nebius transport request failed; provider/network diagnostics quarantined.";
    private const int MaximumStreamingResponseCharacters = 128 * 1024;
    private const int MaximumSseLineCharacters = 64 * 1024;
    private const int MaximumSseEventCharacters = 1024 * 1024;
    private const int MaximumStreamingToolCalls = 16;
    private const int MaximumStreamingToolArgumentCharacters = 16 * 1024;

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
            catch (HttpRequestException ex)
            {
                var quarantined = QuarantineTransportFailure(ex);
                if (attempt == _options.MaxAttempts)
                    throw quarantined;

                lastError = quarantined;
            }

            var delay = TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        throw lastError ?? new InvalidOperationException("Nebius request failed without an error response.");
    }

    public async Task<AgentCompletion> CompleteStreamingAsync(
        AgentRequest request,
        Func<string, ValueTask> onTextDelta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onTextDelta);
        if (request.Messages is null || request.Messages.Count == 0)
            throw new ArgumentException("At least one message is required.", nameof(request));

        var model = _router.Resolve(request.Workload);
        var payloadJson = BuildPayload(request, model, stream: true).ToJsonString(JsonOptions);
        Exception? lastError = null;
        var textWasEmitted = false;

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(_options.RequestTimeout);
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, "chat/completions"));
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                httpRequest.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    linkedCts.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await ReadBoundedErrorBodyAsync(response, linkedCts.Token).ConfigureAwait(false);
                    var error = NebiusApiException.FromResponse(response.StatusCode, errorBody);
                    if (!IsRetryable(response.StatusCode) || attempt == _options.MaxAttempts)
                        throw error;
                    lastError = error;
                }
                else
                {
                    if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Nebius streaming response did not use the documented event-stream content type.");

                    await using var responseStream = await response.Content.ReadAsStreamAsync(linkedCts.Token).ConfigureAwait(false);
                    return await ReadStreamingCompletionAsync(
                        responseStream,
                        model,
                        async delta =>
                        {
                            textWasEmitted = true;
                            await onTextDelta(delta).ConfigureAwait(false);
                        },
                        linkedCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var timeout = new TimeoutException("Nebius streaming request timed out.");
                if (textWasEmitted || attempt == _options.MaxAttempts)
                    throw timeout;
                lastError = timeout;
            }
            catch (HttpRequestException ex)
            {
                var quarantined = QuarantineTransportFailure(ex);
                if (textWasEmitted || attempt == _options.MaxAttempts)
                    throw quarantined;
                lastError = quarantined;
            }

            if (attempt < _options.MaxAttempts)
                await Task.Delay(TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1)), cancellationToken).ConfigureAwait(false);
        }

        throw lastError ?? new InvalidOperationException("Nebius streaming request failed without an error response.");
    }

    private static HttpRequestException QuarantineTransportFailure(HttpRequestException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new HttpRequestException(QuarantinedTransportDiagnostic, inner: null, exception.StatusCode);
    }

    private static JsonObject BuildPayload(AgentRequest request, string model, bool stream = false)
    {
        var messages = new JsonArray();
        foreach (var message in request.Messages)
        {
            if (string.IsNullOrWhiteSpace(message.Role))
                throw new ArgumentException("Chat message role is required.", nameof(request));

            var serializedMessage = new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = message.Content
            };

            if (message.ToolCalls is { Count: > 0 })
            {
                if (!string.Equals(message.Role, "assistant", StringComparison.Ordinal))
                    throw new ArgumentException("Only assistant messages may contain tool calls.", nameof(request));
                if (message.ToolCalls.Count > MaximumStreamingToolCalls)
                    throw new ArgumentException("Chat message contains too many tool calls.", nameof(request));

                var calls = new JsonArray();
                foreach (var call in message.ToolCalls)
                {
                    if (string.IsNullOrWhiteSpace(call.Id) || string.IsNullOrWhiteSpace(call.Name))
                        throw new ArgumentException("Assistant tool calls require an id and name.", nameof(request));
                    calls.Add(new JsonObject
                    {
                        ["id"] = call.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = call.Name,
                            ["arguments"] = call.ArgumentsJson
                        }
                    });
                }
                serializedMessage["tool_calls"] = calls;
            }

            if (string.Equals(message.Role, "tool", StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(message.ToolCallId))
                    throw new ArgumentException("Tool result messages require the id of the assistant tool call.", nameof(request));
                serializedMessage["tool_call_id"] = message.ToolCallId;
                if (!string.IsNullOrWhiteSpace(message.Name))
                    serializedMessage["name"] = message.Name;
            }
            else if (!string.IsNullOrWhiteSpace(message.ToolCallId) || !string.IsNullOrWhiteSpace(message.Name))
            {
                throw new ArgumentException("Only tool result messages may contain a tool call id or tool name.", nameof(request));
            }

            messages.Add(serializedMessage);
        }

        var payload = new JsonObject
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["top_p"] = request.TopP,
            ["stream"] = stream
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

    private static async Task<AgentCompletion> ReadStreamingCompletionAsync(
        Stream stream,
        string requestedModel,
        Func<string, ValueTask> onTextDelta,
        CancellationToken cancellationToken)
    {
        using var reader = new BoundedSseReader(stream, MaximumSseLineCharacters);
        var content = new StringBuilder();
        var eventData = new StringBuilder();
        var toolCalls = new Dictionary<int, StreamingToolCall>();
        var model = requestedModel;
        string? finishReason = null;
        var seenChunk = false;
        var seenDone = false;
        var totalEventCharacters = 0;

        async ValueTask ProcessEventAsync(string data)
        {
            var trimmed = data.Trim();
            if (trimmed.Length == 0) return;
            if (string.Equals(trimmed, "[DONE]", StringComparison.Ordinal))
            {
                seenDone = true;
                return;
            }
            if (trimmed.Length > MaximumSseEventCharacters)
                throw new InvalidDataException("Nebius stream event exceeded the supported size.");

            using var document = JsonDocument.Parse(trimmed, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            if (root.TryGetProperty("error", out _))
                throw new InvalidDataException("Nebius streaming inference returned an error event.");
            if (root.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String)
                model = modelElement.GetString() ?? requestedModel;
            if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Nebius stream event did not contain choices.");

            seenChunk = true;
            if (choices.GetArrayLength() == 0) return; // Optional final usage-only chunk.
            var choice = choices[0];
            if (choice.TryGetProperty("finish_reason", out var finishElement) && finishElement.ValueKind == JsonValueKind.String)
                finishReason = finishElement.GetString();
            if (!choice.TryGetProperty("delta", out var delta) || delta.ValueKind != JsonValueKind.Object)
                return;

            if (delta.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
            {
                var text = contentElement.GetString() ?? string.Empty;
                if (content.Length + text.Length > MaximumStreamingResponseCharacters)
                    throw new InvalidDataException("Nebius streaming response exceeded the supported size.");
                content.Append(text);
                if (text.Length > 0)
                    await onTextDelta(text).ConfigureAwait(false);
            }

            if (delta.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in calls.EnumerateArray())
                {
                    var index = call.TryGetProperty("index", out var indexElement) && indexElement.TryGetInt32(out var parsedIndex)
                        ? parsedIndex
                        : 0;
                    if (index is < 0 or >= MaximumStreamingToolCalls)
                        throw new InvalidDataException("Nebius stream contained an unsupported number of tool calls.");
                    if (!toolCalls.TryGetValue(index, out var accumulator))
                    {
                        accumulator = new StreamingToolCall();
                        toolCalls.Add(index, accumulator);
                    }

                    if (call.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                        accumulator.AppendId(id.GetString());
                    if (call.TryGetProperty("function", out var function) && function.ValueKind == JsonValueKind.Object)
                    {
                        if (function.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                            accumulator.AppendName(name.GetString());
                        if (function.TryGetProperty("arguments", out var arguments) && arguments.ValueKind == JsonValueKind.String)
                            accumulator.AppendArguments(arguments.GetString());
                    }
                }
            }
        }

        while (!seenDone)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            if (line.Length == 0)
            {
                if (eventData.Length > 0)
                {
                    var data = eventData.ToString();
                    eventData.Clear();
                    totalEventCharacters = 0;
                    await ProcessEventAsync(data).ConfigureAwait(false);
                }
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;
            var dataPart = line.Length > 5 ? line[5..].TrimStart() : string.Empty;
            totalEventCharacters += dataPart.Length;
            if (totalEventCharacters > MaximumSseEventCharacters)
                throw new InvalidDataException("Nebius stream event exceeded the supported size.");
            if (eventData.Length > 0) eventData.Append('\n');
            eventData.Append(dataPart);
        }

        if (!seenDone && eventData.Length > 0)
            await ProcessEventAsync(eventData.ToString()).ConfigureAwait(false);
        if (!seenDone || !seenChunk)
            throw new InvalidDataException("Nebius stream ended before a complete completion was received.");

        var completions = toolCalls.OrderBy(static pair => pair.Key).Select(static pair => pair.Value.ToToolCall()).ToArray();
        return new AgentCompletion(content.ToString(), completions, model, finishReason);
    }

    private static async Task<string> ReadBoundedErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false);
        var buffer = new char[16 * 1024];
        var count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
        return new string(buffer, 0, count);
    }

    private sealed class BoundedSseReader(Stream stream, int maxLineCharacters) : IDisposable
    {
        private readonly StreamReader _reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        private readonly char[] _buffer = new char[4096];
        private int _offset;
        private int _count;

        public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            var line = new StringBuilder();
            while (true)
            {
                if (_offset >= _count)
                {
                    _count = await _reader.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    _offset = 0;
                    if (_count == 0)
                        return line.Length == 0 ? null : line.ToString().TrimEnd('\r');
                }

                var value = _buffer[_offset++];
                if (value == '\n') return line.ToString().TrimEnd('\r');
                if (line.Length >= maxLineCharacters)
                    throw new InvalidDataException("Nebius stream line exceeded the supported size.");
                line.Append(value);
            }
        }

        public void Dispose() => _reader.Dispose();
    }

    private sealed class StreamingToolCall
    {
        private readonly StringBuilder _id = new();
        private readonly StringBuilder _name = new();
        private readonly StringBuilder _arguments = new();

        public void AppendId(string? value) => AppendBounded(_id, value, 256);
        public void AppendName(string? value) => AppendBounded(_name, value, 128);
        public void AppendArguments(string? value) => AppendBounded(_arguments, value, MaximumStreamingToolArgumentCharacters);

        public ToolCall ToToolCall()
        {
            var name = _name.ToString();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException("Nebius stream contained a tool call without a name.");
            return new ToolCall(_id.ToString(), name, _arguments.Length == 0 ? "{}" : _arguments.ToString());
        }

        private static void AppendBounded(StringBuilder target, string? value, int maxCharacters)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (target.Length + value.Length > maxCharacters)
                throw new InvalidDataException("Nebius stream tool call exceeded the supported size.");
            target.Append(value);
        }
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
    private const string QuarantinedResponseDiagnostic = "Provider response diagnostics quarantined.";

    public HttpStatusCode StatusCode { get; }

    // Retained for API compatibility, but intentionally never contains the raw
    // provider response body. Provider error bodies can echo credentials, signed
    // URLs, prompts, or other sensitive data and must not cross this boundary.
    public string ResponseExcerpt { get; }

    private NebiusApiException(HttpStatusCode statusCode, string responseExcerpt)
        : base($"Nebius Token Factory returned HTTP {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseExcerpt = responseExcerpt;
    }

    public static NebiusApiException FromResponse(HttpStatusCode statusCode, string body)
    {
        _ = body;
        return new NebiusApiException(statusCode, QuarantinedResponseDiagnostic);
    }
}
