using System.Text;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadQuotaTests
{
    [Fact]
    public async Task OversizedSingleDownloadFailsClosedAndLeavesNoPayload()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(
                root,
                new BrowserDownloadQuarantineOptions(MaxRetainedBytes: 16, MaxSingleDownloadBytes: 4));

            await Assert.ThrowsAsync<BrowserDownloadQuotaExceededException>(() =>
                CaptureAsync(quarantine, "oversized.bin", "12345"));

            var record = Assert.Single(await quarantine.ListAsync());
            Assert.Equal(BrowserDownloadState.Interrupted, record.State);
            Assert.Contains("quota", record.Failure!, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.GetFiles(Path.Combine(root, "browser-downloads", "quarantine"), "*.payload"));
            Assert.Empty(Directory.GetFiles(Path.Combine(root, "browser-downloads", "quarantine"), "*.partial"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TotalQuotaNeverEvictsExistingReadyArtifact()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(
                root,
                new BrowserDownloadQuarantineOptions(MaxRetainedBytes: 8, MaxSingleDownloadBytes: 8));

            var first = await CaptureAsync(quarantine, "first.bin", "123456");
            var firstPayload = PayloadPath(root, first.DownloadId);
            Assert.True(File.Exists(firstPayload));

            await Assert.ThrowsAsync<BrowserDownloadQuotaExceededException>(() =>
                CaptureAsync(quarantine, "second.bin", "abc"));

            Assert.True(File.Exists(firstPayload));
            Assert.Equal("123456", await File.ReadAllTextAsync(firstPayload));

            var records = await quarantine.ListAsync();
            Assert.Equal(BrowserDownloadState.Ready, records.Single(x => x.DownloadId == first.DownloadId).State);
            Assert.Contains(records, x => x.DownloadId != first.DownloadId && x.State == BrowserDownloadState.Interrupted);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FullQuotaRejectsBeforeStartingAnotherBrowserSave()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(
                root,
                new BrowserDownloadQuarantineOptions(MaxRetainedBytes: 4, MaxSingleDownloadBytes: 4));
            await CaptureAsync(quarantine, "full.bin", "1234");

            var saveInvoked = false;
            await Assert.ThrowsAsync<BrowserDownloadQuotaExceededException>(() =>
                quarantine.CaptureAsync(
                    new Uri("https://example.com/blocked"),
                    "blocked.bin",
                    (path, cancellationToken) =>
                    {
                        saveInvoked = true;
                        return File.WriteAllBytesAsync(path, [1], cancellationToken);
                    }));

            Assert.False(saveInvoked);
            Assert.Single(await quarantine.ListAsync());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void InvalidQuotaConfigurationIsRejected()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrowserDownloadQuarantine(root, new BrowserDownloadQuarantineOptions(0, 0)));
            Assert.Throws<ArgumentException>(() =>
                new BrowserDownloadQuarantine(root, new BrowserDownloadQuarantineOptions(4, 5)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Task<BrowserDownloadRecord> CaptureAsync(
        BrowserDownloadQuarantine quarantine,
        string fileName,
        string content) =>
        quarantine.CaptureAsync(
            new Uri("https://example.com/download"),
            fileName,
            (path, cancellationToken) => File.WriteAllTextAsync(path, content, cancellationToken));

    private static string PayloadPath(string root, Guid id) =>
        Path.Combine(root, "browser-downloads", "quarantine", id.ToString("N") + ".payload");

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-download-quota-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
