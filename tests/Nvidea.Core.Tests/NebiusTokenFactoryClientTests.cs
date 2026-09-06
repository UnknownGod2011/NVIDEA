using System.Net;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class NebiusTokenFactoryClientTests
{
    [Fact]
    public void Router_falls_back_to_verified_super_when_optional_tiers_are_unset()
    {
        var options = TestOptions();
        var router = new NemotronModelRouter(options);

        Assert.Equal(NebiusOptions.VerifiedNemotronSuperModel, router.Resolve(WorkloadKind.Fast));
        Assert.Equal(NebiusOptions.VerifiedNemotronSuperModel, router.Resolve(WorkloadKind.Standard));
        Assert.Equal(NebiusOptions.VerifiedNemotronSuperModel, router.Resolve(WorkloadKind.Deep));
    }

    [Fact]
    public void Router_uses_explicit_tier_overrides_without_guessing_model_ids()
    {
        var options = TestOptions() withOverrides;

        static NebiusOptions withOverrides => new()
        {
            ApiKey = "test-key",
            FastModel = "nvidia/verified-fast-model",
            StandardModel = NebiusOptions.VerifiedNemotronSuperModel,
            DeepModel = "nvidia/verified-deep-model"
        };

        var router = new NemotronModelRouter(options);
        Assert.Equal("nvidia/verified-fast-model", router.Resolve(WorkloadKind.Fast));
        Assert.Equal("nvidia/verified-deep-model", router.Resolve(WorkloadKind.Deep));
    }

    [Fact]
    public async Task Client_sends_bearer_auth_messages_tools_and_verified_model()
    {
        string? requestBody = null;
        string? authorization = null;
        var handler = new StubHandler(request =>
        {
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            authorization = request.Headers.Authorization?.ToString();
            return JsonResponse(HttpStatusCode.OK, """
                {
                  "model":"nvidia/nemotron-3-super-120b-a12b",
                  "choices":[{
                    "finish_reason":"tool_calls",
                    "message":{
                      "content":null,
                      "tool_calls":[{
                        "id":"call_1",
                        "type":"function",
                        "function":{"name":"search_memory","arguments":"{\"query\":\"hackathon\"}"}
                      }]
                    }
                  }]
                }
                """);
        });

        using var httpClient = new HttpClient(handler);
        var client = new NebiusTokenFactoryClient(httpClient, TestOptions());
        var toolSchema = JsonSerializer.Deserialize<JsonObject>("""{"type":"object","properties":{"query":{"type":"string"}},"required":["query"]}""")!;

        var completion = await client.CompleteAsync(new AgentRequest(
            [new ChatMessage("user", "Find what I remember about the hackathon")],
            Tools: [new ToolDefinition("search_memory", "Search personal memory", toolSchema)]));

        Assert.Equal("Bearer test-key", authorization);
        Assert.Single(completion.ToolCalls);
        Assert.Equal("search_memory", completion.ToolCalls[0].Name);
        Assert.NotNull(requestBody);

        using var sent = JsonDocument.Parse(requestBody!);
        Assert.Equal(NebiusOptions.VerifiedNemotronSuperModel, sent.RootElement.GetProperty("model").GetString());
        Assert.Equal("user", sent.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("function", sent.RootElement.GetProperty("tools")[0].GetProperty("type").GetString());
        Assert.Equal("auto", sent.RootElement.GetProperty("tool_choice").GetString());
        Assert.Equal(1.0, sent.RootElement.GetProperty("temperature").GetDouble());
        Assert.Equal(0.95, sent.RootElement.GetProperty("top_p").GetDouble());
    }

    [Fact]
    public async Task Client_retries_transient_server_error_then_succeeds()
    {
        var calls = 0;
        var handler = new StubHandler(_ =>
        {
            calls++;
            return calls == 1
                ? JsonResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"busy\"}")
                : JsonResponse(HttpStatusCode.OK, """
                    {"choices":[{"finish_reason":"stop","message":{"content":"ready"}}]}
                    """);
        });

        using var httpClient = new HttpClient(handler);
        var client = new NebiusTokenFactoryClient(httpClient, TestOptions());
        var completion = await client.CompleteAsync(new AgentRequest([new ChatMessage("user", "hello")]));

        Assert.Equal(2, calls);
        Assert.Equal("ready", completion.Content);
    }

    [Fact]
    public async Task Client_does_not_retry_non_transient_auth_failure()
    {
        var calls = 0;
        var handler = new StubHandler(_ =>
        {
            calls++;
            return JsonResponse(HttpStatusCode.Unauthorized, "{\"error\":\"invalid key\"}");
        });

        using var httpClient = new HttpClient(handler);
        var client = new NebiusTokenFactoryClient(httpClient, TestOptions());

        var error = await Assert.ThrowsAsync<NebiusApiException>(() =>
            client.CompleteAsync(new AgentRequest([new ChatMessage("user", "hello")])));

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Equal(1, calls);
        Assert.DoesNotContain("test-key", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("test-key", error.ResponseExcerpt, StringComparison.Ordinal);
    }

    [Fact]
    public void Options_reject_non_https_or_non_nebius_endpoint()
    {
        var options = new NebiusOptions
        {
            ApiKey = "test-key",
            BaseUri = new Uri("http://localhost:1234/v1/")
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    private static NebiusOptions TestOptions() => new()
    {
        ApiKey = "test-key",
        MaxAttempts = 2,
        RequestTimeout = TimeSpan.FromSeconds(5)
    };

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
