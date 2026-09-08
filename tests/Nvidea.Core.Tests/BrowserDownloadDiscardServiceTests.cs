using Nvidea.Core.Browser;
using Nvidea.Core.Capabilities;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadDiscardServiceTests
{
    [Fact]
    public async Task DiscardRequiresSingleUseExactScopeApprovalAndReclaimsQuota()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var options = new BrowserDownloadQuarantineOptions(MaxRetainedBytes: 8, MaxSingleDownloadBytes: 8);
            var quarantine = new BrowserDownloadQuarantine(root, options);
            var first = await CaptureTextAsync(quarantine, "first.txt", "12345678");
            var approvals = new ScopedApprovalAuthorizer();
            var audit = new MemoryAuditTrail();
            var service = CreateService(quarantine, approvals, audit);
            var plan = await service.PrepareAsync(first.DownloadId);

            Assert.True(plan.Decision.RequiresApproval);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DiscardAsync(plan, approval: null));

            await Assert.ThrowsAsync<BrowserDownloadQuotaExceededException>(() =>
                CaptureTextAsync(quarantine, "blocked.txt", "x"));

            var grant = approvals.Grant(plan.Decision, TimeSpan.FromMinutes(2));
            var receipt = await service.DiscardAsync(plan, grant);
            Assert.Equal(BrowserDownloadState.Ready, receipt.PreviousState);

            var discarded = await quarantine.GetAsync(first.DownloadId);
            Assert.NotNull(discarded);
            Assert.Equal(BrowserDownloadState.Discarded, discarded!.State);

            var second = await CaptureTextAsync(quarantine, "second.txt", "12345678");
            Assert.Equal(BrowserDownloadState.Ready, second.State);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DiscardAsync(plan, grant));
            Assert.Contains(audit.Events, e => e.EventType == "download.discard.succeeded" && e.Approved);
            Assert.Contains(audit.Events, e => e.EventType == "download.discard.awaiting_approval" && !e.Approved);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task PreparedPlanCannotBeReusedAfterPayloadIdentityChanges()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "artifact.bin", "verified");
            var approvals = new ScopedApprovalAuthorizer();
            var service = CreateService(quarantine, approvals, new MemoryAuditTrail());
            var plan = await service.PrepareAsync(record.DownloadId);
            var grant = approvals.Grant(plan.Decision, TimeSpan.FromMinutes(2));

            var forged = plan with { Sha256 = new string('0', 64) };
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DiscardAsync(forged, grant));

            var receipt = await service.DiscardAsync(plan, grant);
            Assert.Equal(record.DownloadId, receipt.DownloadId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExportedPayloadStillRequiresFreshDiscardApproval()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "copy.txt", "verified");
            await quarantine.ExportAsync(record.DownloadId, destination, userApproved: true);

            var approvals = new ScopedApprovalAuthorizer();
            var service = CreateService(quarantine, approvals, new MemoryAuditTrail());
            var plan = await service.PrepareAsync(record.DownloadId);
            var grant = approvals.Grant(plan.Decision, TimeSpan.FromMinutes(2));
            var receipt = await service.DiscardAsync(plan, grant);

            Assert.Equal(BrowserDownloadState.Exported, receipt.PreviousState);
            Assert.Equal("verified", await File.ReadAllTextAsync(Path.Combine(destination, "copy.txt")));
            Assert.Equal(BrowserDownloadState.Discarded, (await quarantine.GetAsync(record.DownloadId))!.State);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task RestartRecoveryRestoresPayloadWhenDiscardTombstoneWasNotCommitted()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "resume.bin", "verified");
            var payload = GetQuarantinePath(root, record.DownloadId, ".payload");
            var discarding = GetQuarantinePath(root, record.DownloadId, ".discarding");

            File.Move(payload, discarding);
            Assert.False(File.Exists(payload));
            Assert.True(File.Exists(discarding));

            var restarted = new BrowserDownloadQuarantine(root);
            var recovered = await restarted.GetAsync(record.DownloadId);

            Assert.NotNull(recovered);
            Assert.Equal(BrowserDownloadState.Ready, recovered!.State);
            Assert.True(File.Exists(payload));
            Assert.False(File.Exists(discarding));
            Assert.Equal("verified", await File.ReadAllTextAsync(payload));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task RestartRecoveryDeletesLeftoverDiscardingFileAfterDurableTombstone()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "discarded.bin", "verified");
            await quarantine.DiscardAsync(record.DownloadId, userApproved: true);

            var discarding = GetQuarantinePath(root, record.DownloadId, ".discarding");
            await File.WriteAllTextAsync(discarding, "stale-cleanup-only");
            Assert.True(File.Exists(discarding));

            var restarted = new BrowserDownloadQuarantine(root);
            var recovered = await restarted.GetAsync(record.DownloadId);

            Assert.NotNull(recovered);
            Assert.Equal(BrowserDownloadState.Discarded, recovered!.State);
            Assert.False(File.Exists(discarding));
            Assert.False(File.Exists(GetQuarantinePath(root, record.DownloadId, ".payload")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static BrowserDownloadDiscardService CreateService(
        BrowserDownloadQuarantine quarantine,
        ScopedApprovalAuthorizer approvals,
        IAuditTrail audit)
    {
        var registry = new CapabilityRegistry(new[]
        {
            new CapabilityDescriptor(
                BrowserDownloadDiscardService.CapabilityId,
                "1.0.0",
                "Download discard",
                new HashSet<DataPermission> { DataPermission.FilesWrite },
                CapabilityRiskLevel.High,
                RequiresConfirmation: true,
                "Deletes one verified retained browser-download payload after exact human approval.")
        });
        return new BrowserDownloadDiscardService(
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

    private static string GetQuarantinePath(string root, Guid downloadId, string suffix) =>
        Path.Combine(root, "browser-downloads", "quarantine", downloadId.ToString("N") + suffix);

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-download-discard-tests", Guid.NewGuid().ToString("N"));
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
