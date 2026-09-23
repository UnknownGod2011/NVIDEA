using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchProductJudgeEvidenceTests
{
    [Fact]
    public async Task Completed_remote_result_exposes_canonical_judge_evidence_without_local_runtime()
    {
        var directory = CreateDirectory();
        try
        {
            var record = await CreateCompletedRecordAsync();
            record = record with { ExecutionLocation = JobExecutionLocation.NebiusServerless };
            await new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json")).SaveAsync(record);
            var runtime = new ResearchProductRuntime(directory, local: null);

            var evidence = await runtime.ReadCompletedJudgeEvidenceAsync(record.Id);

            Assert.True(evidence.IsVerifiedForJudging);
            Assert.Equal("verified", evidence.Provenance);
            Assert.Equal(1, evidence.EvidenceSourceCount);
            Assert.Equal(1, evidence.VerifiedCitationCount);
            Assert.Empty(evidence.UnknownSourceIds);
            Assert.False(runtime.LocalExecutionAvailable);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Theory]
    [InlineData("Supported claim [src:s1]", "Supported claim [src:fake]")]
    [InlineData("private source body", "tampered source body")]
    public async Task Post_completion_report_tampering_fails_closed(string original, string replacement)
    {
        var record = await CreateCompletedRecordAsync();
        var payload = record.Checkpoint!.Payload!;
        Assert.Contains(original, payload, StringComparison.Ordinal);
        var tampered = record with
        {
            ExecutionLocation = JobExecutionLocation.NebiusServerless,
            Checkpoint = record.Checkpoint with { Payload = payload.Replace(original, replacement, StringComparison.Ordinal) }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => ResearchJobHandler.ReadCompletedJudgeEvidence(tampered));
        Assert.Contains("does not match its durable receipt", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Legacy_receipt_without_report_and_provenance_bindings_fails_closed()
    {
        var record = await CreateCompletedRecordAsync();
        var receipt = ResearchJobHandler.ReadCompletedReceipt(record);
        var payload = record.Checkpoint!.Payload!;
        payload = payload.Replace($",\"reportSha256\":\"{receipt.ReportSha256}\",\"provenanceSha256\":\"{receipt.ProvenanceSha256}\"", string.Empty, StringComparison.Ordinal);
        var legacy = record with { Checkpoint = record.Checkpoint with { Payload = payload } };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => ResearchJobHandler.ReadCompletedJudgeEvidence(legacy)));
    }

    [Fact]
    public async Task Incomplete_or_corrupt_checkpoint_fails_closed_without_local_runtime()
    {
        var directory = CreateDirectory();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var jobId = Guid.NewGuid();
            var record = new AgentJobRecord(
                jobId,
                new AgentJobDefinition(ResearchJobHandler.Type, ResearchJobRuntime.CapabilityId, new HashSet<DataPermission> { DataPermission.NetworkAccess }, CapabilityRiskLevel.Low, false, true, 3),
                AgentJobState.Completed,
                JobExecutionLocation.NebiusServerless,
                1,
                new AgentJobCheckpoint(ResearchJobHandler.CompletedStep, "{not-json", now),
                null,
                null,
                now,
                now,
                null);
            await new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json")).SaveAsync(record);
            var runtime = new ResearchProductRuntime(directory, local: null);

            await Assert.ThrowsAnyAsync<Exception>(() => runtime.ReadCompletedJudgeEvidenceAsync(jobId));
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static async Task<AgentJobRecord> CreateCompletedRecordAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"private query\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop"),
            new AgentCompletion("Supported claim [src:s1]", [], "model", "stop")
        ]);
        var handler = new ResearchJobHandler(new ResearchEngine(inference, new StaticProvider()));
        var definition = new AgentJobDefinition(ResearchJobHandler.Type, ResearchJobRuntime.CapabilityId, new HashSet<DataPermission> { DataPermission.NetworkAccess }, CapabilityRiskLevel.Low, false, true, 3);
        var record = new AgentJobRecord(Guid.NewGuid(), definition, AgentJobState.Pending, JobExecutionLocation.Local, 0, ResearchJobHandler.CreateInitialCheckpoint("private question"), null, null, now, now, null);

        foreach (var state in new[] { AgentJobState.Pending, AgentJobState.Running, AgentJobState.Running })
        {
            record = record with { State = state };
            var result = await handler.ExecuteStepAsync(record);
            record = record with { Checkpoint = new AgentJobCheckpoint(result.CheckpointStep!, result.CheckpointPayload, DateTimeOffset.UtcNow) };
        }

        return record with { State = AgentJobState.Completed };
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-research-judge-evidence", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class QueueInferenceClient(IEnumerable<AgentCompletion> completions) : IAgentInferenceClient
    {
        private readonly Queue<AgentCompletion> _completions = new(completions);
        public Task<AgentCompletion> CompleteAsync(AgentRequest request, CancellationToken cancellationToken = default) => Task.FromResult(_completions.Dequeue());
    }

    private sealed class StaticProvider : IResearchProvider
    {
        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var url = new Uri("https://example.test/private");
            var citation = new ResearchCitation("s1", "Private title", url, url.AbsoluteUri.TrimEnd('/'), "private query", now, null);
            var source = new ResearchSource("s1", "Private title", url, citation.CanonicalUrl, "private source body", 0.9, citation.Query, now);
            return Task.FromResult(new ResearchBatch([source], [citation], 1, []));
        }
    }
}
