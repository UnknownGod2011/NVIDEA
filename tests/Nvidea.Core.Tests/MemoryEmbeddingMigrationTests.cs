using Nvidea.Core.Memory;

namespace Nvidea.Core.Tests;

public sealed class MemoryEmbeddingMigrationTests
{
    [Fact]
    public async Task Preview_ExcludesSensitiveAndRestrictedByDefault()
    {
        var store = new InMemoryStore
        {
            Records =
            [
                CreateMemory("public", MemorySensitivity.Public),
                CreateMemory("sensitive", MemorySensitivity.Sensitive),
                CreateMemory("restricted", MemorySensitivity.Restricted),
            ],
        };
        var provider = new MigrationProvider();
        using var service = new PersonalMemoryService(store, embeddingProvider: provider);

        var plan = await service.PreviewEmbeddingMigrationAsync();

        Assert.Single(plan.Candidates);
        Assert.Equal("public", plan.Candidates[0].Id);
        Assert.Equal(1, plan.ExcludedSensitive);
        Assert.Equal(1, plan.ExcludedRestricted);
        Assert.True(plan.Target.IsLocal);
    }

    [Fact]
    public async Task Migrate_PersistsEachBatchAndBecomesResumable()
    {
        var store = new InMemoryStore
        {
            Records =
            [
                CreateMemory("one", MemorySensitivity.Personal),
                CreateMemory("two", MemorySensitivity.Personal),
                CreateMemory("three", MemorySensitivity.Personal),
            ],
        };
        var provider = new MigrationProvider(maxBatchSize: 2);
        using var service = new PersonalMemoryService(store, embeddingProvider: provider);

        var result = await service.MigrateEmbeddingsAsync(new MemoryEmbeddingMigrationOptions { BatchSize = 8 });
        var secondPlan = await service.PreviewEmbeddingMigrationAsync();

        Assert.Equal(3, result.Planned);
        Assert.Equal(3, result.Updated);
        Assert.Equal(0, result.SkippedConcurrentChanges);
        Assert.Equal(2, store.WriteCountAfterInitialization);
        Assert.Empty(secondPlan.Candidates);
        Assert.All(store.Records, memory => Assert.Equal("model-current", memory.EmbeddingProvenance?.Model));
    }

    [Fact]
    public async Task Migrate_DoesNotOverwriteConcurrentMemoryChange()
    {
        var store = new InMemoryStore { Records = [CreateMemory("race", MemorySensitivity.Personal)] };
        PersonalMemoryService? service = null;
        var provider = new MigrationProvider(async () =>
        {
            await service!.RememberAsync(new MemoryWriteRequest
            {
                Layer = MemoryLayer.Semantic,
                Key = "race",
                Content = "newer user-authored content",
                Sensitivity = MemorySensitivity.Personal,
                Retention = MemoryRetention.Indefinite,
                Provenance = new MemoryProvenance("user"),
                ExplicitUserApproval = true,
            });
        });
        using (service = new PersonalMemoryService(store, embeddingProvider: provider))
        {
            var result = await service.MigrateEmbeddingsAsync();
            var snapshot = await service.SnapshotAsync();

            Assert.Equal(0, result.Updated);
            Assert.Equal(1, result.SkippedConcurrentChanges);
            Assert.Equal("newer user-authored content", Assert.Single(snapshot).Content);
        }
    }

    [Fact]
    public async Task Preview_RejectsNonLocalMigrationProvider()
    {
        var provider = new MigrationProvider(isLocal: false);
        using var service = new PersonalMemoryService(new InMemoryStore(), embeddingProvider: provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewEmbeddingMigrationAsync());
    }

    private static MemoryRecord CreateMemory(string id, MemorySensitivity sensitivity) => new()
    {
        Id = id,
        Layer = MemoryLayer.Semantic,
        Key = id,
        Content = $"content-{id}",
        Sensitivity = sensitivity,
        Retention = MemoryRetention.Indefinite,
        Provenance = new MemoryProvenance("test"),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        LastAccessedAt = DateTimeOffset.UtcNow,
    };

    private sealed class InMemoryStore : IMemoryStore
    {
        public IReadOnlyList<MemoryRecord> Records { get; set; } = Array.Empty<MemoryRecord>();
        public int WriteCount { get; private set; }
        public int WriteCountAfterInitialization => Math.Max(0, WriteCount - 1);

        public Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Records);

        public Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
        {
            Records = memories.ToArray();
            WriteCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class MigrationProvider : IMemoryEmbeddingMigrationProvider
    {
        private readonly Func<Task>? _beforeFirstResult;
        private bool _callbackInvoked;

        public MigrationProvider(Func<Task>? beforeFirstResult = null, bool isLocal = true, int maxBatchSize = 8)
        {
            _beforeFirstResult = beforeFirstResult;
            MigrationTarget = new MemoryEmbeddingMigrationTarget("test-local", "model-current", isLocal, maxBatchSize, 3);
        }

        public MemoryEmbeddingMigrationTarget MigrationTarget { get; }

        public bool IsCurrentEmbedding(MemoryEmbeddingProvenance provenance) =>
            provenance.IsLocal &&
            string.Equals(provenance.Provider, MigrationTarget.Provider, StringComparison.Ordinal) &&
            string.Equals(provenance.Model, MigrationTarget.Model, StringComparison.Ordinal) &&
            provenance.Dimensions == 3;

        public async Task<IReadOnlyList<MemoryEmbeddingVector>> EmbedBatchWithMetadataAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default)
        {
            if (!_callbackInvoked && _beforeFirstResult is not null)
            {
                _callbackInvoked = true;
                await _beforeFirstResult();
            }

            return texts.Select((_, index) => new MemoryEmbeddingVector(
                new float[] { 1, index + 1, 1 },
                new MemoryEmbeddingProvenance(
                    MigrationTarget.Provider,
                    MigrationTarget.Model,
                    3,
                    MigrationTarget.IsLocal,
                    DateTimeOffset.UtcNow))).ToArray();
        }

        public async Task<MemoryEmbeddingVector> EmbedWithMetadataAsync(string text, CancellationToken cancellationToken = default) =>
            (await EmbedBatchWithMetadataAsync(new[] { text }, cancellationToken))[0];

        public async Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default) =>
            (await EmbedWithMetadataAsync(text, cancellationToken)).Vector;
    }
}
