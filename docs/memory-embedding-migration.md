# Local memory embedding migration

NVIDEA can re-index durable personal-memory records when local semantic embeddings are enabled and older records are missing vectors, missing embedding provenance, or use a stale embedding space.

## Safety model

Migration is intentionally local-only. `PersonalMemoryService` requires an `IMemoryEmbeddingMigrationProvider` whose target declares `IsLocal=true`; the production Ollama adapter satisfies this contract and remains restricted to loopback HTTP(S). There is no automatic Nebius, Tavily, OpenAI, Gemini, Claude, or other cloud fallback for memory re-indexing.

Before any embedding request, callers can run `PreviewEmbeddingMigrationAsync`. The preview returns only record IDs, layer, coarse sensitivity, reason, and target metadata; it does not expose memory content.

By default, `Sensitive` and `Restricted` memories are excluded from migration. They require separate explicit `IncludeSensitive` / `IncludeRestricted` options. Session-only memories are not part of the durable migration set.

## Resumability and crash behavior

Migration runs in bounded batches. The requested batch size is capped by the provider's declared maximum. Each completed batch is persisted through the existing protected memory store before the next batch starts, so a crash or cancellation leaves already-completed records durable. A later preview naturally omits records whose embedding provenance is already current.

Embedding maintenance does not change `UpdatedAt`, `CreatedAt`, memory provenance, sensitivity, retention, importance, confidence, or user content. Only the embedding vector and embedding provenance are replaced.

## Concurrent edits

Embedding requests run outside the memory mutation lock to avoid freezing normal memory operations during local inference. Before applying a returned vector, NVIDEA verifies that the in-memory immutable record instance is still the same record that was snapshotted for embedding. If a search, user edit, deletion, or another mutation replaced the record while inference was running, the migration result is discarded for that record and counted as a concurrent-change skip.

This makes migration fail safe rather than overwriting a newer user-authored state.

## Cancellation and progress

`MigrateEmbeddingsAsync` accepts a cancellation token and optional `IProgress<MemoryEmbeddingMigrationProgress>`. Cancellation is propagated to the local embedding provider and stops before additional batches. Progress reports total planned records, processed records, successful updates, concurrent-change skips, and the last processed record ID without including memory content.

## Stale-space detection

The production Ollama provider marks an embedding current only when provider locality, provider identity, configured model family/tag, and any pinned dimension requirement are compatible. Provenance-less vectors and dimension-mismatched vectors are always migration candidates.

Use `ForceReembedCurrent=true` only for deliberate maintenance; normal migration skips already-current vectors.
