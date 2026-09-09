using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class S3ProtectedResearchTransportTests
{
    [Fact]
    public async Task WorkItem_RoundTrips_ThroughDedicatedPrefix()
    {
        var store = new FakeObjectStore();
        var transport = new S3ProtectedResearchTransport(store);
        var workItems = (IProtectedResearchWorkItemTransport)transport;
        var now = DateTimeOffset.UtcNow;
        var envelope = new ProtectedResearchWorkItemEnvelope(
            ResearchWorkItemProtector.ProtocolVersion,
            "abcdefghijklmnopqrstuvwx12345678",
            "wrapped",
            "nonce",
            "ciphertext",
            "tag",
            now,
            now.AddMinutes(10));

        await workItems.PutAsync(envelope);
        var loaded = await workItems.GetAsync(envelope.OpaqueWorkItemId);

        Assert.Equal(envelope, loaded);
        Assert.Contains("work-items/abcdefghijklmnopqrstuvwx12345678.json", store.Keys);
    }

    [Fact]
    public async Task WorkItem_Result_AndBinding_UseIsolatedNamespaces()
    {
        var store = new FakeObjectStore();
        var transport = new S3ProtectedResearchTransport(store);
        var now = DateTimeOffset.UtcNow;
        const string opaqueId = "abcdefghijklmnopqrstuvwx12345678";

        await ((IProtectedResearchWorkItemTransport)transport).PutAsync(new ProtectedResearchWorkItemEnvelope(
            ResearchWorkItemProtector.ProtocolVersion,
            opaqueId,
            "wrapped",
            "nonce",
            "ciphertext",
            "tag",
            now,
            now.AddMinutes(10)));

        await ((IProtectedResearchResultTransport)transport).PutAsync(new ProtectedResearchResultEnvelope(
            ResearchResultProtector.ProtocolVersion,
            opaqueId,
            "job-123",
            "wrapped",
            "nonce",
            "ciphertext",
            "tag",
            now,
            now.AddMinutes(10)));

        await ((IProtectedResearchDispatchBindingTransport)transport).PutAsync(new ProtectedResearchDispatchBinding(
            ResearchDispatchBindingProtector.ProtocolVersion,
            opaqueId,
            "job-123",
            ResearchDispatchBindingProtector.GetDeterministicRemoteJobName(opaqueId),
            now,
            now.AddMinutes(10),
            "signature"));

        Assert.Equal(3, store.Keys.Count);
        Assert.Contains($"work-items/{opaqueId}.json", store.Keys);
        Assert.Contains($"results/{opaqueId}.json", store.Keys);
        Assert.Contains($"dispatch-bindings/{opaqueId}.json", store.Keys);
    }

    [Fact]
    public async Task DuplicateWrite_FailsClosed_WithoutReplacingOriginal()
    {
        var store = new FakeObjectStore();
        var transport = new S3ProtectedResearchTransport(store);
        var workItems = (IProtectedResearchWorkItemTransport)transport;
        var now = DateTimeOffset.UtcNow;
        const string opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        var first = new ProtectedResearchWorkItemEnvelope(
            ResearchWorkItemProtector.ProtocolVersion,
            opaqueId,
            "wrapped-a",
            "nonce",
            "ciphertext",
            "tag",
            now,
            now.AddMinutes(10));
        var second = first with { WrappedDataKey = "wrapped-b" };

        await workItems.PutAsync(first);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => workItems.PutAsync(second));

        Assert.Contains("already exists", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(first, await workItems.GetAsync(opaqueId));
    }

    [Fact]
    public async Task Delete_IsIdempotent_AndRemovesOnlyRequestedNamespace()
    {
        var store = new FakeObjectStore();
        var transport = new S3ProtectedResearchTransport(store);
        var now = DateTimeOffset.UtcNow;
        const string opaqueId = "abcdefghijklmnopqrstuvwx12345678";
        var result = new ProtectedResearchResultEnvelope(
            ResearchResultProtector.ProtocolVersion,
            opaqueId,
            "job-123",
            "wrapped",
            "nonce",
            "ciphertext",
            "tag",
            now,
            now.AddMinutes(10));

        await ((IProtectedResearchResultTransport)transport).PutAsync(result);
        await ((IProtectedResearchResultTransport)transport).DeleteAsync(opaqueId);
        await ((IProtectedResearchResultTransport)transport).DeleteAsync(opaqueId);

        Assert.Null(await ((IProtectedResearchResultTransport)transport).GetAsync(opaqueId));
    }

    [Theory]
    [InlineData("../../escape")]
    [InlineData("bad/slash/abcdefghijklmnopqrstuvwx")]
    [InlineData("too-short")]
    public async Task InvalidOpaqueId_IsRejectedBeforeObjectStoreAccess(string opaqueId)
    {
        var store = new FakeObjectStore();
        var transport = new S3ProtectedResearchTransport(store);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ((IProtectedResearchWorkItemTransport)transport).GetAsync(opaqueId));
        Assert.Empty(store.Keys);
    }

    [Fact]
    public void NebiusClient_RejectsNonHttpsEndpoint()
    {
        var options = ValidClientOptions() with { Endpoint = "http://storage.eu-north1.nebius.cloud" };
        Assert.Throws<ArgumentException>(() => new NebiusObjectStorageClient(options));
    }

    [Fact]
    public void NebiusClient_RejectsEscapingPrefix()
    {
        var options = ValidClientOptions() with { Prefix = "../other-project" };
        Assert.Throws<ArgumentException>(() => new NebiusObjectStorageClient(options));
    }

    [Fact]
    public void NebiusClient_RejectsUnboundedRetryConfiguration()
    {
        var options = ValidClientOptions() with { MaxRetries = 100 };
        Assert.Throws<ArgumentOutOfRangeException>(() => new NebiusObjectStorageClient(options));
    }

    private static NebiusObjectStorageClientOptions ValidClientOptions() => new(
        "https://storage.eu-north1.nebius.cloud",
        "eu-north1",
        "nvidea-research",
        "test-access-key",
        "test-secret-key");

    private sealed class FakeObjectStore : IProtectedResearchObjectStoreClient
    {
        private readonly Dictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Keys => _objects.Keys;

        public Task PutIfAbsentAsync(string key, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_objects.ContainsKey(key))
                throw new InvalidOperationException("A protected research object already exists for this opaque work-item id.");
            _objects.Add(key, content.ToArray());
            return Task.CompletedTask;
        }

        public Task<byte[]?> GetAsync(string key, int maxBytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_objects.TryGetValue(key, out var value))
                return Task.FromResult<byte[]?>(null);
            if (value.Length > maxBytes)
                throw new InvalidOperationException("Protected research object exceeds the configured transport limit.");
            return Task.FromResult<byte[]?>(value.ToArray());
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _objects.Remove(key);
            return Task.CompletedTask;
        }
    }
}
