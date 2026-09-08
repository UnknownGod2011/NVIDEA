using System.Net;
using System.Text;
using System.Text.Json;
using Nvidea.Core.Research;
using Xunit;

namespace Nvidea.Core.Tests;

public sealed class TavilyExtractEnrichmentTests
{
    [Fact]
    public async Task Enrich_uses_query_focused_advanced_extract_and_replaces_search_snippet()
    {
        var call = 0;
        string? extractBody = null;
        var handler = new StubHandler(request =>
        {
            call++;
            if (call == 1)
            {
                return JsonResponse(HttpStatusCode.OK, """
                    {
                      "results":[{"title":"Official","url":"https://example.com/page","content":"search snippet","score":0.9,"id":"s1"}],
                      "usage":{"credits":1}
                    }
                    """);
            }

            Assert.Equal("/extract", request.RequestUri!.AbsolutePath);
            extractBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(HttpStatusCode.OK, """
                {
                  "results":[{"url":"https://example.com/page","raw_content":"deep extracted evidence"}],
                  "failed_results":[],
                  "usage":{"credits":2}
                }
                """);
        });

        using var http = new HttpClient(handler);
        var client = new TavilyResearchClient(http, TestOptions());
        var searched = await client.SearchAsync([new ResearchQuery("NVIDIA Nebius")]);
        var enriched = await client.EnrichAsync(searched, "How does NVIDIA Nebius integration work?");

        Assert.Equal(2, call);
        Assert.Single(enriched.Sources);
        Assert.Equal("deep extracted evidence", enriched.Sources[0].Content);
        Assert.Equal("deep extracted evidence", enriched.Sources[0].RawContent);
        Assert.Equal(3, enriched.ProviderCreditsUsed);

        using var sent = JsonDocument.Parse(extractBody!);
        var root = sent.RootElement;
        Assert.Equal("How does NVIDIA Nebius integration work?", root.GetProperty("query").GetString());
        Assert.Equal("advanced", root.GetProperty("extract_depth").GetString());
        Assert.Equal("markdown", root.GetProperty("format").GetString());
        Assert.Equal(3, root.GetProperty("chunks_per_source").GetInt32());
        Assert.True(root.GetProperty("include_usage").GetBoolean());
        Assert.False(root.GetProperty("include_images").GetBoolean());
        Assert.Equal("https://example.com/page", root.GetProperty("urls")[0].GetString());
    }

    [Fact]
    public async Task Enrich_preserves_search_evidence_when_one_source_fails_extraction()
    {
        var call = 0;
        var handler = new StubHandler(_ =>
        {
            call++;
            return call == 1
                ? JsonResponse(HttpStatusCode.OK, """
                    {
                      "results":[
                        {"title":"A","url":"https://a.example/page","content":"snippet-a","score":0.9,"id":"a"},
                        {"title":"B","url":"https://b.example/page","content":"snippet-b","score":0.8,"id":"b"}
                      ],
                      "usage":{"credits":1}
                    }
                    """)
                : JsonResponse(HttpStatusCode.OK, """
                    {
                      "results":[{"url":"https://a.example/page","raw_content":"extracted-a"}],
                      "failed_results":[{"url":"https://b.example/page","error":"blocked"}],
                      "usage":{"credits":1}
                    }
                    """);
        });

        using var http = new HttpClient(handler);
        var client = new TavilyResearchClient(http, TestOptions());
        var searched = await client.SearchAsync([new ResearchQuery("q")]);
        var enriched = await client.EnrichAsync(searched, "intent");

        Assert.Equal("extracted-a", enriched.Sources.Single(source => source.Id == "a").Content);
        Assert.Equal("snippet-b", enriched.Sources.Single(source => source.Id == "b").Content);
        Assert.Contains(enriched.Warnings, warning => warning.Contains("b.example", StringComparison.Ordinal));
        Assert.Equal(2, enriched.ProviderCreditsUsed);
    }

    [Fact]
    public async Task Enrich_degrades_to_search_evidence_when_extract_is_unavailable()
    {
        var call = 0;
        var handler = new StubHandler(_ =>
        {
            call++;
            return call == 1
                ? JsonResponse(HttpStatusCode.OK, """
                    {"results":[{"title":"A","url":"https://a.example/page","content":"snippet-a","score":0.9,"id":"a"}],"usage":{"credits":1}}
                    """)
                : JsonResponse(HttpStatusCode.ServiceUnavailable, "{\"detail\":{\"error\":\"temporary\"}}");
        });

        using var http = new HttpClient(handler);
        var client = new TavilyResearchClient(http, new TavilyOptions
        {
            ApiKey = "test-key",
            MaxAttempts = 1,
            RequestTimeout = TimeSpan.FromSeconds(2)
        });

        var searched = await client.SearchAsync([new ResearchQuery("q")]);
        var enriched = await client.EnrichAsync(searched, "intent");

        Assert.Equal("snippet-a", enriched.Sources[0].Content);
        Assert.Equal(1, enriched.ProviderCreditsUsed);
        Assert.Contains(enriched.Warnings, warning => warning.Contains("retained search evidence", StringComparison.OrdinalIgnoreCase));
    }

    private static TavilyOptions TestOptions() => new()
    {
        ApiKey = "test-key",
        MaxAttempts = 2,
        RequestTimeout = TimeSpan.FromSeconds(2)
    };

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
