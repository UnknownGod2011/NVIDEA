using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

public sealed class BrowserDownloadStagingGuardTests
{
    [Fact]
    public async Task OversizedPartialCancelsSourceAndFailsClosed()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var guard = new BrowserDownloadStagingGuard(
                root,
                new BrowserDownloadStagingOptions(MaxStagingBytes: 1024, MaxPartialBytes: 32, PollIntervalMilliseconds: 10));
            var partial = Path.Combine(root, "browser-downloads", "quarantine", "active.partial");
            Directory.CreateDirectory(Path.GetDirectoryName(partial)!);
            var cancelled = false;
            var stop = new CancellationTokenSource();

            var error = await Assert.ThrowsAsync<BrowserDownloadQuotaExceededException>(() =>
                guard.RunBoundedAsync(
                    partial,
                    async () =>
                    {
                        await using var stream = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                        var chunk = new byte[16];
                        while (!stop.IsCancellationRequested)
                        {
                            await stream.WriteAsync(chunk);
                            await stream.FlushAsync();
                            await Task.Delay(5);
                        }
                    },
                    () =>
                    {
                        cancelled = true;
                        stop.Cancel();
                        return Task.CompletedTask;
                    }));

            Assert.True(cancelled);
            Assert.Contains("partial", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task OversizedPlaywrightStagingCancelsSource()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var guard = new BrowserDownloadStagingGuard(
                root,
                new BrowserDownloadStagingOptions(MaxStagingBytes: 32, MaxPartialBytes: 1024, PollIntervalMilliseconds: 10));
            Directory.CreateDirectory(guard.StagingDirectory);
            var stagingFile = Path.Combine(guard.StagingDirectory, "playwright.tmp");
            var partial = Path.Combine(root, "browser-downloads", "quarantine", "active.partial");
            Directory.CreateDirectory(Path.GetDirectoryName(partial)!);
            var cancelled = false;
            var stop = new CancellationTokenSource();

            var error = await Assert.ThrowsAsync<BrowserDownloadQuotaExceededException>(() =>
                guard.RunBoundedAsync(
                    partial,
                    async () =>
                    {
                        await using var stream = new FileStream(stagingFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                        var chunk = new byte[16];
                        while (!stop.IsCancellationRequested)
                        {
                            await stream.WriteAsync(chunk);
                            await stream.FlushAsync();
                            await Task.Delay(5);
                        }
                    },
                    () =>
                    {
                        cancelled = true;
                        stop.Cancel();
                        return Task.CompletedTask;
                    }));

            Assert.True(cancelled);
            Assert.Contains("staging", error.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SuccessfulOperationWithinLimitsIsNotCancelled()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var guard = new BrowserDownloadStagingGuard(
                root,
                new BrowserDownloadStagingOptions(MaxStagingBytes: 1024, MaxPartialBytes: 1024, PollIntervalMilliseconds: 10));
            var partial = Path.Combine(root, "browser-downloads", "quarantine", "active.partial");
            Directory.CreateDirectory(Path.GetDirectoryName(partial)!);
            var cancelled = false;

            await guard.RunBoundedAsync(
                partial,
                () => File.WriteAllBytesAsync(partial, new byte[64]),
                () =>
                {
                    cancelled = true;
                    return Task.CompletedTask;
                });

            Assert.False(cancelled);
            Assert.Equal(64, new FileInfo(partial).Length);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CallerCancellationCancelsUnderlyingSource()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var guard = new BrowserDownloadStagingGuard(
                root,
                new BrowserDownloadStagingOptions(MaxStagingBytes: 1024, MaxPartialBytes: 1024, PollIntervalMilliseconds: 10));
            var partial = Path.Combine(root, "browser-downloads", "quarantine", "active.partial");
            var sourceStop = new CancellationTokenSource();
            using var callerStop = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
            var cancelled = false;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                guard.RunBoundedAsync(
                    partial,
                    async () =>
                    {
                        while (!sourceStop.IsCancellationRequested)
                            await Task.Delay(5);
                    },
                    () =>
                    {
                        cancelled = true;
                        sourceStop.Cancel();
                        return Task.CompletedTask;
                    },
                    callerStop.Token));

            Assert.True(cancelled);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void InvalidStagingConfigurationIsRejected()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrowserDownloadStagingGuard(root, new BrowserDownloadStagingOptions(MaxStagingBytes: 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BrowserDownloadStagingGuard(root, new BrowserDownloadStagingOptions(PollIntervalMilliseconds: 1)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "nvidea-download-staging-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
