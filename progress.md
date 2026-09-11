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
- Optional production local semantic embeddings use a loopback-only Ollama `/api/embed` adapter with bounded requests/batches, redirect refusal, model/dimension provenance, incompatible-vector-space protection, explicit desktop opt-in, and a local-only migration/re-index path for stale/provenance-less memories.
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, validated citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
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

### 2026-09-11 — Desktop Nebius lifecycle, dispatch, diagnostics, and voice
Added strict lifecycle/dispatch environment gates, Tavily-independent remote recovery, validated Nebius provider composition, one-shot exact-checkpoint paid dispatch approval, credential-safe readiness diagnostics, successful-start readiness strip/details, focused lifecycle/dispatch/readiness tests, and local review-first Windows voice invocation with explicit microphone consent, cancellation, and no cloud speech dependency.

### 2026-09-11 — Production local semantic memory embeddings
Added embedding provenance contracts, model-space isolation in semantic scoring, loopback-only Ollama `/api/embed` support, bounded requests and batches, finite/dimension/result validation, strict redirect refusal, explicit `NVIDEA_LOCAL_EMBEDDINGS=true` opt-in, trusted desktop composition, regression tests, and `docs/local-memory-embeddings.md`. Old/unavailable/incompatible embeddings fall back to lexical/recency/importance retrieval rather than breaking memory.

### 2026-09-11 — Safe local embedding migration / re-index
Completed:
- Re-read this ledger completely and inspected the current memory models, store, local Ollama provider, `PersonalMemoryService`, recent commits, and focused tests before changing code.
- Added explicit migration contracts: migration target metadata, bounded options, coarse preview candidates/reasons, progress, and result records.
- Added `IMemoryEmbeddingMigrationProvider`; the production Ollama provider now exposes its local migration target, provider batch ceiling, expected dimensions, and compatibility check for current vectors.
- Model compatibility accepts an exact configured tag and, when the configured model is untagged, the corresponding tagged runtime response (for example `embeddinggemma` -> `embeddinggemma:latest`) while still rejecting another provider/model space.
- Converted `PersonalMemoryService` to a partial class without changing existing behavior and added `PersonalMemoryService.Migration.cs` as a cohesive maintenance surface sharing the existing mutation gate.
- Added `PreviewEmbeddingMigrationAsync`, which reports only record ID/layer/coarse sensitivity/reason and target metadata; it does not disclose memory content.
- Default migration excludes `Sensitive` and `Restricted` records. They require separate explicit opt-ins. Session-only memory is never included in the durable migration plan.
- Added `MigrateEmbeddingsAsync` with provider-bounded batching, progress reporting, cancellation, local-only enforcement, strict vector/provenance validation, and no cloud fallback.
- Each successful batch is persisted through the existing protected memory store before the next batch starts. A crash or cancellation therefore leaves completed work durable, and a later preview naturally resumes from the remaining stale records.
- Migration changes only `Embedding` and `EmbeddingProvenance`; it preserves memory content, source provenance, timestamps, sensitivity, retention, confidence, and importance.
- Embedding inference runs outside the main memory mutation gate. Before applying each vector, migration verifies that the immutable record instance is still the exact snapshot embedded. A concurrent edit/search/delete/replacement causes that vector to be discarded and counted as a concurrent-change skip instead of overwriting newer state.
- Added `MemoryEmbeddingMigrationTests.cs` covering default high-sensitivity exclusion, provider batch-bound persistence/resumability, concurrent-edit protection, and rejection of non-local migration providers.
- Added `docs/memory-embedding-migration.md` documenting privacy, resumability, cancellation, concurrent-edit safety, and stale-space detection.

Engineering commits before this ledger update:
- `04a85f38d393345379adc4bd6bfc2550f7b28da8` — add safe embedding migration contracts.
- `69d911c46a1984f690fe4d505918a607bba886ed` — expose local embedding migration target.
- `07cc785b130ba40b6658d015d71e3fb7008b7100` — enable safe memory migration extension.
- `dd0c8cef7b9db8da61e9e9c76e08d70e328c8c36` — bound embedding migration batches.
- `d750db4c1be0401ad2ee172c1a4db63aaa567e37` — bound local embedding migration metadata and correct tagged-model compatibility code.
- `d3003e600b499a0336a6d64b26c637774536ebe1` — add resumable local embedding migration.
- `bb96829fdfeb49ac068c209064f6cf3baa28e385` — test safe local embedding migration.
- `a5ec9397fd28893ed4d5d0522dcecfd0782d364d` — document safe embedding migration.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `c3a9d76d7a8094ae9cd49f943d12fbe7ba90a481` to engineering head `a5ec9397fd28893ed4d5d0522dcecfd0782d364d` is **8 commits ahead / 0 behind** and changes exactly six focused files: migration docs, Ollama migration metadata, memory contracts, the migration partial, the one-line partial declaration change, and focused migration tests.
- The execution environment again exposes no usable `dotnet` command or SDK output. Compilation and test execution are therefore **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, paid Nemotron/Tavily calls, or remote embedding services were used.

Security / privacy / failure review:
- Migration refuses providers that do not explicitly declare a local target; the production target remains the loopback-only Ollama adapter.
- No automatic cloud fallback exists for re-indexing personal memory.
- Sensitive and Restricted records are fail-closed by default and require separate explicit options.
- Preview/progress surfaces do not include memory content.
- Returned embeddings must be finite, dimensionally consistent, locally provenanced, and compatible with the approved target before persistence.
- Provider errors stop the explicit migration operation; existing memory remains available through the normal deterministic retrieval fallback.
- Cancellation propagates to local inference and prevents subsequent batches.
- Per-batch persistence provides restart-safe progress; concurrent user/runtime changes are skipped rather than overwritten.
- Existing memory write policy, edit/delete/retention controls, browser/research permission gates, audit semantics, emergency stop, and cloud lifecycle behavior remain unchanged.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML and tests are not compiled or executed here.
- The local Ollama adapter and migration path require a real .NET 8 build/test pass before they can be claimed executable.
- A real Windows machine still needs microphone permission plus an installed desktop speech recognizer/language for voice validation.
- Local semantic embedding quality still needs retrieval evaluation against a real embedding-capable model; tests currently validate contracts/safety/model-space correctness rather than model quality.
- Migration is currently a Core API and documented maintenance surface; it is not yet exposed as a polished WPF preview/progress/cancel workflow.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; compile WPF/XAML; run the focused memory migration/local-embedding, memory, voice, readiness, lifecycle-only recovery, research dispatch/cloud-mode, browser authority/integration, API-surface, and security suites; then run a real Ollama `embeddinggemma` retrieval-quality evaluation plus microphone recognition/cancel/timeout cycle and fix every compile/runtime defect. If executable validation remains unavailable, integrate the migration API into a **polished WPF Memory maintenance flow** with privacy-safe preview counts, explicit Sensitive/Restricted opt-ins, progress/cancel, and no content disclosure, then add deterministic retrieval-quality fixtures/evals that compare lexical-only vs semantic ranking without relying on a live model.
