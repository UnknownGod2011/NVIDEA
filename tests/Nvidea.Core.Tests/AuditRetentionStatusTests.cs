using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class AuditRetentionStatusTests
{
    [Fact]
    public async Task Status_ReportsRetainedUsageWithoutAuditPayloads()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new BoundedSegmentedAuditTrail(
                path,
                maxEventsPerSegment: 2,
                protector: new TestProtector(),
                retention: new AuditRetentionPolicy(MaxArchivedSegments: 4, MaxArchivedBytes: 4 * 1024 * 1024));

            var sensitiveSummary = "PRIVATE-PROMPT-MUST-NOT-LEAK";
            await trail.AppendAsync(CreateEvent(sensitiveSummary));
            await trail.AppendAsync(CreateEvent("second"));
            await trail.AppendAsync(CreateEvent("third"));

            var status = await trail.GetRetentionStatusAsync();
            var json = JsonSerializer.Serialize(status);

            Assert.Equal(2, status.ActiveSegmentIndex);
            Assert.Equal(1, status.ActiveEventCount);
            Assert.Equal(1, status.RetainedArchivedSegments);
            Assert.True(status.ActiveSegmentBytes > 0);
            Assert.True(status.RetainedArchivedBytes > 0);
            Assert.False(status.HasPrunedHistory);
            Assert.Equal(0, status.PrunedEventCount);
            Assert.DoesNotContain(sensitiveSummary, json, StringComparison.Ordinal);
            Assert.DoesNotContain("summary", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("metadata", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Status_ReportsProtectedPruningTombstoneAfterRetention()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new BoundedSegmentedAuditTrail(
                path,
                maxEventsPerSegment: 2,
                protector: new TestProtector(),
                retention: new AuditRetentionPolicy(MaxArchivedSegments: 1, MaxArchivedBytes: 4 * 1024 * 1024));

            for (var i = 0; i < 5; i++)
                await trail.AppendAsync(CreateEvent($"event-{i}"));

            var status = await trail.GetRetentionStatusAsync();

            Assert.Equal(3, status.ActiveSegmentIndex);
            Assert.Equal(1, status.ActiveEventCount);
            Assert.Equal(1, status.RetainedArchivedSegments);
            Assert.Equal(1, status.MaxArchivedSegments);
            Assert.True(status.RetainedArchivedBytes > 0);
            Assert.Equal(1, status.PrunedThroughSegmentIndex);
            Assert.Equal(2, status.PrunedEventCount);
            Assert.True(status.HasPrunedHistory);
            Assert.Equal(64, status.PrunedAnchorDigest.Length);
            Assert.All(status.PrunedAnchorDigest, c => Assert.True(Uri.IsHexDigit(c)));
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
