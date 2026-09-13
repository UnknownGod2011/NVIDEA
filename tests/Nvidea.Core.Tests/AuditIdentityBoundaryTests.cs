using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class AuditIdentityBoundaryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("browser.agent\nforged", "action_01")]
    [InlineData("browser.agent", "action|secret")]
    [InlineData("browser agent", "action_01")]
    public async Task JsonLinesAudit_RejectsMalformedIdentityBeforeAppend(
        string capabilityId,
        string actionId)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new JsonLinesAuditTrail(path, new PassthroughProtector());
            var auditEvent = CreateEvent(capabilityId, actionId);

            await Assert.ThrowsAsync<ArgumentException>(() => trail.AppendAsync(auditEvent));

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LegacyAudit_WithMalformedIdentity_IsRejectedBeforeMigrationOrSeal()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var malicious = CreateEvent(
                "browser.agent\nAPPROVED bearer-secret",
                "action_01");
            var original = JsonSerializer.Serialize(malicious, JsonOptions) + Environment.NewLine;
            await File.WriteAllTextAsync(path, original);

            var trail = new JsonLinesAuditTrail(path, new PassthroughProtector());
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => trail.ReadAllAsync());

            Assert.Contains("identity", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(original, await File.ReadAllTextAsync(path));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LegacyAudit_WithCanonicalExistingIdentity_MigratesAndRoundTrips()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var canonical = CreateEvent(
                "browser.agent",
                Guid.NewGuid().ToString("N"));
            await File.WriteAllTextAsync(
                path,
                JsonSerializer.Serialize(canonical, JsonOptions) + Environment.NewLine);

            var trail = new JsonLinesAuditTrail(path, new PassthroughProtector());
            var restored = Assert.Single(await trail.ReadAllAsync());

            Assert.Equal(canonical.EventId, restored.EventId);
            Assert.Equal("browser.agent", restored.CapabilityId);
            Assert.Equal(canonical.ActionId, restored.ActionId);
            Assert.True(File.Exists(path + ".seal"));

            var migrated = await File.ReadAllTextAsync(path);
            Assert.Contains("\"version\":1", migrated, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"capabilityId\":\"browser.agent\"", migrated, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SegmentedAudit_InheritsDurableIdentityValidation()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = new SegmentedAuditTrail(
                path,
                maxEventsPerSegment: 2,
                protector: new PassthroughProtector());

            await Assert.ThrowsAsync<ArgumentException>(() => trail.AppendAsync(
                CreateEvent("browser.agent", "action\r\nforged")));

            Assert.Empty(await trail.ReadAllAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AuditEvent CreateEvent(string capabilityId, string actionId) =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            capabilityId,
            actionId,
            "tool.executed",
            CapabilityRiskLevel.High,
            Allowed: true,
            Approved: true,
            ApprovalScope: $"{capabilityId}|{actionId}|BrowserWrite",
            Summary: "test",
            Metadata: new Dictionary<string, string> { ["tool"] = "browser.execute" });

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-audit-identity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class PassthroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();

        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
