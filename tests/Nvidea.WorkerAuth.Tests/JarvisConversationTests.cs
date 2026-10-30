using System.Text.Json;
using Nvidea.Core.Desktop;
using Nvidea.Core.Memory;
using Nvidea.Core.Nebius;
using Xunit;

namespace Nvidea.WorkerAuth.Tests;

public sealed class JarvisConversationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "nvidea-jarvis-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Typed_tool_receipt_is_required_before_the_spoken_follow_up()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory.json"));
        using var memory = new PersonalMemoryService(store);
        var inference = new ScriptedInference(
            new AgentCompletion(null, [new ToolCall("call-1", "open_application", "{\"application\":\"notepad\"}")], "test-nemotron", "tool_calls"),
            new AgentCompletion("Notepad is ready.", [], "test-nemotron", "stop"));
        var executor = new RecordingToolExecutor();
        await using var jarvis = new JarvisConversation(inference, memory);

        var result = await jarvis.ProcessTurnAsync("Open Notepad", new DesktopContext("explorer"), executor);

        Assert.Equal("Notepad is ready.", result.Response);
        Assert.Equal("open_application", Assert.Single(executor.Calls));
        Assert.Equal(2, inference.Requests.Count);
        Assert.Contains(inference.Requests[1].Messages, message => message.Role == "tool" && message.Content.Contains("opened safely", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unsupported_tool_is_rejected_without_delegating_to_windows()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-unsupported.json"));
        using var memory = new PersonalMemoryService(store);
        var inference = new ScriptedInference(
            new AgentCompletion(null, [new ToolCall("call-1", "run_shell", "{\"command\":\"whoami\"}")], "test-nemotron", "tool_calls"),
            new AgentCompletion("I cannot run that action.", [], "test-nemotron", "stop"));
        var executor = new RecordingToolExecutor();
        await using var jarvis = new JarvisConversation(inference, memory);

        var result = await jarvis.ProcessTurnAsync("run a command", new DesktopContext(), executor);

        Assert.Empty(executor.Calls);
        Assert.False(Assert.Single(result.ToolExecutions).Succeeded);
        Assert.Equal("I cannot run that action.", result.Response);
    }

    [Fact]
    public async Task Conversation_keeps_bounded_prior_turn_context_for_follow_ups()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-follow-up.json"));
        using var memory = new PersonalMemoryService(store);
        var inference = new ScriptedInference(
            new AgentCompletion("I will call you Tanush.", [], "test-nemotron", "stop"),
            new AgentCompletion("You asked me to call you Tanush.", [], "test-nemotron", "stop"));
        await using var jarvis = new JarvisConversation(inference, memory);
        var executor = new RecordingToolExecutor();

        await jarvis.ProcessTurnAsync("My name is Tanush", new DesktopContext(), executor);
        var followUp = await jarvis.ProcessTurnAsync("What should you call me?", new DesktopContext(), executor);

        Assert.Equal("You asked me to call you Tanush.", followUp.Response);
        Assert.Contains(inference.Requests[1].Messages, message => message.Role == "assistant" && message.Content.Contains("Tanush", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unconfigured_inference_fails_only_at_the_cloud_reasoning_boundary()
    {
        var client = new UnconfiguredInferenceClient();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.CompleteAsync(new AgentRequest([new ChatMessage("user", "hello")])));
        Assert.Contains("NEBIUS_API_KEY", error.Message, StringComparison.Ordinal);
    }

    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }

    private sealed class ScriptedInference(params AgentCompletion[] completions) : IAgentInferenceClient
    {
        private readonly Queue<AgentCompletion> _completions = new(completions);
        public List<AgentRequest> Requests { get; } = [];
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_completions.Dequeue());
        }
    }

    private sealed class RecordingToolExecutor : IJarvisToolExecutor
    {
        public List<string> Calls { get; } = [];
        public Task<JarvisToolExecution> ExecuteAsync(string name, JsonElement arguments, CancellationToken cancellationToken = default)
        {
            Calls.Add(name);
            return Task.FromResult(new JarvisToolExecution(name, true, "opened safely"));
        }
    }
}
