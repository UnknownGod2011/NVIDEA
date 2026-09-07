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

    private sealed class SequenceInferenceClient(params string[] responses) : IAgentInferenceClient
    {
        private readonly Queue<string> _responses = new(responses);
        private readonly List<string> _responseSchemas = new();

        public int RequestCount { get; private set; }
        public IReadOnlyList<string> ResponseSchemas => _responseSchemas;

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_responses.Count == 0)
                throw new InvalidOperationException("The deterministic planner fixture has no response remaining.");

            RequestCount++;
            _responseSchemas.Add(request.ResponseJsonSchema ?? string.Empty);
            return Task.FromResult(new AgentCompletion(
                _responses.Dequeue(),
                Array.Empty<ToolCall>(),
                "nemotron-browser-integration-fixture",
                "stop"));
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
                    await WriteResponseAsync(
                        stream,
                        "200 OK",
                        "text/html; charset=utf-8",
                        CompletedHtml,
                        cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(path, "/favicon.ico", StringComparison.Ordinal))
                {
                    await WriteResponseAsync(stream, "204 No Content", "text/plain", string.Empty, cancellationToken).ConfigureAwait(false);
                    return;
                }

                await WriteResponseAsync(stream, "200 OK", "text/html; charset=utf-8", InitialHtml, cancellationToken).ConfigureAwait(false);
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

        private const string InitialHtml = """
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
                <form method="post" action="/mutate">
                  <button id="submit-demo" type="submit">Submit demo mutation</button>
                </form>
              </main>
            </body>
            </html>
            """;

        private const string CompletedHtml = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <title>NVIDEA browser approval harness</title>
            </head>
            <body>
              <main>
                <h1>Controlled browser approval harness</h1>
                <p id="state">approved mutation complete</p>
              </main>
            </body>
            </html>
            """;
    }
}
