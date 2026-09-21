using System.Net;
using System.Net.Sockets;
using System.Text;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Xunit;

namespace Nvidea.Core.Tests;

/// <summary>
/// Real-Chromium production-host proof that lifecycle transitions which do not
/// represent a newly verified side effect cannot leave an older green receipt visible.
/// </summary>
public sealed class BrowserHostVerificationInvalidationIntegrationTests
{
    [BrowserIntegrationFact]
    public async Task NewApproval_Rearm_AndCancellation_KeepPriorVerifiedReceiptInvalidated()
    {
        await using var site = await LocalSite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-browser-invalidation-it", Guid.NewGuid().ToString("N"));
        try
        {
            await using var runtime = await BrowserHostRuntime.CreateAsync(stateDirectory, new BrowserHostOptions(site.StartUri, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost }, Headless: true));
            var product = runtime.CreateProductRuntime();
            var action = ConsequentialClick();
            var firstPaused = await runtime.StartActionAsync(action);
            Assert.Equal(AgentJobState.WaitingForApproval, firstPaused.State);
            Assert.NotNull(firstPaused.Approval);
            var firstCompleted = await runtime.ApproveAndResumeAsync(firstPaused.JobId, firstPaused.Approval!.ExactScope);
            Assert.Equal(AgentJobState.Completed, firstCompleted.State);
            Assert.Equal(1, site.MutationCount);
            Assert.True((await product.ReadVerificationPresentationAsync()).Verified);

            await runtime.StartActionAsync(new BrowserAction(BrowserActionKind.Navigate, Destination: site.StartUri, Rationale: "Return to the controlled test page."));
            var secondPaused = await runtime.StartActionAsync(action);
            Assert.Equal(AgentJobState.WaitingForApproval, secondPaused.State);
            Assert.NotNull(secondPaused.Approval);
            Assert.False((await product.ReadVerificationPresentationAsync()).Verified);
            Assert.Equal(1, site.MutationCount);
            var rearmed = await runtime.RearmApprovalAsync(secondPaused.JobId, secondPaused.Approval!.ExactScope);
            Assert.Equal(AgentJobState.WaitingForApproval, rearmed.State);
            Assert.False((await product.ReadVerificationPresentationAsync()).Verified);
            Assert.Equal(1, site.MutationCount);
            var cancelled = await runtime.CancelAsync(secondPaused.JobId);
            Assert.Equal(AgentJobState.Cancelled, cancelled.State);
            var afterCancellation = await product.ReadVerificationPresentationAsync();
            Assert.False(afterCancellation.Verified);
            Assert.Equal(0, afterCancellation.ActionCount);
            Assert.Equal(0, afterCancellation.ApprovalCount);
            Assert.Equal(1, site.MutationCount);
        }
        finally { TryDeleteDirectory(stateDirectory); }
    }

    [BrowserIntegrationFact]
    public async Task AmbiguousRunningReconciliation_ClearsPriorGreenReceipt_WithoutPublishingReplacement()
    {
        await using var site = await LocalSite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-browser-ambiguous-invalidation-it", Guid.NewGuid().ToString("N"));
        try
        {
            await using var runtime = await BrowserHostRuntime.CreateAsync(stateDirectory, new BrowserHostOptions(site.StartUri, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost }, Headless: true));
            var product = runtime.CreateProductRuntime();

            var firstPaused = await runtime.StartActionAsync(ConsequentialClick());
            Assert.Equal(AgentJobState.WaitingForApproval, firstPaused.State);
            Assert.NotNull(firstPaused.Approval);
            var firstCompleted = await runtime.ApproveAndResumeAsync(firstPaused.JobId, firstPaused.Approval!.ExactScope);
            Assert.Equal(AgentJobState.Completed, firstCompleted.State);
            Assert.Equal(1, site.MutationCount);
            Assert.True((await product.ReadVerificationPresentationAsync()).Verified);

            // Reproduce the durable shape left by a crash after a browser side effect but before
            // terminal persistence. This trusted-store seam is test-only; product code cannot mint Running state.
            var ambiguousAction = new BrowserAction(BrowserActionKind.Click, BrowserLocator.ByRole("button", "Submit demo mutation"), ExpectedState: "approved mutation complete", Rationale: "Reconcile a crash-ambiguous controlled mutation from observed post-state.");
            var ambiguousId = Guid.NewGuid();
            await runtime.CreateActionAsync(ambiguousId, ambiguousAction);
            var store = new JsonAgentJobStore(Path.Combine(stateDirectory, "jobs.json"));
            var created = await store.GetAsync(ambiguousId);
            Assert.NotNull(created);
            await store.SaveAsync(created! with { State = AgentJobState.Running, UpdatedAt = DateTimeOffset.UtcNow });

            var reconciled = await runtime.TryReconcileAmbiguousAsync(ambiguousId);
            Assert.Equal(BrowserAmbiguousRecoveryStatus.Reconciled, reconciled.Status);
            var durable = await runtime.GetAsync(ambiguousId);
            Assert.NotNull(durable);
            Assert.Equal(AgentJobState.Completed, durable!.State);
            Assert.NotNull(durable.VerifiedStep);

            var afterReconciliation = await product.ReadVerificationPresentationAsync();
            Assert.False(afterReconciliation.Verified);
            Assert.Equal(0, afterReconciliation.ActionCount);
            Assert.Equal(0, afterReconciliation.ApprovalCount);
            Assert.Equal(1, site.MutationCount);
        }
        finally { TryDeleteDirectory(stateDirectory); }
    }

    private static BrowserAction ConsequentialClick() => new(BrowserActionKind.Click, BrowserLocator.ByRole("button", "Submit demo mutation"), Rationale: "Submit the controlled demo mutation after explicit user approval.", Postconditions: new[] { new BrowserPostcondition(BrowserPostconditionKind.VisibleTextContains, Expected: "approved mutation complete") });

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { }
    }

    private sealed class LocalSite : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _server;
        private int _mutationCount;
        private LocalSite(TcpListener listener) { _listener = listener; var endpoint = (IPEndPoint)listener.LocalEndpoint; StartUri = new Uri($"http://127.0.0.1:{endpoint.Port}/"); _server = Task.Run(() => ServeAsync(_shutdown.Token)); }
        public Uri StartUri { get; }
        public int MutationCount => Volatile.Read(ref _mutationCount);
        public static Task<LocalSite> StartAsync() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return Task.FromResult(new LocalSite(listener)); }
        public async ValueTask DisposeAsync() { _shutdown.Cancel(); _listener.Stop(); try { await _server.ConfigureAwait(false); } catch (OperationCanceledException) { } catch (SocketException) when (_shutdown.IsCancellationRequested) { } _shutdown.Dispose(); }
        private async Task ServeAsync(CancellationToken cancellationToken) { while (!cancellationToken.IsCancellationRequested) { TcpClient client; try { client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false); } catch (OperationCanceledException) { break; } catch (SocketException) when (cancellationToken.IsCancellationRequested) { break; } _ = Task.Run(() => HandleAsync(client, cancellationToken), CancellationToken.None); } }
        private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, leaveOpen: true))
            {
                var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
                string? line; while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false))) { }
                var mutation = requestLine.StartsWith("GET /mutate", StringComparison.Ordinal); if (mutation) Interlocked.Increment(ref _mutationCount);
                var body = mutation ? "<html><body><p>approved mutation complete</p></body></html>" : "<html><body><button onclick=\"location.href='/mutate'\">Submit demo mutation</button></body></html>";
                var bytes = Encoding.UTF8.GetBytes(body);
                var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers, cancellationToken).ConfigureAwait(false); await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
