using System.Net;
using System.Net.Sockets;
using System.Text;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

/// <summary>
/// Real Chromium integration proof for the trusted desktop browser runtime.
/// It is intentionally opt-in so normal unit tests never download/launch browsers.
/// Enable with NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing the matching
/// Playwright Chromium binary for the repository's Microsoft.Playwright version.
/// </summary>
public sealed class BrowserHostRuntimeIntegrationTests
{
    [BrowserIntegrationFact]
    public async Task ConsequentialClick_DoesNotMutateBeforeApproval_ExecutesOnce_AndCannotReplay()
    {
        await using var site = await LocalBrowserTestSite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-browser-it", Guid.NewGuid().ToString("N"));

        try
        {
            await using var runtime = await BrowserHostRuntime.CreateAsync(
                stateDirectory,
                new BrowserHostOptions(
                    site.StartUri,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost },
                    Headless: true));

            var action = new BrowserAction(
                BrowserActionKind.Click,
                BrowserLocator.ByRole("button", "Submit demo mutation"),
                ExpectedState: "approved mutation complete",
                Rationale: "Submit the controlled demo mutation after explicit user approval.");

            var paused = await runtime.StartActionAsync(action);

            Assert.Equal(AgentJobState.WaitingForApproval, paused.State);
            Assert.NotNull(paused.Approval);
            Assert.False(string.IsNullOrWhiteSpace(paused.Approval!.ExactScope));
            Assert.Equal(0, site.MutationCount);

            var persistedWhilePaused = await File.ReadAllTextAsync(Path.Combine(stateDirectory, "jobs.json"));
            Assert.DoesNotContain("grant", persistedWhilePaused, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", persistedWhilePaused, StringComparison.OrdinalIgnoreCase);

            var completed = await runtime.ApproveAndResumeAsync(paused.JobId, paused.Approval.ExactScope);

            Assert.Equal(AgentJobState.Completed, completed.State);
            Assert.Equal(1, site.MutationCount);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                runtime.ApproveAndResumeAsync(paused.JobId, paused.Approval.ExactScope));

            Assert.Equal(1, site.MutationCount);

            var persistedAfterCompletion = await File.ReadAllTextAsync(Path.Combine(stateDirectory, "jobs.json"));
            Assert.DoesNotContain("grant", persistedAfterCompletion, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", persistedAfterCompletion, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDeleteDirectory(stateDirectory);
        }
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
            // Test cleanup is best-effort and must not hide the security assertion result.
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
                Skip = "Set NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing Playwright Chromium to run the real-browser integration harness.";
            }
        }
    }

    private sealed class LocalBrowserTestSite : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _acceptLoop;
        private int _mutationCount;

        private LocalBrowserTestSite(TcpListener listener, Uri startUri)
        {
            _listener = listener;
            StartUri = startUri;
            _acceptLoop = AcceptLoopAsync(_stop.Token);
        }

        public Uri StartUri { get; }

        public int MutationCount => Volatile.Read(ref _mutationCount);

        public static Task<LocalBrowserTestSite> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            var site = new LocalBrowserTestSite(listener, new Uri($"http://127.0.0.1:{endpoint.Port}/"));
            return Task.FromResult(site);
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();
            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _stop.Dispose();
            }
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
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (SocketException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = HandleClientAsync(client, cancellationToken);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(requestLine))
                    return;

                while (true)
                {
                    var header = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(header))
                        break;
                }

                var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var method = parts.Length > 0 ? parts[0] : string.Empty;
                var path = parts.Length > 1 ? parts[1] : string.Empty;

                if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path, "/mutate", StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _mutationCount);
                    await WriteResponseAsync(stream, "200 OK", "text/plain; charset=utf-8", "ok", cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path, "/favicon.ico", StringComparison.Ordinal))
                {
                    await WriteResponseAsync(stream, "204 No Content", "text/plain", string.Empty, cancellationToken).ConfigureAwait(false);
                    return;
                }

                await WriteResponseAsync(stream, "200 OK", "text/html; charset=utf-8", Html, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task WriteResponseAsync(
            NetworkStream stream,
            string status,
            string contentType,
            string body,
            CancellationToken cancellationToken)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\nCache-Control: no-store\r\n\r\n");
            await stream.WriteAsync(headers, cancellationToken).ConfigureAwait(false);
            if (bodyBytes.Length > 0)
                await stream.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private const string Html = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <title>NVIDEA browser approval harness</title>
            </head>
            <body>
              <main>
                <h1>Controlled browser approval harness</h1>
                <p id="state">no mutation yet</p>
                <button id="submit-demo" type="button">Submit demo mutation</button>
              </main>
              <script>
                document.getElementById('submit-demo').addEventListener('click', async () => {
                  const response = await fetch('/mutate', { method: 'POST' });
                  if (!response.ok) throw new Error('mutation failed');
                  document.getElementById('state').textContent = 'approved mutation complete';
                });
              </script>
            </body>
            </html>
            """;
    }
}
