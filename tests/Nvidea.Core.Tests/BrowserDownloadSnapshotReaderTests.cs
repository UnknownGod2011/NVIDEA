using System.Text.Json;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadSnapshotReaderTests
{
    [Fact]
    public async Task Snapshot_ReportsSanitizedStableMetadataAndQuotaWithoutSensitivePaths()
    {
        var directory = CreateTempDirectory();
        try
        {
            var protector = new TestProtector();
            var options = new BrowserDownloadQuarantineOptions(1024 * 1024, 512 * 1024);
            var quarantine = new BrowserDownloadQuarantine(directory, options, protector);
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var created = await quarantine.CaptureAsync(
                new Uri("https://example.test/private/path?token=secret"),
                "report.txt",
                async (path, ct) => await File.WriteAllBytesAsync(path, bytes, ct));

            var runtime = new LocalStateRuntime(
                directory,
                protector,
                AuditRetentionPolicy.Default,
                options);
            var snapshot = await runtime.GetBrowserDownloadSnapshotAsync();
            var item = Assert.Single(snapshot.RetainedDownloads);

            Assert.Equal(created.DownloadId, item.DownloadId);
            Assert.Equal("example.test", item.SourceHost);
            Assert.Equal("report.txt", item.SuggestedFileName);
            Assert.Equal(BrowserDownloadState.Ready, item.State);
            Assert.Equal(bytes.LongLength, item.LengthBytes);
            Assert.Equal(created.Sha256, item.Sha256, ignoreCase: true);
            Assert.Equal(bytes.LongLength, snapshot.RetainedBytes);
            Assert.Equal(options.MaxRetainedBytes, snapshot.MaxRetainedBytes);
            Assert.Equal(options.MaxSingleDownloadBytes, snapshot.MaxSingleDownloadBytes);
            Assert.False(snapshot.HasPendingRecovery);

            var serialized = JsonSerializer.Serialize(snapshot);
            Assert.DoesNotContain("/private/path", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("token=secret", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("ExportedPath", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("Failure", serialized, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Snapshot_FailsClosedWhenRetainedPayloadLengthDiffersFromProtectedMetadata()
    {
        var directory = CreateTempDirectory();
        try
        {
            var protector = new TestProtector();
            var quarantine = new BrowserDownloadQuarantine(directory, protector);
            var created = await quarantine.CaptureAsync(
                new Uri("https://example.test/download"),
                "artifact.bin",
                async (path, ct) => await File.WriteAllBytesAsync(path, new byte[] { 9, 8, 7, 6 }, ct));

            var payloadPath = Path.Combine(
                directory,
                "browser-downloads",
                "quarantine",
                created.DownloadId.ToString("N") + ".payload");
            await using (var stream = new FileStream(payloadPath, FileMode.Append, FileAccess.Write, FileShare.None))
                await stream.WriteAsync(new byte[] { 5 });

            var runtime = new LocalStateRuntime(
                directory,
                protector,
                AuditRetentionPolicy.Default,
                new BrowserDownloadQuarantineOptions());

            var error = await Assert.ThrowsAsync<InvalidDataException>(
                () => runtime.GetBrowserDownloadSnapshotAsync());
            Assert.Contains("length", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Snapshot_DoesNotPerformReceivingRecoveryOrDeletePartialBytes()
    {
        var directory = CreateTempDirectory();
        try
        {
            var protector = new TestProtector();
            var downloadRoot = Path.Combine(directory, "browser-downloads");
            var quarantineRoot = Path.Combine(downloadRoot, "quarantine");
            Directory.CreateDirectory(quarantineRoot);

            var id = Guid.NewGuid();
            var record = new BrowserDownloadRecord(
                id,
                new Uri("https://example.test/slow"),
                "slow.bin",
                BrowserDownloadState.Receiving,
                null,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
            var json = JsonSerializer.SerializeToUtf8Bytes(new[] { record }, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
            var persisted = LocalStateEnvelope.Encode(json, protector, "browser-download-metadata-v1");
            await File.WriteAllBytesAsync(Path.Combine(downloadRoot, "downloads.json"), persisted);

            var partialPath = Path.Combine(quarantineRoot, id.ToString("N") + ".partial");
            await File.WriteAllBytesAsync(partialPath, new byte[] { 1, 2, 3 });

            var runtime = new LocalStateRuntime(
                directory,
                protector,
                AuditRetentionPolicy.Default,
                new BrowserDownloadQuarantineOptions());
            var snapshot = await runtime.GetBrowserDownloadSnapshotAsync();

            Assert.Empty(snapshot.RetainedDownloads);
            Assert.Equal(1, snapshot.PendingRecoveryCount);
            Assert.True(File.Exists(partialPath));

            var metadataBytes = await File.ReadAllBytesAsync(Path.Combine(downloadRoot, "downloads.json"));
            var decoded = LocalStateEnvelope.Decode(metadataBytes, protector, "browser-download-metadata-v1");
            var after = JsonSerializer.Deserialize<List<BrowserDownloadRecord>>(decoded.Plaintext, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(after);
            Assert.Equal(BrowserDownloadState.Receiving, Assert.Single(after!).State);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

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
