using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadHandoffServiceTests
{
    [Fact]
    public async Task ExportRequiresRegisteredSingleUseExactScopeApproval()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "report.txt", "verified");
            var approvals = new ScopedApprovalAuthorizer();
            var audit = new MemoryAuditTrail();
            var service = CreateService(quarantine, approvals, audit);
            var plan = await service.PrepareAsync(record.DownloadId, destination);

            Assert.True(plan.Decision.Allowed);
            Assert.True(plan.Decision.RequiresApproval);
            Assert.Contains(record.DownloadId.ToString("N"), plan.ActionId, StringComparison.Ordinal);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ExportAsync(plan, approval: null));
            Assert.False(File.Exists(Path.Combine(destination, "report.txt")));

            var grant = approvals.Grant(plan.Decision, TimeSpan.FromMinutes(2));
            var receipt = await service.ExportAsync(plan, grant);
            Assert.Equal("verified", await File.ReadAllTextAsync(receipt.DestinationPath));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ExportAsync(plan, grant));
            Assert.Contains(audit.Events, e => e.EventType == "download.handoff.succeeded" && e.Approved);
            Assert.Contains(audit.Events, e => e.EventType == "download.handoff.awaiting_approval" && !e.Approved);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task ApprovalForOneDestinationCannotAuthorizeAnotherDestination()
    {
        var root = CreateTemporaryDirectory();
        var firstDestination = CreateTemporaryDirectory();
        var secondDestination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "artifact.bin", "bytes");
            var approvals = new ScopedApprovalAuthorizer();
            var service = CreateService(quarantine, approvals, new MemoryAuditTrail());
            var firstPlan = await service.PrepareAsync(record.DownloadId, firstDestination);
            var secondPlan = await service.PrepareAsync(record.DownloadId, secondDestination);
            var grant = approvals.Grant(firstPlan.Decision, TimeSpan.FromMinutes(2));

            Assert.NotEqual(firstPlan.Decision.ApprovalScope, secondPlan.Decision.ApprovalScope);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ExportAsync(secondPlan, grant));
            Assert.Empty(Directory.GetFiles(secondDestination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(firstDestination, recursive: true);
            Directory.Delete(secondDestination, recursive: true);
        }
    }

    [Fact]
    public async Task PreparedPlanCannotBeMutatedAfterUserConfirmation()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "safe.txt", "safe");
            var approvals = new ScopedApprovalAuthorizer();
            var service = CreateService(quarantine, approvals, new MemoryAuditTrail());
            var plan = await service.PrepareAsync(record.DownloadId, destination);
            var grant = approvals.Grant(plan.Decision, TimeSpan.FromMinutes(2));
            var forged = plan with { ActionId = plan.ActionId + "-changed" };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ExportAsync(forged, grant));
            Assert.Empty(Directory.GetFiles(destination));

            // Scope mismatch is rejected before authorization consumption, so the original exact
            // confirmation remains usable once for the unchanged plan.
            var receipt = await service.ExportAsync(plan, grant);
            Assert.True(File.Exists(receipt.DestinationPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task FailedExportConsumesApprovalAndRequiresFreshConfirmation()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "existing.txt", "new");
            var existing = Path.Combine(destination, "existing.txt");
            await File.WriteAllTextAsync(existing, "old");
            var approvals = new ScopedApprovalAuthorizer();
            var service = CreateService(quarantine, approvals, new MemoryAuditTrail());
            var plan = await service.PrepareAsync(record.DownloadId, destination);
            var grant = approvals.Grant(plan.Decision, TimeSpan.FromMinutes(2));

            await Assert.ThrowsAsync<IOException>(() => service.ExportAsync(plan, grant));
            Assert.Equal("old", await File.ReadAllTextAsync(existing));

            File.Delete(existing);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ExportAsync(plan, grant));
            Assert.False(File.Exists(existing));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    private static BrowserDownloadHandoffService CreateService(
        BrowserDownloadQuarantine quarantine,
        ScopedApprovalAuthorizer approvals,
        IAuditTrail audit)
    {
        var registry = new CapabilityRegistry(new[]
        {
            new CapabilityDescriptor(
                BrowserDownloadHandoffService.CapabilityId,
                "1.0.0",
                "Download handoff",
                new HashSet<DataPermission> { DataPermission.FilesWrite },
                CapabilityRiskLevel.High,
                RequiresConfirmation: true,
                "Exports a verified quarantined browser download to a user-selected folder.")
        });
        return new BrowserDownloadHandoffService(
            quarantine,
            new CapabilityPermissionPolicy(registry),
            approvals,
            audit);
    }

    private static Task<BrowserDownloadRecord> CaptureTextAsync(
        BrowserDownloadQuarantine quarantine,
        string fileName,
        string content) =>
        quarantine.CaptureAsync(
            new Uri("https://example.com/download"),
            fileName,
            (path, cancellationToken) => File.WriteAllTextAsync(path, content, cancellationToken));

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-download-handoff-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(Events.ToArray());
    }
}
