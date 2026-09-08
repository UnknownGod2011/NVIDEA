using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchJobHandlerTests
{
    [Fact]
    public async Task Research_job_checkpoints_plan_evidence_and_final_report_without_repeating_remote_work()
    {
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"Nebius NVIDIA research\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop"),
            new AgentCompletion("Supported answer [src:s1].", [], "model", "stop")
        ]);
        var provider = new CountingProvider(CreateBatch());
        var handler = new ResearchJobHandler(new ResearchEngine(inference, provider));
        var job = CreateJob(ResearchJobHandler.CreateInitialCheckpoint("What is new?"));

        var planned = await handler.ExecuteStepAsync(job);
        Assert.False(planned.Completed);
        Assert.Equal(ResearchJobHandler.PlannedStep, planned.CheckpointStep);
        Assert.Equal(1, inference.Requests.Count);
        Assert.Equal(0, provider.SearchCalls);

        job = job with { Checkpoint = new AgentJobCheckpoint(planned.CheckpointStep!, planned.CheckpointPayload, DateTimeOffset.UtcNow) };
        var evidence = await handler.ExecuteStepAsync(job);
        Assert.False(evidence.Completed);
        Assert.Equal(ResearchJobHandler.EvidenceStep, evidence.CheckpointStep);
        Assert.Equal(1, provider.SearchCalls);
        Assert.Contains("providerCreditsUsed", evidence.CheckpointPayload!, StringComparison.Ordinal);
        Assert.Contains("qualityBySourceId", evidence.CheckpointPayload!, StringComparison.Ordinal);

        job = job with { Checkpoint = new AgentJobCheckpoint(evidence.CheckpointStep!, evidence.CheckpointPayload, DateTimeOffset.UtcNow) };
        var completed = await handler.ExecuteStepAsync(job);
        Assert.True(completed.Completed);
        Assert.Equal(ResearchJobHandler.CompletedStep, completed.CheckpointStep);
        Assert.Equal(1, provider.SearchCalls);
        Assert.Equal(2, inference.Requests.Count);
    }

    [Fact]
    public async Task Resume_from_evidence_checkpoint_does_not_call_research_provider_again()
    {
        var initialInference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"checkpoint query\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop")
        ]);
        var provider = new CountingProvider(CreateBatch());
        var firstHandler = new ResearchJobHandler(new ResearchEngine(initialInference, provider));
        var job = CreateJob(ResearchJobHandler.CreateInitialCheckpoint("Resume test"));

        var planned = await firstHandler.ExecuteStepAsync(job);
        job = job with { Checkpoint = new AgentJobCheckpoint(planned.CheckpointStep!, planned.CheckpointPayload, DateTimeOffset.UtcNow) };
        var evidence = await firstHandler.ExecuteStepAsync(job);
        Assert.Equal(1, provider.SearchCalls);

        var resumedInference = new QueueInferenceClient([
            new AgentCompletion("Resumed answer [src:s1].", [], "model", "stop")
        ]);
        var throwingProvider = new ThrowingProvider();
        var resumedHandler = new ResearchJobHandler(new ResearchEngine(resumedInference, throwingProvider));
        var resumedJob = job with
        {
            Checkpoint = new AgentJobCheckpoint(evidence.CheckpointStep!, evidence.CheckpointPayload, DateTimeOffset.UtcNow)
        };

        var completed = await resumedHandler.ExecuteStepAsync(resumedJob);

        Assert.True(completed.Completed);
        Assert.Equal(ResearchJobHandler.CompletedStep, completed.CheckpointStep);
        Assert.Equal(1, resumedInference.Requests.Count);
        Assert.Equal(0, throwingProvider.SearchCalls);
    }

    [Fact]
    public async Task Oversized_checkpoint_is_rejected_before_remote_work()
    {
        var inference = new QueueInferenceClient([]);
        var provider = new CountingProvider(CreateBatch());
        var handler = new ResearchJobHandler(new ResearchEngine(inference, provider));
        var oversized = new string('x', (2 * 1024 * 1024) + 1);
        var job = CreateJob(new AgentJobCheckpoint(ResearchJobHandler.RequestedStep, oversized, DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteStepAsync(job));
        Assert.Empty(inference.Requests);
        Assert.Equal(0, provider.SearchCalls);
    }

    private static AgentJobRecord CreateJob(AgentJobCheckpoint checkpoint)
    {
        var definition = new AgentJobDefinition(
            ResearchJobHandler.Type,
            "research.deep",
            new HashSet<DataPermission>(),
            CapabilityRiskLevel.Low,
            ContainsPrivateOsData: false,
            BenefitsFromBackgroundExecution: true);
        var now = DateTimeOffset.UtcNow;
        return new AgentJobRecord(Guid.NewGuid(), definition, AgentJobState.Pending, JobExecutionLocation.Local, 0, checkpoint, null, null, now, now);
    }

    private static ResearchBatch CreateBatch()
    {
        var url = new Uri("https://docs.example.com/research");
        return new ResearchBatch(
            [new ResearchSource("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), "Extracted evidence", 0.9, "checkpoint query", DateTimeOffset.UtcNow)],
            [new ResearchCitation("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), "checkpoint query", DateTimeOffset.UtcNow, null)],
            3,
            []);
    }

    private sealed class QueueInferenceClient(IEnumerable<AgentCompletion> completions) : IAgentInferenceClient
    {
        private readonly Queue<AgentCompletion> _completions = new(completions);
        public List<AgentRequest> Requests { get; } = [];

        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_completions.Dequeue());
        }
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
            throw new InvalidOperationException("Provider must not be called while resuming from prepared evidence.");
        }
    }
}
