# NVIDEA

NVIDEA is the open-source hackathon edition of a Windows-first personal AI operating layer inspired by keyboard.wtf. The goal is not another chat wrapper: NVIDEA is being built as a permissioned agent that can understand desktop context, remember useful information over time, research with provenance, execute multi-step browser and computer workflows, and hand off long-running work to Nebius infrastructure.

## Hackathon target

- **Nebius x NVIDIA Global AI Hackathon**
- Primary track: **Personal AI**
- Bonus target: **Best Use of Tavily**
- Core runtime: **NVIDIA Nemotron on Nebius Token Factory / Nebius AI Cloud**

## Current status

The repository is being built in layers. `src/Nvidea.Core` now contains the NVIDIA/Nebius inference foundation, privacy-aware personal memory, and a Tavily-backed research core connected to Nemotron planning/synthesis.

Implemented now:

- OpenAI-compatible Nebius Token Factory client implemented directly over `HttpClient`.
- Verified default model: `nvidia/nemotron-3-super-120b-a12b`.
- Structured tool/function definitions and tool-call parsing.
- Optional JSON-schema structured-output requests.
- Retry handling for timeouts, HTTP 429 and 5xx responses.
- Per-request timeout and cancellation support.
- Endpoint validation and secret-safe API errors.
- Workload routing abstraction for fast/standard/deep tasks without inventing unverified model IDs.
- Typed personal-memory layers: working, episodic, semantic, project and skill memory.
- Memory provenance, importance, confidence, sensitivity and retention metadata.
- Hybrid retrieval combining semantic similarity (when an embedding provider is configured), lexical relevance, recency and importance.
- Deterministic lexical fallback when embeddings are unavailable; unrelated recent memories are filtered rather than returned simply because they are recent.
- Privacy-aware write policy: likely credentials/private keys/tokens are never persisted; sensitive and indefinite-retention memories require explicit approval.
- Session memory is held in process and deliberately excluded from persistent storage.
- Atomic JSON persistence, expiry cleanup, update-in-place by layer/key, deletion controls and embedding-provider abstraction.
- Tavily research provider with multi-query batching, `general`/`news` topic support, date bounds, domain filters, bounded retries/timeouts/cancellation and usage-credit tracking.
- Query-focused Tavily Extract enrichment for top-ranked sources using advanced extraction, bounded URL/chunk counts, markdown output and exact usage-credit accounting.
- Extraction failures degrade to the original search evidence rather than discarding the research run; per-source extraction failures remain explicit warnings.
- Canonical URL normalization and source deduplication that strips common tracking parameters and retains the stronger provider result.
- Deterministic evidence-quality ranking that combines Tavily relevance with conservative authority heuristics, timestamp/date-window freshness evidence and cross-host diversity penalties before synthesis.
- Freshness is never fabricated: when the provider has no publication timestamp, NVIDEA distinguishes a bounded search-window signal from unknown freshness and emits explicit warnings for unverified or stale news evidence.
- Authority scoring is intentionally heuristic and modest; syntactic `.gov`/`.edu`/country-code institutional suffixes are recognized, while deceptive subdomains such as `gov.example.com` are not treated as government sources.
- Typed source provenance/citation objects instead of reducing research to a plain answer string.
- An explicit untrusted-web-content envelope for prompt-injection resistance before evidence reaches Nemotron.
- Nemotron-powered query planning through the existing Nebius structured-output path.
- Nemotron research synthesis that receives deterministic quality metadata separately from untrusted web text, prefers extracted evidence, requires `[src:SOURCE_ID]` markers and validates referenced IDs against the collected evidence set.
- Contract-focused tests using in-memory HTTP/memory/inference/provider fakes; no real cloud credentials are required.

Still under active development: a verified production embedding adapter, memory compaction/summarization, richer durable research checkpoints, browser automation polish, skills/permissions, desktop-shell integration, resumable jobs, security hardening, packaging and the final hackathon demo.

## Why model routing is conservative

Nebius currently documents Nemotron 3 Nano, Super and Ultra for different workload classes. NVIDEA only hard-codes the exact model identifier that has been verified in current Nebius documentation: Nemotron 3 Super. Fast/deep model IDs are configuration values until their exact Token Factory identifiers are verified. This prevents silent breakage from guessed or stale model names.

## Configuration

Required environment variables for live cloud use:

```powershell
$env:NEBIUS_API_KEY = "your-token-factory-key"
$env:TAVILY_API_KEY = "your-tavily-key"
```

Optional overrides:

```powershell
$env:NVIDEA_NEBIUS_BASE_URL = "https://api.tokenfactory.us-central1.nebius.com/v1/"
$env:NVIDEA_MODEL_STANDARD = "nvidia/nemotron-3-super-120b-a12b"
$env:NVIDEA_MODEL_FAST = "<verified-token-factory-model-id>"
$env:NVIDEA_MODEL_DEEP = "<verified-token-factory-model-id>"
```

Do not commit API keys. NVIDEA's future Windows shell will store user secrets using OS-backed encrypted storage rather than plaintext configuration.

## Build and test

Prerequisite: .NET 8 SDK.

```powershell
dotnet build .\src\Nvidea.Core\Nvidea.Core.csproj
dotnet test .\tests\Nvidea.Core.Tests\Nvidea.Core.Tests.csproj
```

The tests mock network/storage dependencies and cover request structure, model routing, bearer authentication, tool-call parsing, retry behavior, endpoint safety, memory privacy policy, session-only persistence, hybrid retrieval, embedding failure fallback, expiry, deduplicated updates, JSON round trips, Tavily Search/Extract request shapes, canonical URL deduplication, rate-limit retries, extraction fallback behavior, prompt-injection boundaries, research planning, deterministic authority/freshness/diversity ranking and citation-ID validation.

## Personal memory contract

Memory is a product/security boundary, not just a larger prompt buffer:

- `Working`: current-session context; session retention is never persisted.
- `Episodic`: interactions and completed actions with provenance.
- `Semantic`: stable preferences/profile facts.
- `Project`: durable project/entity context.
- `Skill`: reusable workflow/skill knowledge.

Search defaults to public/personal memories. Sensitive/restricted memories are excluded unless the caller explicitly expands the allowed sensitivity set. Credential-like material is denied by the write policy even if a caller requests indefinite retention.

Embedding generation is intentionally behind `IMemoryEmbeddingProvider`. Until a production embedding model is verified for the Nebius stack, memory remains useful via deterministic lexical + recency + importance retrieval rather than coupling the core to an unverified model/API.

## Research safety and provenance contract

Research is implemented as a pipeline rather than a single opaque search call:

1. Nemotron creates a bounded, structured query plan.
2. `TavilyResearchClient` executes those searches with cancellation, retry and endpoint controls.
3. Results are normalized and deduplicated by canonical URL while preserving source IDs and provider scores.
4. The strongest bounded subset is re-read through query-focused **Tavily Extract** so synthesis can rely on source-page evidence rather than snippets alone.
5. Extracted content replaces only the matching source's evidence; failed extraction retains the original search evidence and emits a warning.
6. `ResearchEvidenceRanker` deterministically reorders evidence using relevance, conservative host authority, publication/search-window freshness and host diversity. Unknown freshness and stale current-event evidence become explicit warnings rather than silently high-confidence facts.
7. Quality metadata is labeled as a local heuristic and kept separate from source text; web text itself is wrapped as **untrusted evidence**, explicitly preventing source text from becoming agent instructions.
8. Nemotron synthesizes only from the evidence and is instructed to emit exact `[src:SOURCE_ID]` markers.
9. The engine resolves only markers that actually exist in collected evidence and surfaces unknown/missing markers as warnings.

This makes provenance machine-checkable, keeps Tavily central to both discovery and evidence acquisition, and creates a security boundary that the autonomous browser/skill system can reuse.

## Target architecture

```text
Windows interaction shell
  hotkeys / voice / text / active-app context
                    |
                    v
              Personal AI core
        plan -> act -> observe -> verify
          /          |             \
         v           v              v
      Memory       Skills       Risk/approval
         \           |              /
          \          v             /
             Nemotron routing
                    |
          Nebius Token Factory
                    |
      +-------------+-------------+
      |                           |
      v                           v
 Tavily research             Tool execution
                              browser / OS
      |                           |
      +-------------+-------------+
                    v
          verified task outcome

Long-running cloud-safe work -> Nebius Serverless
Private OS actions            -> local Windows runtime
```

## Personal AI safety contract

NVIDEA is designed around least privilege:

- read-only observation should be separated from actions;
- consequential actions such as send, submit, publish, purchase, deletion, account/security changes and financial actions require explicit approval;
- browser content and tool output are untrusted input and must not silently redefine system policy;
- CAPTCHA, login, browser/OS permission boundaries and site safeguards must never be bypassed;
- every autonomous action should become auditable and cancellable;
- cloud inference should receive only the data required for the current task;
- credentials and authentication secrets must not be stored as personal memory.

## Relationship to keyboard.wtf

keyboard.wtf is used as read-only reference material for the interaction patterns that already work well: Windows hotkeys, voice UI, local speech, allow-listed actions, context capture, workflows and permission concepts. NVIDEA is a separate repository and is intended to materially upgrade the backend architecture, especially memory, research, complex browser automation, long-running work and open-model infrastructure.

No automation working on NVIDEA is permitted to mutate the keyboard.wtf repository.

## Development ledger

See [`progress.md`](progress.md) for the current architecture decisions, verified progress, known gaps, risks and next engineering priority.

## License

MIT. See [`LICENSE`](LICENSE).
