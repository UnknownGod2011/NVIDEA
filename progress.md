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
- Durable research pipeline is staged at remote-work boundaries: request -> Nemotron plan -> Tavily Search/Extract + ranking -> prepared evidence -> Nemotron synthesis -> completed report.
- `ResearchJobHandler` persists versioned `research.requested.v1`, `research.planned.v1`, `research.evidence.v1`, and `research.completed.v1` checkpoints. Prepared evidence includes exact ranked provenance, deterministic quality metadata, warnings, and cumulative Tavily credits so resume does not repeat Search/Extract or rerank evidence.
- Research checkpoint payloads are bounded to 2 MiB UTF-8. `JsonAgentJobStore` uses Windows DPAPI by default on Windows, so durable job payloads can be locally protected without storing plaintext checkpoint evidence.
- `ResearchJobStatus` now provides a privacy-safe desktop projection of durable research state. It exposes job identity, coarse stage, durable state, attempt count, timestamps, retry timing, safe run/cancel affordances, terminal state, and fixed display copy while deliberately excluding checkpoint payloads, source URLs/content, query text, approval grants, and raw error details.
- Research status maps saved checkpoints to judging-visible stages: Nemotron planning, Tavily evidence gathering, saved-evidence Nemotron synthesis, completed, retry-paused, cancelled, failed, and unknown/review-needed.
- Playwright browser agent includes persistent Chromium profile, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit download recovery, emergency stop, and crash recovery.
- Browser download metadata and audit state are protected/bounded; read-only local telemetry does not initialize Playwright/Chromium or expose action authority.
- `StateDirectoryLease` provides single-owner durable browser-state protection with process-local ownership plus OS-backed file locking. Browser ownership is acquired before Playwright transport startup and transferred through persistent-context lifetime; cross-process contention/crash/stale-file fixtures exist.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Root README + MIT license exist; Tavily Search/Extract and evidence-quality ranking are judging-visible.

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

### 2026-09-09 — Privacy-safe durable research status surface
Completed this run:
- Re-read `progress.md`, recent research commits, `ResearchJobHandler`, job contracts/orchestrator, durable JSON job store, capability risk contracts, and the production composition root before implementation.
- Added `src/Nvidea.Core/Jobs/ResearchJobStatus.cs` (`e6409e2260deb7f9938596d372e89adc7bee5978`).
- Added a coarse `ResearchJobStage` model for Requested, Planning, GatheringEvidence, Synthesizing, Completed, WaitingToRetry, Cancelled, Failed, and Unknown states.
- Added `ResearchJobStatus.FromRecord` as a deliberately privacy-safe projection for future Windows UI. It exposes only fixed stage/status telemetry and safe action affordances; it never returns checkpoint payloads, research source text/URLs, the original query, provider errors, or approval material.
- `CanRunNextStep` is fail-closed: Pending can run; RetryScheduled can run only after its due time; Running/terminal/approval states do not advertise resume authority.
- Fixed display text makes Nemotron/Tavily stages visibly demonstrable without rendering potentially sensitive checkpoint contents.
- Added `tests/Nvidea.Core.Tests/ResearchJobStatusTests.cs` (`73cb6c8e15f4c4941dc7f3fa839f6ef34f026ef5`) covering evidence-checkpoint -> synthesis projection, payload non-disclosure, retry timing, terminal behavior, and rejection of non-research jobs.
- Reviewed `JsonAgentJobStore`: Windows already defaults to `WindowsDpapiLocalStateProtector`, so research checkpoints have an existing protected local persistence path rather than requiring a second storage framework.
- Reviewed `NvideaCompositionRoot`: durable research jobs are not yet wired into the production root/WPF. I deliberately did not claim that integration or mislabel local handler execution as Nebius Serverless execution.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- Added unit-level regression coverage for the new status projection.
- No GitHub Actions workflow was triggered simply to obtain a green result.
- Executable .NET/Windows validation remains unavailable in this runtime, so compilation, unit-test execution, WPF launch, Chromium execution, DPAPI execution, and real Windows retry timing are not claimed.
- No other repository was mutated.

Security / privacy / cost review:
- Status projection is local deterministic computation and adds no API calls, cloud disclosure, token usage, or Tavily credits.
- Checkpoint payloads and raw errors are intentionally excluded because either may contain user questions, source evidence, provider details, or other sensitive material.
- Resume affordance does not itself execute work or grant permissions; it only states whether a normal orchestrator step is eligible from the durable state.
- Unknown/mismatched research state fails toward `Unknown` or explicit rejection rather than guessing a stage.

## Known Blockers / Risks
- This environment still lacks a verified .NET 8/Windows execution signal; source-level work and tests are not substitutes for a real build/run.
- The production composition root/WPF still does not instantiate and surface `ResearchJobHandler`, protected `JsonAgentJobStore`, status polling, cancel/resume controls, or completed-report recovery.
- `ResumableJobOrchestrator` has Nebius Serverless execution-location contracts, but the current research handler executes in-process. Production wiring must not label that path as cloud execution until actual serverless dispatch is connected.
- Research checkpoint payloads can approach 2 MiB; production UI must never bind raw payloads.
- Local voice/transcription is absent; a verified production embedding adapter remains absent.

## Single Best Next Task
Wire durable research into the trusted production composition root and Windows UX using protected `JsonAgentJobStore`, `ResearchJobHandler`, and `ResearchJobStatus`: create/recover research jobs, show privacy-safe Nemotron/Tavily stages, add explicit cancel/resume, recover completed reports, and keep execution location truthful (local until real Nebius Serverless dispatch exists). Then obtain the first real Windows/.NET 8 build + tests + WPF launch signal and repair any compile/runtime issues.