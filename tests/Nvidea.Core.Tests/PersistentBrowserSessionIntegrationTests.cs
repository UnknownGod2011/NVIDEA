using System.Net;
using System.Net.Sockets;
using System.Text;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

/// <summary>
/// Opt-in real Chromium coverage for the persistent NVIDEA browser profile and session driver.
/// Enable with NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing the matching Playwright Chromium.
/// </summary>
public sealed class PersistentBrowserSessionIntegrationTests
{
    [BrowserIntegrationFact]
    public async Task CookieState_SurvivesRuntimeRestart_WithoutRestoringOldTabs()
    {
        await using var site = await LocalSessionSite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-persistent-browser-it", Guid.NewGuid().ToString("N"));
        var allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost };

        try
        {
            await using (var first = await BrowserHostRuntime.CreateAsync(
                stateDirectory,
                new BrowserHostOptions(new Uri(site.StartUri, "/seed"), allowedHosts, Headless: true)))
            {
                var seeded = await first.ObserveAsync();
                Assert.Contains("session-seeded", seeded.VisibleText, StringComparison.Ordinal);
            }

            await using (var second = await BrowserHostRuntime.CreateAsync(
                stateDirectory,
                new BrowserHostOptions(new Uri(site.StartUri, "/check"), allowedHosts, Headless: true)))
            {
                var restored = await second.ObserveAsync();
                Assert.Contains("session-restored", restored.VisibleText, StringComparison.Ordinal);

                var snapshot = await second.GetSessionSnapshotAsync();
                Assert.Single(snapshot.Pages);
                Assert.Equal("/check", snapshot.ActivePage.Url?.AbsolutePath);
                Assert.All(snapshot.Pages, page => Assert.True(page.IsPermitted));
            }
        }
        finally
        {
            TryDeleteDirectory(stateDirectory);
        }
    }

    [BrowserIntegrationFact]
    public async Task AllowedPopup_BecomesActive_AndCrossBoundaryPopup_IsNeverAdopted()
    {
        await using var site = await LocalSessionSite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-popup-browser-it", Guid.NewGuid().ToString("N"));
        var allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost };

        try
        {
            await using var runtime = await BrowserHostRuntime.CreateAsync(
                stateDirectory,
                new BrowserHostOptions(new Uri(site.StartUri, "/popups"), allowedHosts, Headless: true));

            var allowed = new BrowserAction(
                BrowserActionKind.Click,
                BrowserLocator.ByRole("button", "Open allowed popup"),
                Rationale: "Open the controlled same-boundary popup.",
                Postconditions: new[]
                {
                    new BrowserPostcondition(BrowserPostconditionKind.VisibleTextContains, Expected: "allowed-popup")
                });
            var allowedOutcome = await ExecuteWithApprovalIfNeededAsync(runtime, allowed);
            Assert.Equal(AgentJobState.Completed, allowedOutcome.State);

            var allowedSnapshot = await runtime.GetSessionSnapshotAsync();
            Assert.Equal("/allowed-popup", allowedSnapshot.ActivePage.Url?.AbsolutePath);
            Assert.All(allowedSnapshot.Pages, page => Assert.True(page.IsPermitted));

            var returnToHarness = new BrowserAction(
                BrowserActionKind.Navigate,
                Destination: new Uri(site.StartUri, "/popups"),
                Rationale: "Return to the controlled popup harness.",
                Postconditions: new[]
                {
                    new BrowserPostcondition(BrowserPostconditionKind.UrlEquals, Expected: new Uri(site.StartUri, "/popups").AbsoluteUri)
                });
            var returnOutcome = await ExecuteWithApprovalIfNeededAsync(runtime, returnToHarness);
            Assert.Equal(AgentJobState.Completed, returnOutcome.State);

            var blocked = new BrowserAction(
                BrowserActionKind.Click,
                BrowserLocator.ByRole("button", "Open blocked popup"),
                Rationale: "Attempt the controlled cross-boundary popup.",
                Postconditions: new[]
                {
                    new BrowserPostcondition(BrowserPostconditionKind.VisibleTextContains, Expected: "popup-harness")
                });
            var blockedOutcome = await ExecuteWithApprovalIfNeededAsync(runtime, blocked);
            Assert.Equal(AgentJobState.Completed, blockedOutcome.State);

            await Task.Delay(200);
            var blockedSnapshot = await runtime.GetSessionSnapshotAsync();
            Assert.All(blockedSnapshot.Pages, page =>
            {
                Assert.True(page.IsPermitted);
                if (page.Url is not null)
                    Assert.Equal(site.StartUri.IdnHost, page.Url.IdnHost, ignoreCase: true);
            });
            Assert.DoesNotContain(
                blockedSnapshot.Pages,
                page => string.Equals(page.Url?.IdnHost, "example.invalid", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TryDeleteDirectory(stateDirectory);
        }
    }

    private static async Task<BrowserJobOutcome> ExecuteWithApprovalIfNeededAsync(
        BrowserHostRuntime runtime,
        BrowserAction action)
    {
        var outcome = await runtime.StartActionAsync(action);
        if (outcome.State != AgentJobState.WaitingForApproval)
            return outcome;

        Assert.NotNull(outcome.Approval);
        return await runtime.ApproveAndResumeAsync(outcome.JobId, outcome.Approval!.ExactScope);
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
            // Best-effort cleanup must not hide the test assertion result.
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
                Skip = "Set NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing Playwright Chromium to run persistent-browser integration tests.";
            }
        }
    }

    private sealed class LocalSessionSite : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _acceptLoop;

        private LocalSessionSite(TcpListener listener, Uri startUri)
        {
            _listener = listener;
            StartUri = startUri;
            _acceptLoop = AcceptLoopAsync(_stop.Token);
        }

        public Uri StartUri { get; }

        public static Task<LocalSessionSite> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            return Task.FromResult(new LocalSessionSite(listener, new Uri($"http://127.0.0.1:{endpoint.Port}/")));
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

        private static async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
            {
                var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
                var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var path = parts.Length >= 2 ? parts[1] : "/";
                var cookie = string.Empty;

                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)))
                {
                    if (line.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
                        cookie = line["Cookie:".Length..].Trim();
                }

                var (body, extraHeader) = path switch
                {
                    "/seed" => ("<html><body>session-seeded</body></html>", "Set-Cookie: nvidea_session=proof; Path=/; SameSite=Lax\r\n"),
                    "/check" => (cookie.Contains("nvidea_session=proof", StringComparison.Ordinal)
                        ? "<html><body>session-restored</body></html>"
                        : "<html><body>session-missing</body></html>", string.Empty),
                    "/allowed-popup" => ("<html><body>allowed-popup</body></html>", string.Empty),
                    "/popups" => ("""
                        <html><body>
                        <div>popup-harness</div>
                        <button onclick="window.open('/allowed-popup','_blank')">Open allowed popup</button>
                        <button onclick="window.open('http://example.invalid/blocked','_blank')">Open blocked popup</button>
                        </body></html>
                        """, string.Empty),
                    _ => ("<html><body>not-found</body></html>", string.Empty)
                };

                var bytes = Encoding.UTF8.GetBytes(body);
                var header = Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bytes.Length}\r\n{extraHeader}Connection: close\r\n\r\n");
                await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
