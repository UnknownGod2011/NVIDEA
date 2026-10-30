using System.Net;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Nebius;
using Xunit;

namespace Nvidea.WorkerAuth.Tests;

public sealed class NebiusTokenFactoryStreamingTests
{
    [Fact]
    public async Task Streaming_client_requests_event_stream_and_emits_text_deltas()
    {
        string? requestBody = null;
        string? accept = null;
        var deltas = new List<string>();
        using var httpClient = new HttpClient(new StubHandler(request =>
        {
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            accept = request.Headers.Accept.Single().MediaType;
            return EventResponse(
                "data: {\"model\":\"nvidia/test\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"Hello\"},\"finish_reason\":null}]}\n\n"
                + "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\" there!\"},\"finish_reason\":\"stop\"}]}\n\n"
                + "data: [DONE]\n\n");
        }));

        var client = new NebiusTokenFactoryClient(httpClient, TestOptions());
        var completion = await client.CompleteStreamingAsync(
            new AgentRequest([new ChatMessage("user", "hello")]),
            delta =>
            {
                deltas.Add(delta);
                return ValueTask.CompletedTask;
            });

        Assert.Equal("text/event-stream", accept);
        Assert.Equal(new[] { "Hello", " there!" }, deltas);
        Assert.Equal("Hello there!", completion.Content);
        Assert.Equal("stop", completion.FinishReason);
        using var sent = JsonDocument.Parse(requestBody!);
        Assert.True(sent.RootElement.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task Streaming_client_reassembles_fragmented_typed_tool_calls()
    {
        var firstFragment = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    index = 0,
                    delta = new { tool_calls = new[] { new { index = 0, id = "call_1", function = new { name = "open_", arguments = "{\"application\":\"not" } } } },
                    finish_reason = (string?)null
                }
            }
        });
        var secondFragment = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    index = 0,
                    delta = new { tool_calls = new[] { new { index = 0, function = new { name = "application", arguments = "epad\"}" } } } },
                    finish_reason = "tool_calls"
                }
            }
        });
        using var httpClient = new HttpClient(new StubHandler(_ => EventResponse(
            $"data: {firstFragment}\n\n"
            + $"data: {secondFragment}\n\n"
            + "data: [DONE]\n\n")));
        var client = new NebiusTokenFactoryClient(httpClient, TestOptions());

        var completion = await client.CompleteStreamingAsync(
            new AgentRequest([new ChatMessage("user", "Open Notepad")]),
            _ => ValueTask.CompletedTask);

        var call = Assert.Single(completion.ToolCalls);
        Assert.Equal("open_application", call.Name);
        Assert.Equal("{\"application\":\"notepad\"}", call.ArgumentsJson);
    }

    [Fact]
    public async Task Streaming_client_rejects_a_truncated_stream_without_done_marker()
    {
        using var httpClient = new HttpClient(new StubHandler(_ => EventResponse(
            "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"partial\"},\"finish_reason\":null}]}\n\n")));
        var client = new NebiusTokenFactoryClient(httpClient, TestOptions());

        await Assert.ThrowsAsync<InvalidDataException>(() => client.CompleteStreamingAsync(
            new AgentRequest([new ChatMessage("user", "hello")]),
            _ => ValueTask.CompletedTask));
    }

    private static NebiusOptions TestOptions() => new()
    {
        ApiKey = "test-key",
        MaxAttempts = 2,
        RequestTimeout = TimeSpan.FromSeconds(5)
    };

    private static HttpResponseMessage EventResponse(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(send(request));
    }
}
