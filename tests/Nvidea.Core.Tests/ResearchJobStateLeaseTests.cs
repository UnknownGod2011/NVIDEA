using Nvidea.Core.Desktop;
using Nvidea.Core.Jobs;
using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchJobStateLeaseTests
{
    [Fact]
    public async Task Long_running_research_step_blocks_second_runtime_mutation_but_not_read_only_status()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-research-lease-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var blockingInference = new BlockingInferenceClient();
            var first = new ResearchJobRuntime(
                directory,
                new ResearchEngine(blockingInference, new EmptyProvider()));
            var created = await first.CreateAsync("Hold the research lease");

            var running = first.RunNextStepAsync(created.JobId);
            await blockingInference.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var second = new ResearchJobRuntime(
                directory,
                new ResearchEngine(new ImmediateInferenceClient(), new EmptyProvider()));

            var visible = await second.GetStatusAsync(created.JobId);
            Assert.Equal(created.JobId, visible.JobId);

            await Assert.ThrowsAsync<StateDirectoryLeaseUnavailableException>(() =>
                second.CancelAsync(created.JobId));

            blockingInference.Release.TrySetResult();
            var planned = await running;
            Assert.Equal(ResearchJobStage.GatheringEvidence, planned.Stage);

            var cancelled = await second.CancelAsync(created.JobId);
            Assert.Equal(ResearchJobStage.Cancelled, cancelled.Stage);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Completed_mutation_releases_state_lease_for_another_runtime()
    {
        var directory = Path.Combine(Path.GetTempPath(), "nvidea-research-lease-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var first = new ResearchJobRuntime(
                directory,
                new ResearchEngine(new ImmediateInferenceClient(), new EmptyProvider()));
            var created = await first.CreateAsync("Release after mutation");

            var second = new ResearchJobRuntime(
                directory,
                new ResearchEngine(new ImmediateInferenceClient(), new EmptyProvider()));
            var cancelled = await second.CancelAsync(created.JobId);

            Assert.Equal(ResearchJobStage.Cancelled, cancelled.Stage);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class BlockingInferenceClient : IAgentInferenceClient
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<AgentCompletion> CompleteAsync(
            AgentRequest request,
            CancellationToken cancellationToken = default)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return PlanCompletion();
        }
    }

    private sealed class ImmediateInferenceClient : IAgentInferenceClient
    {
        public Task<AgentCompletion> CompleteAsync(
            AgentRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PlanCompletion());
    }

    private sealed class EmptyProvider : IResearchProvider
    {
        public Task<ResearchBatch> SearchAsync(
            IReadOnlyList<ResearchQuery> queries,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResearchBatch([], [], 0, []));
    }

    private static AgentCompletion PlanCompletion() => new(
        "{\"queries\":[{\"query\":\"lease test\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}",
        [],
        "model",
        "stop");
}
