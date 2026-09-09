namespace Nvidea.Core.Jobs;

/// <summary>
/// Reads a complete Nebius job listing only within explicit safety bounds. Continuation tokens
/// are provider-controlled input: repeated tokens, excessive page counts, or excessive aggregate
/// resources fail closed instead of creating an unbounded control-plane loop.
/// </summary>
public static class NebiusBoundedJobListReader
{
    public const int DefaultMaxPages = 8;
    public const int DefaultMaxItems = 2_000;

    public static async Task<IReadOnlyList<NebiusRemoteJobSnapshot>> ReadAllAsync(
        INebiusServerlessJobClient client,
        int maxPages = DefaultMaxPages,
        int maxItems = DefaultMaxItems,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (maxPages is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(maxPages), "Page bound must be between 1 and 32.");
        if (maxItems is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(maxItems), "Item bound must be between 1 and 10,000.");

        var all = new List<NebiusRemoteJobSnapshot>();
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);
        string? pageToken = null;

        for (var page = 0; page < maxPages; page++)
        {
            var response = await client.ListAsync(pageToken, cancellationToken).ConfigureAwait(false);
            var items = NebiusServerlessJobSnapshotParser.ParseList(response);
            if (items.Count > maxItems - all.Count)
                throw new InvalidOperationException("Nebius job listing exceeded the configured aggregate item bound.");
            all.AddRange(items);

            var next = NebiusServerlessJobSnapshotParser.TryGetNextPageToken(response);
            if (string.IsNullOrWhiteSpace(next))
                return all;

            next = next.Trim();
            if (!seenTokens.Add(next))
                throw new InvalidOperationException("Nebius returned a repeated continuation token; refusing a pagination loop.");
            pageToken = next;
        }

        throw new InvalidOperationException("Nebius job listing exceeded the configured page bound before reaching a terminal page.");
    }
}
