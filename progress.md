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
- Layered personal memory with privacy-aware writes, durable provenance, hybrid lexical/semantic/recency/importance retrieval, edit/delete/retention controls, deterministic fallback when embeddings are unavailable, deterministic retrieval-quality regression fixtures, and local-only embedding maintenance.
- Optional production local semantic embeddings use a loopback-only Ollama `/api/embed` adapter with bounded requests/batches, redirect refusal, model/dimension provenance, incompatible-vector-space protection, explicit desktop opt-in, and a local-only migration/re-index path for stale/provenance-less memories.
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, validated citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify execution, prompt-injection/tool-output trust boundaries, consequential-action permission gates, durable download quarantine, emergency stop, and crash recovery.
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
- `tools/Nvidea.PersonalAiDemoEval` now provides a deterministic, credential-free, machine-readable cross-cutting Personal AI quality gate over real Core contracts with synthetic external edges.

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
Added `PersonalMemoryRetrievalQualityTests.cs` with deterministic model-free fixtures for synonym/paraphrase recovery, a 0.20 semantic ranking margin, incompatible-vector-space fallback, and sensitivity-boundary enforcement. Production ranking weights were intentionally left unchanged until executable evidence is available.

### 2026-09-11 — Deterministic end-to-end Personal AI demo evaluator
Completed:
- Re-read this ledger completely and inspected the current desktop invocation, memory, research, browser safety/verification, approval, audit, and resumable-job contracts before implementation.
- Added `tools/Nvidea.PersonalAiDemoEval/Nvidea.PersonalAiDemoEval.csproj` as a .NET 8 console quality gate referencing the real `Nvidea.Core` product contracts.
- Added `tools/Nvidea.PersonalAiDemoEval/Program.cs` with deterministic synthetic external adapters and stable pass/fail checks spanning the judge-visible Personal AI flow rather than isolated helper methods.
- The evaluator writes a real Personal memory, then invokes the real `DesktopInvocationService` and verifies that durable preference memory + selected-text context influence the trusted inference boundary while a sentinel clipboard value remains withheld when clipboard permission is not opted in.
- The evaluator runs the real `ResearchEngine` through plan → evidence preparation/ranking → synthesis using a fixture research provider, then verifies two machine-readable source markers survive citation validation.
- The evaluator runs the real `BrowserAgentExecutor` and `BrowserSafetyPolicy` against a fixture DOM/action target. A Submit action must be classified High risk, request approval exactly once, execute only after approval, and pass a fresh post-action verification before counting as successful.
- The evaluator creates a real `ResumableJobOrchestrator` job, persists a research checkpoint, reconstructs the orchestrator to simulate process restart, resumes into `WaitingForApproval`, mints an exact-scope ephemeral approval through the real orchestrator, consumes it once at the consequential step, and requires auditable `job.approved` + `job.completed` events.
- The same job is marked background-beneficial but private-OS-data-bearing; the real `ConservativeJobExecutionPolicy` must keep it local, proving private context does not become cloud-eligible merely because background execution would be useful.
- Output is indented JSON with `schemaVersion`, `generatedAt`, `overallPassed`, stable check IDs, non-secret evidence summaries, and compact metrics. `--output <path>` persists the artifact and exit code is 0 only when every check passes.
- Added `docs/personal-ai-demo-eval.md` describing invocation, evidence schema, privacy boundaries, exactly what a green result proves, and—critically—what it does **not** prove about live Nebius/Tavily/Playwright/Windows integration.
- Static review caught and fixed an invalid attempt to construct/subclass the sealed internal `JobExecutionContext`; the evaluator now relies exclusively on the orchestrator's context-aware execution hook for consequential approval consumption.

Engineering commits before this ledger update:
- `24f9539d436176f5e4c8237fe3e343b22cc160f8` — add deterministic personal AI demo evaluator project.
- `a4567e52b0f4c1eb5918430bc2a09f77b3f14098` — implement deterministic personal AI demo evaluator.
- `4a3052966570977629e767592ef8be7017a82bd9` — fix demo evaluator job execution boundary.
- `fbf53c08f054ede85eb04a3ec9fbb7030b15251a` — document deterministic personal AI demo evaluator.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `ba22ebc1e8a713eb2ee4c9676263f9dabfd2d074` to engineering head `fbf53c08f054ede85eb04a3ec9fbb7030b15251a` is **4 commits ahead / 0 behind** and changes only `tools/Nvidea.PersonalAiDemoEval/{Nvidea.PersonalAiDemoEval.csproj,Program.cs}` plus `docs/personal-ai-demo-eval.md`.
- `command -v dotnet` and `dotnet --info` again produced no usable .NET execution signal in the available runtime. Therefore the evaluator, Core/WPF/Worker compilation, XAML compilation, and tests are **not claimed as executed or passing**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron calls, Tavily calls, Playwright browser, Ollama runtime, or paid service was used by this run.

Security / privacy / failure review:
- The evaluator uses synthetic fixtures only and is explicitly documented as product-contract evidence, not live-provider evidence.
- The clipboard sentinel check fails if private clipboard content crosses the non-opted-in desktop inference boundary.
- Browser success requires both approval for the consequential Submit target and post-action verification; driver-reported success alone is insufficient.
- Restart simulation uses the durable job store but not persisted approval authority; the exact execution grant is created only after explicit resume approval and is consumed once by `JobExecutionContext`.
- Private OS data remains local under the actual conservative execution policy.
- The evidence artifact is designed to contain no API keys, browser cookies, real user memories, provider resource IDs, raw cloud errors, or live external content.
- The evaluator cannot substitute for real integration validation, and the documentation states this explicitly to prevent hackathon/demo overclaiming.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML, tests, and the new evaluator are not compiled or executed here.
- The new evaluator has been statically reviewed but still requires a real .NET 8 build/run before its JSON PASS output can be treated as executable evidence.
- The WPF maintenance XAML/event bindings, voice integration, dynamic busy-state coordination, and Windows-specific behavior require a real Windows .NET 8 build/run pass.
- Real `embeddinggemma` semantic quality and ranking calibration still require a local Ollama evaluation corpus.
- A real Windows machine still needs microphone permission plus an installed desktop speech recognizer/language for voice validation.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, Nebius contract tools, and `Nvidea.PersonalAiDemoEval`; compile WPF/XAML; run the focused memory/retrieval/migration, voice, readiness, lifecycle/research, browser authority/integration, API-surface, and security suites; then run the evaluator with `--output` and fix every compile/runtime defect before treating its JSON as judging evidence. If executable validation remains unavailable, extend the deterministic evaluator with **negative/adversarial cross-cutting scenarios**: prompt-injection page content must not authorize tools, denied browser approval must prevent mutation, failed post-action verification must stop the plan, unknown research citations must be flagged, stale/wrong exact approval scope must fail closed, and an ambiguous durable Running job must never auto-replay.
