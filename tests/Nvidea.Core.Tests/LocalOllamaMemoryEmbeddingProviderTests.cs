using System.Net;
using System.Text;
using Nvidea.Core.Memory;

namespace Nvidea.Core.Tests;

public sealed class LocalOllamaMemoryEmbeddingProviderTests
{
    [Fact]
    public async Task EmbedWithMetadataAsync_UsesLoopbackApiAndReturnsProvenance()
    {
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, """
                {"model":"embeddinggemma:latest","embeddings":[[0.25,0.5,0.75]]}
                """);
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new LocalOllamaMemoryEmbeddingProvider(
            new LocalOllamaMemoryEmbeddingOptions
            {
                Model = "embeddinggemma",
                ExpectedDimensions = 3,
            },
            httpClient);

        var result = await provider.EmbedWithMetadataAsync("  remember this  ");

        Assert.Equal(new float[] { 0.25f, 0.5f, 0.75f }, result.Vector.ToArray());
        Assert.Equal("ollama-local", result.Provenance.Provider);
        Assert.Equal("embeddinggemma:latest", result.Provenance.Model);
        Assert.Equal(3, result.Provenance.Dimensions);
        Assert.True(result.Provenance.IsLocal);
        Assert.NotNull(requestBody);
        Assert.Contains("\"truncate\":false", requestBody!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remember this", requestBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsNonLoopbackEndpoint()
    {
        var options = new LocalOllamaMemoryEmbeddingOptions
        {
            Endpoint = new Uri("https://example.com/api/embed"),
        };

        Assert.Throws<ArgumentException>(() => new LocalOllamaMemoryEmbeddingProvider(options));
    }

    [Fact]
    public async Task EmbedBatchWithMetadataAsync_RejectsDimensionMismatch()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(Json(
            HttpStatusCode.OK,
            """{"model":"embeddinggemma","embeddings":[[1,0],[0,1,0]]}""")));
        using var httpClient = new HttpClient(handler);
        using var provider = new LocalOllamaMemoryEmbeddingProvider(
            new LocalOllamaMemoryEmbeddingOptions { MaxBatchSize = 2 },
            httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.EmbedBatchWithMetadataAsync(new[] { "first", "second" }));
    }

    [Fact]
    public async Task EmbedBatchWithMetadataAsync_RejectsRedirects()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("https://example.com/api/embed") },
        };
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(response));
        using var httpClient = new HttpClient(handler);
        using var provider = new LocalOllamaMemoryEmbeddingProvider(
            new LocalOllamaMemoryEmbeddingOptions(),
            httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.EmbedWithMetadataAsync("no redirect escape"));
    }

    [Fact]
    public async Task PersonalMemoryService_PersistsEmbeddingProvenance()
    {
        var store = new InMemoryStore();
        var provider = new StaticProvenancedProvider("model-a", new float[] { 1, 0, 0 });
        using var service = new PersonalMemoryService(store, embeddingProvider: provider);

        var memory = await service.RememberAsync(new MemoryWriteRequest
        {
            Layer = MemoryLayer.Semantic,
            Key = "preferred editor",
            Content = "Uses Visual Studio Code",
            Provenance = new MemoryProvenance("user"),
            ExplicitUserApproval = true,
        });

        Assert.NotNull(memory.EmbeddingProvenance);
        Assert.Equal("test-local", memory.EmbeddingProvenance!.Provider);
        Assert.Equal("model-a", memory.EmbeddingProvenance.Model);
        Assert.Equal(3, memory.EmbeddingProvenance.Dimensions);
        Assert.True(memory.EmbeddingProvenance.IsLocal);
        Assert.Single(store.Records);
        Assert.Equal("model-a", store.Records[0].EmbeddingProvenance?.Model);
    }

    [Fact]
    public async Task PersonalMemoryService_DoesNotCompareVectorsFromDifferentModels()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new InMemoryStore
        {
            Records =
            [
                new MemoryRecord
                {
                    Id = "old",
                    Layer = MemoryLayer.Semantic,
                    Key = "unrelated",
                    Content = "no lexical overlap here",
                    Importance = 0.5,
                    Confidence = 1,
                    Sensitivity = MemorySensitivity.Personal,
                    Retention = MemoryRetention.Indefinite,
                    Provenance = new MemoryProvenance("test"),
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastAccessedAt = now,
                    Embedding = new float[] { 1, 0, 0 },
                    EmbeddingProvenance = new MemoryEmbeddingProvenance(
                        "test-local", "model-old", 3, true, now),
                },
            ],
        };
        var provider = new StaticProvenancedProvider("model-new", new float[] { 1, 0, 0 });
        using var service = new PersonalMemoryService(store, embeddingProvider: provider);

        var results = await service.SearchAsync(new MemoryQuery { Text = "completely different query" });

        Assert.Empty(results);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }

    private sealed class InMemoryStore : IMemoryStore
    {
        public IReadOnlyList<MemoryRecord> Records { get; set; } = Array.Empty<MemoryRecord>();

        public Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Records);

        public Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
        {
            Records = memories.ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class StaticProvenancedProvider(string model, IReadOnlyList<float> vector) : IProvenancedMemoryEmbeddingProvider
    {
        public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(vector);

        public Task<MemoryEmbeddingVector> EmbedWithMetadataAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MemoryEmbeddingVector(
                vector,
                new MemoryEmbeddingProvenance(
                    "test-local",
                    model,
                    vector.Count,
                    true,
                    DateTimeOffset.UtcNow)));
    }
}
