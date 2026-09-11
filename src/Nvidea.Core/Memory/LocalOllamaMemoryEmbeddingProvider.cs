using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Nvidea.Core.Memory;

public sealed record LocalOllamaMemoryEmbeddingOptions
{
    public Uri Endpoint { get; init; } = new("http://127.0.0.1:11434/api/embed");
    public string Model { get; init; } = "embeddinggemma";
    public int MaxInputCharacters { get; init; } = 16_000;
    public int MaxBatchSize { get; init; } = 16;
    public int? ExpectedDimensions { get; init; }
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(20);
}

public sealed class LocalOllamaMemoryEmbeddingProvider : IProvenancedMemoryEmbeddingProvider, IMemoryBatchEmbeddingProvider, IDisposable
{
    private const int AbsoluteMaximumDimensions = 32_768;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly LocalOllamaMemoryEmbeddingOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    public LocalOllamaMemoryEmbeddingProvider(LocalOllamaMemoryEmbeddingOptions? options = null)
        : this(options ?? new LocalOllamaMemoryEmbeddingOptions(), CreateLoopbackHttpClient(), ownsHttpClient: true)
    {
    }

    internal LocalOllamaMemoryEmbeddingProvider(
        LocalOllamaMemoryEmbeddingOptions options,
        HttpClient httpClient,
        bool ownsHttpClient = false)
    {
        _options = ValidateOptions(options ?? throw new ArgumentNullException(nameof(options)));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = ownsHttpClient;
    }

    public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await EmbedWithMetadataAsync(text, cancellationToken).ConfigureAwait(false);
        return result.Vector;
    }

    public async Task<MemoryEmbeddingVector> EmbedWithMetadataAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var results = await EmbedBatchWithMetadataAsync(new[] { text }, cancellationToken).ConfigureAwait(false);
        return results[0];
    }

    public async Task<IReadOnlyList<MemoryEmbeddingVector>> EmbedBatchWithMetadataAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count is < 1)
            throw new ArgumentException("At least one text value is required.", nameof(texts));
        if (texts.Count > _options.MaxBatchSize)
            throw new ArgumentOutOfRangeException(nameof(texts), $"Embedding batches are limited to {_options.MaxBatchSize} items.");

        var normalized = texts.Select(ValidateInput).ToArray();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.RequestTimeout);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(new OllamaEmbedRequest(_options.Model, normalized, Truncate: false), options: JsonOptions),
        };
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeoutCts.Token).ConfigureAwait(false);

        if ((int)response.StatusCode is >= 300 and < 400)
            throw new InvalidOperationException("Local embedding endpoint redirects are not allowed.");

        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false);
        var payload = await JsonSerializer.DeserializeAsync<OllamaEmbedResponse>(
            responseStream,
            JsonOptions,
            timeoutCts.Token).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Local embedding endpoint returned an empty response.");

        if (payload.Embeddings is null || payload.Embeddings.Count != normalized.Length)
            throw new InvalidOperationException("Local embedding endpoint returned an unexpected embedding count.");

        var responseModel = string.IsNullOrWhiteSpace(payload.Model) ? _options.Model : payload.Model.Trim();
        var createdAt = DateTimeOffset.UtcNow;
        var results = new MemoryEmbeddingVector[payload.Embeddings.Count];
        int? dimensions = null;

        for (var index = 0; index < payload.Embeddings.Count; index++)
        {
            var vector = ValidateVector(payload.Embeddings[index]);
            dimensions ??= vector.Count;
            if (vector.Count != dimensions.Value)
                throw new InvalidOperationException("Local embedding endpoint returned inconsistent vector dimensions.");
            if (_options.ExpectedDimensions is { } expected && vector.Count != expected)
                throw new InvalidOperationException($"Local embedding dimension mismatch. Expected {expected}, received {vector.Count}.");

            results[index] = new MemoryEmbeddingVector(
                vector,
                new MemoryEmbeddingProvenance(
                    Provider: "ollama-local",
                    Model: responseModel,
                    Dimensions: vector.Count,
                    IsLocal: true,
                    CreatedAt: createdAt));
        }

        return results;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (_ownsHttpClient)
            _httpClient.Dispose();
        _disposed = true;
    }

    private string ValidateInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Embedding input cannot be blank.", nameof(text));

        var normalized = text.Trim();
        if (normalized.Length > _options.MaxInputCharacters)
            throw new ArgumentOutOfRangeException(nameof(text), $"Embedding input exceeds {_options.MaxInputCharacters} characters.");
        return normalized;
    }

    private static IReadOnlyList<float> ValidateVector(IReadOnlyList<float>? vector)
    {
        if (vector is null || vector.Count == 0)
            throw new InvalidOperationException("Local embedding endpoint returned an empty vector.");
        if (vector.Count > AbsoluteMaximumDimensions)
            throw new InvalidOperationException("Local embedding endpoint returned an unexpectedly large vector.");

        var copy = new float[vector.Count];
        for (var index = 0; index < vector.Count; index++)
        {
            var value = vector[index];
            if (!float.IsFinite(value))
                throw new InvalidOperationException("Local embedding endpoint returned a non-finite vector value.");
            copy[index] = value;
        }
        return copy;
    }

    private static LocalOllamaMemoryEmbeddingOptions ValidateOptions(LocalOllamaMemoryEmbeddingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Endpoint);
        if (!IsStrictLoopback(options.Endpoint))
            throw new ArgumentException("The local embedding endpoint must use HTTP(S) on loopback only.", nameof(options));
        if (!string.Equals(options.Endpoint.AbsolutePath.TrimEnd('/'), "/api/embed", StringComparison.Ordinal))
            throw new ArgumentException("The local Ollama embedding endpoint path must be /api/embed.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new ArgumentException("A local embedding model is required.", nameof(options));
        if (options.MaxInputCharacters is < 256 or > 250_000)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxInputCharacters must be between 256 and 250000.");
        if (options.MaxBatchSize is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxBatchSize must be between 1 and 64.");
        if (options.ExpectedDimensions is <= 0 or > AbsoluteMaximumDimensions)
            throw new ArgumentOutOfRangeException(nameof(options), $"ExpectedDimensions must be between 1 and {AbsoluteMaximumDimensions}.");
        if (options.RequestTimeout <= TimeSpan.Zero || options.RequestTimeout > TimeSpan.FromMinutes(2))
            throw new ArgumentOutOfRangeException(nameof(options), "RequestTimeout must be positive and no greater than two minutes.");
        return options with { Model = options.Model.Trim() };
    }

    private static bool IsStrictLoopback(Uri endpoint)
    {
        if (!endpoint.IsAbsoluteUri || endpoint.UserInfo.Length > 0)
            return false;
        if (endpoint.Scheme is not ("http" or "https"))
            return false;
        if (string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        return IPAddress.TryParse(endpoint.Host, out var address) && IPAddress.IsLoopback(address);
    }

    private static HttpClient CreateLoopbackHttpClient() => new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
    });

    private sealed record OllamaEmbedRequest(string Model, IReadOnlyList<string> Input, bool Truncate);

    private sealed record OllamaEmbedResponse(string? Model, IReadOnlyList<IReadOnlyList<float>>? Embeddings);
}
