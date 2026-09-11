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
- Windows now exposes a local Memory maintenance workflow for previewing and safely re-indexing stale/missing local semantic embeddings with explicit high-sensitivity opt-ins, stale-preview revalidation, progress/cancellation, and no memory-content disclosure.

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
Added explicit local-only migration contracts and target metadata; privacy-safe preview; default exclusion of Sensitive/Restricted records; bounded per-provider batching; progress/cancellation; per-batch protected persistence; stale-model/provenance detection; concurrent-edit protection; focused regression tests; and `docs/memory-embedding-migration.md`. Migration changes only vectors/provenance and preserves content, memory/source provenance, timestamps, sensitivity, retention, confidence, and importance.

### 2026-09-11 — WPF Local memory maintenance workflow
Completed:
- Re-read this ledger completely and inspected the current memory migration API, composition root, Windows shell, voice/research busy-state handling, XAML, docs, and existing migration tests before changing code.
- Added `MainWindow.Memory.cs` as the product-facing WPF maintenance workflow over the existing trusted `PersonalMemoryService` authority. The UI never constructs or receives raw Ollama/HTTP provider authority.
- Added a **Preview re-index** action that shows only local provider/model metadata plus aggregate candidate and sensitivity counts. Candidate IDs, memory keys/content, source URIs, vectors, and raw provider errors are not displayed.
- Added separate unchecked **Include Sensitive** and **Include Restricted** controls. Changing either privacy scope invalidates the prior preview and forces a new preview before execution.
- Added stale-consent protection: immediately before execution NVIDEA re-runs the Core migration preview and requires the full reviewed plan (target, exclusions, ordered candidates/reasons/sensitivities) to still match. If durable memory changed, re-indexing does not start and the user must review the refreshed scope.
- Added a final confirmation describing the exact local provider/model, total durable candidate count, and any Sensitive/Restricted records included by the selected scope. `ForceReembedCurrent` is intentionally not exposed in WPF.
- Added aggregate progress UI and a dedicated **Cancel re-index** action. The global Emergency stop and window close also cancel active migration. Completed batches remain protected/persisted by the existing Core migration semantics and a later preview naturally resumes remaining stale records.
- Added product-level cross-workflow exclusion: while migration is active, desktop inference, browser work, browser recovery, durable research action buttons, and voice capture are disabled. The global voice hotkey also refuses to begin microphone capture while migration is active.
- Migration progress intentionally omits Core's `LastProcessedId`; only total/processed/updated/concurrent-change counts are surfaced.
- Updated `docs/memory-embedding-migration.md` with the Windows workflow, consent/revalidation boundary, local-only disclosure, cancellation/resume behavior, and UI non-disclosure guarantees.

Engineering commits before this ledger update:
- `f1d02dcaf36e2404072d303be7dd32a3d59f8ebf` — add safe memory maintenance workflow.
- `049e6612de0771719246d9b0b8c0f53f05d9ba5a` — integrate memory maintenance cancellation.
- `ce74a985b6a3cdc6db0643a75f6c83f680680e94` — add memory maintenance panel.
- `800d2d0815bfbb085eba23ff93071edf4c9c7800` — coordinate memory maintenance with other desktop work.
- `c99dbfa89b267ddd86871c9e9d81d55642ce95ab` — block voice capture during memory maintenance.
- `838450d66b5e0f90896ca3cd0d7239d83f457155` — isolate memory maintenance from research and voice.
- `6019af1926bbdf2aa25bb72bdafdfa200b43eb43` — document desktop memory maintenance flow.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `9957759dda35be0e286ad8e1fbd1ee44192cfa6b` to engineering head `6019af1926bbdf2aa25bb72bdafdfa200b43eb43` is **7 commits ahead / 0 behind**.
- The execution environment again exposes no usable `dotnet` command/SDK output. Core/WPF/Worker compilation, XAML compilation, and test execution are therefore **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Ollama embedding runtime, Nebius credentials/resources, Object Storage operations, Serverless jobs, or paid Nemotron/Tavily calls were used.

Security / privacy / failure review:
- The desktop migration path remains local-only through the existing `IMemoryEmbeddingMigrationProvider` guard and production loopback-only Ollama transport; there is no cloud fallback.
- Sensitive and Restricted records remain fail-closed by default and require distinct explicit opt-ins. A privacy-scope change invalidates the reviewed plan.
- Re-index approval is not reusable across stale state: durable memory is re-previewed immediately before final confirmation/run, and any plan drift blocks execution.
- No memory payload, candidate ID, source URI, embedding vector, local endpoint, or raw provider exception crosses into the maintenance status UI.
- Only aggregate progress is shown; the Core progress record's last-memory identifier is deliberately suppressed.
- Emergency stop, dedicated cancellation, and window close propagate cancellation; existing per-batch persistence provides restart-safe progress.
- Existing Core concurrent-edit protection remains authoritative, so a memory changed while local inference is running is skipped rather than overwritten.
- Memory maintenance does not create Nebius/Tavily/browser authority and is mutually exclusive with other active desktop/voice/research/browser operations.
- Existing memory write policy, edit/delete/retention controls, browser/research permission gates, audit semantics, cloud lifecycle behavior, and normal deterministic retrieval fallback remain unchanged.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML and tests are not compiled or executed here.
- The new WPF maintenance XAML/event bindings, partial-class integration, dynamic Voice-button coordination, and Windows busy-state behavior require a real Windows .NET 8 build/run pass before executable correctness can be claimed.
- The local Ollama adapter/migration path still needs a real `embeddinggemma` retrieval-quality run; current tests validate contracts/safety/model-space correctness rather than semantic model quality.
- A real Windows machine still needs microphone permission plus an installed desktop speech recognizer/language for voice validation.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; compile WPF/XAML; run the focused memory migration/local-embedding, memory, voice, readiness, lifecycle-only recovery, research dispatch/cloud-mode, browser authority/integration, API-surface, and security suites; then exercise the Memory maintenance preview/opt-in/revalidation/progress/cancel workflow against local Ollama and fix every compile/runtime defect. If executable validation remains unavailable, add **deterministic retrieval-quality evaluation fixtures** that compare lexical-only and semantic ranking using a fake deterministic embedding provider, including synonym/paraphrase wins, stale/incompatible model-space fallback, sensitivity filtering, and ranking-regression thresholds without relying on a live model.
