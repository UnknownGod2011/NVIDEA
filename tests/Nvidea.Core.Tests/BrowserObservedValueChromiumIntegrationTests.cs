using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;
using Nvidea.Core.Browser;
using Xunit;

namespace Nvidea.Core.Tests;

/// <summary>
/// Opt-in real Chromium coverage for observed form-value privacy.
/// Enable with NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing the matching Playwright Chromium.
/// The fixture is loopback-only and uses synthetic secrets.
/// </summary>
public sealed class BrowserObservedValueChromiumIntegrationTests
{
    [BrowserIntegrationFact]
    public async Task SensitiveFormValues_AreSuppressed_WhileSemanticsSurvive_AndAccessibilityTypingIsBlocked()
    {
        await using var site = await FormPrivacySite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-form-privacy-it", Guid.NewGuid().ToString("N"));
        var allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost };

        IPlaywright? playwright = null;
        PersistentBrowserContextSession? session = null;
        try
        {
            playwright = await Playwright.CreateAsync();
            session = await PersistentBrowserContextFactory.LaunchAsync(
                playwright,
                stateDirectory,
                new Uri(site.StartUri, "/form-privacy"),
                new PlaywrightBrowserDriverOptions(allowedHosts, MaxObservationCharacters: 8_000, MaxObservedElements: 50),
                headless: true,
                cancellationToken: CancellationToken.None);

            var observation = await session.Driver.ObserveAsync();
            var policy = new BrowserSafetyPolicy();

            AssertSensitive(observation, policy, "Password field", "password", "current-password");
            AssertSensitive(observation, policy, "Verification field", "text", "one-time-code");
            AssertSensitive(observation, policy, "Payment number", "text", "cc-number");
            AssertSensitive(observation, policy, "Payment security", "text", "cc-csc");
            AssertSensitive(observation, policy, "Payment expiry", "text", "cc-exp");

            var benign = Assert.Single(observation.Elements.Where(e => e.Name == "Contact field"));
            Assert.Equal("synthetic@example.test", benign.Value);
            Assert.Equal("email", benign.InputType);
            Assert.Equal("email", benign.AutoComplete);

            var benignDecision = policy.Evaluate(
                new BrowserAction(BrowserActionKind.Type, BrowserLocator.Accessibility(benign.Reference), Value: "new@example.test"),
                observation);
            Assert.True(benignDecision.Allowed);
            Assert.False(benignDecision.RequiresApproval);
            Assert.NotEqual(BrowserRiskLevel.Blocked, benignDecision.Risk);
        }
        finally
        {
            if (session is not null)
            {
                try { await session.Context.CloseAsync(); } catch { }
            }
            playwright?.Dispose();
            TryDeleteDirectory(stateDirectory);
        }
    }

    private static void AssertSensitive(
        BrowserObservation observation,
        BrowserSafetyPolicy policy,
        string name,
        string expectedInputType,
        string expectedAutoComplete)
    {
        var element = Assert.Single(observation.Elements.Where(e => e.Name == name));
        Assert.Null(element.Value);
        Assert.Equal(expectedInputType, element.InputType);
        Assert.Equal(expectedAutoComplete, element.AutoComplete);

        var decision = policy.Evaluate(
            new BrowserAction(BrowserActionKind.Type, BrowserLocator.Accessibility(element.Reference), Value: "replacement"),
            observation);
        Assert.False(decision.Allowed);
        Assert.Equal(BrowserRiskLevel.Blocked, decision.Risk);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch { }
    }

    private sealed class BrowserIntegrationFactAttribute : FactAttribute
    {
        public BrowserIntegrationFactAttribute()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("NVIDEA_RUN_BROWSER_INTEGRATION"), "1", StringComparison.Ordinal))
                Skip = "Set NVIDEA_RUN_BROWSER_INTEGRATION=1 after installing Playwright Chromium to run browser integration tests.";
        }
    }

    private sealed class FormPrivacySite : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _acceptLoop;

        private FormPrivacySite(TcpListener listener, Uri startUri)
        {
            _listener = listener;
            StartUri = startUri;
            _acceptLoop = AcceptLoopAsync(_stop.Token);
        }

        public Uri StartUri { get; }

        public static Task<FormPrivacySite> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            return Task.FromResult(new FormPrivacySite(listener, new Uri($"http://127.0.0.1:{endpoint.Port}/")));
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
                try { client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                _ = Task.Run(() => HandleAsync(client, cancellationToken), CancellationToken.None);
            }
        }

        private static async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
            {
                try
                {
                    _ = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    string? line;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false))) { }

                    const string body = """
                        <!doctype html><html><body>
                        <label>Password field<input type='password' autocomplete='current-password' value='synthetic-password'></label>
                        <label>Verification field<input type='text' autocomplete='one-time-code' value='123456'></label>
                        <label>Payment number<input type='text' autocomplete='cc-number' value='4111111111111111'></label>
                        <label>Payment security<input type='text' autocomplete='cc-csc' value='123'></label>
                        <label>Payment expiry<input type='text' autocomplete='cc-exp' value='12/34'></label>
                        <label>Contact field<input type='email' autocomplete='email' value='synthetic@example.test'></label>
                        </body></html>
                        """;
                    var bodyBytes = Encoding.UTF8.GetBytes(body);
                    var headerBytes = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                catch (IOException) { }
                catch (SocketException) { }
            }
        }
    }
}
