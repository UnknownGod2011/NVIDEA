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
- Layered personal memory with privacy-aware writes, durable provenance, hybrid lexical/semantic/recency/importance retrieval, edit/delete/retention controls, deterministic fallback when embeddings are unavailable, and deterministic retrieval-quality regression fixtures.
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
- Windows exposes a local Memory maintenance workflow for previewing and safely re-indexing stale/missing local semantic embeddings with explicit high-sensitivity opt-ins, stale-preview revalidation, progress/cancellation, and no memory-content disclosure.

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

### 2026-09-11 — Safe local embedding migration + WPF maintenance
Added explicit local-only migration contracts and target metadata; privacy-safe preview; default exclusion of Sensitive/Restricted records; bounded batching; progress/cancellation; per-batch protected persistence; stale-model/provenance detection; concurrent-edit protection; and `docs/memory-embedding-migration.md`. WPF now exposes Preview re-index, separate Sensitive/Restricted opt-ins, stale-consent revalidation immediately before execution, final local provider/model disclosure, aggregate progress only, dedicated cancellation, emergency-stop/window-close cancellation, and mutual exclusion with browser/research/voice/inference work. `ForceReembedCurrent` remains intentionally unavailable in the UI.

### 2026-09-11 — Deterministic semantic-memory retrieval quality evals
Completed:
- Re-read this ledger completely and inspected `PersonalMemoryService`, memory models, existing memory tests, migration tests, and current retrieval weighting before changing code.
- Added `tests/Nvidea.Core.Tests/PersonalMemoryRetrievalQualityTests.cs` as a deterministic, model-free retrieval-quality gate.
- Added a synonym fixture where lexical-only retrieval is deliberately attracted to a keyword trap while semantic retrieval must recover the intended memory and maintain at least a **0.20 score margin** over the runner-up. This catches material ranking-weight regressions instead of merely checking that embeddings are called.
- Added a paraphrase fixture with no shared target keywords; lexical-only retrieval returns the Tokyo keyword trap while semantic retrieval must recover the semantically intended lodging preference.
- Added an incompatible embedding-space fixture proving that a current query vector is not compared against memories tagged with an older model. The semantic-enabled service must produce the same ordering/scores as lexical fallback and report zero semantic score for incompatible records.
- Added a sensitivity fixture proving a perfect semantic match cannot bypass the default Public/Personal sensitivity boundary; Sensitive memory only becomes eligible after explicit query-level sensitivity opt-in.
- Fixtures use a deterministic provenanced fake embedding provider and fixed time source, so the tests require no Ollama model, network, API key, paid service, or nondeterministic ML inference.

Engineering commit before this ledger update:
- `638ad6951c72f4f0a3121a2a7832f85166d91288` — add deterministic memory retrieval quality evals.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `59055126c929a71dcf998971df10bd5dbf7d6fbf` to engineering head `638ad6951c72f4f0a3121a2a7832f85166d91288` is **1 commit ahead / 0 behind**, with exactly one added test file and no production/runtime behavior changed.
- The current execution environment still provides no trusted usable .NET 8 execution signal, so Core/WPF/Worker compilation and test execution are **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Ollama runtime, Nebius credentials/resources, Object Storage operation, Serverless job, Nemotron call, or Tavily call was used.

Security / privacy / failure review:
- Eval fixtures operate entirely on synthetic in-memory records and do not contain user data, secrets, source URIs, provider credentials, or external network calls.
- The new tests explicitly protect the sensitivity boundary and incompatible-vector fallback rather than weakening either for recall quality.
- No production scoring weights were changed without executable evidence; the current ranking behavior is captured first as a regression baseline.
- Existing memory write policy, retention/edit/delete controls, migration privacy policy, browser/research approval gates, audit semantics, cloud lifecycle behavior, and deterministic embedding-failure fallback remain unchanged.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML and tests are not compiled or executed here.
- The WPF maintenance XAML/event bindings, partial-class integration, dynamic Voice-button coordination, and Windows busy-state behavior require a real Windows .NET 8 build/run pass before executable correctness can be claimed.
- The new retrieval-quality fixtures are deterministic contract/eval tests; real `embeddinggemma` semantic quality and ranking calibration still require a local Ollama evaluation corpus.
- A real Windows machine still needs microphone permission plus an installed desktop speech recognizer/language for voice validation.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and Nebius contract tools; compile WPF/XAML; run the focused retrieval-quality, memory migration/local-embedding, memory, voice, readiness, lifecycle-only recovery, research dispatch/cloud-mode, browser authority/integration, API-surface, and security suites; then exercise Memory maintenance against local Ollama and fix every compile/runtime defect. If executable validation remains unavailable, build a **deterministic end-to-end Personal AI demo/eval harness** that exercises context → memory recall → Tavily-style cited research fixtures → permission-gated browser plan/verification → resumable task state with synthetic adapters, producing machine-readable pass/fail evidence without live credentials or paid calls.
