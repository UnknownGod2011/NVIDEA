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

public enum MemoryEmbeddingMigrationReason
{
    MissingEmbedding,
    MissingProvenance,
    StaleEmbeddingSpace,
    ForcedRefresh,
}

public sealed record MemoryProvenance(
    string SourceType,
    string? SourceId = null,
    string? SourceUri = null,
    DateTimeOffset? ObservedAt = null);

public sealed record MemoryEmbeddingProvenance(
    string Provider,
    string Model,
    int Dimensions,
    bool IsLocal,
    DateTimeOffset CreatedAt);

public sealed record MemoryEmbeddingVector(
    IReadOnlyList<float> Vector,
    MemoryEmbeddingProvenance Provenance);

public sealed record MemoryEmbeddingMigrationTarget(
    string Provider,
    string Model,
    bool IsLocal,
    int MaxBatchSize,
    int? ExpectedDimensions = null);

public sealed record MemoryEmbeddingMigrationOptions
{
    public int BatchSize { get; init; } = 8;
    public bool IncludeSensitive { get; init; }
    public bool IncludeRestricted { get; init; }
    public bool ForceReembedCurrent { get; init; }
}

public sealed record MemoryEmbeddingMigrationCandidate(
    string Id,
    MemoryLayer Layer,
    MemorySensitivity Sensitivity,
    MemoryEmbeddingMigrationReason Reason);

public sealed record MemoryEmbeddingMigrationPlan(
    MemoryEmbeddingMigrationTarget Target,
    IReadOnlyList<MemoryEmbeddingMigrationCandidate> Candidates,
    int ExcludedSensitive,
    int ExcludedRestricted)
{
    public int TotalCandidates => Candidates.Count;
}

public sealed record MemoryEmbeddingMigrationProgress(
    int Total,
    int Processed,
    int Updated,
    int SkippedConcurrentChanges,
    string? LastProcessedId);

public sealed record MemoryEmbeddingMigrationResult(
    int Planned,
    int Updated,
    int SkippedConcurrentChanges);

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
    public MemoryEmbeddingProvenance? EmbeddingProvenance { get; init; }
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

public interface IProvenancedMemoryEmbeddingProvider : IMemoryEmbeddingProvider
{
    Task<MemoryEmbeddingVector> EmbedWithMetadataAsync(string text, CancellationToken cancellationToken = default);
}

public interface IMemoryBatchEmbeddingProvider : IProvenancedMemoryEmbeddingProvider
{
    Task<IReadOnlyList<MemoryEmbeddingVector>> EmbedBatchWithMetadataAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}

public interface IMemoryEmbeddingMigrationProvider : IMemoryBatchEmbeddingProvider
{
    MemoryEmbeddingMigrationTarget MigrationTarget { get; }
    bool IsCurrentEmbedding(MemoryEmbeddingProvenance provenance);
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
