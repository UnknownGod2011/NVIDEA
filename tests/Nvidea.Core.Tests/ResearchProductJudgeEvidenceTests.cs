using System.Text.Json;
using Nvidea.Core.Capabilities;
using Nvidea.Core.Jobs;
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
            var now = DateTimeOffset.UtcNow;
            var citation = new ResearchCitation("s1", "Private title", new Uri("https://example.test/private"), "https://example.test/private", "private query", now, null);
            var source = new ResearchSource("s1", "Private title", citation.Url, citation.CanonicalUrl, "private source body", 0.9, citation.Query, now);
            var batch = new ResearchBatch(new[] { source }, new[] { citation }, 1, Array.Empty<string>());
            var report = new ResearchReport("private question", "Supported claim [src:s1]", batch, new[] { citation }, Array.Empty<string>());
            var receipt = new DurableResearchReceipt("aa", "bb", "cc", 1, 1, 1, true);
            var payload = JsonSerializer.Serialize(new { report, receipt }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var jobId = Guid.NewGuid();
            var record = new AgentJobRecord(
                jobId,
                new AgentJobDefinition(ResearchJobHandler.Type, ResearchJobRuntime.CapabilityId, new HashSet<DataPermission> { DataPermission.NetworkAccess }, CapabilityRiskLevel.Low, false, true, 3),
                AgentJobState.Completed,
                JobExecutionLocation.NebiusServerless,
                1,
                new AgentJobCheckpoint(ResearchJobHandler.CompletedStep, payload, now),
                null,
                null,
                now,
                now,
                null);
            await new JsonAgentJobStore(Path.Combine(directory, "research-jobs.json")).SaveAsync(record);
            var runtime = new ResearchProductRuntime(directory, local: null);

            var evidence = await runtime.ReadCompletedJudgeEvidenceAsync(jobId);

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
}
