using System.Security.Cryptography;
using System.Text;
using Nvidea.Core.Browser;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadQuarantineTests
{
    [Fact]
    public async Task CapturePersistsVerifiedPayloadAndMetadata()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var bytes = Encoding.UTF8.GetBytes("verified download payload");

            var record = await quarantine.CaptureAsync(
                new Uri("https://example.com/report"),
                "report.txt",
                async (path, cancellationToken) =>
                {
                    await File.WriteAllBytesAsync(path, bytes, cancellationToken);
                });

            Assert.Equal(BrowserDownloadState.Ready, record.State);
            Assert.Equal("report.txt", record.SuggestedFileName);
            Assert.Equal(bytes.LongLength, record.LengthBytes);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), record.Sha256);
            Assert.Null(record.ExportedPath);

            var payload = Path.Combine(root, "browser-downloads", "quarantine", record.DownloadId.ToString("N") + ".payload");
            Assert.True(File.Exists(payload));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(payload));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExportRequiresExplicitUserApproval()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "safe.txt", "hello");

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                quarantine.ExportAsync(record.DownloadId, destination, userApproved: false));

            Assert.Empty(Directory.GetFiles(destination));
            Assert.Equal(BrowserDownloadState.Ready, (await quarantine.GetAsync(record.DownloadId))!.State);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task ExportVerifiesPayloadBeforeAtomicHandoff()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "evidence.txt", "original");
            var payload = Path.Combine(root, "browser-downloads", "quarantine", record.DownloadId.ToString("N") + ".payload");
            await File.WriteAllTextAsync(payload, "tampered");

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                quarantine.ExportAsync(record.DownloadId, destination, userApproved: true));

            Assert.Empty(Directory.GetFiles(destination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task SuccessfulExportPreservesQuarantineAndRecordsHandoff()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "result.txt", "result-data");

            var receipt = await quarantine.ExportAsync(record.DownloadId, destination, userApproved: true);

            Assert.Equal(Path.Combine(destination, "result.txt"), receipt.DestinationPath);
            Assert.Equal("result-data", await File.ReadAllTextAsync(receipt.DestinationPath));
            var persisted = await quarantine.GetAsync(record.DownloadId);
            Assert.NotNull(persisted);
            Assert.Equal(BrowserDownloadState.Exported, persisted.State);
            Assert.Equal(receipt.DestinationPath, persisted.ExportedPath);

            var payload = Path.Combine(root, "browser-downloads", "quarantine", record.DownloadId.ToString("N") + ".payload");
            Assert.True(File.Exists(payload));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task UnsafeSuggestedNameCannotEscapeApprovedDirectory()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "../../outside.txt", "safe");

            Assert.Equal("outside.txt", record.SuggestedFileName);
            var receipt = await quarantine.ExportAsync(record.DownloadId, destination, userApproved: true);
            Assert.Equal(Path.Combine(destination, "outside.txt"), receipt.DestinationPath);
            Assert.True(File.Exists(receipt.DestinationPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task CancelledCaptureIsInterruptedAndNeverExportable()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            using var cancellation = new CancellationTokenSource();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                quarantine.CaptureAsync(
                    new Uri("https://example.com/cancelled"),
                    "cancelled.bin",
                    async (path, cancellationToken) =>
                    {
                        await File.WriteAllTextAsync(path, "partial", cancellationToken);
                        cancellation.Cancel();
                        cancellationToken.ThrowIfCancellationRequested();
                    },
                    cancellation.Token));

            var record = Assert.Single(await quarantine.ListAsync());
            Assert.Equal(BrowserDownloadState.Interrupted, record.State);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                quarantine.ExportAsync(record.DownloadId, destination, userApproved: true));
            Assert.Empty(Directory.GetFiles(destination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
    }

    [Fact]
    public async Task MetadataCanBeProtectedWithoutLeakingFileNameOrSource()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var protector = new TestProtector();
            var quarantine = new BrowserDownloadQuarantine(root, protector);
            await CaptureTextAsync(quarantine, "private-name.txt", "secret-data");

            var metadata = await File.ReadAllTextAsync(Path.Combine(root, "browser-downloads", "downloads.json"));
            Assert.StartsWith("NVIDEA-STATE-V1", metadata, StringComparison.Ordinal);
            Assert.DoesNotContain("private-name.txt", metadata, StringComparison.Ordinal);
            Assert.DoesNotContain("https://example.com", metadata, StringComparison.Ordinal);

            var loaded = Assert.Single(await quarantine.ListAsync());
            Assert.Equal("private-name.txt", loaded.SuggestedFileName);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExistingDestinationIsNeverOverwritten()
    {
        var root = CreateTemporaryDirectory();
        var destination = CreateTemporaryDirectory();
        try
        {
            var quarantine = new BrowserDownloadQuarantine(root);
            var record = await CaptureTextAsync(quarantine, "report.txt", "new");
            var destinationPath = Path.Combine(destination, "report.txt");
            await File.WriteAllTextAsync(destinationPath, "existing");

            await Assert.ThrowsAsync<IOException>(() =>
                quarantine.ExportAsync(record.DownloadId, destination, userApproved: true));

            Assert.Equal("existing", await File.ReadAllTextAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(destination, recursive: true);
        }
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
        var path = Path.Combine(Path.GetTempPath(), "nvidea-download-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => Transform(plaintext, purpose);
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => Transform(protectedData, purpose);

        private static byte[] Transform(ReadOnlySpan<byte> input, string purpose)
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(purpose));
            var result = input.ToArray();
            for (var i = 0; i < result.Length; i++)
                result[i] ^= key[i % key.Length];
            return result;
        }
    }
}
