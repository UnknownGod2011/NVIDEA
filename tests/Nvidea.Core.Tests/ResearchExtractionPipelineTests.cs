using Nvidea.Core.Nebius;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class ResearchExtractionPipelineTests
{
    [Fact]
    public async Task Research_engine_uses_enriched_evidence_before_synthesis()
    {
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"Nebius NVIDIA\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop"),
            new AgentCompletion("Supported claim [src:s1].", [], "model", "stop")
        ]);

        var url = new Uri("https://example.com/source");
        var searched = new ResearchBatch(
            [new ResearchSource("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), "search snippet", 0.9, "Nebius NVIDIA", DateTimeOffset.UtcNow)],
            [new ResearchCitation("s1", "Source", url, url.AbsoluteUri.TrimEnd('/'), "Nebius NVIDIA", DateTimeOffset.UtcNow, null)],
            1,
            []);
        var enriched = searched with
        {
            Sources = [searched.Sources[0] with { Content = "extracted primary-source evidence", RawContent = "extracted primary-source evidence" }],
            ProviderCreditsUsed = 2
        };
        var provider = new ExtractingProvider(searched, enriched);

        var report = await new ResearchEngine(inference, provider).ResearchAsync("How does the integration work?");

        Assert.Equal("How does the integration work?", provider.LastResearchIntent);
        Assert.Equal(1, provider.ExtractCalls);
        Assert.Equal(2, report.Evidence.ProviderCreditsUsed);
        Assert.Contains("extracted primary-source evidence", inference.Requests[1].Messages.Last().Content);
        Assert.DoesNotContain("search snippet", inference.Requests[1].Messages.Last().Content);
        Assert.Single(report.UsedCitations);
        Assert.Equal("s1", report.UsedCitations[0].SourceId);
    }

    [Fact]
    public async Task Research_engine_does_not_extract_when_search_returns_no_sources()
    {
        var inference = new QueueInferenceClient([
            new AgentCompletion("{\"queries\":[{\"query\":\"nothing\",\"topic\":\"general\",\"maxResults\":3,\"startDate\":null,\"endDate\":null}]}", [], "model", "stop")
        ]);
        var empty = new ResearchBatch([], [], 1, []);
        var provider = new ExtractingProvider(empty, empty);

        var report = await new ResearchEngine(inference, provider).ResearchAsync("nothing");

        Assert.Equal(0, provider.ExtractCalls);
        Assert.Empty(report.Evidence.Sources);
        Assert.Equal(1, inference.Requests.Count);
    }

    private sealed class ExtractingProvider(ResearchBatch searched, ResearchBatch enriched) : IResearchProvider, IResearchExtractionProvider
    {
        public int ExtractCalls { get; private set; }
        public string? LastResearchIntent { get; private set; }

        public Task<ResearchBatch> SearchAsync(IReadOnlyList<ResearchQuery> queries, CancellationToken cancellationToken = default) =>
            Task.FromResult(searched);

        public Task<ResearchBatch> EnrichAsync(ResearchBatch batch, string researchIntent, CancellationToken cancellationToken = default)
        {
            ExtractCalls++;
            LastResearchIntent = researchIntent;
            return Task.FromResult(enriched);
        }
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
}
