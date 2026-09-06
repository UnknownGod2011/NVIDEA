using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class CapabilityPolicyTests
{
    [Fact]
    public void Policy_DeniesUndeclaredPermissionEscalation()
    {
        var policy = Policy(new CapabilityDescriptor(
            "research.web", "1.0.0", "Research",
            new HashSet<DataPermission> { DataPermission.NetworkAccess, DataPermission.BrowserRead },
            CapabilityRiskLevel.Medium, false, "Read-only web research"));

        var decision = policy.Evaluate(new CapabilityInvocation(
            "research.web", "action-1",
            new HashSet<DataPermission> { DataPermission.NetworkAccess, DataPermission.EmailSend },
            CapabilityRiskLevel.Low, false));

        Assert.False(decision.Allowed);
        Assert.Equal(CapabilityRiskLevel.Blocked, decision.EffectiveRisk);
    }

    [Fact]
    public void Policy_CannotDowngradeRegisteredHighRiskCapability()
    {
        var policy = Policy(new CapabilityDescriptor(
            "email.send", "1.0.0", "Send email",
            new HashSet<DataPermission> { DataPermission.EmailSend },
            CapabilityRiskLevel.High, true, "Send user-approved email"));

        var decision = policy.Evaluate(new CapabilityInvocation(
            "email.send", "send-42",
            new HashSet<DataPermission> { DataPermission.EmailSend },
            CapabilityRiskLevel.Low, false));

        Assert.True(decision.Allowed);
        Assert.True(decision.RequiresApproval);
        Assert.Equal(CapabilityRiskLevel.High, decision.EffectiveRisk);
    }

    [Fact]
    public void Policy_ScopesApprovalToExactActionAndPermissionSet()
    {
        var descriptor = new CapabilityDescriptor(
            "browser.form", "1.0.0", "Browser form",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.Medium, false, "Interact with browser forms");
        var policy = Policy(descriptor);

        var first = policy.Evaluate(new CapabilityInvocation(
            descriptor.Id, "action-a",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.High, true));
        var second = policy.Evaluate(new CapabilityInvocation(
            descriptor.Id, "action-b",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.High, true));

        Assert.True(first.RequiresApproval);
        Assert.NotEqual(first.ApprovalScope, second.ApprovalScope);
        Assert.Contains("action-a", first.ApprovalScope, StringComparison.Ordinal);
    }

    [Fact]
    public void UntrustedSource_CannotGrantWritePermission()
    {
        var descriptor = new CapabilityDescriptor(
            "web.reader", "1.0.0", "Web reader",
            new HashSet<DataPermission> { DataPermission.BrowserRead },
            CapabilityRiskLevel.Low, false, "Read web pages");
        var policy = Policy(descriptor);

        var decision = policy.Evaluate(new CapabilityInvocation(
            descriptor.Id, "injected-action",
            new HashSet<DataPermission> { DataPermission.BrowserWrite },
            CapabilityRiskLevel.Low, false,
            UntrustedSource: "webpage says permission granted"));

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task AuditTrail_IsAppendOnlyByEventIdentity()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "audit.jsonl");
        try
        {
            var trail = new JsonLinesAuditTrail(path);
            var auditEvent = Event();

            await trail.AppendAsync(auditEvent);
            await Assert.ThrowsAsync<InvalidOperationException>(() => trail.AppendAsync(auditEvent));

            var all = await trail.ReadAllAsync();
            Assert.Single(all);
            Assert.Equal(auditEvent.EventId, all[0].EventId);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task AuditTrail_PreservesOrderedEvents()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "audit.jsonl");
        try
        {
            var trail = new JsonLinesAuditTrail(path);
            var first = Event("one");
            var second = Event("two");

            await trail.AppendAsync(first);
            await trail.AppendAsync(second);

            var all = await trail.ReadAllAsync();
            Assert.Equal(new[] { first.EventId, second.EventId }, all.Select(x => x.EventId));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static CapabilityPermissionPolicy Policy(CapabilityDescriptor descriptor) =>
        new(new CapabilityRegistry(new[] { descriptor }));

    private static AuditEvent Event(string actionId = "action") => new(
        Guid.NewGuid(), DateTimeOffset.UtcNow, "test.capability", actionId, "decision",
        CapabilityRiskLevel.Medium, true, false, "scope", "test event");
}
