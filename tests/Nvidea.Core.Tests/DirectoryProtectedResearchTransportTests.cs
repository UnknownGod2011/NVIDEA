using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class DirectoryProtectedResearchTransportTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "nvidea-transport-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task WorkItem_RoundTrips_AndDeletes()
    {
        var transport = new DirectoryProtectedResearchTransport(_root);
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
        await workItems.DeleteAsync(envelope.OpaqueWorkItemId);
        Assert.Null(await workItems.GetAsync(envelope.OpaqueWorkItemId));
    }

    [Fact]
    public async Task Result_DuplicateWrite_FailsClosed()
    {
        var transport = new DirectoryProtectedResearchTransport(_root);
        var results = (IProtectedResearchResultTransport)transport;
        var now = DateTimeOffset.UtcNow;
        var envelope = new ProtectedResearchResultEnvelope(
            ResearchResultProtector.ProtocolVersion,
            "abcdefghijklmnopqrstuvwx12345678",
            "job-123",
            "wrapped",
            "nonce",
            "ciphertext",
            "tag",
            now,
            now.AddMinutes(10));

        await results.PutAsync(envelope);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => results.PutAsync(envelope));
        Assert.Contains("already exists", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("../../escape")]
    [InlineData("bad/slash/abcdefghijklmnopqrstuvwx")]
    [InlineData("too-short")]
    public async Task OpaqueId_PathTraversalOrInvalidValue_IsRejected(string opaqueId)
    {
        var transport = new DirectoryProtectedResearchTransport(_root);
        var workItems = (IProtectedResearchWorkItemTransport)transport;

        await Assert.ThrowsAsync<InvalidOperationException>(() => workItems.GetAsync(opaqueId));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
