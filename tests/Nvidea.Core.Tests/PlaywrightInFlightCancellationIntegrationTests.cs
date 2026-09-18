using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

/// <summary>
/// Opt-in real Chromium proof that cancellation tears down the agent page instead of merely
/// abandoning the caller's await while a browser command continues in the background.
/// </summary>
public sealed class PlaywrightInFlightCancellationIntegrationTests
{
    [Fact]
    public async Task Navigate_CancelledAfterDispatch_ClosesPageAndReturnsCancellationPromptly()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("NVIDEA_RUN_PLAYWRIGHT_INTEGRATION"), "1", StringComparison.Ordinal))
            return;

        await using var server = await SlowLoopbackServer.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-browser-cancel", Guid.NewGuid().ToString("N"));
        PersistentBrowserContextSession? session = null;
        try
        {
            using var playwright = await Playwright.CreateAsync();
            var options = new PlaywrightBrowserDriverOptions(ActionTimeoutMilliseconds: 30_000);
            session = await PersistentBrowserContextFactory.LaunchAsync(playwright, stateDirectory, server.UriFor("/ready"), options, headless: true);
            var page = session.Context.Pages.Single();
            var driver = new PlaywrightBrowserDriver(page, options);
            using var cancellation = new CancellationTokenSource();

            var action = new BrowserAction(
                BrowserActionKind.Navigate,
                "Open deliberately slow local page",
                Destination: server.UriFor("/slow"));

            var running = driver.ExecuteAsync(action, cancellation.Token);
            await server.WaitUntilSlowRequestArrivesAsync(TimeSpan.FromSeconds(3));

            var stopwatch = Stopwatch.StartNew();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
            stopwatch.Stop();

            Assert.True(page.IsClosed, "Emergency cancellation must close the agent-owned page to abort the in-flight Playwright command.");
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Cancellation took {stopwatch.Elapsed}; it should not wait for the slow navigation response.");
            Assert.Equal(1, server.SlowRequestCount);
        }
        finally
        {
            if (session is not null)
            {
                try { await session.Context.CloseAsync(); } catch (PlaywrightException) { }
            }
            try { if (Directory.Exists(stateDirectory)) Directory.Delete(stateDirectory, recursive: true); } catch { }
        }
    }

    private sealed class SlowLoopbackServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly TaskCompletionSource _slowArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task _acceptLoop;
        private int _slowRequestCount;

        private SlowLoopbackServer(TcpListener listener)
        {
            _listener = listener;
            _acceptLoop = AcceptLoopAsync();
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public int SlowRequestCount => Volatile.Read(ref _slowRequestCount);
        public Uri UriFor(string path) => new($"http://127.0.0.1:{Port}{path}");

        public static Task<SlowLoopbackServer> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return Task.FromResult(new SlowLoopbackServer(listener));
        }

        public async Task WaitUntilSlowRequestArrivesAsync(TimeSpan timeout) =>
            await _slowArrived.Task.WaitAsync(timeout).ConfigureAwait(false);

        private async Task AcceptLoopAsync()
        {
            while (!_shutdown.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_shutdown.Token).ConfigureAwait(false);
                    _ = HandleAsync(client);
                }
                catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { return; }
                catch (ObjectDisposedException) when (_shutdown.IsCancellationRequested) { return; }
            }
        }

        private async Task HandleAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                    var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(requestLine)) return;
                    string? line;
                    do { line = await reader.ReadLineAsync().ConfigureAwait(false); } while (!string.IsNullOrEmpty(line));
                    var parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                    var path = parts.Length >= 2 ? parts[1].Split('?', 2)[0] : "/";

                    if (path == "/slow")
                    {
                        Interlocked.Increment(ref _slowRequestCount);
                        _slowArrived.TrySetResult();
                        await Task.Delay(TimeSpan.FromSeconds(10), _shutdown.Token).ConfigureAwait(false);
                    }

                    var body = Encoding.UTF8.GetBytes("ok");
                    var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(headers, _shutdown.Token).ConfigureAwait(false);
                    await stream.WriteAsync(body, _shutdown.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
                catch (IOException) { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _listener.Stop();
            try { await _acceptLoop.ConfigureAwait(false); } catch (OperationCanceledException) { }
            _shutdown.Dispose();
        }
    }
}
