using Nvidea.Core.Memory;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class PersonalMemoryRelevanceTests
{
    [Fact]
    public async Task SearchAsync_WithoutEmbeddings_DoesNotReturnUnrelatedRecentMemory()
    {
        var store = new TestMemoryStore();
        using var service = new PersonalMemoryService(store);

        await service.RememberAsync(new MemoryWriteRequest
        {
            Layer = MemoryLayer.Project,
            Key = "hackathon deployment",
            Content = "Deploy the final demo to Nebius before submission.",
            Importance = 1,
            Confidence = 1,
            Sensitivity = MemorySensitivity.Personal,
            Retention = MemoryRetention.ThirtyDays,
            Provenance = new MemoryProvenance("test"),
        });

        var unrelated = await service.SearchAsync(new MemoryQuery
        {
            Text = "favorite pasta recipe",
            MaxResults = 5,
        });
        var related = await service.SearchAsync(new MemoryQuery
        {
            Text = "Nebius deployment",
            MaxResults = 5,
        });

        Assert.Empty(unrelated);
        var match = Assert.Single(related);
        Assert.Equal("hackathon deployment", match.Memory.Key);
        Assert.True(match.LexicalScore > 0);
    }

    [Fact]
    public async Task SearchAsync_WhenEmbeddingProviderFails_FallsBackToLexicalRetrieval()
    {
        var store = new TestMemoryStore();
        using var service = new PersonalMemoryService(store, embeddingProvider: new FailingEmbeddingProvider());

        await service.RememberAsync(new MemoryWriteRequest
        {
            Layer = MemoryLayer.Semantic,
            Key = "response preference",
            Content = "Prefer concise technical explanations.",
            Importance = 0.9,
            Confidence = 1,
            Sensitivity = MemorySensitivity.Personal,
            Retention = MemoryRetention.ThirtyDays,
            Provenance = new MemoryProvenance("test"),
        });

        var results = await service.SearchAsync(new MemoryQuery
        {
            Text = "technical explanations",
            MaxResults = 5,
        });

        var result = Assert.Single(results);
        Assert.Equal("response preference", result.Memory.Key);
        Assert.Equal(0, result.SemanticScore);
        Assert.True(result.LexicalScore > 0);
    }

    private sealed class TestMemoryStore : IMemoryStore
    {
        private IReadOnlyList<MemoryRecord> _records = Array.Empty<MemoryRecord>();

        public Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_records);

        public Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
        {
            _records = memories.ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class FailingEmbeddingProvider : IMemoryEmbeddingProvider
    {
        public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<float>>(new HttpRequestException("simulated embedding outage"));
    }
}
