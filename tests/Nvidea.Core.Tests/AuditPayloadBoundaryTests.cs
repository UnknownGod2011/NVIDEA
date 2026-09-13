using Nvidea.Core.Capabilities;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class AuditPayloadBoundaryTests
{
    [Theory]
    [InlineData("tool.execution_started\nforged")]
    [InlineData("tool execution started")]
    [InlineData("tool|approved")]
    public async Task ProductionAudit_RejectsMalformedEventTypeBeforePersistence(string eventType)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = CreateTrail(path);

            await Assert.ThrowsAsync<ArgumentException>(() => trail.AppendAsync(CreateEvent(eventType: eventType)));

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
            Assert.False(File.Exists(path + ".segments"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ProductionAudit_RejectsControlBearingSummaryBeforePersistence()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = CreateTrail(path);

            await Assert.ThrowsAsync<ArgumentException>(() => trail.AppendAsync(
                CreateEvent(summary: "Succeeded.\r\nAPPROVED bearer-secret")));

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ProductionAudit_RejectsOversizedApprovalScopeBeforePersistence()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = CreateTrail(path);
            var scope = new string('x', AuditPayloadTrust.MaxApprovalScopeLength + 1);

            await Assert.ThrowsAsync<ArgumentException>(() => trail.AppendAsync(CreateEvent(approvalScope: scope)));

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("api_token")]
    [InlineData("private_key_hint")]
    [InlineData("sessionCookie")]
    public async Task ProductionAudit_RejectsSecretDesignatingMetadataKeys(string key)
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = CreateTrail(path);
            var auditEvent = CreateEvent(metadata: new Dictionary<string, string>
            {
                [key] = "Bearer should-not-be-durable"
            });

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
    public async Task ProductionAudit_RejectsMetadataStorageAmplificationBeforePersistence()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = CreateTrail(path);
            var metadata = Enumerable.Range(0, AuditPayloadTrust.MaxMetadataEntries + 1)
                .ToDictionary(i => $"field_{i}", _ => "value");

            await Assert.ThrowsAsync<ArgumentException>(() => trail.AppendAsync(CreateEvent(metadata: metadata)));

            Assert.False(File.Exists(path));
            Assert.False(File.Exists(path + ".seal"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ProductionAudit_PreservesExistingLegacyShapedForensicPayload()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var trail = CreateTrail(path);
            var auditEvent = CreateEvent(
                eventType: "execution",
                approvalScope: "exact-action",
                summary: "Existing forensic summary with punctuation: ok.",
                metadata: new Dictionary<string, string> { ["host"] = "example.test" });

            await trail.AppendAsync(auditEvent);
            var restored = Assert.Single(await trail.ReadAllAsync());

            Assert.Equal(auditEvent.EventType, restored.EventType);
            Assert.Equal(auditEvent.ApprovalScope, restored.ApprovalScope);
            Assert.Equal(auditEvent.Summary, restored.Summary);
            Assert.Equal("example.test", restored.Metadata!["host"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static BoundedSegmentedAuditTrail CreateTrail(string path) =>
        new(
            path,
            maxEventsPerSegment: 2,
            protector: new PassthroughProtector(),
            payloadPolicy: new AuditPayloadPolicy(
                MaxEventPayloadBytes: 16 * 1024,
                MaxActiveSegmentPayloadBytes: 32 * 1024));

    private static AuditEvent CreateEvent(
        string eventType = "tool.execution_started",
        string approvalScope = "browser.agent|0123456789abcdef0123456789abcdef|BrowserWrite",
        string summary = "Tool execution started.",
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            Guid.NewGuid(),
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
                ["permissions"] = "BrowserWrite",
                ["untrustedSourcePresent"] = "False"
            });

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-audit-payload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class PassthroughProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
