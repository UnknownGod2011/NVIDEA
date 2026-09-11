using Nvidea.Core.Memory;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class PersonalMemoryRetrievalQualityTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly MemoryEmbeddingProvenance CurrentSpace =
        new("eval", "semantic-v1", 3, IsLocal: true, Now);

    [Fact]
    public async Task SemanticRanking_BeatsLexicalTrap_ForSynonymQuery()
    {
        var records = new[]
        {
            Record(
                id: "target",
                key: "vehicle upkeep",
                content: "Schedule the car service before the long trip.",
                embedding: [1f, 0f, 0f]),
            Record(
                id: "lexical-trap",
                key: "policy reminder",
                content: "Automobile insurance renewal paperwork is due next month.",
                embedding: [0f, 1f, 0f]),
        };

        var lexical = await SearchAsync(records, provider: null, "automobile maintenance");
        var semantic = await SearchAsync(records, new FixtureEmbeddingProvider(), "automobile maintenance");

        Assert.Equal("lexical-trap", Assert.Single(lexical).Memory.Id);
        Assert.Equal("target", semantic[0].Memory.Id);
        Assert.Equal(1d, semantic[0].SemanticScore, precision: 6);
        Assert.True(
            semantic[0].Score - semantic[1].Score >= 0.20,
            $"Semantic ranking margin regressed: target={semantic[0].Score:F4}, runner-up={semantic[1].Score:F4}.");
    }

    [Fact]
    public async Task SemanticRanking_RecoversParaphraseWithoutSharedKeywords()
    {
        var records = new[]
        {
            Record(
                id: "target",
                key: "travel lodging preference",
                content: "Book a quiet hotel near a train station in Japan's capital.",
                embedding: [0f, 0f, 1f]),
            Record(
                id: "lexical-trap",
                key: "weather note",
                content: "Tokyo weather is usually humid during this part of the year.",
                embedding: [0f, 1f, 0f]),
        };

        var lexical = await SearchAsync(records, provider: null, "where should I stay when visiting Tokyo");
        var semantic = await SearchAsync(records, new FixtureEmbeddingProvider(), "where should I stay when visiting Tokyo");

        Assert.Equal("lexical-trap", Assert.Single(lexical).Memory.Id);
        Assert.Equal("target", semantic[0].Memory.Id);
        Assert.True(semantic[0].SemanticScore >= 0.99);
        Assert.True(semantic[0].Score > semantic[1].Score);
    }

    [Fact]
    public async Task IncompatibleEmbeddingSpace_FallsBackToLexicalRanking()
    {
        var staleSpace = CurrentSpace with { Model = "semantic-v0" };
        var records = new[]
        {
            Record(
                id: "rust",
                key: "build preference",
                content: "Use Rust for the systems build.",
                embedding: [1f, 0f, 0f],
                provenance: staleSpace),
            Record(
                id: "python",
                key: "scripting preference",
                content: "Use Python for quick scripts.",
                embedding: [0f, 1f, 0f],
                provenance: staleSpace),
        };

        var lexical = await SearchAsync(records, provider: null, "rust build");
        var incompatibleSemantic = await SearchAsync(records, new FixtureEmbeddingProvider(), "rust build");

        Assert.Equal(lexical.Select(result => result.Memory.Id), incompatibleSemantic.Select(result => result.Memory.Id));
        Assert.Equal(lexical.Select(result => result.Score), incompatibleSemantic.Select(result => result.Score));
        Assert.All(incompatibleSemantic, result => Assert.Equal(0d, result.SemanticScore));
    }

    [Fact]
    public async Task SemanticRanking_NeverBypassesSensitivityFilter()
    {
        var records = new[]
        {
            Record(
                id: "sensitive-target",
                key: "private destination",
                content: "The confidential retreat location is the mountain cabin.",
                embedding: [1f, 0f, 0f],
                sensitivity: MemorySensitivity.Sensitive),
            Record(
                id: "public-decoy",
                key: "travel checklist",
                content: "Pack a charger before travel.",
                embedding: [0f, 1f, 0f],
                sensitivity: MemorySensitivity.Public),
        };

        var defaultResults = await SearchAsync(records, new FixtureEmbeddingProvider(), "automobile maintenance");
        var optedInResults = await SearchAsync(
            records,
            new FixtureEmbeddingProvider(),
            "automobile maintenance",
            new HashSet<MemorySensitivity>
            {
                MemorySensitivity.Public,
                MemorySensitivity.Personal,
                MemorySensitivity.Sensitive,
            });

        Assert.DoesNotContain(defaultResults, result => result.Memory.Id == "sensitive-target");
        Assert.Equal("sensitive-target", optedInResults[0].Memory.Id);
    }

    private static async Task<IReadOnlyList<MemorySearchResult>> SearchAsync(
        IReadOnlyList<MemoryRecord> records,
        IMemoryEmbeddingProvider? provider,
        string query,
        IReadOnlySet<MemorySensitivity>? sensitivities = null)
    {
        var store = new InMemoryMemoryStore(records);
        using var service = new PersonalMemoryService(
            store,
            embeddingProvider: provider,
            timeProvider: new FixedTimeProvider(Now));

        return await service.SearchAsync(new MemoryQuery
        {
            Text = query,
            MaxResults = 10,
            AllowedSensitivities = sensitivities,
        });
    }

    private static MemoryRecord Record(
        string id,
        string key,
        string content,
        IReadOnlyList<float> embedding,
        MemoryEmbeddingProvenance? provenance = null,
        MemorySensitivity sensitivity = MemorySensitivity.Personal) => new()
        {
            Id = id,
            Layer = MemoryLayer.Semantic,
            Key = key,
            Content = content,
            Importance = 0.8,
            Confidence = 1,
            Sensitivity = sensitivity,
            Retention = MemoryRetention.ThirtyDays,
            Provenance = new MemoryProvenance("retrieval-eval", ObservedAt: Now),
            CreatedAt = Now,
            UpdatedAt = Now,
            LastAccessedAt = Now,
            ExpiresAt = Now.AddDays(30),
            Embedding = embedding,
            EmbeddingProvenance = provenance ?? CurrentSpace,
        };

    private sealed class FixtureEmbeddingProvider : IProvenancedMemoryEmbeddingProvider
    {
        public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            (await EmbedWithMetadataAsync(text, cancellationToken)).Vector;

        public Task<MemoryEmbeddingVector> EmbedWithMetadataAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<float> vector = text switch
            {
                var value when value.Contains("automobile maintenance", StringComparison.OrdinalIgnoreCase) => [1f, 0f, 0f],
                var value when value.Contains("stay", StringComparison.OrdinalIgnoreCase) &&
                                   value.Contains("Tokyo", StringComparison.OrdinalIgnoreCase) => [0f, 0f, 1f],
                var value when value.Contains("rust build", StringComparison.OrdinalIgnoreCase) => [1f, 0f, 0f],
                _ => [0f, 1f, 0f],
            };

            return Task.FromResult(new MemoryEmbeddingVector(vector, CurrentSpace));
        }
    }

    private sealed class InMemoryMemoryStore : IMemoryStore
    {
        private IReadOnlyList<MemoryRecord> _records;

        public InMemoryMemoryStore(IReadOnlyList<MemoryRecord> records)
        {
            _records = records.ToArray();
        }

        public Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_records);

        public Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
        {
            _records = memories.ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
