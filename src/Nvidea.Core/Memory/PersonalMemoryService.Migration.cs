namespace Nvidea.Core.Memory;

public sealed partial class PersonalMemoryService
{
    public async Task<MemoryEmbeddingMigrationPlan> PreviewEmbeddingMigrationAsync(
        MemoryEmbeddingMigrationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var provider = GetMigrationProvider();
        var validatedOptions = ValidateMigrationOptions(options ?? new MemoryEmbeddingMigrationOptions());
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var removedExpired = RemoveExpiredLocked(_timeProvider.GetUtcNow());
            if (removedExpired > 0)
                await PersistLockedAsync(cancellationToken).ConfigureAwait(false);

            var candidates = new List<MemoryEmbeddingMigrationCandidate>();
            var excludedSensitive = 0;
            var excludedRestricted = 0;

            foreach (var memory in _memories.Values
                         .Where(memory => memory.Retention != MemoryRetention.Session)
                         .OrderBy(memory => memory.Layer)
                         .ThenBy(memory => memory.Id, StringComparer.Ordinal))
            {
                var reason = GetMigrationReason(memory, provider, validatedOptions.ForceReembedCurrent);
                if (reason is null)
                    continue;

                if (memory.Sensitivity == MemorySensitivity.Sensitive && !validatedOptions.IncludeSensitive)
                {
                    excludedSensitive++;
                    continue;
                }

                if (memory.Sensitivity == MemorySensitivity.Restricted && !validatedOptions.IncludeRestricted)
                {
                    excludedRestricted++;
                    continue;
                }

                candidates.Add(new MemoryEmbeddingMigrationCandidate(
                    memory.Id,
                    memory.Layer,
                    memory.Sensitivity,
                    reason.Value));
            }

            return new MemoryEmbeddingMigrationPlan(
                provider.MigrationTarget,
                candidates,
                excludedSensitive,
                excludedRestricted);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MemoryEmbeddingMigrationResult> MigrateEmbeddingsAsync(
        MemoryEmbeddingMigrationOptions? options = null,
        IProgress<MemoryEmbeddingMigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var provider = GetMigrationProvider();
        var validatedOptions = ValidateMigrationOptions(options ?? new MemoryEmbeddingMigrationOptions());
        var plan = await PreviewEmbeddingMigrationAsync(validatedOptions, cancellationToken).ConfigureAwait(false);
        if (plan.TotalCandidates == 0)
            return new MemoryEmbeddingMigrationResult(0, 0, 0);

        var effectiveBatchSize = Math.Min(validatedOptions.BatchSize, provider.MigrationTarget.MaxBatchSize);
        var updated = 0;
        var skippedConcurrent = 0;
        var processed = 0;

        for (var offset = 0; offset < plan.Candidates.Count; offset += effectiveBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plannedBatch = plan.Candidates.Skip(offset).Take(effectiveBatchSize).ToArray();
            var snapshots = await SnapshotMigrationBatchAsync(
                plannedBatch,
                provider,
                validatedOptions,
                cancellationToken).ConfigureAwait(false);

            skippedConcurrent += plannedBatch.Length - snapshots.Count;

            if (snapshots.Count > 0)
            {
                var vectors = await provider.EmbedBatchWithMetadataAsync(
                    snapshots.Select(snapshot => snapshot.EmbeddingText).ToArray(),
                    cancellationToken).ConfigureAwait(false);

                if (vectors.Count != snapshots.Count)
                    throw new InvalidOperationException("Local embedding migration returned an unexpected result count.");

                var validatedVectors = new MemoryEmbeddingVector[vectors.Count];
                for (var index = 0; index < vectors.Count; index++)
                {
                    var vector = ValidateEmbedding(vectors[index])
                        ?? throw new InvalidOperationException("Local embedding migration returned an invalid vector.");
                    if (!vector.Provenance.IsLocal || !provider.IsCurrentEmbedding(vector.Provenance))
                        throw new InvalidOperationException("Local embedding migration returned provenance outside the approved local target.");
                    validatedVectors[index] = vector;
                }

                await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var batchChanged = false;
                    for (var index = 0; index < snapshots.Count; index++)
                    {
                        var snapshot = snapshots[index];
                        if (!_memories.TryGetValue(snapshot.Record.Id, out var current) ||
                            !ReferenceEquals(current, snapshot.Record))
                        {
                            skippedConcurrent++;
                            continue;
                        }

                        var vector = validatedVectors[index];
                        _memories[current.Id] = current with
                        {
                            Embedding = vector.Vector.ToArray(),
                            EmbeddingProvenance = vector.Provenance,
                        };
                        updated++;
                        batchChanged = true;
                    }

                    if (batchChanged)
                        await PersistLockedAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _gate.Release();
                }
            }

            processed += plannedBatch.Length;
            progress?.Report(new MemoryEmbeddingMigrationProgress(
                plan.TotalCandidates,
                processed,
                updated,
                skippedConcurrent,
                plannedBatch[^1].Id));
        }

        return new MemoryEmbeddingMigrationResult(plan.TotalCandidates, updated, skippedConcurrent);
    }

    private async Task<IReadOnlyList<MemoryEmbeddingMigrationSnapshot>> SnapshotMigrationBatchAsync(
        IReadOnlyList<MemoryEmbeddingMigrationCandidate> plannedBatch,
        IMemoryEmbeddingMigrationProvider provider,
        MemoryEmbeddingMigrationOptions options,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var snapshots = new List<MemoryEmbeddingMigrationSnapshot>(plannedBatch.Count);
            foreach (var candidate in plannedBatch)
            {
                if (!_memories.TryGetValue(candidate.Id, out var memory) ||
                    memory.Retention == MemoryRetention.Session ||
                    GetMigrationReason(memory, provider, options.ForceReembedCurrent) is null ||
                    !IsSensitivityAllowedForMigration(memory.Sensitivity, options))
                {
                    continue;
                }

                snapshots.Add(new MemoryEmbeddingMigrationSnapshot(
                    memory,
                    BuildEmbeddingText(memory.Key, memory.Content, memory.Tags)));
            }

            return snapshots;
        }
        finally
        {
            _gate.Release();
        }
    }

    private IMemoryEmbeddingMigrationProvider GetMigrationProvider()
    {
        ThrowIfDisposed();
        if (_embeddingProvider is not IMemoryEmbeddingMigrationProvider provider)
            throw new InvalidOperationException("Embedding migration requires a migration-capable local embedding provider.");

        var target = provider.MigrationTarget;
        if (!target.IsLocal)
            throw new InvalidOperationException("Embedding migration is restricted to explicitly local providers.");
        if (string.IsNullOrWhiteSpace(target.Provider) || string.IsNullOrWhiteSpace(target.Model))
            throw new InvalidOperationException("Embedding migration target metadata is incomplete.");
        if (target.MaxBatchSize is < 1 or > 64)
            throw new InvalidOperationException("Embedding migration target declared an invalid batch bound.");
        if (target.ExpectedDimensions is <= 0)
            throw new InvalidOperationException("Embedding migration target declared invalid dimensions.");
        return provider;
    }

    private static MemoryEmbeddingMigrationOptions ValidateMigrationOptions(MemoryEmbeddingMigrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.BatchSize is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(options), "Embedding migration batch size must be between 1 and 64.");
        return options;
    }

    private static MemoryEmbeddingMigrationReason? GetMigrationReason(
        MemoryRecord memory,
        IMemoryEmbeddingMigrationProvider provider,
        bool forceReembedCurrent)
    {
        if (forceReembedCurrent)
            return MemoryEmbeddingMigrationReason.ForcedRefresh;
        if (memory.Embedding is null || memory.Embedding.Count == 0)
            return MemoryEmbeddingMigrationReason.MissingEmbedding;
        if (memory.EmbeddingProvenance is null)
            return MemoryEmbeddingMigrationReason.MissingProvenance;
        if (memory.EmbeddingProvenance.Dimensions != memory.Embedding.Count ||
            !provider.IsCurrentEmbedding(memory.EmbeddingProvenance))
        {
            return MemoryEmbeddingMigrationReason.StaleEmbeddingSpace;
        }
        return null;
    }

    private static bool IsSensitivityAllowedForMigration(
        MemorySensitivity sensitivity,
        MemoryEmbeddingMigrationOptions options) => sensitivity switch
        {
            MemorySensitivity.Sensitive => options.IncludeSensitive,
            MemorySensitivity.Restricted => options.IncludeRestricted,
            _ => true,
        };

    private sealed record MemoryEmbeddingMigrationSnapshot(
        MemoryRecord Record,
        string EmbeddingText);
}
