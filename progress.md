# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must be independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Architecture / Product State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`.
- Nebius Token Factory / NVIDIA Nemotron inference abstraction with structured output/tool handling, retry/timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and retrieval.
- Tavily Search + query-focused advanced Extract research pipeline with canonical URL deduplication, bounded extraction, credit accounting, fail-soft extraction fallback, and Nemotron synthesis.
- Deterministic evidence-quality ranking combines provider relevance, conservative authority heuristics, freshness evidence, and bounded same-host diversity penalties. Freshness is never fabricated; stale/unknown current-event evidence produces uncertainty warnings.
- Source text remains untrusted data; source-quality metadata is separate and explicitly heuristic; `[src:SOURCE_ID]` citations are machine-validated.
- Durable research is staged at remote-work boundaries: request -> Nemotron plan -> Tavily Search/Extract + ranking -> prepared evidence -> Nemotron synthesis -> completed report.
- `ResearchJobHandler` persists versioned `research.requested.v1`, `research.planned.v1`, `research.evidence.v1`, and `research.completed.v1` checkpoints. Prepared evidence includes exact ranked provenance, deterministic quality metadata, warnings, and cumulative Tavily credits so resume does not repeat Search/Extract or rerank evidence.
- Research checkpoint payloads are bounded to 2 MiB UTF-8. `JsonAgentJobStore` and `JsonLinesAuditTrail` use Windows CurrentUser DPAPI by default on Windows.
- `ResearchJobStatus` is a privacy-safe desktop projection that excludes checkpoint payloads, source URLs/content, query text, approval material, and raw provider errors while exposing coarse stage, attempt, retry timing, safe actions, and actual execution location.
- `ResearchJobRuntime` is now the trusted production host for durable research. It creates protected job/audit stores, registers only `ResearchJobHandler`, and deliberately forces `JobExecutionLocation.Local` until a real Nebius Serverless dispatcher exists.
- `NvideaCompositionRoot` now exposes `ResearchJobs` whenever Tavily is configured, using the same trusted Nemotron/Tavily graph as ordinary research.
- The WPF shell now surfaces durable Nemotron + Tavily research: create a protected job from the prompt, execute one persisted remote boundary per explicit Start/Resume action, recover the latest unfinished job after restart, show privacy-safe stage telemetry, cancel work, and recover completed report text.
- Global Emergency stop now cancels an in-flight durable research stage; the research path also participates in desktop busy-state gating so browser/chat actions cannot be started concurrently from the same foreground shell.
- Research execution is truthfully displayed as local today. No serverless execution claim is made merely because an execution-location contract exists.
- Playwright browser agent includes persistent Chromium profile, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit download recovery, emergency stop, and crash recovery.
- Browser download metadata and audit state are protected/bounded; read-only local telemetry does not initialize Playwright/Chromium or expose action authority.
- `StateDirectoryLease` provides single-owner durable browser-state protection with process-local ownership plus OS-backed file locking. Browser ownership is acquired before Playwright transport startup and transferred through persistent-context lifetime; cross-process contention/crash/stale-file fixtures exist.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Root README + MIT license exist; Tavily Search/Extract, evidence-quality ranking, durable research checkpoints, truthful execution location, and Windows resume UX are judging-visible.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily Search research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, typed verification, live Nebius contract probe, DPAPI state protection, and protected audit infrastructure.

### 2026-09-07 to 2026-09-08 — Persistent browser + safe local state
Added owned persistent Chromium sessions, popup/new-tab tracking, durable verified downloads, exact-scope export/discard, single-use grants, WPF confirmation, crash recovery, retained/in-progress quotas, protected audit retention, browser-free telemetry, same-path synchronization, deliberate download recovery UX, and emergency-stop cancellation.

### 2026-09-08 — Single-owner browser durable state
Added `StateDirectoryLease`, same-state exclusivity/reacquisition, real child-process contention/crash coverage, intrinsic persistent-context ownership, and then moved ownership before `Playwright.CreateAsync` with explicit single-lease transfer and failure/cancellation cleanup.

Representative commits: `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `79b9338dceb55470f775d5467fd8b5ad2461ea29`, `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily Extract + evidence quality
Added `IResearchExtractionProvider`, advanced Tavily Extract enrichment, shared bounded transport, partial/full extraction fallback, credit accounting, synthesis integration, deterministic authority/freshness/diversity ranking, stale/unknown-current-event warnings, deceptive-subdomain hardening, exact provenance preservation, tests, and README documentation.

Representative commits: `9b45da151dedd946625962f485713a4543543cd6`, `cb521700f2edd588be5385b33c72f2275d7aea3e`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `36d41fcf65835cb28a2d373d68c460371ead1c43`, `c36bf2dc3462619c9b404dce2c77ef6f3e767416`.

### 2026-09-09 — Durable resumable Nemotron + Tavily research
Split `ResearchEngine` at durable remote-work boundaries and added `ResearchPreparedEvidence` plus `ResearchJobHandler`. Resume from saved evidence performs Nemotron synthesis without repeating Tavily calls or reranking evidence. Added bounded checkpoint validation and regression coverage proving zero provider calls after an evidence checkpoint.

Representative commits: `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `5affb9d37266b3d137fe2adb4c84dc367bdfcadb`, `a536f9ab9af8f09db74d5752a3147ce1e8dfe557`, `d22608d4bfd1b5ee38cf53dd400894ffb736f1c0`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`, `33453ffa3368dbc9ea8f3e598f99b24a316b308f`.

### 2026-09-09 — Privacy-safe durable research status
Added `ResearchJobStatus` with coarse Nemotron/Tavily stage projection, retry timing, safe run/cancel affordances, and strict exclusion of checkpoint/source/query/error data. Confirmed existing Windows DPAPI job storage path and documented that production integration still needed to keep local/serverless execution truthful.

Representative commits: `e6409e2260deb7f9938596d372e89adc7bee5978`, `73cb6c8e15f4c4941dc7f3fa839f6ef34f026ef5`, `1f96e109a97ace231c7f23eb404136d880b5e18a`.

### 2026-09-09 — Production durable research + Windows resume UX
Completed this run:
- Re-read `progress.md` completely and inspected the current composition root, research handler/orchestrator/contracts, protected job store, audit trail, research status projection, ResearchEngine report shape, WPF shell, and existing tests before mutation.
- Added `src/Nvidea.Core/Jobs/ResearchJobRuntime.cs` (`ac305dd8158518274de5779c681f6f29c6ed5b71`). The runtime owns the production durable research store/orchestrator boundary, exposes create/list/status/run/cancel/completed-report APIs, and validates that every surfaced job is both research type and `JobExecutionLocation.Local`.
- The runtime intentionally uses a dedicated local-only execution policy rather than `ConservativeJobExecutionPolicy`; this prevents a `BenefitsFromBackgroundExecution` flag from being mistaken for real Nebius Serverless dispatch.
- Wired the runtime into `NvideaCompositionRoot` whenever Tavily is configured (`3cc1b1820307f2cc0f37bc725edc249f915d99fe`). It reuses the trusted Nemotron/Tavily `ResearchEngine` and stores state under the application's research state directory.
- Added `src/Nvidea.Windows/MainWindow.Research.cs` (`06cdfec11fb3a055861ac263ca7784aa2c12f4c8`, then hardened in later commits) and a WPF research panel (`3cea246ffcc939076c429100bfff96fa74874430`).
- Windows can now create a durable research job from the prompt, execute one stage at a time, rediscover the latest unfinished job after restart, explicitly resume the next persisted stage, cancel it, and recover/display a completed `AnswerMarkdown` report.
- Corrected an initial report-property mistake during static review (`0d33d39d90269f089556a4a39f3945bfab405249`): `ResearchReport` exposes `AnswerMarkdown`, not `Answer`.
- Added `ExecutionLocation` to `ResearchJobStatus` (`92e20d754acc70741481684902484981b71dd6aa`) and changed WPF to display the actual persisted location (`4c9c2b74612c46fd41aa9f6fc3c491b53bf92116`) rather than hard-coding a location string.
- Added `tests/Nvidea.Core.Tests/ResearchJobRuntimeTests.cs` (`9152ae71fa700356547332943c28e243b9946725`) covering restart recovery from the same durable store, exact local execution, completion recovery, terminal cancellation, and zero provider calls when resuming from saved evidence.
- Caught and removed a test-only DPAPI portability flaw (`a2a4c8ede6a612327574c562ce980e096903675a`): the regression fixture no longer tries to decode a production Windows-DPAPI store with an unrelated pass-through protector. A second `ResearchJobRuntime` now reopens the same store through the real platform-default protection path.
- Wired the global Emergency stop into the in-flight research cancellation token and included research in foreground busy-state gating (`eec2cdb014494b1129373eae0f95d65190a2eb4b`). Closing the window also cancels/disposes the research CTS.
- Updated `README.md` (`64f66e2c6c4424449aca121c14add8ce04985169`) so durable checkpoints, Windows restart/resume, DPAPI protection, local execution truthfulness, cancellation semantics, and the interrupted-Running limitation are judging-visible rather than described as future work.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Source-level regression coverage was added for the new production runtime, including a second runtime instance reopening the same job state and completing synthesis while a `ThrowingProvider` proves Tavily Search is not repeated from an evidence checkpoint.
- Static review caught and repaired both the invalid `ResearchReport.Answer` reference and the Windows DPAPI test-protector mismatch before finalization.
- Existing `ResearchJobStatusTests` construct status via `FromRecord`; adding execution-location projection does not require callers to construct the positional status record directly in those tests.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- The current execution environment still does not expose `dotnet`, `msbuild`, or `csc`; therefore compilation, unit-test execution, WPF/XAML load, Windows DPAPI execution, and real Nebius/Tavily provider calls are **not claimed**.
- No other repository was mutated.

Security / privacy / permission / cost review:
- Durable job and audit files use the existing Windows CurrentUser DPAPI default; API keys are not placed in checkpoint payloads.
- WPF binds only `ResearchJobStatus`, whose fixed display text omits original questions, source contents/URLs, checkpoint JSON, approval grants, and raw provider errors.
- The completed report is shown only when the user reaches a durable completed checkpoint; the status panel itself remains payload-free.
- Execution location is derived from the durable record and the production runtime rejects non-local research records, so judges/users are not shown a fictitious serverless path.
- One explicit stage is executed per Start/Resume action. Normal resume from saved evidence avoids duplicate Tavily Search/Extract cost and preserves exact prior ranking/provenance.
- User cancellation is terminal under the current generic orchestrator. Global Emergency stop cancels an active remote stage and causes the orchestrator cancellation path to durably mark the job Cancelled.
- Foreground research disables prompt/browser/chat controls while a stage is running, reducing accidental overlapping work from the same WPF shell.
- Research has network permission only and no OS/browser/file-write permission in its job definition.

## Known Blockers / Risks
- This environment still lacks a verified .NET 8/Windows execution signal. The new source/tests are not substitutes for `dotnet build`, `dotnet test`, XAML load, DPAPI execution, and real WPF interaction.
- A process crash **during** `RunNextStepAsync` leaves the generic durable record in `Running`. The generic orchestrator intentionally refuses to replay `Running` work because other job types may contain side effects. For research this means the UI currently fails closed and cannot resume that exact interrupted stage without a dedicated recovery transition.
- Re-running an interrupted research stage may duplicate an unfinished Nemotron/Tavily request and therefore cost/latency. Any recovery transition must be explicit, user-visible, research-only, and must never auto-replay a `Running` stage.
- The WPF research panel currently surfaces the newest nonterminal job (or newest job) rather than offering a full multi-job history/selector.
- Real Nebius Serverless dispatch is not connected; all durable research execution is correctly local today.
- Local voice/transcription is absent; a verified production embedding adapter remains absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every compile/XAML/runtime issue found. If that execution signal remains unavailable, implement a **research-only explicit interrupted-Running recovery transition**: detect a stale `Running` research checkpoint after restart, show a privacy-safe “interrupted during <stage>” state, require the user to explicitly retry that stage with a duplicate-cost warning, atomically re-arm it to `Pending`, and add regression tests proving the generic orchestrator still never auto-replays Running browser/consequential jobs.