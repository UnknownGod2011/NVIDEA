using Nvidea.Core.Desktop;
using Nvidea.Core.Memory;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class DesktopInvocationTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "nvidea-desktop-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Chat_withholds_clipboard_unless_explicitly_allowed()
    {
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var service = new DesktopInvocationService(inference, memory);

        var result = await service.InvokeAsync(new DesktopInvocationRequest(
            "Summarize what I am looking at",
            new DesktopContext("Editor", "selected safe text", "private clipboard secret", "notes.txt")));

        Assert.Equal("ok", result.Answer);
        var prompt = Assert.Single(inference.Requests).Messages.Last().Content;
        Assert.Contains("selected safe text", prompt, StringComparison.Ordinal);
        Assert.Contains("intentionally withheld", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private clipboard secret", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chat_includes_clipboard_only_after_local_disclosure_flag()
    {
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var service = new DesktopInvocationService(inference, memory);

        await service.InvokeAsync(new DesktopInvocationRequest(
            "Explain this",
            new DesktopContext(ClipboardText: "clipboard payload"),
            AllowClipboardContext: true));

        var prompt = Assert.Single(inference.Requests).Messages.Last().Content;
        Assert.Contains("UNTRUSTED PRIVATE DATA", prompt, StringComparison.Ordinal);
        Assert.Contains("clipboard payload", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Relevant_personal_memory_is_supplied_as_untrusted_data()
    {
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        await memory.RememberAsync(new MemoryWriteRequest
        {
            Layer = MemoryLayer.Project,
            Key = "preferred editor",
            Content = "Use Visual Studio Code for this project.",
            Tags = ["editor"],
            Sensitivity = MemorySensitivity.Personal,
            Retention = MemoryRetention.ThirtyDays,
            Provenance = new MemoryProvenance("user")
        });

        var service = new DesktopInvocationService(inference, memory);
        await service.InvokeAsync(new DesktopInvocationRequest(
            "Which editor should I use for this project?",
            new DesktopContext()));

        var prompt = Assert.Single(inference.Requests).Messages.Last().Content;
        Assert.Contains("RELEVANT PERSONAL MEMORY (UNTRUSTED DATA", prompt, StringComparison.Ordinal);
        Assert.Contains("Visual Studio Code", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Auto_research_fails_closed_when_tavily_is_not_configured()
    {
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var service = new DesktopInvocationService(inference, memory);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InvokeAsync(
            new DesktopInvocationRequest("research current Nemotron models", new DesktopContext())));

        Assert.Contains("Tavily", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(inference.Requests);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private sealed class RecordingInferenceClient : IAgentInferenceClient
    {
        public List<AgentRequest> Requests { get; } = [];

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new AgentCompletion("ok", [], "test-nemotron", "stop"));
        }
    }
}
