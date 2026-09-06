using Nvidea.Core.Desktop;
using Nvidea.Core.Memory;
using Nvidea.Core.Nebius;

namespace Nvidea.Core.Tests;

public sealed class DesktopSessionControllerTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "nvidea-session-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Emergency_stop_cancels_active_invocation_and_updates_status()
    {
        var inference = new BlockingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var desktop = new DesktopInvocationService(inference, memory);
        using var session = new DesktopSessionController(desktop);

        var invocation = session.InvokeAsync(new DesktopInvocationRequest("Think for a while", new DesktopContext()));
        await inference.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(DesktopAgentState.Thinking, session.Status.State);
        Assert.True(session.EmergencyStop());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation);
        Assert.Equal(DesktopAgentState.Cancelled, session.Status.State);
        Assert.False(session.EmergencyStop());
    }

    [Fact]
    public async Task Concurrent_invocation_is_rejected()
    {
        var inference = new BlockingInferenceClient();
        using var memoryStore = new JsonFileMemoryStore(Path.Combine(_tempDirectory, "memory.json"));
        using var memory = new PersonalMemoryService(memoryStore);
        var desktop = new DesktopInvocationService(inference, memory);
        using var session = new DesktopSessionController(desktop);

        var first = session.InvokeAsync(new DesktopInvocationRequest("First", new DesktopContext()));
        await inference.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.InvokeAsync(
            new DesktopInvocationRequest("Second", new DesktopContext())));

        session.EmergencyStop();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    private sealed class BlockingInferenceClient : IAgentInferenceClient
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        }
    }
}
