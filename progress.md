# NVIDEA Hackathon Progress

## Mission
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must remain independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Product / Architecture State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes, durable provenance, hybrid lexical/semantic/recency/importance retrieval, edit/delete/retention controls, and deterministic fallback when embeddings are unavailable.
- Optional production local semantic embeddings now use a loopback-only Ollama `/api/embed` adapter with bounded requests/batches, redirect refusal, model/dimension provenance, incompatible-vector-space protection, and explicit desktop opt-in.
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, validated citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Concrete privileged persistence/runtime boundaries are Core-only where appropriate, including durable agent/research stores, raw browser-host/session construction, goal-session storage, persistent Playwright transport, and download staging/quarantine authorities.
- Product research flows through `ResearchProductRuntime`; provider-aware remote execution flows through `ResearchCloudExecutionCoordinator`; WPF durable research uses lifecycle-aware product/UI projections.
- Desktop Nebius research lifecycle is explicitly opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE=true`; lifecycle-only recovery remains available without Tavily so already-remote work can still be reconciled/cancelled.
- New paid dispatch is separately opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH=true`; it requires lifecycle support, local research availability, and one-shot exact-checkpoint approval in WPF.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol; deployment preflight enforces mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned worker image, RSA identity consistency, bounded resources, and redacted fingerprints.
- `Nvidea.NebiusContractProbe` supports planner, zero-cost live preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.
- Desktop diagnostics use `DesktopResearchReadiness`; successful-start UI distinguishes ready/blocked/locked research capabilities without exposing secrets or creating provider authority.
- Windows voice invocation exists as a local, review-first path: `Ctrl+Shift+V` or Voice asks for explicit microphone consent, transcribes with the installed Windows speech recognizer, and places text into the prompt for review without auto-running it or routing audio to cloud speech.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current lifecycle states, live runtime factory, explicit live modes, reproducible/redacted deployment fingerprints, MysteryBox validation, machine-readable PASS evidence, atomic artifact persistence, strict evidence verification, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product lifecycle and authority hardening
Added `ResearchCloudExecutionCoordinator`, `ResearchProductRuntime`, WPF lifecycle-aware research, explicit reconciliation/disclosure UX, `ResearchProductUiState`, `BrowserProductRuntime`, restart-safe browser-goal recovery, and assembly-internal construction for privileged persistence/browser/runtime authorities with API-surface regression tests.

### 2026-09-11 — Desktop Nebius lifecycle, dispatch, and diagnostics
Added strict lifecycle/dispatch environment gates, Tavily-independent remote recovery, validated Nebius provider composition, one-shot exact-checkpoint paid dispatch approval, credential-safe readiness diagnostics, successful-start readiness strip/details, and focused lifecycle/dispatch/readiness tests plus docs.

### 2026-09-11 — Local review-first Windows voice invocation
Added least-authority local voice contracts, `System.Speech`/SAPI one-shot transcription, explicit microphone consent, `Ctrl+Shift+V`, 20-second bound, emergency-stop/window-close cancellation, low-confidence review labeling, transcript validation tests, and `docs/local-voice.md`. Voice transcripts never auto-execute.

### 2026-09-11 — Production local semantic memory embeddings
Completed:
- Re-read this ledger completely and inspected the current memory models, `PersonalMemoryService`, desktop composition root, tests, and recent commits before implementation.
- Verified current official Ollama `/api/embed` behavior: POST endpoint, text-or-array input, returned model/vector arrays, optional dimensions, and `truncate=false` fail behavior rather than silent truncation.
- Added `MemoryEmbeddingProvenance` and `MemoryEmbeddingVector` plus provenanced/batch embedding interfaces while preserving the original `IMemoryEmbeddingProvider` compatibility surface.
- Extended `MemoryRecord` with optional embedding provenance so old persisted records remain loadable while new vectors carry provider/model/dimension/locality/time metadata.
- Updated `PersonalMemoryService` to persist provenance and compare semantic vectors only when provider, model, and dimensions match. A model switch can no longer cause cosine comparison across incompatible semantic spaces; those records fall back to lexical/recency/importance scoring.
- Kept deterministic retrieval fallback: provider failures, timeouts, malformed vectors, missing vectors, or incompatible provenance do not make memory unavailable.
- Added `LocalOllamaMemoryEmbeddingProvider`, a real loopback local adapter for Ollama `/api/embed` with `truncate=false`, bounded input length, bounded batch size, bounded timeout, max-dimension guard, finite-value validation, result-count validation, consistent-dimension validation, optional expected-dimension pinning, credential-in-URI rejection, HTTP(S)-only loopback restriction, and redirect refusal.
- Corrected the JSON contract during static review to use web/camel-case serialization options for both request and response and fixed the named `Truncate` argument before recording the run.
- Hardened constructor resource ownership so options are validated before allocating the owned HTTP client.
- Added `LocalMemoryEmbeddingConfiguration` with explicit `NVIDEA_LOCAL_EMBEDDINGS=true` opt-in and optional endpoint/model/dimension environment settings. Disabled/missing opt-in leaves existing lexical/recency behavior unchanged.
- Wired the optional local provider into the trusted `NvideaCompositionRoot`; WPF/plugin code continues to receive `PersonalMemoryService`, not raw embedding HTTP transport. The root owns and disposes the provider.
- Added `LocalOllamaMemoryEmbeddingProviderTests.cs` covering loopback API contract/provenance, remote-endpoint rejection, dimension mismatch, redirect rejection, durable provenance, and the crucial no-cross-model-comparison invariant.
- Added `docs/local-memory-embeddings.md` with setup, safety, provenance, fallback, and demo guidance.

Engineering commits before this ledger update:
- `486ca3f771ae38c04cc304c0d8ad85dfdc2d3c4e` — add loopback local memory embedding provider.
- `c05c647fe1b969915faebaf28186848c4e2f7a85` — add embedding provenance contracts.
- `bd656ab2c9ad280afa0a5af86350a94270113caa` — persist embedding provenance and prevent model mixing.
- `f99d2e675f5f82b645c0f198fb1e5352cc487718` — test local embedding provider safety and provenance.
- `bf83a443fac86dc5d721eedeb776e9adc4e4625e` — fix local embedding JSON contract parsing.
- `1f1fb02373cd92e4bea223a9071cc57978f2faea` — tighten local embedding regression tests.
- `8550860c2a6d4113c125cdce30d258b1597baf4e` — add explicit local embedding configuration.
- `c349c872edc1f043aa98c1f5ae382580b5cfe36c` — wire opt-in local embeddings into desktop memory.
- `6869098efc679f1e715c9e5fd7657ba6367cf173` — validate local embedding options before allocation.
- `bbdd9c95be0e402f678626b4871cdc4159ce6eeb` — document local semantic memory embeddings.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `f6f3241e9e89732a88c5142839ef12cc80e93e49` to engineering head `bbdd9c95be0e402f678626b4871cdc4159ce6eeb` is **10 commits ahead / 0 behind** and changes exactly seven focused files: embedding docs, composition wiring, local configuration, local provider, memory contracts, memory service scoring/provenance, and focused tests.
- Current official Ollama docs were checked on 2026-09-11 before implementing the HTTP contract.
- `command -v dotnet` and `dotnet --info` again produced no usable .NET SDK signal in this execution environment. Compilation and test execution are therefore **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, paid Nemotron/Tavily calls, or remote embedding services were used.

Security / privacy / failure review:
- Desktop embeddings are disabled by default and opt-in only.
- The production adapter accepts only HTTP(S) loopback endpoints and refuses redirects, reducing SSRF/data-exfiltration risk from the personal-memory path.
- The adapter sends only embedding text to the configured local loopback runtime; it does not receive Nebius/Tavily credentials, browser credentials, approval grants, durable research state, or cloud signing keys.
- Model/version and dimension provenance is durable. Semantic scoring refuses cross-provider/cross-model/cross-dimension comparison.
- Malformed/non-finite/empty/oversized vectors and inconsistent batch shapes fail closed into the existing deterministic retrieval path.
- Existing memory sensitivity/write policy, retention/delete controls, research/browser permission gates, audit semantics, emergency stop, and cloud lifecycle behavior remain unchanged.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML and tests are not compiled or executed here.
- The local Ollama adapter and its JSON/test contract require a real .NET 8 build/test pass before they can be claimed executable.
- A real Windows machine still needs microphone permission plus an installed desktop speech recognizer/language for voice validation.
- Local semantic embedding quality still needs retrieval evaluation against a real embedding-capable model; tests currently validate contracts/safety/model-space correctness, not model quality.
- Existing persisted vectors without the new provenance intentionally stop contributing semantic cosine score until rewritten/re-embedded; lexical/recency/importance retrieval remains available.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; compile WPF/XAML; run the focused local-embedding, memory, voice, readiness, lifecycle-only recovery, research dispatch/cloud-mode, browser authority/integration, API-surface, and security suites; then run a real Ollama `embeddinggemma` retrieval-quality evaluation plus microphone recognition/cancel/timeout cycle and fix every compile/runtime defect. If executable validation remains unavailable, add a **bounded embedding migration/re-index service** that can re-embed provenance-less or stale-model memories locally with preview/progress/cancellation, per-sensitivity safeguards, crash-safe persistence, and no automatic cloud fallback.
