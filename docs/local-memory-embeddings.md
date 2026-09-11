# Local semantic memory embeddings

NVIDEA can optionally use a local Ollama embedding model to improve semantic memory retrieval without making a cloud embedding service part of the memory path.

## Enable

The feature is disabled by default. Set:

```text
NVIDEA_LOCAL_EMBEDDINGS=true
```

Optional settings:

```text
NVIDEA_LOCAL_EMBEDDING_ENDPOINT=http://127.0.0.1:11434/api/embed
NVIDEA_LOCAL_EMBEDDING_MODEL=embeddinggemma
NVIDEA_LOCAL_EMBEDDING_DIMENSIONS=<expected dimension count>
```

`NVIDEA_LOCAL_EMBEDDING_ENDPOINT` must be an absolute HTTP(S) loopback endpoint whose path is `/api/embed`. Redirects are rejected. This prevents configuration from silently turning the personal-memory embedding path into an arbitrary remote HTTP destination.

The adapter follows Ollama's documented `POST /api/embed` contract. Requests use `truncate=false`, so an input that exceeds the local model context fails instead of being silently shortened.

Official API reference: https://docs.ollama.com/api/embed

## Retrieval behavior

Every successful vector now carries durable embedding provenance:

- provider (`ollama-local` for this adapter),
- model identifier returned by the local runtime,
- vector dimensions,
- whether the path is local,
- creation time.

`PersonalMemoryService` compares vectors only when provider, model, and dimensions match. If the configured model changes, old vectors are not compared against new vectors as if they shared one semantic space. Those records remain fully usable through lexical, recency, importance, and confidence scoring until they are rewritten/re-embedded.

If the local runtime is absent, times out, returns malformed vectors, changes dimensions unexpectedly, or otherwise fails, memory retrieval falls back to the existing deterministic lexical/recency path. Embedding failure does not make memory unavailable.

## Safety limits

The local adapter bounds input length, batch size, request duration, and maximum vector dimensions. It rejects blank inputs, non-finite vector values, inconsistent dimensions within a batch, unexpected result counts, redirects, credentials embedded in the endpoint URI, and non-loopback destinations.

The desktop composition owns and disposes the embedding provider. UI/plugin code receives the higher-level `PersonalMemoryService`, not raw transport authority.

## Demo setup

1. Install and run Ollama locally.
2. Pull an embedding-capable model such as `embeddinggemma`.
3. Set `NVIDEA_LOCAL_EMBEDDINGS=true` before starting NVIDEA.
4. Optionally pin `NVIDEA_LOCAL_EMBEDDING_DIMENSIONS` once the chosen model's vector width is known.
5. Add memories whose wording differs from a later query and verify that semantic retrieval can still surface the relevant record.

For judging, show that the model is local and then demonstrate that switching the embedding model does not cause NVIDEA to compare incompatible vector spaces.
