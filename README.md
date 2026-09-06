# NVIDEA

NVIDEA is the open-source hackathon edition of a Windows-first personal AI operating layer inspired by keyboard.wtf. The goal is not another chat wrapper: NVIDEA is being built as a permissioned agent that can understand desktop context, remember useful information over time, research with provenance, execute multi-step browser and computer workflows, and hand off long-running work to Nebius infrastructure.

## Hackathon target

- **Nebius x NVIDIA Global AI Hackathon**
- Primary track: **Personal AI**
- Bonus target: **Best Use of Tavily**
- Core runtime: **NVIDIA Nemotron on Nebius Token Factory / Nebius AI Cloud**

## Current status

The repository is being built in layers. `src/Nvidea.Core` now contains both the NVIDIA/Nebius inference foundation and the first privacy-aware personal-memory core.

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
- Contract-focused tests using in-memory HTTP/memory stores and deterministic/failing embedding providers; no real cloud credentials are required.

Still under active development: a verified production embedding adapter, memory compaction/summarization, Tavily research, browser automation, skills/permissions, desktop shell integration, resumable jobs, security hardening, packaging and the final hackathon demo.

## Why model routing is conservative

Nebius currently documents Nemotron 3 Nano, Super and Ultra for different workload classes. NVIDEA only hard-codes the exact model identifier that has been verified in current Nebius documentation: Nemotron 3 Super. Fast/deep model IDs are configuration values until their exact Token Factory identifiers are verified. This prevents silent breakage from guessed or stale model names.

## Configuration

Required environment variable:

```powershell
$env:NEBIUS_API_KEY = "your-token-factory-key"
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

The tests mock network/storage dependencies and cover request structure, model routing, bearer authentication, tool-call parsing, retry behavior, endpoint safety, memory privacy policy, session-only persistence, hybrid retrieval, embedding failure fallback, expiry, deduplicated updates and JSON round trips.

## Personal memory contract

Memory is a product/security boundary, not just a larger prompt buffer:

- `Working`: current-session context; session retention is never persisted.
- `Episodic`: interactions and completed actions with provenance.
- `Semantic`: stable preferences/profile facts.
- `Project`: durable project/entity context.
- `Skill`: reusable workflow/skill knowledge.

Search defaults to public/personal memories. Sensitive/restricted memories are excluded unless the caller explicitly expands the allowed sensitivity set. Credential-like material is denied by the write policy even if a caller requests indefinite retention.

Embedding generation is intentionally behind `IMemoryEmbeddingProvider`. Until a production embedding model is verified for the Nebius stack, memory remains useful via deterministic lexical + recency + importance retrieval rather than coupling the core to an unverified model/API.

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
