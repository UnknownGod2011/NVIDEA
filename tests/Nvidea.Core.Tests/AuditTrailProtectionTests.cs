using System.Text;
using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class AuditTrailProtectionTests
{
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

            await Assert.ThrowsAsync<InvalidOperationException>(() => trail.AppendAsync(auditEvent));
            Assert.Equal(before, await File.ReadAllTextAsync(path));
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
