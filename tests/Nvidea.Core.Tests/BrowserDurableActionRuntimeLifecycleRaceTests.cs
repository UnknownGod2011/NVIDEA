using Nvidea.Core.Capabilities;
using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Security;

namespace Nvidea.Core.Tests;

/// <summary>
/// Behavioral coverage for the real BrowserDurableActionRuntime gate at the exact
/// evidence-clear boundary. The fixture deliberately uses a non-browser handler so
/// it cannot mint browser verification evidence; the test is about isolation of the
/// transient clear/execute state, not about weakening publication validation.
/// </summary>
public sealed class BrowserDurableActionRuntimeLifecycleRaceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "nvidea-browser-durable-race-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ExecutionEvidenceClear_BlocksJudgeReadAndNewAdmission_UntilTransitionSettles()
    {
        Directory.CreateDirectory(_root);
        var receiptPath = Path.Combine(_root, "receipt.protected");
        await File.WriteAllTextAsync(receiptPath, "stale-green-evidence");

        var store = new InMemoryStore();
        var audit = new InMemoryAudit();
        var handler = new CompletingHandler();
        var jobs = new ResumableJobOrchestrator(
            store,
            new ConservativeJobExecutionPolicy(),
            audit,
            new[] { handler });
        var verification = BrowserVerificationRuntime.Create(receiptPath, new ReversibleTestProtector());
        var observerEntered = NewSignal();
        var releaseObserver = NewSignal();
        var observer = new PausingExecutionClearObserver(observerEntered, releaseObserver);
        var runtime = BrowserDurableActionRuntime.CreateForTesting(jobs, verification, observer);

        var definition = Definition();
        var jobA = await jobs.CreateAsync(definition);

        var runA = runtime.RunNextStepAsync(jobA.JobId);
        await observerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(File.Exists(receiptPath));
        Assert.Equal(0, handler.ExecutionCount);
        Assert.False(runA.IsCompleted);

        var judgeRead = runtime.ReadVerificationPresentationAsync();
        var jobBId = Guid.NewGuid();
        var admitB = runtime.CreateAsync(
            jobBId,
            definition,
            new AgentJobCheckpoint("queued", null, DateTimeOffset.UtcNow));

        // Both operations are queued behind the same transition gate while A is paused after
        // evidence invalidation. Neither may observe or mutate the half-settled lifecycle.
        await Task.Delay(50);
        Assert.False(judgeRead.IsCompleted);
        Assert.False(admitB.IsCompleted);
        Assert.Null(await store.GetAsync(jobBId));
        Assert.Equal(0, handler.ExecutionCount);

        releaseObserver.TrySetResult();

        var completedA = await runA.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(AgentJobState.Completed, completedA.State);
        Assert.Equal(1, handler.ExecutionCount);

        var presentation = await judgeRead.WaitAsync(TimeSpan.FromSeconds(5));
        var admittedB = await admitB.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(jobBId, admittedB.JobId);
        Assert.NotNull(await store.GetAsync(jobBId));
        Assert.False(File.Exists(receiptPath));
        Assert.False(presentation.Verified);
    }

    private static AgentJobDefinition Definition() =>
        new(
            "race.fixture",
            "browser.race.fixture",
            new HashSet<DataPermission> { DataPermission.BrowserRead },
            CapabilityRiskLevel.Medium,
            ContainsPrivateOsData: true,
            BenefitsFromBackgroundExecution: false);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
        }
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class PausingExecutionClearObserver : IBrowserVerificationLifecycleObserver
    {
        private readonly TaskCompletionSource _entered;
        private readonly TaskCompletionSource _release;

        internal PausingExecutionClearObserver(TaskCompletionSource entered, TaskCompletionSource release)
        {
            _entered = entered;
            _release = release;
        }

        public async ValueTask ObserveAsync(
            BrowserVerificationLifecycleStage stage,
            CancellationToken cancellationToken)
        {
            if (stage != BrowserVerificationLifecycleStage.ExecutionEvidenceCleared)
                return;

            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class CompletingHandler : IAgentJobHandler
    {
        private int _executionCount;
        public string JobType => "race.fixture";
        internal int ExecutionCount => Volatile.Read(ref _executionCount);

        public Task<JobStepResult> ExecuteStepAsync(
            AgentJobRecord job,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _executionCount);
            return Task.FromResult(new JobStepResult(
                Completed: true,
                CheckpointStep: "completed",
                CheckpointPayload: null));
        }
    }

    private sealed class InMemoryStore : IAgentJobStore
    {
        private readonly Dictionary<Guid, AgentJobRecord> _records = new();
        private readonly object _sync = new();

        public Task<AgentJobRecord?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            lock (_sync)
                return Task.FromResult(_records.TryGetValue(jobId, out var value) ? value : null);
        }

        public Task<IReadOnlyList<AgentJobRecord>> ListAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
                return Task.FromResult<IReadOnlyList<AgentJobRecord>>(_records.Values.ToArray());
        }

        public Task SaveAsync(AgentJobRecord record, CancellationToken cancellationToken = default)
        {
            lock (_sync)
                _records[record.JobId] = record;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAudit : IAuditTrail
    {
        private readonly List<AuditEvent> _events = new();
        private readonly object _sync = new();

        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            lock (_sync)
                _events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
                return Task.FromResult<IReadOnlyList<AuditEvent>>(_events.ToArray());
        }
    }

    private sealed class ReversibleTestProtector : ILocalStateProtector
    {
        public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose) => plaintext.ToArray();
        public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose) => protectedData.ToArray();
    }
}
