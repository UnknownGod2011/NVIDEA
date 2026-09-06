namespace Nvidea.Core.Memory;

public enum MemoryLayer
{
    Working,
    Episodic,
    Semantic,
    Project,
    Skill,
}

public enum MemorySensitivity
{
    Public,
    Personal,
    Sensitive,
    Restricted,
}

public enum MemoryRetention
{
    Session,
    SevenDays,
    ThirtyDays,
    Indefinite,
}

public sealed record MemoryProvenance(
    string SourceType,
    string? SourceId = null,
    string? SourceUri = null,
    DateTimeOffset? ObservedAt = null);

public sealed record MemoryRecord
{
    public required string Id { get; init; }
    public required MemoryLayer Layer { get; init; }
    public required string Key { get; init; }
    public required string Content { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public double Importance { get; init; } = 0.5;
    public double Confidence { get; init; } = 1.0;
    public MemorySensitivity Sensitivity { get; init; } = MemorySensitivity.Personal;
    public MemoryRetention Retention { get; init; } = MemoryRetention.ThirtyDays;
    public required MemoryProvenance Provenance { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset LastAccessedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public IReadOnlyList<float>? Embedding { get; init; }
}

public sealed record MemoryWriteRequest
{
    public required MemoryLayer Layer { get; init; }
    public required string Key { get; init; }
    public required string Content { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public double Importance { get; init; } = 0.5;
    public double Confidence { get; init; } = 1.0;
    public MemorySensitivity Sensitivity { get; init; } = MemorySensitivity.Personal;
    public MemoryRetention Retention { get; init; } = MemoryRetention.ThirtyDays;
    public required MemoryProvenance Provenance { get; init; }
    public bool ExplicitUserApproval { get; init; }
}

public sealed record MemoryQuery
{
    public required string Text { get; init; }
    public IReadOnlySet<MemoryLayer>? Layers { get; init; }
    public IReadOnlySet<MemorySensitivity>? AllowedSensitivities { get; init; }
    public IReadOnlyList<string> RequiredTags { get; init; } = Array.Empty<string>();
    public int MaxResults { get; init; } = 8;
}

public sealed record MemorySearchResult(
    MemoryRecord Memory,
    double Score,
    double SemanticScore,
    double LexicalScore,
    double RecencyScore,
    double ImportanceScore);

public sealed record MemoryWriteDecision(bool Allowed, string Reason)
{
    public static MemoryWriteDecision Allow(string reason = "Memory write permitted.") => new(true, reason);
    public static MemoryWriteDecision Deny(string reason) => new(false, reason);
}

public sealed class MemoryWriteRejectedException : InvalidOperationException
{
    public MemoryWriteRejectedException(string message) : base(message)
    {
    }
}

public interface IMemoryEmbeddingProvider
{
    Task<IReadOnlyList<float>> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public interface IMemoryStore
{
    Task<IReadOnlyList<MemoryRecord>> ReadAllAsync(CancellationToken cancellationToken = default);
    Task WriteAllAsync(IReadOnlyCollection<MemoryRecord> memories, CancellationToken cancellationToken = default);
}

public interface IMemoryWritePolicy
{
    MemoryWriteDecision Evaluate(MemoryWriteRequest request);
}
