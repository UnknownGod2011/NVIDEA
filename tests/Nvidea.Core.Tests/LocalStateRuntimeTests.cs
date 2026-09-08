using System.Reflection;
using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class LocalStateRuntimeTests
{
    [Fact]
    public void PublicSurface_ExposesOnlyPassiveTelemetryReads()
    {
        var methods = typeof(LocalStateRuntime)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(static method => method.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(2, methods.Length);
        Assert.Contains(methods, static method =>
            method.Name == nameof(LocalStateRuntime.GetAuditRetentionStatusAsync) &&
            method.ReturnType == typeof(Task<AuditRetentionStatus>));
        Assert.Contains(methods, static method =>
            method.Name == nameof(LocalStateRuntime.GetBrowserDownloadSnapshotAsync) &&
            method.ReturnType == typeof(Task<BrowserDownloadSnapshot>));
        Assert.All(methods, static method =>
        {
            var parameter = Assert.Single(method.GetParameters());
            Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
        });

        var surface = string.Join("|", methods.Select(static candidate =>
            candidate.ReturnType.FullName + ":" + candidate.Name + ":" +
            string.Join(",", candidate.GetParameters().Select(static p => p.ParameterType.FullName))));

        foreach (var forbidden in new[]
                 {
                     "BrowserAction",
                     "BrowserHostRuntime",
                     "ApprovalGrant",
                     "ScopedApprovalAuthorizer",
                     "IAuditTrail",
                     "AppendAsync",
                     "Delete",
                     "Discard",
                     "Export",
                     "Recover",
                     "Repair"
                 })
        {
            Assert.DoesNotContain(forbidden, surface, StringComparison.Ordinal);
        }

        var declaredFields = typeof(LocalStateRuntime)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(static field => field.FieldType.FullName ?? field.FieldType.Name)
            .ToArray();
        Assert.DoesNotContain(declaredFields, static typeName => typeName.Contains("BoundedSegmentedAuditTrail", StringComparison.Ordinal));
        Assert.DoesNotContain(declaredFields, static typeName => typeName.Contains("BrowserHostRuntime", StringComparison.Ordinal));
        Assert.DoesNotContain(declaredFields, static typeName => typeName.Contains("Approval", StringComparison.Ordinal));
        Assert.DoesNotContain(declaredFields, static typeName => typeName.Contains("Quarantine", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StatusRead_UsesExistingProtectedAuditWithoutPayloadExposure()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var retention = new AuditRetentionPolicy(MaxArchivedSegments: 4, MaxArchivedBytes: 4 * 1024 * 1024);
            var trail = new BoundedSegmentedAuditTrail(
                path,
                maxEventsPerSegment: 2,
                protector: protector,
                retention: retention);
            await trail.AppendAsync(CreateEvent("PRIVATE-LOCAL-STATE-PAYLOAD"));

            var runtime = new LocalStateRuntime(path, protector, retention);
            var status = await runtime.GetAuditRetentionStatusAsync();

            Assert.Equal(1, status.ActiveEventCount);
            Assert.False(status.HasPrunedHistory);
            Assert.True(status.ActiveSegmentBytes > 0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LocalStatusAndBrowserAuditForSamePath_ShareProcessSynchronizationGate()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "audit.jsonl");
            var protector = new TestProtector();
            var audit = new BoundedSegmentedAuditTrail(path, protector: protector);
            var local = new LocalStateRuntime(path, protector);

            var auditGateField = typeof(BoundedSegmentedAuditTrail)
                .GetField("_gate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Bounded audit gate field was not found.");
            var localGateField = typeof(LocalStateRuntime)
                .GetField("_auditGate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Local-state audit gate field was not found.");

            Assert.Same(auditGateField.GetValue(audit), localGateField.GetValue(local));
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
