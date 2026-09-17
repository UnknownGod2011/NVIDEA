using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;
using Nvidea.Core.Browser;

namespace Nvidea.Core.Tests;

/// <summary>
/// Real Chromium transport checks. These are opt-in because Playwright's browser binary is not
/// guaranteed to exist on every developer/CI machine. Set NVIDEA_RUN_PLAYWRIGHT_INTEGRATION=1
/// after installing Chromium with Playwright to execute the network assertions.
/// </summary>
public sealed class PersistentBrowserRedirectIntegrationTests
{
    [Fact]
    public async Task CredentialBearingRedirect_IsStoppedBeforeDestinationRequest()
    {
        if (!IntegrationEnabled()) return;
        await using var server = await LoopbackHttpServer.StartAsync();
        var stateDirectory = CreateStateDirectory();
        PersistentBrowserContextSession? session = null;
        try
        {
            using var playwright = await Playwright.CreateAsync();
            session = await PersistentBrowserContextFactory.LaunchAsync(playwright, stateDirectory, server.UriFor("/safe"), new PlaywrightBrowserDriverOptions(ActionTimeoutMilliseconds: 5_000), headless: true);
            var destinationBefore = server.Count("/forbidden-destination");
            await Assert.ThrowsAnyAsync<Exception>(() => session.Context.Pages.Single().GotoAsync(server.UriFor("/redirect-to-credential-url").AbsoluteUri, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 5_000 }));
            await server.WaitForCountAsync("/redirect-to-credential-url", 1, TimeSpan.FromSeconds(2));
            await Task.Delay(250);
            Assert.Equal(1, server.Count("/redirect-to-credential-url"));
            Assert.Equal(destinationBefore, server.Count("/forbidden-destination"));
        }
        finally { if (session is not null) await session.Context.CloseAsync(); TryDeleteDirectory(stateDirectory); }
    }

    [Fact]
    public async Task CredentialBearingWebSocket_IsStoppedBeforeServerHandshake()
    {
        if (!IntegrationEnabled()) return;
        await using var server = await LoopbackHttpServer.StartAsync();
        var stateDirectory = CreateStateDirectory();
        PersistentBrowserContextSession? session = null;
        try
        {
            using var playwright = await Playwright.CreateAsync();
            session = await PersistentBrowserContextFactory.LaunchAsync(playwright, stateDirectory, server.UriFor("/safe"), new PlaywrightBrowserDriverOptions(ActionTimeoutMilliseconds: 5_000), headless: true);
            var page = session.Context.Pages.Single();
            var before = server.Count("/forbidden-websocket");

            // A credential-bearing loopback WS would otherwise be transport-eligible. The production
            // WebSocket route must reject it without ConnectToServer(), so no HTTP Upgrade handshake
            // may reach the controlled server. Resolve on close/error to avoid depending on browser text.
            await page.EvaluateAsync("""
                url => new Promise(resolve => {
                  try {
                    const ws = new WebSocket(url);
                    const done = () => resolve(true);
                    ws.addEventListener('close', done, { once: true });
                    ws.addEventListener('error', done, { once: true });
                    setTimeout(done, 1000);
                  } catch (_) { resolve(true); }
                })
                """, $"ws://nvidea-test-secret@127.0.0.1:{server.Port}/forbidden-websocket");

            await Task.Delay(250);
            Assert.Equal(before, server.Count("/forbidden-websocket"));
        }
        finally { if (session is not null) await session.Context.CloseAsync(); TryDeleteDirectory(stateDirectory); }
    }

    [Fact]
    public async Task AllowedLoopbackNavigation_ReachesDestination()
    {
        if (!IntegrationEnabled()) return;
        await using var server = await LoopbackHttpServer.StartAsync();
        var stateDirectory = CreateStateDirectory();
        PersistentBrowserContextSession? session = null;
        try
        {
            using var playwright = await Playwright.CreateAsync();
            session = await PersistentBrowserContextFactory.LaunchAsync(playwright, stateDirectory, server.UriFor("/safe"), new PlaywrightBrowserDriverOptions(ActionTimeoutMilliseconds: 5_000), headless: true);
            await session.Context.Pages.Single().GotoAsync(server.UriFor("/allowed-destination").AbsoluteUri, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 5_000 });
            await server.WaitForCountAsync("/allowed-destination", 1, TimeSpan.FromSeconds(2));
            Assert.Equal(1, server.Count("/allowed-destination"));
        }
        finally { if (session is not null) await session.Context.CloseAsync(); TryDeleteDirectory(stateDirectory); }
    }

    private static bool IntegrationEnabled() => string.Equals(Environment.GetEnvironmentVariable("NVIDEA_RUN_PLAYWRIGHT_INTEGRATION"), "1", StringComparison.Ordinal);
    private static string CreateStateDirectory() => Path.Combine(Path.GetTempPath(), "nvidea-browser-integration", Guid.NewGuid().ToString("N"));
    private static void TryDeleteDirectory(string path) { try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { } }

    private sealed class LoopbackHttpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener; private readonly CancellationTokenSource _shutdown = new(); private readonly Task _acceptLoop;
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal); private readonly object _gate = new();
        private LoopbackHttpServer(TcpListener listener) { _listener = listener; _acceptLoop = AcceptLoopAsync(); }
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public static Task<LoopbackHttpServer> StartAsync() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return Task.FromResult(new LoopbackHttpServer(listener)); }
        public Uri UriFor(string path) => new($"http://127.0.0.1:{Port}{path}");
        public int Count(string path) { lock (_gate) return _counts.TryGetValue(path, out var count) ? count : 0; }
        public async Task WaitForCountAsync(string path, int minimumCount, TimeSpan timeout) { using var cts = new CancellationTokenSource(timeout); while (Count(path) < minimumCount) { try { await Task.Delay(20, cts.Token).ConfigureAwait(false); } catch (OperationCanceledException) when (cts.IsCancellationRequested) { throw new TimeoutException($"Timed out waiting for {path} to receive {minimumCount} request(s); observed {Count(path)}."); } } }
        private void Increment(string path) { lock (_gate) { _counts.TryGetValue(path, out var count); _counts[path] = count + 1; } }
        private async Task AcceptLoopAsync() { while (!_shutdown.IsCancellationRequested) { try { var client = await _listener.AcceptTcpClientAsync(_shutdown.Token).ConfigureAwait(false); _ = HandleAsync(client); } catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { return; } catch (ObjectDisposedException) when (_shutdown.IsCancellationRequested) { return; } } }
        private async Task HandleAsync(TcpClient client)
        {
            using (client) try
            {
                using var stream = client.GetStream(); using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync().ConfigureAwait(false); if (string.IsNullOrWhiteSpace(requestLine)) return;
                string? line; do { line = await reader.ReadLineAsync().ConfigureAwait(false); } while (!string.IsNullOrEmpty(line));
                var parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries); var path = parts.Length >= 2 && Uri.TryCreate(parts[1], UriKind.RelativeOrAbsolute, out var target) ? (target.IsAbsoluteUri ? target.PathAndQuery : parts[1]) : "/";
                var queryIndex = path.IndexOf('?'); if (queryIndex >= 0) path = path[..queryIndex]; Increment(path);
                if (path == "/redirect-to-credential-url") { var location = $"http://nvidea-test-secret@127.0.0.1:{Port}/forbidden-destination"; await WriteResponseAsync(stream, "302 Found", "redirect", $"Location: {location}\r\n").ConfigureAwait(false); }
                else await WriteResponseAsync(stream, "200 OK", "ok", string.Empty).ConfigureAwait(false);
            } catch (IOException) { }
        }
        private static async Task WriteResponseAsync(NetworkStream stream, string status, string body, string extraHeaders) { var bodyBytes = Encoding.UTF8.GetBytes(body); var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: text/plain\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n{extraHeaders}\r\n"); await stream.WriteAsync(headers).ConfigureAwait(false); await stream.WriteAsync(bodyBytes).ConfigureAwait(false); await stream.FlushAsync().ConfigureAwait(false); }
        public async ValueTask DisposeAsync() { _shutdown.Cancel(); _listener.Stop(); try { await _acceptLoop.ConfigureAwait(false); } catch (OperationCanceledException) { } _shutdown.Dispose(); }
    }
}
