using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchEngineTests
{
    [Fact]
    public async Task Research_plans_searches_and_returns_only_verified_used_citations()
    {
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"Nebius NVIDIA hackathon\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop"),
            new AgentCompletion("Nebius and NVIDIA are running the hackathon [src:s1]. Unknown marker [src:fake].", [], "model", "stop")
        ]);
        var sourceUrl = new Uri("https://example.com/source");
        var provider = new FakeProvider(new ResearchBatch(
            [new ResearchSource("s1", "Source", sourceUrl, sourceUrl.AbsoluteUri.TrimEnd('/'), "Evidence", 0.9, "Nebius NVIDIA hackathon", DateTimeOffset.UtcNow)],
            [new ResearchCitation("s1", "Source", sourceUrl, sourceUrl.AbsoluteUri.TrimEnd('/'), "Nebius NVIDIA hackathon", DateTimeOffset.UtcNow, null)],
            1,
            []));

        var engine = new ResearchEngine(inference, provider);
        var report = await engine.ResearchAsync("What is this hackathon?");

        Assert.Single(provider.LastQueries!);
        Assert.Single(report.UsedCitations);
        Assert.Equal("s1", report.UsedCitations[0].SourceId);
        Assert.Contains(report.Warnings, w => w.Contains("fake", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, inference.Requests.Count);
        Assert.Contains("UNTRUSTED WEB EVIDENCE", inference.Requests[1].Messages.Last().Content);
    }

    [Fact]
    public async Task Research_does_not_call_synthesis_when_no_sources_exist()
    {
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"rare thing\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop")
        ]);
        var provider = new FakeProvider(new ResearchBatch([], [], 1, []));
        var engine = new ResearchEngine(inference, provider);

        var report = await engine.ResearchAsync("rare thing");

        Assert.Equal(1, inference.Requests.Count);
        Assert.Empty(report.UsedCitations);
        Assert.Contains("could not find", report.AnswerMarkdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Planner_rejects_invalid_date_ranges()
    {
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"test\",\"topic\":\"news\",\"maxResults\":3,\"startDate\":\"2026-09-06\",\"endDate\":\"2026-09-01\"}]}", [], "model", "stop")
        ]);
        var engine = new ResearchEngine(inference, new FakeProvider(new ResearchBatch([], [], 0, [])));

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.PlanAsync("test"));
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

    private sealed class FakeProvider(ResearchBatch response) : IResearchProvider
    {
        public IReadOnlyList<ResearchQuery>? LastQueries { get; private set; }

        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default)
        {
            LastQueries = queries;
            return Task.FromResult(response);
        }
    }
}
