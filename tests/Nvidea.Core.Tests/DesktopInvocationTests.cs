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
        var result = await service.InvokeAsync(new DesktopInvocationRequest("Summarize what I am looking at", new DesktopContext("Editor", "selected safe text", "private clipboard secret", "notes.txt")));
        Assert.Equal("ok", result.Answer);
        var prompt = Assert.Single(inference.Requests).Messages.Last().Content;
        Assert.Contains("selected safe text", prompt, StringComparison.Ordinal);
        Assert.Contains("intentionally withheld", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private clipboard secret", prompt, StringComparison.Ordinal);
        Assert.True(service.SessionEvidenceSnapshot().Contains(SessionEvidenceKind.NemotronInferenceCompleted));
    }

    [Fact]
    public async Task Chat_includes_clipboard_only_after_local_disclosure_flag()
    {
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var service = new DesktopInvocationService(inference, memory);
        await service.InvokeAsync(new DesktopInvocationRequest("Explain this", new DesktopContext(ClipboardText: "clipboard payload"), AllowClipboardContext: true));
        var prompt = Assert.Single(inference.Requests).Messages.Last().Content;
        Assert.Contains("UNTRUSTED PRIVATE DATA", prompt, StringComparison.Ordinal);
        Assert.Contains("clipboard payload", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Relevant_personal_memory_is_supplied_as_untrusted_data_and_proved_only_after_success()
    {
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        await memory.RememberAsync(new MemoryWriteRequest
        {
            Layer = MemoryLayer.Project, Key = "preferred editor", Content = "Use Visual Studio Code for this project.", Tags = ["editor"],
            Sensitivity = MemorySensitivity.Personal, Retention = MemoryRetention.ThirtyDays, Provenance = new MemoryProvenance("user")
        });
        var service = new DesktopInvocationService(inference, memory);
        await service.InvokeAsync(new DesktopInvocationRequest("Which editor should I use for this project?", new DesktopContext()));
        var prompt = Assert.Single(inference.Requests).Messages.Last().Content;
        Assert.Contains("RELEVANT PERSONAL MEMORY (UNTRUSTED DATA", prompt, StringComparison.Ordinal);
        Assert.Contains("Visual Studio Code", prompt, StringComparison.Ordinal);
        var evidence = service.SessionEvidenceSnapshot();
        Assert.True(evidence.Contains(SessionEvidenceKind.NemotronInferenceCompleted));
        Assert.True(evidence.Contains(SessionEvidenceKind.MemoryInfluencedInvocation));
    }

    [Fact]
    public async Task Reset_session_evidence_clears_only_injected_projection_and_genuine_success_can_reestablish_proof()
    {
        var observedAt = new Queue<DateTimeOffset>(new[]
        {
            new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 16, 12, 5, 0, TimeSpan.Zero)
        });
        var ledger = new SessionEvidenceLedger(() => observedAt.Dequeue());
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "reset-memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var service = new DesktopInvocationService(inference, memory, sessionEvidence: ledger);

        await service.InvokeAsync(new DesktopInvocationRequest("hello", new DesktopContext()));
        var first = Assert.Single(service.SessionEvidenceSnapshot().Entries);
        Assert.Equal(SessionEvidenceKind.NemotronInferenceCompleted, first.Kind);
        Assert.Equal(new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero), first.FirstObservedAt);

        service.ResetSessionEvidence();

        Assert.Empty(service.SessionEvidenceSnapshot().Entries);
        Assert.Empty(ledger.Snapshot().Entries);
        Assert.Single(inference.Requests);

        await service.InvokeAsync(new DesktopInvocationRequest("hello again", new DesktopContext()));

        var fresh = Assert.Single(service.SessionEvidenceSnapshot().Entries);
        Assert.Equal(SessionEvidenceKind.NemotronInferenceCompleted, fresh.Kind);
        Assert.Equal(new DateTimeOffset(2026, 9, 16, 12, 5, 0, TimeSpan.Zero), fresh.FirstObservedAt);
        Assert.Equal(2, inference.Requests.Count);
    }

    [Fact]
    public async Task Failed_inference_records_no_session_proof()
    {
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "failed-memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var service = new DesktopInvocationService(new FailingInferenceClient(), memory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InvokeAsync(new DesktopInvocationRequest("hello", new DesktopContext())));
        Assert.Empty(service.SessionEvidenceSnapshot().Entries);
    }

    [Fact]
    public async Task Auto_research_fails_closed_when_tavily_is_not_configured_and_records_no_proof()
    {
        var inference = new RecordingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var service = new DesktopInvocationService(inference, memory);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InvokeAsync(new DesktopInvocationRequest("research current Nemotron models", new DesktopContext())));
        Assert.Contains("Tavily", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(inference.Requests);
        Assert.Empty(service.SessionEvidenceSnapshot().Entries);
    }

    public void Dispose() { if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true); }

    private sealed class RecordingInferenceClient : IAgentInferenceClient
    {
        public List<AgentRequest> Requests { get; } = [];
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        { Requests.Add(request); return Task.FromResult(new AgentCompletion("ok", [], "test-nemotron", "stop")); }
    }

    private sealed class FailingInferenceClient : IAgentInferenceClient
    {
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("synthetic provider failure");
    }
}