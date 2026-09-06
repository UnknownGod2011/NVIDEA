using System.Globalization;
using System.Text.RegularExpressions;

namespace Nvidea.Core.Memory;

public sealed class PersonalMemoryService : IDisposable
{
    private static readonly IReadOnlySet<MemorySensitivity> DefaultAllowedSensitivities =
        new HashSet<MemorySensitivity> { MemorySensitivity.Public, MemorySensitivity.Personal };

    private readonly IMemoryStore _store;
    private readonly IMemoryWritePolicy _writePolicy;
    private readonly IMemoryEmbeddingProvider? _embeddingProvider;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, MemoryRecord> _memories = new(StringComparer.Ordinal);
    private bool _initialized;
    private bool _disposed;

    public PersonalMemoryService(
        IMemoryStore store,
        IMemoryWritePolicy? writePolicy = null,
        IMemoryEmbeddingProvider? embeddingProvider = null,
        TimeProvider? timeProvider = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _writePolicy = writePolicy ?? new DefaultMemoryWritePolicy();
        _embeddingProvider = embeddingProvider;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;

            var now = _timeProvider.GetUtcNow();
            var records = await _store.ReadAllAsync(cancellationToken).ConfigureAwait(false);
            _memories.Clear();
            foreach (var record in records)
            {
                if (record.Retention == MemoryRetention.Session || IsExpired(record, now))
                    continue;

                _memories[record.Id] = record;
            }

            _initialized = true;
            await PersistLockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MemoryRecord> RememberAsync(MemoryWriteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var decision = _writePolicy.Evaluate(request);
        if (!decision.Allowed)
            throw new MemoryWriteRejectedException(decision.Reason);

        var embedding = await TryEmbedAsync(BuildEmbeddingText(request.Key, request.Content, request.Tags), cancellationToken)
            .ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = _memories.Values.FirstOrDefault(memory =>
                memory.Layer == request.Layer &&
                string.Equals(memory.Key, request.Key.Trim(), StringComparison.OrdinalIgnoreCase));

            var record = new MemoryRecord
            {
                Id = existing?.Id ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                Layer = request.Layer,
                Key = request.Key.Trim(),
                Content = request.Content.Trim(),
                Tags = NormalizeTags(request.Tags),
                Importance = request.Importance,
                Confidence = request.Confidence,
                Sensitivity = request.Sensitivity,
                Retention = request.Retention,
                Provenance = NormalizeProvenance(request.Provenance, now),
                CreatedAt = existing?.CreatedAt ?? now,
                UpdatedAt = now,
                LastAccessedAt = now,
                ExpiresAt = CalculateExpiry(request.Retention, now),
                Embedding = embedding,
            };

            _memories[record.Id] = record;
            await PersistLockedAsync(cancellationToken).ConfigureAwait(false);
            return record;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<MemorySearchResult>> SearchAsync(
        MemoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.MaxResults is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(query), "MaxResults must be between 1 and 100.");

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        var queryEmbedding = string.IsNullOrWhiteSpace(query.Text)
            ? null
            : await TryEmbedAsync(query.Text, cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RemoveExpiredLocked(now);
            var allowedSensitivities = query.AllowedSensitivities ?? DefaultAllowedSensitivities;
            var requiredTags = NormalizeTags(query.RequiredTags);

            var results = _memories.Values
                .Where(memory => query.Layers is null || query.Layers.Contains(memory.Layer))
                .Where(memory => allowedSensitivities.Contains(memory.Sensitivity))
                .Where(memory => requiredTags.Count == 0 || requiredTags.All(tag => memory.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                .Select(memory => Score(memory, query.Text, queryEmbedding, now))
                .Where(result => string.IsNullOrWhiteSpace(query.Text) || result.Score > 0.05)
                .OrderByDescending(result => result.Score)
                .ThenByDescending(result => result.Memory.UpdatedAt)
                .Take(query.MaxResults)
                .ToArray();

            if (results.Length > 0)
            {
                foreach (var result in results)
                    _memories[result.Memory.Id] = result.Memory with { LastAccessedAt = now };

                await PersistLockedAsync(cancellationToken).ConfigureAwait(false);
                results = results
                    .Select(result => result with { Memory = _memories[result.Memory.Id] })
                    .ToArray();
            }

            return results;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<MemoryRecord>> SnapshotAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            RemoveExpiredLocked(now);
            return _memories.Values
                .OrderBy(memory => memory.Layer)
                .ThenByDescending(memory => memory.UpdatedAt)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_memories.Remove(id.Trim()))
                return false;

            await PersistLockedAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> DeleteLayerAsync(MemoryLayer layer, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var ids = _memories.Values.Where(memory => memory.Layer == layer).Select(memory => memory.Id).ToArray();
            foreach (var id in ids)
                _memories.Remove(id);

            if (ids.Length > 0)
                await PersistLockedAsync(cancellationToken).ConfigureAwait(false);
            return ids.Length;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var removed = RemoveExpiredLocked(_timeProvider.GetUtcNow());
            if (removed > 0)
                await PersistLockedAsync(cancellationToken).ConfigureAwait(false);
            return removed;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _gate.Dispose();
        _disposed = true;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (_initialized)
            return;

        await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PersistLockedAsync(CancellationToken cancellationToken)
    {
        var persistent = _memories.Values
            .Where(memory => memory.Retention != MemoryRetention.Session)
            .OrderBy(memory => memory.Id, StringComparer.Ordinal)
            .ToArray();
        await _store.WriteAllAsync(persistent, cancellationToken).ConfigureAwait(false);
    }

    private int RemoveExpiredLocked(DateTimeOffset now)
    {
        var expiredIds = _memories.Values
            .Where(memory => IsExpired(memory, now))
            .Select(memory => memory.Id)
            .ToArray();
        foreach (var id in expiredIds)
            _memories.Remove(id);
        return expiredIds.Length;
    }

    private MemorySearchResult Score(
        MemoryRecord memory,
        string query,
        IReadOnlyList<float>? queryEmbedding,
        DateTimeOffset now)
    {
        var lexical = LexicalSimilarity(query, memory);
        var semantic = queryEmbedding is null || memory.Embedding is null
            ? 0
            : CosineSimilarity(queryEmbedding, memory.Embedding);
        var ageDays = Math.Max(0, (now - memory.UpdatedAt).TotalDays);
        var recency = Math.Exp(-Math.Log(2) * ageDays / 30.0);
        var confidenceFactor = 0.7 + (0.3 * memory.Confidence);

        var score = queryEmbedding is not null && memory.Embedding is not null
            ? (0.45 * semantic) + (0.20 * lexical) + (0.20 * recency) + (0.15 * memory.Importance)
            : (0.50 * lexical) + (0.30 * recency) + (0.20 * memory.Importance);

        if (string.IsNullOrWhiteSpace(query))
            score = (0.65 * recency) + (0.35 * memory.Importance);

        score = Math.Clamp(score * confidenceFactor, 0, 1);
        return new MemorySearchResult(memory, score, semantic, lexical, recency, memory.Importance);
    }

    private async Task<IReadOnlyList<float>?> TryEmbedAsync(string text, CancellationToken cancellationToken)
    {
        if (_embeddingProvider is null || string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var result = await _embeddingProvider.EmbedAsync(text, cancellationToken).ConfigureAwait(false);
            return result.Count == 0 ? null : result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Memory remains usable with deterministic lexical/recency retrieval when embeddings are temporarily unavailable.
            return null;
        }
    }

    private static double LexicalSimilarity(string query, MemoryRecord memory)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 0;

        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0)
            return 0;

        var documentTokens = Tokenize($"{memory.Key} {memory.Content} {string.Join(' ', memory.Tags)}");
        if (documentTokens.Count == 0)
            return 0;

        var overlap = queryTokens.Intersect(documentTokens, StringComparer.OrdinalIgnoreCase).Count();
        return (2.0 * overlap) / (queryTokens.Count + documentTokens.Count);
    }

    private static HashSet<string> Tokenize(string value) =>
        Regex.Matches(value.ToLowerInvariant(), @"[\p{L}\p{N}][\p{L}\p{N}._-]*")
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        if (left.Count == 0 || left.Count != right.Count)
            return 0;

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;
        for (var index = 0; index < left.Count; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        if (leftMagnitude <= double.Epsilon || rightMagnitude <= double.Epsilon)
            return 0;

        return Math.Clamp(dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude)), -1, 1);
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags) =>
        tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Take(32)
            .ToArray();

    private static MemoryProvenance NormalizeProvenance(MemoryProvenance provenance, DateTimeOffset now) =>
        provenance with
        {
            SourceType = string.IsNullOrWhiteSpace(provenance.SourceType) ? "unknown" : provenance.SourceType.Trim(),
            SourceId = string.IsNullOrWhiteSpace(provenance.SourceId) ? null : provenance.SourceId.Trim(),
            SourceUri = string.IsNullOrWhiteSpace(provenance.SourceUri) ? null : provenance.SourceUri.Trim(),
            ObservedAt = provenance.ObservedAt ?? now,
        };

    private static string BuildEmbeddingText(string key, string content, IEnumerable<string> tags) =>
        $"{key.Trim()}\n{content.Trim()}\n{string.Join(' ', NormalizeTags(tags))}";

    private static DateTimeOffset? CalculateExpiry(MemoryRetention retention, DateTimeOffset now) => retention switch
    {
        MemoryRetention.Session => null,
        MemoryRetention.SevenDays => now.AddDays(7),
        MemoryRetention.ThirtyDays => now.AddDays(30),
        MemoryRetention.Indefinite => null,
        _ => throw new ArgumentOutOfRangeException(nameof(retention), retention, "Unknown memory retention policy."),
    };

    private static bool IsExpired(MemoryRecord memory, DateTimeOffset now) =>
        memory.ExpiresAt is { } expiry && expiry <= now;

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
