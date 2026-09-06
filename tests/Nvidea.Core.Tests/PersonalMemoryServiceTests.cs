using Nvidea.Core.Memory;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class PersonalMemoryServiceTests
{
    [Fact]
    public async Task RememberAsync_RejectsCredentialLikeContentEvenWithApproval()
    {
        var store = new InMemoryMemoryStore();
        using var service = new PersonalMemoryService(store);

        var request = NewRequest(
            key: "deployment credential",
            content: "api_key=super-secret-token-value",
            sensitivity: MemorySensitivity.Restricted,
            explicitApproval: true,
            retention: MemoryRetention.Indefinite);

        var exception = await Assert.ThrowsAsync<MemoryWriteRejectedException>(() => service.RememberAsync(request));

        Assert.Contains("credentials", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await service.SnapshotAsync());
    }

    [Fact]
    public async Task RememberAsync_RequiresApprovalForSensitiveOrIndefiniteMemory()
    {
        var store = new InMemoryMemoryStore();
        using var service = new PersonalMemoryService(store);

        await Assert.ThrowsAsync<MemoryWriteRejectedException>(() => service.RememberAsync(NewRequest(
            "private preference",
            "Keep this private.",
            MemorySensitivity.Sensitive,
            explicitApproval: false)));

        await Assert.ThrowsAsync<MemoryWriteRejectedException>(() => service.RememberAsync(NewRequest(
            "long preference",
            "Always use concise responses.",
            MemorySensitivity.Personal,
            explicitApproval: false,
            retention: MemoryRetention.Indefinite)));

        var approved = await service.RememberAsync(NewRequest(
            "long preference",
            "Always use concise responses.",
            MemorySensitivity.Personal,
            explicitApproval: true,
            retention: MemoryRetention.Indefinite));

        Assert.Equal(MemoryRetention.Indefinite, approved.Retention);
    }

    [Fact]
    public async Task SessionMemory_RemainsInProcessButIsNeverPersisted()
    {
        var store = new InMemoryMemoryStore();
        using var service = new PersonalMemoryService(store);

        var memory = await service.RememberAsync(NewRequest(
            "current task",
            "Editing the hackathon README.",
            MemorySensitivity.Personal,
            explicitApproval: false,
            retention: MemoryRetention.Session,
            layer: MemoryLayer.Working));

        Assert.Contains((await service.SnapshotAsync()), item => item.Id == memory.Id);
        Assert.Empty(store.Records);

        using var restarted = new PersonalMemoryService(store);
        await restarted.InitializeAsync();
        Assert.Empty(await restarted.SnapshotAsync());
    }

    [Fact]
    public async Task SearchAsync_UsesSemanticSimilarityAndDoesNotExposeSensitiveMemoryByDefault()
    {
        var store = new InMemoryMemoryStore();
        var embeddings = new DeterministicEmbeddingProvider();
        using var service = new PersonalMemoryService(store, embeddingProvider: embeddings);

        await service.RememberAsync(NewRequest(
            "preferred systems language",
            "Rust is preferred for low-level systems work.",
            MemorySensitivity.Personal,
            explicitApproval: false,
            retention: MemoryRetention.ThirtyDays,
            layer: MemoryLayer.Semantic));
        await service.RememberAsync(NewRequest(
            "preferred scripting language",
            "Python is preferred for quick scripts.",
            MemorySensitivity.Personal,
            explicitApproval: false,
            retention: MemoryRetention.ThirtyDays,
            layer: MemoryLayer.Semantic));
        await service.RememberAsync(NewRequest(
            "medical context",
            "A user-approved sensitive fact for testing.",
            MemorySensitivity.Sensitive,
            explicitApproval: true,
            retention: MemoryRetention.ThirtyDays,
            layer: MemoryLayer.Semantic));

        var results = await service.SearchAsync(new MemoryQuery
        {
            Text = "best systems language",
            MaxResults = 10,
        });

        Assert.NotEmpty(results);
        Assert.Equal("preferred systems language", results[0].Memory.Key);
        Assert.DoesNotContain(results, result => result.Memory.Key == "medical context");

        var sensitiveResults = await service.SearchAsync(new MemoryQuery
        {
            Text = "testing context",
            MaxResults = 10,
            AllowedSensitivities = new HashSet<MemorySensitivity>
            {
                MemorySensitivity.Public,
                MemorySensitivity.Personal,
                MemorySensitivity.Sensitive,
            },
        });

        Assert.Contains(sensitiveResults, result => result.Memory.Key == "medical context");
    }

    [Fact]
    public async Task SevenDayMemory_ExpiresAndIsPurgedFromPersistence()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));
        var store = new InMemoryMemoryStore();
        using var service = new PersonalMemoryService(store, timeProvider: clock);

        await service.RememberAsync(NewRequest(
            "temporary project fact",
            "The demo branch is temporary.",
            MemorySensitivity.Personal,
            explicitApproval: false,
            retention: MemoryRetention.SevenDays,
            layer: MemoryLayer.Project));

        Assert.Single(store.Records);
        clock.Advance(TimeSpan.FromDays(8));

        var removed = await service.PurgeExpiredAsync();

        Assert.Equal(1, removed);
        Assert.Empty(await service.SnapshotAsync());
        Assert.Empty(store.Records);
    }

    [Fact]
    public async Task RememberAsync_UpdatesMatchingLayerAndKeyInsteadOfCreatingDuplicates()
    {
        var store = new InMemoryMemoryStore();
        using var service = new PersonalMemoryService(store);

        var first = await service.RememberAsync(NewRequest(
            "response style",
            "Prefer short answers.",
            MemorySensitivity.Personal,
            explicitApproval: true,
            retention: MemoryRetention.Indefinite,
            layer: MemoryLayer.Semantic));
        var second = await service.RememberAsync(NewRequest(
            "response style",
            "Prefer concise answers with technical detail when useful.",
            MemorySensitivity.Personal,
            explicitApproval: true,
            retention: MemoryRetention.Indefinite,
            layer: MemoryLayer.Semantic));

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.CreatedAt, second.CreatedAt);
        var snapshot = await service.SnapshotAsync();
        Assert.Single(snapshot);
        Assert.Contains("technical detail", snapshot[0].Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task JsonFileMemoryStore_RoundTripsRecords()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"nvidea-memory-test-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "memory.json");
        try
        {
            using var store = new JsonFileMemoryStore(path);
            var now = DateTimeOffset.UtcNow;
            var record = new MemoryRecord
            {
                Id = "memory-1",
                Layer = MemoryLayer.Project,
                Key = "hackathon",
                Content = "Nebius x NVIDIA",
                Tags = new[] { "hackathon", "nvidia" },
                Importance = 0.9,
                Confidence = 1,
                Sensitivity = MemorySensitivity.Personal,
                Retention = MemoryRetention.ThirtyDays,
                Provenance = new MemoryProvenance("test", ObservedAt: now),
                CreatedAt = now,
                UpdatedAt = now,
                LastAccessedAt = now,
                ExpiresAt = now.AddDays(30),
            };

            await store.WriteAllAsync(new[] { record });
            var loaded = await store.ReadAllAsync();

            var restored = Assert.Single(loaded);
            Assert.Equal(record.Id, restored.Id);
            Assert.Equal(record.Layer, restored.Layer);
            Assert.Equal(record.Tags, restored.Tags);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static MemoryWriteRequest NewRequest(
        string key,
        string content,
        MemorySensitivity sensitivity,
        bool explicitApproval,
        MemoryRetention retention = MemoryRetention.ThirtyDays,
        MemoryLayer layer = MemoryLayer.Semantic) => new()
        {
            Layer = layer,
            Key = key,
            Content = content,
            Importance = 0.8,
            Confidence = 1,
            Sensitivity = sensitivity,
            Retention = retention,
            ExplicitUserApproval = explicitApproval,
            Provenance = new MemoryProvenance("test"),
        };

    private sealed class InMemoryMemoryStore : IMemoryStore
    {
        public IReadOnlyList<MemoryRecord> Records { get; private set; } = Array.Empty<MemoryRecord>();

        public Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Records);

        public Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default)
        {
            Records = memories.ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class DeterministicEmbeddingProvider : IMemoryEmbeddingProvider
    {
        public Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<float> vector = text.Contains("Rust", StringComparison.OrdinalIgnoreCase) ||
                                          text.Contains("systems language", StringComparison.OrdinalIgnoreCase)
                ? new[] { 1f, 0f }
                : text.Contains("Python", StringComparison.OrdinalIgnoreCase)
                    ? new[] { 0f, 1f }
                    : new[] { 0.5f, 0.5f };
            return Task.FromResult(vector);
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public MutableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
    }
}
