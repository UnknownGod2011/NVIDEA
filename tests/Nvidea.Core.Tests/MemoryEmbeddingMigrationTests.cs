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
    public async Task Preview_CorruptSemanticStateStillHonorsSensitivityOptIns()
    {
        var corruptSensitive = WithCorruptEmbedding(CreateMemory("sensitive", MemorySensitivity.Sensitive));
        var corruptRestricted = WithCorruptEmbedding(CreateMemory("restricted", MemorySensitivity.Restricted));
        var store = new InMemoryStore { Records = [corruptSensitive, corruptRestricted] };
        using var service = new PersonalMemoryService(store, embeddingProvider: new MigrationProvider());

        var defaultPlan = await service.PreviewEmbeddingMigrationAsync();
        var sensitivePlan = await service.PreviewEmbeddingMigrationAsync(new MemoryEmbeddingMigrationOptions
        {
            IncludeSensitive = true,
        });
        var allPlan = await service.PreviewEmbeddingMigrationAsync(new MemoryEmbeddingMigrationOptions
        {
            IncludeSensitive = true,
            IncludeRestricted = true,
        });

        Assert.Empty(defaultPlan.Candidates);
        Assert.Equal(1, defaultPlan.ExcludedSensitive);
        Assert.Equal(1, defaultPlan.ExcludedRestricted);
        Assert.Single(sensitivePlan.Candidates);
        Assert.Equal("sensitive", sensitivePlan.Candidates[0].Id);
        Assert.Equal(MemoryEmbeddingMigrationReason.StaleEmbeddingSpace, sensitivePlan.Candidates[0].Reason);
        Assert.Single(allPlan.Candidates, candidate => candidate.Id == "sensitive");
        Assert.Single(allPlan.Candidates, candidate => candidate.Id == "restricted");
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

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public async Task Migrate_InvalidProviderVectorFailsClosedWithoutApplyingBatch(float invalidValue)
    {
        var originalOne = CreateMemory("one", MemorySensitivity.Personal);
        var originalTwo = CreateMemory("two", MemorySensitivity.Personal);
        var store = new InMemoryStore { Records = [originalOne, originalTwo] };
        var provider = new MigrationProvider(vectorFactory: index =>
            index == 0
                ? new float[] { 1, 2, 3 }
                : new float[] { 1, invalidValue, 3 });
        using var service = new PersonalMemoryService(store, embeddingProvider: provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateEmbeddingsAsync(
            new MemoryEmbeddingMigrationOptions { BatchSize = 2 }));

        Assert.Equal(0, store.WriteCountAfterInitialization);
        Assert.All(store.Records, memory =>
        {
            Assert.Null(memory.Embedding);
            Assert.Null(memory.EmbeddingProvenance);
        });
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

    private static MemoryRecord WithCorruptEmbedding(MemoryRecord memory) => memory with
    {
        Embedding = new float[] { 1, float.NaN, 3 },
        EmbeddingProvenance = new MemoryEmbeddingProvenance(
            "test-local",
            "model-current",
            3,
            true,
            DateTimeOffset.UtcNow),
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
        private readonly Func<int, IReadOnlyList<float>>? _vectorFactory;
        private bool _callbackInvoked;

        public MigrationProvider(
            Func<Task>? beforeFirstResult = null,
            bool isLocal = true,
            int maxBatchSize = 8,
            Func<int, IReadOnlyList<float>>? vectorFactory = null)
        {
            _beforeFirstResult = beforeFirstResult;
            _vectorFactory = vectorFactory;
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
                _vectorFactory?.Invoke(index) ?? new float[] { 1, index + 1, 1 },
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
