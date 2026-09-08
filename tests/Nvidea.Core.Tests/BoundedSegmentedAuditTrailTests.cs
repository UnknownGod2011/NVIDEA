using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BoundedSegmentedAuditTrailTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task OversizedSingleEvent_IsRejectedBeforeAuditFileExists()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var oversized = CreateEvent(new string('x', 2_000));
            var measured = JsonSerializer.SerializeToUtf8Bytes(oversized, JsonOptions).Length;
            var trail = new BoundedSegmentedAuditTrail(
                path,
                maxEventsPerSegment: 4,
                new TestProtector(),
                payloadPolicy: new AuditPayloadPolicy(measured - 1, measured * 4L));

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => trail.AppendAsync(oversized));

            Assert.Contains("event logical payload", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(path));
            Assert.Empty(await trail.ReadAllAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ActiveSegmentLogicalBytes_FailClosedBeforeSecondAppend()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var first = CreateEvent(new string('a', 500));
            var second = CreateEvent(new string('b', 500));
            var firstBytes = JsonSerializer.SerializeToUtf8Bytes(first, JsonOptions).Length;
            var secondBytes = JsonSerializer.SerializeToUtf8Bytes(second, JsonOptions).Length;
            var perEventLimit = Math.Max(firstBytes, secondBytes) + 16;
            var activeLimit = firstBytes + secondBytes - 1L;
            var trail = new BoundedSegmentedAuditTrail(
                path,
                maxEventsPerSegment: 4,
                new TestProtector(),
                payloadPolicy: new AuditPayloadPolicy(perEventLimit, activeLimit));

            await trail.AppendAsync(first);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => trail.AppendAsync(second));

            Assert.Contains("active-segment logical payload", error.Message, StringComparison.OrdinalIgnoreCase);
            var retained = await trail.ReadAllAsync();
            Assert.Single(retained);
            Assert.Equal(first.EventId, retained[0].EventId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task FullSegment_RotatesBeforeApplyingNewActiveByteBudget()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var first = CreateEvent(new string('a', 300));
            var second = CreateEvent(new string('b', 300));
            var third = CreateEvent(new string('c', 300));
            var sizes = new[] { first, second, third }
                .Select(x => JsonSerializer.SerializeToUtf8Bytes(x, JsonOptions).Length)
                .ToArray();
            var perEventLimit = sizes.Max() + 16;
            var activeLimit = sizes[0] + sizes[1] + 16L;
            Assert.True(sizes[2] <= activeLimit);

            var trail = new BoundedSegmentedAuditTrail(
                path,
                maxEventsPerSegment: 2,
                new TestProtector(),
                payloadPolicy: new AuditPayloadPolicy(perEventLimit, activeLimit));

            await trail.AppendAsync(first);
            await trail.AppendAsync(second);
            await trail.AppendAsync(third);

            var retained = await trail.ReadAllAsync();
            Assert.Equal(new[] { first.EventId, second.EventId, third.EventId }, retained.Select(x => x.EventId));
            Assert.True(File.Exists(path + ".segment-000002.jsonl"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PayloadPolicy_RejectsActiveLimitSmallerThanEventLimit()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedSegmentedAuditTrail(
                path,
                payloadPolicy: new AuditPayloadPolicy(MaxEventPayloadBytes: 1024, MaxActiveSegmentPayloadBytes: 512)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AuditEvent CreateEvent(string summary) => new(
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        "browser.click",
        "action-1",
        "execution",
        CapabilityRiskLevel.High,
        Allowed: true,
        Approved: true,
        ApprovalScope: "exact-action",
        Summary: summary,
        Metadata: new Dictionary<string, string> { ["host"] = "example.test" });

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedBytes, string purpose) => protectedBytes.ToArray();
    }
}
