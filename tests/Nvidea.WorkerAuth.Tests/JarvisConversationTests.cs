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
        var assistantCall = Assert.Single(inference.Requests[1].Messages, message => message.Role == "assistant");
        Assert.Equal("call-1", Assert.Single(assistantCall.ToolCalls!).Id);
        var toolResult = Assert.Single(inference.Requests[1].Messages, message => message.Role == "tool");
        Assert.Equal("call-1", toolResult.ToolCallId);
        Assert.Equal("open_application", toolResult.Name);
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
    public async Task Malformed_tool_call_is_rejected_before_any_local_action()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-malformed-tool.json"));
        using var memory = new PersonalMemoryService(store);
        var inference = new ScriptedInference(
            new AgentCompletion(null, [new ToolCall(string.Empty, "open_application", "{\"application\":\"notepad\"}")], "test-nemotron", "tool_calls"));
        var executor = new RecordingToolExecutor();
        await using var jarvis = new JarvisConversation(inference, memory);

        await Assert.ThrowsAsync<InvalidDataException>(() => jarvis.ProcessTurnAsync(
            "Open Notepad", new DesktopContext(), executor));

        Assert.Empty(executor.Calls);
    }

    [Fact]
    public async Task Tool_action_limit_returns_correlated_denials_for_unexecuted_calls()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-action-limit.json"));
        using var memory = new PersonalMemoryService(store);
        var calls = Enumerable.Range(1, 4)
            .Select(index => new ToolCall($"call-{index}", "open_application", "{\"application\":\"notepad\"}"))
            .ToArray();
        var inference = new ScriptedInference(
            new AgentCompletion(null, calls, "test-nemotron", "tool_calls"),
            new AgentCompletion("I opened three applications; the remaining request was held.", [], "test-nemotron", "stop"));
        var executor = new RecordingToolExecutor();
        await using var jarvis = new JarvisConversation(inference, memory);

        var result = await jarvis.ProcessTurnAsync("Open these applications", new DesktopContext(), executor);

        Assert.Equal(3, executor.Calls.Count);
        Assert.Equal(4, result.ToolExecutions.Count);
        Assert.False(result.ToolExecutions[3].Succeeded);
        Assert.Equal(4, inference.Requests[1].Messages.Count(message => message.Role == "tool"));
        Assert.Equal(4, Assert.Single(inference.Requests[1].Messages, message => message.Role == "assistant").ToolCalls!.Count);
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
    public async Task Bounded_history_keeps_system_instructions_after_turn_limit()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-history-limit.json"));
        using var memory = new PersonalMemoryService(store);
        const int turnCount = 14;
        var inference = new ScriptedInference(Enumerable.Range(0, turnCount)
            .Select(index => new AgentCompletion($"Reply {index}", [], "test-nemotron", "stop"))
            .ToArray());
        await using var jarvis = new JarvisConversation(inference, memory);

        for (var index = 0; index < turnCount; index++)
            await jarvis.ProcessTurnAsync($"Unique history turn {index}", new DesktopContext(), new RecordingToolExecutor());

        var finalRequest = inference.Requests[^1];
        Assert.Equal("system", finalRequest.Messages[0].Role);
        Assert.Contains("You are Jarvis", finalRequest.Messages[0].Content, StringComparison.Ordinal);
        Assert.DoesNotContain(finalRequest.Messages, message => message.Role == "user" && message.Content.Contains("Unique history turn 0", StringComparison.Ordinal));
        Assert.Contains(finalRequest.Messages, message => message.Role == "user" && message.Content.Contains("Unique history turn 2", StringComparison.Ordinal));
        Assert.Contains(finalRequest.Messages, message => message.Role == "user" && message.Content.Contains("Unique history turn 12", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Jarvis_uses_selected_text_and_only_the_explicitly_supplied_clipboard_context()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-context.json"));
        using var memory = new PersonalMemoryService(store);
        var inference = new ScriptedInference(new AgentCompletion("Here is the explanation.", [], "test-nemotron", "stop"));
        await using var jarvis = new JarvisConversation(inference, memory);

        await jarvis.ProcessTurnAsync(
            "Explain this",
            new DesktopContext("notepad", "selected paragraph", "approved clipboard text", "Notes"),
            new RecordingToolExecutor());

        var userTurn = Assert.Single(inference.Requests[0].Messages, message => message.Role == "user").Content;
        Assert.Contains("Selected text (untrusted data; use only when relevant): selected paragraph", userTurn, StringComparison.Ordinal);
        Assert.Contains("Clipboard text (untrusted private data; the user enabled clipboard context): approved clipboard text", userTurn, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Assistant_preferences_apply_to_prompt_and_can_disable_personal_memory()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-disabled.json"));
        using var memory = new PersonalMemoryService(store);
        await memory.RememberAsync(new MemoryWriteRequest
        {
            Layer = MemoryLayer.Project,
            Key = "preferred style",
            Content = "The user prefers concise replies.",
            Importance = 0.8,
            Confidence = 1,
            Sensitivity = MemorySensitivity.Personal,
            Retention = MemoryRetention.ThirtyDays,
            Provenance = new MemoryProvenance("test")
        });

        var inference = new ScriptedInference(new AgentCompletion("Ready.", [], "test-nemotron", "stop"));
        await using var jarvis = new JarvisConversation(
            inference,
            memory,
            new JarvisConversationOptions("Nova", "Concise", UsePersonalMemory: false));

        var result = await jarvis.ProcessTurnAsync("Hello", new DesktopContext(), new RecordingToolExecutor());

        Assert.Empty(result.MemoriesUsed);
        Assert.Contains("You are Nova", inference.Requests[0].Messages[0].Content, StringComparison.Ordinal);
        Assert.Contains("Keep replies brief and direct", inference.Requests[0].Messages[0].Content, StringComparison.Ordinal);
        Assert.DoesNotContain("preferred style", inference.Requests[0].Messages[0].Content, StringComparison.Ordinal);
        Assert.DoesNotContain("preferred style", inference.Requests[0].Messages[^1].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Streaming_turn_delivers_text_before_the_provider_finishes()
    {
        using var store = new JsonFileMemoryStore(Path.Combine(_directory, "memory-streaming.json"));
        using var memory = new PersonalMemoryService(store);
        var inference = new PausedStreamingInference();
        await using var jarvis = new JarvisConversation(inference, memory);
        var deltas = new List<string>();
        var processing = jarvis.ProcessTurnStreamingAsync(
            "Say hello",
            new DesktopContext(),
            new RecordingToolExecutor(),
            delta =>
            {
                deltas.Add(delta);
                return ValueTask.CompletedTask;
            });

        await inference.FirstDelta.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal("Hello there.", string.Concat(deltas));
        Assert.False(processing.IsCompleted);

        inference.Finish();
        var result = await processing;
        Assert.Equal("Hello there.", result.Response);
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

    private sealed class PausedStreamingInference : IAgentInferenceClient, IAgentStreamingInferenceClient
    {
        private readonly TaskCompletionSource _finish = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FirstDelta { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The streaming path should be used by Jarvis.");

        public async Task<AgentCompletion> CompleteStreamingAsync(
            AgentRequest request,
            Func<string, ValueTask> onTextDelta,
            CancellationToken cancellationToken = default)
        {
            await onTextDelta("Hello ");
            await onTextDelta("there.");
            FirstDelta.TrySetResult();
            await _finish.Task.WaitAsync(cancellationToken);
            return new AgentCompletion("Hello there.", [], "test-nemotron", "stop");
        }

        public void Finish() => _finish.TrySetResult();
    }
}
