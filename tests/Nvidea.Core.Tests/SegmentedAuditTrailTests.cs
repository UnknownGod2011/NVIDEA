using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class SegmentedAuditTrailTests
{
    private const string ManifestPurpose = "audit-segment-manifest-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Rotation_BoundsActiveSegmentAndPreservesGlobalHistory()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var trail = new SegmentedAuditTrail(path, maxEventsPerSegment: 2, protector);

            var first = CreateEvent("first");
            var second = CreateEvent("second");
            var third = CreateEvent("third");
            await trail.AppendAsync(first);
            await trail.AppendAsync(second);
            await trail.AppendAsync(third);

            var restored = await trail.ReadAllAsync();
            Assert.Equal(new[] { first.EventId, second.EventId, third.EventId }, restored.Select(x => x.EventId));
            Assert.Equal(2, (await File.ReadAllLinesAsync(path)).Length);
            Assert.Single(await File.ReadAllLinesAsync(path + ".segment-000002.jsonl"));
            Assert.True(File.Exists(path + ".segments"));

            var manifestBytes = await File.ReadAllBytesAsync(path + ".segments");
            Assert.DoesNotContain("first", Encoding.UTF8.GetString(manifestBytes), StringComparison.Ordinal);
            var manifest = ReadManifest(manifestBytes, protector);
            Assert.Equal("committed", manifest.State);
            Assert.Equal(2, manifest.ActiveIndex);
            Assert.Single(manifest.Archived);
            Assert.Equal(2, manifest.Archived[0].EventCount);
            Assert.Equal(0, manifest.PrunedThroughIndex);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ArchivedSegmentMutation_IsRejectedByProtectedCrossSegmentAnchor()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new SegmentedAuditTrail(path, maxEventsPerSegment: 2, new TestProtector());
            await trail.AppendAsync(CreateEvent("first"));
            await trail.AppendAsync(CreateEvent("second"));
            await trail.AppendAsync(CreateEvent("third"));

            var bytes = await File.ReadAllBytesAsync(path);
            bytes[^1] ^= 0x01;
            await File.WriteAllBytesAsync(path, bytes);

            var error = await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());
            Assert.Contains("protected anchor", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PendingRollover_WithoutNewSegmentRestoresPreviousCommittedState()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var trail = new SegmentedAuditTrail(path, maxEventsPerSegment: 2, protector);
            await trail.AppendAsync(CreateEvent("first"));
            await trail.AppendAsync(CreateEvent("second"));

            var anchor = await BuildAnchorAsync(path, index: 1, eventCount: 2);
            var pending = new TestManifest(1, "pending", 1, new[] { anchor }, 2);
            await WriteManifestAsync(path, protector, pending);

            Assert.Equal(2, (await trail.ReadAllAsync()).Count);
            var recovered = ReadManifest(await File.ReadAllBytesAsync(path + ".segments"), protector);
            Assert.Equal("committed", recovered.State);
            Assert.Equal(1, recovered.ActiveIndex);
            Assert.Empty(recovered.Archived);
            Assert.Null(recovered.PendingActiveIndex);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task PendingRollover_WithValidNewSegmentFinalizesWithoutReplayingAnything()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var trail = new SegmentedAuditTrail(path, maxEventsPerSegment: 2, protector);
            var first = CreateEvent("first");
            var second = CreateEvent("second");
            var third = CreateEvent("third");
            await trail.AppendAsync(first);
            await trail.AppendAsync(second);

            var anchor = await BuildAnchorAsync(path, index: 1, eventCount: 2);
            await WriteManifestAsync(path, protector, new TestManifest(1, "pending", 1, new[] { anchor }, 2));
            await new JsonLinesAuditTrail(path + ".segment-000002.jsonl", protector).AppendAsync(third);

            var restored = await trail.ReadAllAsync();
            Assert.Equal(new[] { first.EventId, second.EventId, third.EventId }, restored.Select(x => x.EventId));
            var recovered = ReadManifest(await File.ReadAllBytesAsync(path + ".segments"), protector);
            Assert.Equal("committed", recovered.State);
            Assert.Equal(2, recovered.ActiveIndex);
            Assert.Null(recovered.PendingActiveIndex);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DuplicateEventAcrossSegments_IsRejectedGlobally()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var trail = new SegmentedAuditTrail(path, maxEventsPerSegment: 2, protector);
            var duplicate = CreateEvent("duplicate");
            await trail.AppendAsync(duplicate);
            await trail.AppendAsync(CreateEvent("second"));
            await trail.AppendAsync(CreateEvent("third"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => trail.AppendAsync(duplicate));
            Assert.Equal(3, (await trail.ReadAllAsync()).Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retention_PrunesOldestArchivedSegment_WithProtectedTombstone()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var trail = new SegmentedAuditTrail(
                path,
                maxEventsPerSegment: 2,
                protector,
                new AuditRetentionPolicy(MaxArchivedSegments: 1, MaxArchivedBytes: 1024 * 1024));

            var first = CreateEvent("first");
            var second = CreateEvent("second");
            var third = CreateEvent("third");
            var fourth = CreateEvent("fourth");
            var fifth = CreateEvent("fifth");
            await trail.AppendAsync(first);
            await trail.AppendAsync(second);
            await trail.AppendAsync(third);
            await trail.AppendAsync(fourth);
            await trail.AppendAsync(fifth);

            var retained = await trail.ReadAllAsync();
            Assert.Equal(new[] { third.EventId, fourth.EventId, fifth.EventId }, retained.Select(x => x.EventId));
            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
            Assert.True(File.Exists(path + ".segment-000002.jsonl"));
            Assert.True(File.Exists(path + ".segment-000003.jsonl"));

            var manifest = ReadManifest(await File.ReadAllBytesAsync(path + ".segments"), protector);
            Assert.Equal(3, manifest.ActiveIndex);
            Assert.Single(manifest.Archived);
            Assert.Equal(2, manifest.Archived[0].Index);
            Assert.Equal(1, manifest.PrunedThroughIndex);
            Assert.Equal(2, manifest.PrunedEventCount);
            Assert.Matches("^[0-9A-F]{64}$", manifest.PrunedAnchorDigest!);
            Assert.Empty(manifest.PendingDeleteIndices ?? Array.Empty<int>());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retention_ByteQuotaFailsClosed_BeforeOversizedActiveSegmentIsArchived()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new SegmentedAuditTrail(
                path,
                maxEventsPerSegment: 2,
                new TestProtector(),
                new AuditRetentionPolicy(MaxArchivedSegments: 8, MaxArchivedBytes: 1));

            var first = CreateEvent("first");
            var second = CreateEvent("second");
            await trail.AppendAsync(first);
            await trail.AppendAsync(second);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => trail.AppendAsync(CreateEvent("blocked")));
            Assert.Contains("retention quota", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(new[] { first.EventId, second.EventId }, (await trail.ReadAllAsync()).Select(x => x.EventId));
            Assert.False(File.Exists(path + ".segment-000002.jsonl"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retention_PendingDeleteManifest_RecoversExactDeletionAfterCrash()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var first = CreateEvent("first");
            var second = CreateEvent("second");
            var third = CreateEvent("third");
            var baseTrail = new JsonLinesAuditTrail(path, protector);
            await baseTrail.AppendAsync(first);
            await baseTrail.AppendAsync(second);
            await new JsonLinesAuditTrail(path + ".segment-000002.jsonl", protector).AppendAsync(third);

            var prunedAnchor = await BuildAnchorAsync(path, index: 1, eventCount: 2);
            var digest = ComputePrunedAnchorDigest(string.Empty, prunedAnchor);
            await WriteManifestAsync(
                path,
                protector,
                new TestManifest(
                    1,
                    "committed",
                    2,
                    Array.Empty<TestAnchor>(),
                    null,
                    PrunedThroughIndex: 1,
                    PrunedEventCount: 2,
                    PrunedAnchorDigest: digest,
                    PendingDeleteIndices: new[] { 1 }));

            var trail = new SegmentedAuditTrail(path, 2, protector, new AuditRetentionPolicy(1, 1024 * 1024));
            var restored = await trail.ReadAllAsync();

            Assert.Single(restored);
            Assert.Equal(third.EventId, restored[0].EventId);
            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
            var recovered = ReadManifest(await File.ReadAllBytesAsync(path + ".segments"), protector);
            Assert.Empty(recovered.PendingDeleteIndices ?? Array.Empty<int>());
            Assert.Equal(1, recovered.PrunedThroughIndex);
            Assert.Equal(digest, recovered.PrunedAnchorDigest);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<TestAnchor> BuildAnchorAsync(string path, int index, int eventCount)
    {
        var segmentPath = index == 1 ? path : path + $".segment-{index:D6}.jsonl";
        return new TestAnchor(
            index,
            eventCount,
            await Sha256Async(segmentPath),
            await Sha256Async(segmentPath + ".seal"));
    }

    private static string ComputePrunedAnchorDigest(string previousDigest, TestAnchor anchor)
    {
        var canonical = string.Join(
            "\n",
            previousDigest,
            anchor.Index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            anchor.EventCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            anchor.DataSha256,
            anchor.SealSha256);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static async Task<string> Sha256Async(string path)
    {
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToHexString(await sha.ComputeHashAsync(stream));
    }

    private static async Task WriteManifestAsync(string path, ILocalStateProtector protector, TestManifest manifest)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        var encoded = LocalStateEnvelope.Encode(plaintext, protector, ManifestPurpose);
        await File.WriteAllBytesAsync(path + ".segments", encoded);
    }

    private static TestManifest ReadManifest(byte[] persisted, ILocalStateProtector protector)
    {
        var decoded = LocalStateEnvelope.Decode(persisted, protector, ManifestPurpose);
        Assert.True(decoded.WasProtected);
        return JsonSerializer.Deserialize<TestManifest>(decoded.Plaintext, JsonOptions)!;
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

    private sealed record TestManifest(
        int Version,
        string State,
        int ActiveIndex,
        IReadOnlyList<TestAnchor> Archived,
        int? PendingActiveIndex,
        int PrunedThroughIndex = 0,
        long PrunedEventCount = 0,
        string? PrunedAnchorDigest = null,
        IReadOnlyList<int>? PendingDeleteIndices = null);

    private sealed record TestAnchor(
        int Index,
        int EventCount,
        string DataSha256,
        string SealSha256);

    private sealed class TestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose)
        {
            var key = Encoding.UTF8.GetBytes(purpose + "|");
            var result = new byte[key.Length + plaintext.Length];
            key.CopyTo(result, 0);
            plaintext.CopyTo(result.AsSpan(key.Length));
            Array.Reverse(result);
            return result;
        }

        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose)
        {
            var copy = protectedData.ToArray();
            Array.Reverse(copy);
            var prefix = Encoding.UTF8.GetBytes(purpose + "|");
            if (!copy.AsSpan().StartsWith(prefix))
                throw new InvalidOperationException("Purpose mismatch.");
            return copy[prefix.Length..];
        }
    }
}