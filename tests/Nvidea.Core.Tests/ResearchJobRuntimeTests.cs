using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchJobRuntimeTests
{
    [Fact]
    public async Task Runtime_persists_local_job_and_resumes_saved_evidence_without_repeating_search()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-research-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var inference = new QueueInferenceClient([
                new AgentCompletion("{\"queries\":[{\"query\":\"durable query\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop")
            ]);
            var provider = new CountingProvider(CreateBatch());
            var runtime = new ResearchJobRuntime(directory, new ResearchEngine(inference, provider), new MemoryAuditTrail());

            var created = await runtime.CreateAsync("What changed?");
            Assert.True(created.CanRunNextStep);
            Assert.Equal(JobExecutionLocation.Local, created.ExecutionLocation);

            var planned = await runtime.RunNextStepAsync(created.JobId);
            Assert.Equal(ResearchJobStage.GatheringEvidence, planned.Stage);
            Assert.Equal(JobExecutionLocation.Local, planned.ExecutionLocation);
            var evidence = await runtime.RunNextStepAsync(created.JobId);
            Assert.Equal(ResearchJobStage.Synthesizing, evidence.Stage);
            Assert.Equal(1, provider.SearchCalls);

            var resumedInference = new QueueInferenceClient([
                new AgentCompletion("Durable answer [src:s1].", [], "model", "stop")
            ]);
            var throwingProvider = new ThrowingProvider();
            var resumed = new ResearchJobRuntime(directory, new ResearchEngine(resumedInference, throwingProvider), new MemoryAuditTrail());

            var statuses = await resumed.ListAsync();
            Assert.Contains(statuses, status =>
                status.JobId == created.JobId
                && status.Stage == ResearchJobStage.Synthesizing
                && status.ExecutionLocation == JobExecutionLocation.Local);

            var completed = await resumed.RunNextStepAsync(created.JobId);
            Assert.Equal(ResearchJobStage.Completed, completed.Stage);
            Assert.Equal(JobExecutionLocation.Local, completed.ExecutionLocation);
            Assert.Equal(0, throwingProvider.SearchCalls);

            var report = await resumed.ReadCompletedReportAsync(created.JobId);
            Assert.Equal("Durable answer [src:s1].", report.AnswerMarkdown);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Cancelled_job_is_terminal_and_cannot_advertise_resume()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-research-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var runtime = new ResearchJobRuntime(
                directory,
                new ResearchEngine(new QueueInferenceClient([]), new CountingProvider(CreateBatch())),
                new MemoryAuditTrail());

            var created = await runtime.CreateAsync("Cancel me");
            var cancelled = await runtime.CancelAsync(created.JobId);

            Assert.Equal(ResearchJobStage.Cancelled, cancelled.Stage);
            Assert.True(cancelled.IsTerminal);
            Assert.False(cancelled.CanRunNextStep);
            Assert.False(cancelled.CanCancel);
            Assert.Equal(JobExecutionLocation.Local, cancelled.ExecutionLocation);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static ResearchBatch CreateBatch()
    {
        var url = new Uri("https://docs.example.com/research");
        return new ResearchBatch(
            [new ResearchSource("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), "Evidence", 0.9, "durable query", DateTimeOffset.UtcNow)],
            [new ResearchCitation("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), "durable query", DateTimeOffset.UtcNow, null)],
            2,
            []);
    }

    private sealed class QueueInferenceClient(IEnumerable<AgentCompletion> completions) : IAgentInferenceClient
    {
        private readonly Queue<AgentCompletion> _completions = new(completions);
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(_completions.Dequeue());
    }

    private sealed class CountingProvider(ResearchBatch batch) : IResearchProvider
    {
        public int SearchCalls { get; private set; }
        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            return Task.FromResult(batch);
        }
    }

    private sealed class ThrowingProvider : IResearchProvider
    {
        public int SearchCalls { get; private set; }
        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default)
        {
            SearchCalls++;
            throw new InvalidOperationException("Search must not repeat after an evidence checkpoint.");
        }
    }

    private sealed class MemoryAuditTrail : IAuditTrail
    {
        private readonly List<AuditEvent> _events = [];
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            _events.Add(auditEvent);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AuditEvent>> ReadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>(_events.ToArray());
    }
}
