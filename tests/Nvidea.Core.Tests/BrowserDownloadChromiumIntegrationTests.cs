using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;
using Nvidea.Core.Browser;
using Xunit;

namespace Nvidea.Core.Tests;

/// <summary>
/// Opt-in real Chromium coverage for the browser download staging boundary.
/// Enable with NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing the matching Playwright Chromium.
/// The fixture deliberately uses small test-only quotas so cancellation is observable without
/// downloading a production-sized 128 MiB payload.
/// </summary>
public sealed class BrowserDownloadChromiumIntegrationTests
{
    [BrowserIntegrationFact]
    public async Task ThrottledOversizedDownload_IsCancelled_Interrupted_AndTransientStateIsRemoved()
    {
        await using var site = await ThrottledDownloadSite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-download-browser-it", Guid.NewGuid().ToString("N"));
        var allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost };

        IPlaywright? playwright = null;
        PersistentBrowserContextSession? session = null;
        try
        {
            playwright = await Playwright.CreateAsync();
            var driverOptions = new PlaywrightBrowserDriverOptions(
                allowedHosts,
                MaxObservationCharacters: 4_000,
                MaxObservedElements: 50,
                ActionTimeoutMilliseconds: 10_000);

            session = await PersistentBrowserContextFactory.LaunchAsync(
                playwright: playwright,
                stateDirectory: stateDirectory,
                startUri: new Uri(site.StartUri, "/download-harness"),
                driverOptions: driverOptions,
                headless: true,
                quarantineOptions: new BrowserDownloadQuarantineOptions(
                    MaxRetainedBytes: 2L * 1024L * 1024L,
                    MaxSingleDownloadBytes: 512L * 1024L),
                stagingOptions: new BrowserDownloadStagingOptions(
                    MaxStagingBytes: 128L * 1024L,
                    MaxPartialBytes: 128L * 1024L,
                    PollIntervalMilliseconds: 20),
                cancellationToken: CancellationToken.None);

            var action = new BrowserAction(
                BrowserActionKind.Download,
                BrowserLocator.ByRole("link", "Download oversized fixture"),
                Rationale: "Exercise the bounded browser-download staging path.");

            var exception = await Assert.ThrowsAsync<BrowserDownloadQuotaExceededException>(
                () => session.Driver.ExecuteAsync(action));
            Assert.Contains("quota", exception.Message, StringComparison.OrdinalIgnoreCase);

            var records = await session.Downloads.ListAsync();
            var interrupted = Assert.Single(records);
            Assert.Equal(BrowserDownloadState.Interrupted, interrupted.State);
            Assert.Null(interrupted.LengthBytes);
            Assert.Null(interrupted.Sha256);
            Assert.False(string.IsNullOrWhiteSpace(interrupted.Failure));

            await AssertDirectoryEventuallyEmptyAsync(session.DownloadStaging.StagingDirectory, TimeSpan.FromSeconds(2));

            var quarantineDirectory = Path.Combine(stateDirectory, "browser-downloads", "quarantine");
            if (Directory.Exists(quarantineDirectory))
            {
                Assert.DoesNotContain(
                    Directory.EnumerateFiles(quarantineDirectory, "*", SearchOption.TopDirectoryOnly),
                    path => path.EndsWith(".partial", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".payload", StringComparison.OrdinalIgnoreCase));
            }

            Assert.True(site.DownloadStarted);
            await AssertConditionEventuallyAsync(
                () => site.DownloadDisconnectedBeforeCompletion,
                TimeSpan.FromSeconds(2),
                "The throttled server should observe Chromium disconnecting before the full fixture is sent after NVIDEA cancellation.");
        }
        finally
        {
            if (session is not null)
            {
                try { await session.Context.CloseAsync(); }
                catch { }
            }
            playwright?.Dispose();
            TryDeleteDirectory(stateDirectory);
        }
    }

    private static async Task AssertDirectoryEventuallyEmptyAsync(string path, TimeSpan timeout)
    {
        await AssertConditionEventuallyAsync(
            () => !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any(),
            timeout,
            "Playwright transient download staging should be empty after cancellation and cleanup.");
    }

    private static async Task AssertConditionEventuallyAsync(Func<bool> condition, TimeSpan timeout, string message)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(25);
        }

        Assert.True(condition(), message);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup must not hide the integration assertion result.
        }
    }

    private sealed class BrowserIntegrationFactAttribute : FactAttribute
    {
        public BrowserIntegrationFactAttribute()
        {
            if (!string.Equals(
                    Environment.GetEnvironmentVariable("NVIDEA_RUN_BROWSER_INTEGRATION"),
                    "1",
                    StringComparison.Ordinal))
            {
                Skip = "Set NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing Playwright Chromium to run browser integration tests.";
            }
        }
    }

    private sealed class ThrottledDownloadSite : IAsyncDisposable
    {
        private const int PayloadBytes = 2 * 1024 * 1024;
        private const int ChunkBytes = 16 * 1024;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _acceptLoop;
        private int _downloadStarted;
        private int _downloadDisconnectedBeforeCompletion;

        private ThrottledDownloadSite(TcpListener listener, Uri startUri)
        {
            _listener = listener;
            StartUri = startUri;
            _acceptLoop = AcceptLoopAsync(_stop.Token);
        }

        public Uri StartUri { get; }
        public bool DownloadStarted => Volatile.Read(ref _downloadStarted) != 0;
        public bool DownloadDisconnectedBeforeCompletion => Volatile.Read(ref _downloadDisconnectedBeforeCompletion) != 0;

        public static Task<ThrottledDownloadSite> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            return Task.FromResult(new ThrottledDownloadSite(listener, new Uri($"http://127.0.0.1:{endpoint.Port}/")));
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();
            try { await _acceptLoop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            _stop.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                _ = Task.Run(() => HandleAsync(client, cancellationToken), CancellationToken.None);
            }
        }

        private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
            {
                try
                {
                    var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
                    var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var path = parts.Length >= 2 ? parts[1] : "/";

                    string? line;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)))
                    {
                    }

                    if (path == "/slow-download")
                    {
                        await WriteThrottledDownloadAsync(stream, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    var body = path == "/download-harness"
                        ? "<html><body><a href='/slow-download' download='oversized-fixture.bin'>Download oversized fixture</a></body></html>"
                        : "<html><body>not-found</body></html>";
                    var bodyBytes = Encoding.UTF8.GetBytes(body);
                    var headerBytes = Encoding.ASCII.GetBytes(
                        $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (IOException)
                {
                    // Expected when Chromium closes a cancelled download connection.
                }
                catch (SocketException)
                {
                    // Expected when Chromium closes a cancelled download connection.
                }
            }
        }

        private async Task WriteThrottledDownloadAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _downloadStarted, 1);
            var header = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Disposition: attachment; filename=oversized-fixture.bin\r\nContent-Length: {PayloadBytes}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);

            var chunk = new byte[ChunkBytes];
            var sent = 0;
            try
            {
                while (sent < PayloadBytes)
                {
                    var count = Math.Min(chunk.Length, PayloadBytes - sent);
                    await stream.WriteAsync(chunk.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    sent += count;
                    await Task.Delay(15, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException)
            {
                if (sent < PayloadBytes)
                    Interlocked.Exchange(ref _downloadDisconnectedBeforeCompletion, 1);
                throw;
            }
        }
    }
}
