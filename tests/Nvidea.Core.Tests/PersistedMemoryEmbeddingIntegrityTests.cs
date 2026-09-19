using Nvidea.Core.Memory;

namespace Nvidea.Core.Tests;

public sealed class PersistedMemoryEmbeddingIntegrityTests
{
    [Theory]
    [MemberData(nameof(InvalidEmbeddingRecords))]
    public async Task InitializeAsync_StripsMalformedPersistedEmbedding_WithoutDeletingMemory(MemoryRecord malformed)
    {
        var store = new RecordingMemoryStore(malformed);
        using var service = new PersonalMemoryService(store);

        await service.InitializeAsync();

        var snapshot = await service.SnapshotAsync();
        var memory = Assert.Single(snapshot);
        Assert.Equal(malformed.Id, memory.Id);
        Assert.Equal(malformed.Content, memory.Content);
        Assert.Null(memory.Embedding);
        Assert.Null(memory.EmbeddingProvenance);

        var persisted = Assert.Single(store.LastWritten);
        Assert.Equal(malformed.Id, persisted.Id);
        Assert.Null(persisted.Embedding);
        Assert.Null(persisted.EmbeddingProvenance);
    }

    [Fact]
    public async Task InitializeAsync_PreservesValidPersistedEmbedding()
    {
        var record = CreateRecord(
            embedding: new[] { 0.25f, -0.5f },
            provenance: EmbeddingProvenance("nebius", "nvidia/embedding", 2));
        var store = new RecordingMemoryStore(record);
        using var service = new PersonalMemoryService(store);

        await service.InitializeAsync();

        var memory = Assert.Single(await service.SnapshotAsync());
        Assert.Equal(record.Embedding, memory.Embedding);
        Assert.Equal(record.EmbeddingProvenance, memory.EmbeddingProvenance);
        Assert.Equal(record.Embedding, Assert.Single(store.LastWritten).Embedding);
    }

    [Fact]
    public async Task SearchAsync_FallsBackToLexicalRetrieval_AfterMalformedEmbeddingIsSanitized()
    {
        var record = CreateRecord(
            key: "preferred editor",
            content: "Use Visual Studio Code for project work",
            embedding: new[] { float.NaN, 1f },
            provenance: EmbeddingProvenance("nebius", "nvidia/embedding", 2));
        var store = new RecordingMemoryStore(record);
        using var service = new PersonalMemoryService(store, embeddingProvider: new FixedEmbeddingProvider());

        await service.InitializeAsync();
        var results = await service.SearchAsync(new MemoryQuery { Text = "preferred editor Visual Studio Code" });

        var result = Assert.Single(results);
        Assert.Equal(record.Id, result.Memory.Id);
        Assert.True(result.LexicalScore > 0);
        Assert.Equal(0, result.SemanticScore);
        Assert.Null(result.Memory.Embedding);
        Assert.Null(result.Memory.EmbeddingProvenance);
    }

    public static IEnumerable<object[]> InvalidEmbeddingRecords()
    {
        yield return new object[] { CreateRecord(embedding: new[] { float.NaN, 1f }, provenance: EmbeddingProvenance("nebius", "model", 2)) };
        yield return new object[] { CreateRecord(embedding: new[] { float.PositiveInfinity, 1f }, provenance: EmbeddingProvenance("nebius", "model", 2)) };
        yield return new object[] { CreateRecord(embedding: new[] { 1f, 2f }, provenance: EmbeddingProvenance("nebius", "model", 3)) };
        yield return new object[] { CreateRecord(embedding: new[] { 1f, 2f }, provenance: null) };
        yield return new object[] { CreateRecord(embedding: null, provenance: EmbeddingProvenance("nebius", "model", 2)) };
        yield return new object[] { CreateRecord(embedding: new[] { 1f, 2f }, provenance: EmbeddingProvenance("", "model", 2)) };
        yield return new object[] { CreateRecord(embedding: new[] { 1f, 2f }, provenance: EmbeddingProvenance("nebius", " ", 2)) };
        yield return new object[] { CreateRecord(embedding: Array.Empty<float>(), provenance: EmbeddingProvenance("nebius", "model", 0)) };
    }

    private static MemoryRecord CreateRecord(
        string key = "memory-key",
        string content = "durable user memory",
        IReadOnlyList<float>? embedding = null,
        MemoryEmbeddingProvenance? provenance = null) => new()
    {
        Id = "memory-1",
        Layer = MemoryLayer.Semantic,
        Key = key,
        Content = content,
        Importance = 0.8,
        Confidence = 0.9,
        Sensitivity = MemorySensitivity.Personal,
        Retention = MemoryRetention.Indefinite,
        Provenance = new MemoryProvenance("test"),
        CreatedAt = DateTimeOffset.Parse("2026-09-19T12:00:00Z"),
        UpdatedAt = DateTimeOffset.Parse("2026-09-19T12:00:00Z"),
        LastAccessedAt = DateTimeOffset.Parse("2026-09-19T12:00:00Z"),
        Embedding = embedding,
        EmbeddingProvenance = provenance,
    };

    private static MemoryEmbeddingProvenance EmbeddingProvenance(string provider, string model, int dimensions) =>
        new(provider, model, dimensions, false, DateTimeOffset.Parse("2026-09-19T12:00:00Z"));

    private sealed class RecordingMemoryStore(params MemoryRecord[] records) : IMemoryStore
    {
        private IReadOnlyList<MemoryRecord> _records = records;
        public IReadOnlyCollection<MemoryRecord> LastWritten { get; private set; } = Array.Empty<MemoryRecord>();

        public Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_records);

        public Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
        {
            LastWritten = memories.ToArray();
            _records = LastWritten.ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class FixedEmbeddingProvider : IProvenancedMemoryEmbeddingProvider
    {
        public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<float>>(new[] { 1f, 0f });

        public Task<MemoryEmbeddingVector> EmbedWithMetadataAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(new MemoryEmbeddingVector(
                new[] { 1f, 0f },
                EmbeddingProvenance("nebius", "nvidia/embedding", 2)));
    }
}
