using System.Text;
using System.Text.Json;
using Nvidea.Core.Memory;

namespace Nvidea.Core.Tests;

public sealed class JsonFileMemoryStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"nvidea-memory-store-{Guid.NewGuid():N}");

    [Fact]
    public async Task WriteAllAsync_ReplacesExistingSnapshotWithoutLeavingTempFiles()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        await File.WriteAllTextAsync(path, "legacy-corrupt-content");

        using var store = new JsonFileMemoryStore(path, protector: null);
        var records = new[] { CreateRecord("durable replacement") };

        await store.WriteAllAsync(records);
        var loaded = await store.ReadAllAsync();

        var record = Assert.Single(loaded);
        Assert.Equal("durable replacement", record.Content);
        Assert.Empty(Directory.GetFiles(_directory, "memory.json.*.tmp"));
    }

    [Fact]
    public async Task ReadAllAsync_TruncatedJsonFailsClosedWithoutRewritingSource()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        var truncated = Encoding.UTF8.GetBytes("[{\"id\":\"broken\"");
        await File.WriteAllBytesAsync(path, truncated);

        using var store = new JsonFileMemoryStore(path, protector: null);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAllAsync());

        Assert.IsType<JsonException>(error.InnerException);
        Assert.Equal(truncated, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.GetFiles(_directory, "memory.json.*.tmp"));
    }

    [Fact]
    public async Task WriteAllAsync_PreCanceledTokenPreservesExistingSnapshot()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        using var store = new JsonFileMemoryStore(path, protector: null);
        await store.WriteAllAsync(new[] { CreateRecord("before cancellation") });
        var before = await File.ReadAllBytesAsync(path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            store.WriteAllAsync(new[] { CreateRecord("must not replace") }, cancellation.Token));

        Assert.Equal(before, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.GetFiles(_directory, "memory.json.*.tmp"));
    }

    [Fact]
    public async Task ConcurrentWrites_AreSerializedIntoACompleteSnapshot()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        using var store = new JsonFileMemoryStore(path, protector: null);
        var first = Enumerable.Range(0, 64).Select(i => CreateRecord($"first-{i}")).ToArray();
        var second = Enumerable.Range(0, 64).Select(i => CreateRecord($"second-{i}")).ToArray();

        await Task.WhenAll(store.WriteAllAsync(first), store.WriteAllAsync(second));
        var loaded = await store.ReadAllAsync();

        Assert.Equal(64, loaded.Count);
        var prefix = loaded[0].Content.StartsWith("first-", StringComparison.Ordinal) ? "first-" : "second-";
        Assert.All(loaded, record => Assert.StartsWith(prefix, record.Content, StringComparison.Ordinal));
        Assert.Empty(Directory.GetFiles(_directory, "memory.json.*.tmp"));
    }

    [Fact]
    public async Task ReadAllAsync_CorruptPrimaryDoesNotSilentlyFallBackToBackup()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        using var store = new JsonFileMemoryStore(path, protector: null);
        await store.WriteAllAsync(new[] { CreateRecord("generation one") });
        await store.WriteAllAsync(new[] { CreateRecord("generation two") });
        var backupBeforeCorruption = await File.ReadAllBytesAsync($"{path}.bak");
        await File.WriteAllTextAsync(path, "{truncated");

        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAllAsync());

        Assert.Equal("{truncated", await File.ReadAllTextAsync(path));
        Assert.Equal(backupBeforeCorruption, await File.ReadAllBytesAsync($"{path}.bak"));
    }

    [Fact]
    public async Task RecoverLastKnownGoodAsync_RequiresExplicitIntentAndRestoresPreviousGeneration()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        using var store = new JsonFileMemoryStore(path, protector: null);
        await store.WriteAllAsync(new[] { CreateRecord("known good") });
        await store.WriteAllAsync(new[] { CreateRecord("new current") });
        await File.WriteAllTextAsync(path, "not-json");

        var recovered = await store.RecoverLastKnownGoodAsync();
        var record = Assert.Single(recovered);
        Assert.Equal("known good", record.Content);

        var current = Assert.Single(await store.ReadAllAsync());
        Assert.Equal("known good", current.Content);
        Assert.Empty(Directory.GetFiles(_directory, "memory.json*.tmp"));
    }

    [Fact]
    public async Task WriteAllAsync_CorruptPrimaryNeverDisplacesKnownGoodBackup()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        using var store = new JsonFileMemoryStore(path, protector: null);
        await store.WriteAllAsync(new[] { CreateRecord("backup source") });
        await store.WriteAllAsync(new[] { CreateRecord("current before corruption") });
        var backup = await File.ReadAllBytesAsync($"{path}.bak");
        await File.WriteAllTextAsync(path, "broken-primary");

        await store.WriteAllAsync(new[] { CreateRecord("repaired current") });

        Assert.Equal(backup, await File.ReadAllBytesAsync($"{path}.bak"));
        Assert.Equal("repaired current", Assert.Single(await store.ReadAllAsync()).Content);
    }

    [Fact]
    public async Task RecoverLastKnownGoodAsync_WithoutBackupFailsWithoutChangingPrimary()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "memory.json");
        var corrupt = Encoding.UTF8.GetBytes("corrupt-primary");
        await File.WriteAllBytesAsync(path, corrupt);
        using var store = new JsonFileMemoryStore(path, protector: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.RecoverLastKnownGoodAsync());

        Assert.Equal(corrupt, await File.ReadAllBytesAsync(path));
    }

    private static MemoryRecord CreateRecord(string content) => new()
    {
        Id = Guid.NewGuid(),
        Layer = MemoryLayer.Episodic,
        Content = content,
        Source = "json-store-test",
        Confidence = 1,
        Importance = 0.5,
        Sensitivity = MemorySensitivity.Normal,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        UpdatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
