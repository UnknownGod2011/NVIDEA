using Nvidea.Core.Browser;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadSynchronizationTests
{
    [Fact]
    public void SameStatePath_QuarantineAndPassiveReaderShareExactGate()
    {
        var directory = CreateTempDirectory();
        try
        {
            var protector = new TestProtector();
            var options = new BrowserDownloadQuarantineOptions();
            var quarantine = new BrowserDownloadQuarantine(directory, options, protector);
            var reader = new BrowserDownloadSnapshotReader(directory, options, protector);

            Assert.Same(quarantine.SynchronizationGate, reader.SynchronizationGate);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DifferentStatePaths_DoNotShareGate()
    {
        var first = CreateTempDirectory();
        var second = CreateTempDirectory();
        try
        {
            var protector = new TestProtector();
            var firstQuarantine = new BrowserDownloadQuarantine(first, protector);
            var secondReader = new BrowserDownloadSnapshotReader(second, protector: protector);

            Assert.NotSame(firstQuarantine.SynchronizationGate, secondReader.SynchronizationGate);
        }
        finally
        {
            Directory.Delete(first, recursive: true);
            Directory.Delete(second, recursive: true);
        }
    }

    [Fact]
    public async Task PassiveSnapshot_WaitsForInProcessQuarantineTransitionGate()
    {
        var directory = CreateTempDirectory();
        try
        {
            var protector = new TestProtector();
            var options = new BrowserDownloadQuarantineOptions();
            var quarantine = new BrowserDownloadQuarantine(directory, options, protector);
            var reader = new BrowserDownloadSnapshotReader(directory, options, protector);

            await quarantine.SynchronizationGate.WaitAsync();
            Task<BrowserDownloadSnapshot> readTask;
            try
            {
                readTask = reader.ReadAsync();
                await Task.Delay(50);
                Assert.False(readTask.IsCompleted);
            }
            finally
            {
                quarantine.SynchronizationGate.Release();
            }

            var snapshot = await readTask;
            Assert.Empty(snapshot.RetainedDownloads);
            Assert.False(snapshot.HasPendingRecovery);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SnapshotAfterCapture_ObservesOnlyCompletedRetainedIdentity()
    {
        var directory = CreateTempDirectory();
        try
        {
            var protector = new TestProtector();
            var options = new BrowserDownloadQuarantineOptions(1024 * 1024, 512 * 1024);
            var quarantine = new BrowserDownloadQuarantine(directory, options, protector);
            var reader = new BrowserDownloadSnapshotReader(directory, options, protector);
            var payload = new byte[] { 4, 3, 2, 1 };

            var captured = await quarantine.CaptureAsync(
                new Uri("https://example.test/download"),
                "artifact.bin",
                async (path, ct) => await File.WriteAllBytesAsync(path, payload, ct));

            var snapshot = await reader.ReadAsync();
            var item = Assert.Single(snapshot.RetainedDownloads);

            Assert.Equal(captured.DownloadId, item.DownloadId);
            Assert.Equal(BrowserDownloadState.Ready, item.State);
            Assert.Equal(payload.LongLength, item.LengthBytes);
            Assert.Equal(captured.Sha256, item.Sha256, ignoreCase: true);
            Assert.False(snapshot.HasPendingRecovery);
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
