using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class DurableResearchReceiptTests
{
    [Fact]
    public async Task Completed_receipt_binds_plan_evidence_and_synthesis_across_restart()
    {
        const string secretQuestion = "PRIVATE_QUESTION_MARKER";
        const string secretQuery = "PRIVATE_QUERY_MARKER";
        const string secretContent = "PRIVATE_EVIDENCE_MARKER";
        const string secretAnswer = "PRIVATE_ANSWER_MARKER [src:s1].";
        var firstInference = new QueueInferenceClient([
            new AgentCompletion($"{{\"queries\":[{{\"query\":\"{secretQuery}\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}}]}}", [], "model", "stop")
        ]);
        var provider = new CountingProvider(CreateBatch(secretQuery, secretContent));
        var first = new ResearchJobHandler(new ResearchEngine(firstInference, provider));
        var job = CreateJob(ResearchJobHandler.CreateInitialCheckpoint(secretQuestion));

        var planned = await first.ExecuteStepAsync(job);
        job = WithCheckpoint(job, planned);
        var evidence = await first.ExecuteStepAsync(job);

        // Simulate process restart: a new handler receives only the persisted evidence checkpoint.
        var resumed = new ResearchJobHandler(new ResearchEngine(
            new QueueInferenceClient([new AgentCompletion(secretAnswer, [], "model", "stop")]),
            new ThrowingProvider()));
        job = WithCheckpoint(job, evidence);
        var completed = await resumed.ExecuteStepAsync(job);
        job = WithCheckpoint(job with { State = AgentJobState.Completed }, completed);

        var receipt = ResearchJobHandler.ReadCompletedReceipt(job);
        Assert.Equal(64, receipt.PlanSha256.Length);
        Assert.Equal(64, receipt.EvidenceSha256.Length);
        Assert.Equal(64, receipt.SynthesisSha256.Length);
        Assert.Equal(1, receipt.PlannedQueryCount);
        Assert.Equal(1, receipt.EvidenceSourceCount);
        Assert.Equal(1, receipt.ValidatedCitationCount);
        Assert.True(receipt.HasMachineVerifiableCitations);

        var projected = receipt.ToString();
        Assert.DoesNotContain(secretQuestion, projected, StringComparison.Ordinal);
        Assert.DoesNotContain(secretQuery, projected, StringComparison.Ordinal);
        Assert.DoesNotContain(secretContent, projected, StringComparison.Ordinal);
        Assert.DoesNotContain(secretAnswer, projected, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tampered_evidence_checkpoint_is_rejected_before_synthesis()
    {
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"receipt query\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop")
        ]);
        var provider = new CountingProvider(CreateBatch("receipt query", "trusted evidence"));
        var handler = new ResearchJobHandler(new ResearchEngine(inference, provider));
        var job = CreateJob(ResearchJobHandler.CreateInitialCheckpoint("Receipt integrity"));
        var planned = await handler.ExecuteStepAsync(job);
        job = WithCheckpoint(job, planned);
        var evidence = await handler.ExecuteStepAsync(job);

        var tamperedPayload = evidence.CheckpointPayload!.Replace("trusted evidence", "tampered evidence", StringComparison.Ordinal);
        var synthesisInference = new QueueInferenceClient([new AgentCompletion("must not run", [], "model", "stop")]);
        var resumed = new ResearchJobHandler(new ResearchEngine(synthesisInference, new ThrowingProvider()));
        var tamperedJob = job with
        {
            Checkpoint = new AgentJobCheckpoint(ResearchJobHandler.EvidenceStep, tamperedPayload, DateTimeOffset.UtcNow)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => resumed.ExecuteStepAsync(tamperedJob));
        Assert.Empty(synthesisInference.Requests);
    }

    private static AgentJobRecord WithCheckpoint(AgentJobRecord job, JobStepResult result) => job with
    {
        Checkpoint = new AgentJobCheckpoint(result.CheckpointStep!, result.CheckpointPayload, DateTimeOffset.UtcNow)
    };

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

    private static ResearchBatch CreateBatch(string query, string content)
    {
        var url = new Uri("https://docs.example.com/research");
        return new ResearchBatch(
            [new ResearchSource("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), content, 0.9, query, DateTimeOffset.UtcNow)],
            [new ResearchCitation("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), query, DateTimeOffset.UtcNow, null)],
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
        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default) =>
            Task.FromResult(batch);
    }

    private sealed class ThrowingProvider : IResearchProvider
    {
        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Provider must not be called while resuming from prepared evidence.");
    }
}
