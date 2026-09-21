using System.Net;
using System.Net.Sockets;
using System.Text;
using Nvidea.Core.Browser;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
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
            var product = runtime.CreateProductRuntime();

            var action = new BrowserAction(
                BrowserActionKind.Click,
                BrowserLocator.ByRole("button", "Submit demo mutation"),
                Rationale: "Submit the controlled demo mutation after explicit user approval.",
                Postconditions: new[]
                {
                    new BrowserPostcondition(
                        BrowserPostconditionKind.VisibleTextContains,
                        Expected: "approved mutation complete")
                });

            var paused = await runtime.StartActionAsync(action);

            Assert.Equal(AgentJobState.WaitingForApproval, paused.State);
            Assert.NotNull(paused.Approval);
            Assert.False(string.IsNullOrWhiteSpace(paused.Approval!.ExactScope));
            Assert.Equal(0, site.MutationCount);
            var pausedVerification = await product.ReadVerificationPresentationAsync();
            Assert.False(pausedVerification.IsVerified);

            var jobStore = new JsonAgentJobStore(Path.Combine(stateDirectory, "jobs.json"));
            var persistedWhilePaused = await jobStore.GetAsync(paused.JobId);
            Assert.NotNull(persistedWhilePaused);
            Assert.Equal(AgentJobState.WaitingForApproval, persistedWhilePaused!.State);
            Assert.Equal(paused.Approval.ExactScope, persistedWhilePaused.ApprovalScope);
            Assert.NotNull(persistedWhilePaused.Checkpoint);
            Assert.Contains("postconditions", persistedWhilePaused.Checkpoint!.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("approved mutation complete", persistedWhilePaused.Checkpoint.Payload ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("grant", persistedWhilePaused.Checkpoint.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", persistedWhilePaused.Checkpoint.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            var completed = await runtime.ApproveAndResumeAsync(paused.JobId, paused.Approval.ExactScope);

            Assert.Equal(AgentJobState.Completed, completed.State);
            Assert.Equal(1, site.MutationCount);
            Assert.NotNull(completed.VerifiedStep);
            Assert.Contains("typed browser postcondition", completed.VerifiedStep!.VerificationDetail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            var completedVerification = await product.ReadVerificationPresentationAsync();
            Assert.True(completedVerification.IsVerified);
            Assert.Equal(paused.JobId, completedVerification.JobId);
            Assert.True(completedVerification.ApprovalObserved);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                runtime.ApproveAndResumeAsync(paused.JobId, paused.Approval.ExactScope));

            Assert.Equal(1, site.MutationCount);

            var persistedAfterCompletion = await jobStore.GetAsync(paused.JobId);
            Assert.NotNull(persistedAfterCompletion);
            Assert.Equal(AgentJobState.Completed, persistedAfterCompletion!.State);
            Assert.Null(persistedAfterCompletion.ApprovalScope);
            Assert.NotNull(persistedAfterCompletion.Checkpoint);
            Assert.DoesNotContain("grant", persistedAfterCompletion.Checkpoint!.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", persistedAfterCompletion.Checkpoint.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDeleteDirectory(stateDirectory);
        }
    }

    [BrowserIntegrationFact]
    public async Task NemotronPlanner_TypedPostconditions_SurviveDurableGoalAndRealChromiumVerification()
    {
        await using var site = await LocalBrowserTestSite.StartAsync();
        var stateDirectory = Path.Combine(Path.GetTempPath(), "nvidea-browser-planner-it", Guid.NewGuid().ToString("N"));

        try
        {
            await using var runtime = await BrowserHostRuntime.CreateAsync(
                stateDirectory,
                new BrowserHostOptions(
                    site.StartUri,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { site.StartUri.IdnHost },
                    Headless: true));

            var inference = new SequenceInferenceClient(
                """
                {
                  "decision": "act",
                  "reason": "Submit the controlled form and verify the resulting page state.",
                  "action": {
                    "kind": "click",
                    "locator_kind": "role_and_name",
                    "locator_value": "Submit demo mutation",
                    "locator_name": "Submit demo mutation",
                    "locator_role": "button",
                    "value": null,
                    "destination": null,
                    "postconditions": [
                      {
                        "kind": "visible_text_contains",
                        "expected": "approved mutation complete",
                        "locator_kind": null,
                        "locator_value": null,
                        "locator_name": null,
                        "locator_role": null,
                        "expected_boolean": null
                      }
                    ],
                    "rationale": "This is the requested controlled submission."
                  }
                }
                """,
                """
                {
                  "decision": "complete",
                  "reason": "The controlled mutation is verified complete.",
                  "action": null
                }
                """);

            var planner = new NemotronBrowserPlanner(inference);
            var sessionStorePath = Path.Combine(stateDirectory, "goal-sessions.json");
            var sessionStore = new JsonBrowserGoalSessionStore(sessionStorePath);
            var agent = new BrowserGoalAgent(runtime, planner, sessionStore);
            var session = BrowserGoalSession.Create("Submit the controlled demo mutation", maxActions: 3);

            var paused = await agent.RunUntilPauseAsync(session);

            Assert.Equal(BrowserGoalStatus.WaitingForApproval, paused.Status);
            Assert.Equal(0, site.MutationCount);
            Assert.NotNull(paused.PendingJobId);
            Assert.NotNull(paused.PendingAction);
            Assert.Null(paused.PendingAction!.ExpectedState);
            Assert.Single(paused.PendingAction.Postconditions!);
            Assert.Equal(
                BrowserPostconditionKind.VisibleTextContains,
                paused.PendingAction.Postconditions![0].Kind);
            Assert.False(string.IsNullOrWhiteSpace(paused.PendingExactScope));

            var jobStore = new JsonAgentJobStore(Path.Combine(stateDirectory, "jobs.json"));
            var durableChild = await jobStore.GetAsync(paused.PendingJobId!.Value);
            Assert.NotNull(durableChild);
            Assert.Equal(AgentJobState.WaitingForApproval, durableChild!.State);
            Assert.Equal(paused.PendingExactScope, durableChild.ApprovalScope);
            Assert.NotNull(durableChild.Checkpoint);
            Assert.Contains("postconditions", durableChild.Checkpoint!.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("approved mutation complete", durableChild.Checkpoint.Payload ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("grant", durableChild.Checkpoint.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("bearer", durableChild.Checkpoint.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            var completed = await agent.ApproveAndContinueAsync(paused, paused.PendingExactScope!);

            Assert.Equal(BrowserGoalStatus.Completed, completed.Status);
            Assert.Equal(1, site.MutationCount);
            Assert.Equal(2, completed.PlannerTurnCount);
            Assert.Single(completed.VerifiedSteps!);
            Assert.Contains(
                "typed browser postcondition",
                completed.VerifiedSteps![0].VerificationDetail ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);

            var restored = await sessionStore.GetAsync(completed.SessionId);
            Assert.NotNull(restored);
            Assert.Equal(BrowserGoalStatus.Completed, restored!.Status);
            Assert.Null(restored.PendingAction);
            Assert.Single(restored.VerifiedSteps!);
            Assert.Equal(completed.VerifiedSteps![0].JobId, restored.VerifiedSteps![0].JobId);

            var completedChild = await jobStore.GetAsync(paused.PendingJobId.Value);
            Assert.NotNull(completedChild);
            Assert.Equal(AgentJobState.Completed, completedChild!.State);
            Assert.Null(completedChild.ApprovalScope);
            Assert.DoesNotContain("grant", completedChild.Checkpoint?.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("bearer", completedChild.Checkpoint?.Payload ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(2, inference.RequestCount);
            Assert.All(inference.ResponseSchemas, schema =>
            {
                Assert.Contains("postconditions", schema, StringComparison.Ordinal);
                Assert.DoesNotContain("expected_state", schema, StringComparison.Ordinal);
            });
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

    private sealed class SequenceInferenceClient : IInferenceClient
    {
        private readonly Queue<string> _responses;

        public SequenceInferenceClient(params string[] responses) => _responses = new Queue<string>(responses);

        public int RequestCount { get; private set; }
        public List<string> ResponseSchemas { get; } = new();

        public Task<InferenceResponse> CompleteAsync(InferenceRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            ResponseSchemas.Add(request.ResponseJsonSchema ?? string.Empty);
            if (_responses.Count == 0)
                throw new InvalidOperationException("No inference response remains for the integration test.");

            return Task.FromResult(new InferenceResponse(_responses.Dequeue(), "test-nemotron", null));
        }
    }

    private sealed class LocalBrowserTestSite : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly Task _server;
        private int _mutationCount;

        private LocalBrowserTestSite(TcpListener listener)
        {
            _listener = listener;
            var endpoint = (IPEndPoint)listener.LocalEndpoint;
            StartUri = new Uri($"http://127.0.0.1:{endpoint.Port}/");
            _server = Task.Run(() => ServeAsync(_shutdown.Token));
        }

        public Uri StartUri { get; }
        public int MutationCount => Volatile.Read(ref _mutationCount);

        public static Task<LocalBrowserTestSite> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return Task.FromResult(new LocalBrowserTestSite(listener));
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            _listener.Stop();
            try { await _server.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (SocketException) when (_shutdown.IsCancellationRequested) { }
            _shutdown.Dispose();
        }

        private async Task ServeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
                catch (SocketException) when (cancellationToken.IsCancellationRequested) { break; }

                _ = Task.Run(() => HandleAsync(client, cancellationToken), CancellationToken.None);
            }
        }

        private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, leaveOpen: true))
            {
                var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false))) { }

                var isMutation = requestLine.StartsWith("GET /mutate", StringComparison.Ordinal);
                if (isMutation)
                    Interlocked.Increment(ref _mutationCount);

                var body = isMutation
                    ? "<html><body><p>approved mutation complete</p></body></html>"
                    : "<html><body><button onclick=\"location.href='/mutate'\">Submit demo mutation</button></body></html>";
                var bytes = Encoding.UTF8.GetBytes(body);
                var headers = Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
