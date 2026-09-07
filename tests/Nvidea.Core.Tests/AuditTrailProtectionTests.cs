using System.Text;
using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class AuditTrailProtectionTests
{
    private const string TailSealPurpose = "audit-tail-seal-v1";

    [Fact]
    public async Task ProtectedAudit_RoundTripsWithoutPersistingSummary()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new JsonLinesAuditTrail(path, new TestProtector());
            var auditEvent = CreateEvent("sensitive-summary");

            await trail.AppendAsync(auditEvent);

            var persisted = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("sensitive-summary", persisted, StringComparison.Ordinal);
            Assert.Contains("\"protected\":true", persisted, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(path + ".seal"));

            var restored = Assert.Single(await trail.ReadAllAsync());
            Assert.Equal(auditEvent.EventId, restored.EventId);
            Assert.Equal(auditEvent.CapabilityId, restored.CapabilityId);
            Assert.Equal(auditEvent.ActionId, restored.ActionId);
            Assert.Equal(auditEvent.Summary, restored.Summary);
            Assert.NotNull(restored.Metadata);
            Assert.Equal("example.test", restored.Metadata!["host"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task HashChain_RejectsPayloadTampering()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new JsonLinesAuditTrail(path, new TestProtector());
            await trail.AppendAsync(CreateEvent("first"));
            await trail.AppendAsync(CreateEvent("second"));

            var lines = await File.ReadAllLinesAsync(path);
            using var doc = JsonDocument.Parse(lines[0]);
            var root = doc.RootElement;
            var tampered = new Dictionary<string, object?>
            {
                ["version"] = root.GetProperty("version").GetInt32(),
                ["sequence"] = root.GetProperty("sequence").GetInt64(),
                ["previousHash"] = root.GetProperty("previousHash").GetString(),
                ["protected"] = root.GetProperty("protected").GetBoolean(),
                ["payload"] = root.GetProperty("payload").GetString() + "A",
                ["hash"] = root.GetProperty("hash").GetString()
            };
            lines[0] = JsonSerializer.Serialize(tampered, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await File.WriteAllLinesAsync(path, lines);

            await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task HashChain_RejectsRecordDeletion()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new JsonLinesAuditTrail(path, new TestProtector());
            await trail.AppendAsync(CreateEvent("first"));
            await trail.AppendAsync(CreateEvent("second"));
            await trail.AppendAsync(CreateEvent("third"));

            var lines = await File.ReadAllLinesAsync(path);
            await File.WriteAllLinesAsync(path, new[] { lines[0], lines[2] });

            await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TailSeal_RejectsFinalRecordDeletion()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new JsonLinesAuditTrail(path, new TestProtector());
            await trail.AppendAsync(CreateEvent("first"));
            await trail.AppendAsync(CreateEvent("second"));

            var lines = await File.ReadAllLinesAsync(path);
            await File.WriteAllLinesAsync(path, new[] { lines[0] });

            var error = await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());
            Assert.Contains("tail seal", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TailSeal_PendingStateRecoversWhenAppendReachedDisk()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var trail = new JsonLinesAuditTrail(path, protector);
            await trail.AppendAsync(CreateEvent("first"));
            await trail.AppendAsync(CreateEvent("second"));

            var hashes = await ReadLineHashesAsync(path);
            await WriteTestSealAsync(path, protector, new TestSeal(1, "pending", 1, hashes[0], 2, hashes[1]));

            Assert.Equal(2, (await trail.ReadAllAsync()).Count);
            var recovered = await ReadTestSealAsync(path, protector);
            Assert.Equal("committed", recovered.State);
            Assert.Equal(2, recovered.CommittedSequence);
            Assert.Equal(hashes[1], recovered.CommittedHash);
            Assert.Null(recovered.PendingSequence);
            Assert.Null(recovered.PendingHash);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TailSeal_PendingStateRecoversWhenAppendDidNotReachDisk()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var trail = new JsonLinesAuditTrail(path, protector);
            await trail.AppendAsync(CreateEvent("first"));
            await trail.AppendAsync(CreateEvent("second"));

            var lines = await File.ReadAllLinesAsync(path);
            var hashes = await ReadLineHashesAsync(path);
            await WriteTestSealAsync(path, protector, new TestSeal(1, "pending", 1, hashes[0], 2, hashes[1]));
            await File.WriteAllLinesAsync(path, new[] { lines[0] });

            Assert.Single(await trail.ReadAllAsync());
            var recovered = await ReadTestSealAsync(path, protector);
            Assert.Equal("committed", recovered.State);
            Assert.Equal(1, recovered.CommittedSequence);
            Assert.Equal(hashes[0], recovered.CommittedHash);
            Assert.Null(recovered.PendingSequence);
            Assert.Null(recovered.PendingHash);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LegacyPlaintextJsonl_MigratesAndCanAppendToExactWrittenChain()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var legacyEvent = CreateEvent("legacy-private-summary");
            var json = JsonSerializer.Serialize(legacyEvent, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await File.WriteAllTextAsync(path, json + Environment.NewLine);

            var trail = new JsonLinesAuditTrail(path, new TestProtector());
            Assert.Single(await trail.ReadAllAsync());
            await trail.AppendAsync(CreateEvent("after-migration"));

            var restored = await trail.ReadAllAsync();
            Assert.Equal(2, restored.Count);
            var persisted = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("legacy-private-summary", persisted, StringComparison.Ordinal);
            Assert.DoesNotContain("after-migration", persisted, StringComparison.Ordinal);
            Assert.Contains("\"sequence\":2", persisted, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidLegacyJson_IsNotRewrittenOrLegitimized()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            const string invalid = "{ definitely-not-valid-json";
            await File.WriteAllTextAsync(path, invalid);

            var trail = new JsonLinesAuditTrail(path, new TestProtector());
            await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());
            Assert.Equal(invalid, await File.ReadAllTextAsync(path));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DuplicateEventId_IsRejectedWithoutAppending()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new JsonLinesAuditTrail(path, new TestProtector());
            var auditEvent = CreateEvent("once");
            await trail.AppendAsync(auditEvent);
            var before = await File.ReadAllTextAsync(path);
            var sealBefore = await File.ReadAllBytesAsync(path + ".seal");

            await Assert.ThrowsAsync<InvalidOperationException>(() => trail.AppendAsync(auditEvent));
            Assert.Equal(before, await File.ReadAllTextAsync(path));
            Assert.Equal(sealBefore, await File.ReadAllBytesAsync(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<string[]> ReadLineHashesAsync(string path)
    {
        var lines = await File.ReadAllLinesAsync(path);
        return lines.Select(line =>
        {
            using var doc = JsonDocument.Parse(line);
            return doc.RootElement.GetProperty("hash").GetString()!;
        }).ToArray();
    }

    private static async Task WriteTestSealAsync(string path, ILocalStateProtector protector, TestSeal seal)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(seal, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var encoded = LocalStateEnvelope.Encode(plaintext, protector, TailSealPurpose);
        await File.WriteAllBytesAsync(path + ".seal", encoded);
    }

    private static async Task<TestSeal> ReadTestSealAsync(string path, ILocalStateProtector protector)
    {
        var persisted = await File.ReadAllBytesAsync(path + ".seal");
        var decoded = LocalStateEnvelope.Decode(persisted, protector, TailSealPurpose);
        Assert.True(decoded.WasProtected);
        return JsonSerializer.Deserialize<TestSeal>(decoded.Plaintext, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
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

    private sealed record TestSeal(
        int Version,
        string State,
        long CommittedSequence,
        string CommittedHash,
        long? PendingSequence,
        string? PendingHash);

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
