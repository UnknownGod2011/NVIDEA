using System.Net;
using Nvidea.Core.Jobs;

namespace Nvidea.Core.Tests;

public sealed class NebiusBoundedJobListReaderTests
{
    [Fact]
    public async Task ReadAllAsync_FollowsContinuationTokensAndReturnsCompleteListing()
    {
        var client = new PagedClient(new Dictionary<string, NebiusServerlessResponse>
        {
            [""] = Page("a", "job-a", "RUNNING", "next-1"),
            ["next-1"] = Page("b", "job-b", "COMPLETED", null)
        });

        var jobs = await NebiusBoundedJobListReader.ReadAllAsync(client);

        Assert.Equal(2, jobs.Count);
        Assert.Equal(new string?[] { null, "next-1" }, client.RequestedTokens);
        Assert.Equal(NebiusRemoteJobState.Completed, jobs[1].State);
    }

    [Fact]
    public async Task ReadAllAsync_RejectsRepeatedContinuationToken()
    {
        var client = new PagedClient(new Dictionary<string, NebiusServerlessResponse>
        {
            [""] = Page("a", "job-a", "RUNNING", "loop"),
            ["loop"] = Page("b", "job-b", "RUNNING", "loop")
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NebiusBoundedJobListReader.ReadAllAsync(client));

        Assert.Contains("repeated continuation token", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAllAsync_RejectsPageBoundBeforePartialListingCanBeTrusted()
    {
        var client = new PagedClient(new Dictionary<string, NebiusServerlessResponse>
        {
            [""] = Page("a", "job-a", "RUNNING", "next-1"),
            ["next-1"] = Page("b", "job-b", "RUNNING", "next-2")
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NebiusBoundedJobListReader.ReadAllAsync(client, maxPages: 2));

        Assert.Contains("page bound", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAllAsync_RejectsAggregateItemBound()
    {
        var client = new PagedClient(new Dictionary<string, NebiusServerlessResponse>
        {
            [""] = new(HttpStatusCode.OK,
                "{\"items\":[{\"metadata\":{\"id\":\"a\",\"name\":\"job-a\"}},{\"metadata\":{\"id\":\"b\",\"name\":\"job-b\"}}]}")
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NebiusBoundedJobListReader.ReadAllAsync(client, maxItems: 1));

        Assert.Contains("item bound", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static NebiusServerlessResponse Page(string id, string name, string state, string? nextPageToken)
    {
        var next = nextPageToken is null ? string.Empty : $",\"nextPageToken\":\"{nextPageToken}\"";
        return new NebiusServerlessResponse(HttpStatusCode.OK,
            $"{{\"items\":[{{\"metadata\":{{\"id\":\"{id}\",\"name\":\"{name}\"}},\"status\":{{\"state\":\"{state}\"}}}}]{next}}}");
    }

    private sealed class PagedClient : INebiusServerlessJobClient
    {
        private readonly IReadOnlyDictionary<string, NebiusServerlessResponse> _pages;
        public List<string?> RequestedTokens { get; } = new();

        public PagedClient(IReadOnlyDictionary<string, NebiusServerlessResponse> pages) => _pages = pages;

        public Task<NebiusServerlessResponse> ListAsync(CancellationToken cancellationToken = default) =>
            ListAsync(null, cancellationToken);

        public Task<NebiusServerlessResponse> ListAsync(string? pageToken, CancellationToken cancellationToken = default)
        {
            RequestedTokens.Add(pageToken);
            var key = pageToken ?? string.Empty;
            if (!_pages.TryGetValue(key, out var response))
                throw new InvalidOperationException($"Unexpected page token '{key}'.");
            return Task.FromResult(response);
        }

        public Task<NebiusServerlessResponse> CreateAsync(NebiusServerlessJobSpec spec, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NebiusServerlessResponse> GetAsync(string remoteJobId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<NebiusServerlessResponse> CancelAsync(string remoteJobId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
