using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class JsonLinesAuditPayloadTrustTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task AppendAsync_RejectsMalformedPayloadBeforeAnyAuditFileSideEffect()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new JsonLinesAuditTrail(path, new PassthroughProtector());

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                trail.AppendAsync(CreateEvent(summary: "Succeeded.\r\nAPPROVED bearer-secret")));

            Assert.Contains("single-line", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LegacyMigration_RejectsUntrustedPayloadBeforeRewritingOrSealing()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var legacy = CreateEvent(metadata: new Dictionary<string, string>
            {
                ["authorization"] = "Bearer must-not-be-legitimized-by-migration"
            });
            var original = JsonSerializer.Serialize(legacy, JsonOptions) + Environment.NewLine;
            await File.WriteAllTextAsync(path, original, Encoding.UTF8);

            var trail = new JsonLinesAuditTrail(path, new PassthroughProtector());
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());

            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Equal(original, await File.ReadAllTextAsync(path, Encoding.UTF8));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task CurrentFormatReload_RejectsHashValidButPayloadInvalidRecord()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new PassthroughProtector();
            var trail = new JsonLinesAuditTrail(path, protector);
            await trail.AppendAsync(CreateEvent());

            using var lineDocument = JsonDocument.Parse((await File.ReadAllLinesAsync(path)).Single());
            var root = lineDocument.RootElement;
            var poisoned = CreateEvent(
                eventId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                summary: "trusted-looking\nforged-authority");
            var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(poisoned, JsonOptions));
            var version = root.GetProperty("version").GetInt32();
            var sequence = root.GetProperty("sequence").GetInt64();
            var previousHash = root.GetProperty("previousHash").GetString() ?? string.Empty;
            var isProtected = root.GetProperty("protected").GetBoolean();
            var hash = ComputeHash(version, sequence, previousHash, isProtected, payload);
            var rewritten = JsonSerializer.Serialize(new
            {
                version,
                sequence,
                previousHash,
                @protected = isProtected,
                payload,
                hash
            }, JsonOptions);
            await File.WriteAllTextAsync(path, rewritten + Environment.NewLine, Encoding.UTF8);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());

            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Contains("payload trust contract", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LegacyMigration_PreservesCompatibleHistoricalPayloadShape()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var legacy = CreateEvent(
                eventType: "execution",
                approvalScope: "exact-action",
                summary: "Historical forensic summary: ok.",
                metadata: new Dictionary<string, string> { ["host"] = "example.test" });
            await File.WriteAllTextAsync(
                path,
                JsonSerializer.Serialize(legacy, JsonOptions) + Environment.NewLine,
                Encoding.UTF8);

            var trail = new JsonLinesAuditTrail(path, new PassthroughProtector());
            var restored = Assert.Single(await trail.ReadAllAsync());

            Assert.Equal("execution", restored.EventType);
            Assert.Equal("exact-action", restored.ApprovalScope);
            Assert.Equal("Historical forensic summary: ok.", restored.Summary);
            Assert.Equal("example.test", restored.Metadata!["host"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AuditEvent CreateEvent(
        Guid? eventId = null,
        string eventType = "tool.execution_started",
        string approvalScope = "browser.agent|0123456789abcdef0123456789abcdef|BrowserWrite",
        string summary = "Tool execution started.",
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            eventId ?? Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "browser.agent",
            "0123456789abcdef0123456789abcdef",
            eventType,
            CapabilityRiskLevel.High,
            Allowed: true,
            Approved: true,
            ApprovalScope: approvalScope,
            Summary: summary,
            Metadata: metadata ?? new Dictionary<string, string>
            {
                ["tool"] = "browser.execute",
                ["permissions"] = "BrowserWrite"
            });

    private static string ComputeHash(int version, long sequence, string previousHash, bool isProtected, string payload)
    {
        var canonical = $"{version}\n{sequence}\n{previousHash}\n{(isProtected ? 1 : 0)}\n{payload}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-jsonl-audit-payload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class PassthroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
