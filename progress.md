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
- `ResearchJobHandler` persists versioned `research.requested.v1`, `research.planned.v1`, `research.evidence.v1`, and `research.completed.v1` checkpoints. Prepared evidence includes exact ranked provenance, deterministic quality metadata, warnings, and cumulative Tavily credits so normal resume does not repeat Search/Extract or rerank evidence.
- Research checkpoint payloads are bounded to 2 MiB UTF-8. `JsonAgentJobStore` and `JsonLinesAuditTrail` use Windows CurrentUser DPAPI by default on Windows.
- `ResearchJobStatus` is a privacy-safe desktop projection that excludes checkpoint payloads, source URLs/content, query text, approval material, and raw provider errors while exposing coarse stage, attempt, retry timing, safe actions, and actual execution location.
- `ResearchJobRuntime` is the trusted production host for durable research. It creates protected job/audit stores, registers only `ResearchJobHandler`, and forces `JobExecutionLocation.Local` until a real Nebius Serverless dispatcher exists.
- Every **research durable mutation** (`CreateAsync`, `RunNextStepAsync`, interrupted recovery, cancellation) now runs under an OS-backed `StateDirectoryLease` for the research state directory. The lease spans the entire remote provider step and checkpoint persistence, preventing a second NVIDEA process from concurrently mutating or re-arming the same durable research state. Read-only status/list/completed-report access remains lease-free.
- The WPF shell surfaces durable Nemotron + Tavily research: create a protected job from the prompt, execute one persisted remote boundary per explicit Start/Resume action, recover unfinished jobs after restart, show privacy-safe stage telemetry, cancel work, and recover completed report text.
- Research has an explicit interrupted-Running recovery path. A stale local `Running` research record with a known research checkpoint can be explicitly re-armed to `Pending`; the re-arm itself performs no Nemotron/Tavily call, preserves the checkpoint, emits an audit event, and requires a second deliberate UI action before retrying provider work. Status text warns that retry may repeat provider work/cost without exposing query/source payloads.
- Generic `ResumableJobOrchestrator` behavior remains fail-closed for `Running` jobs. Browser/consequential jobs are not automatically replayed or re-armed by the research-specific recovery API.
- Global Emergency stop cancels an in-flight durable research stage; research participates in desktop busy-state gating so browser/chat actions cannot be started concurrently from the same foreground shell.
- Research execution is truthfully displayed as local today. No serverless execution claim is made merely because an execution-location contract exists.
- Playwright browser agent includes persistent Chromium profile, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit download recovery, emergency stop, and crash recovery.
- Browser download metadata and audit state are protected/bounded; read-only local telemetry does not initialize Playwright/Chromium or expose action authority.
- `StateDirectoryLease` provides process-local plus OS-backed file locking with bounded owner metadata, stale-file recovery, and reparse-point rejection. Browser ownership is acquired before Playwright transport startup and transferred through persistent-context lifetime; cross-process contention/crash/stale-file fixtures exist.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Root README + MIT license exist; Tavily Search/Extract, evidence-quality ranking, durable research checkpoints, truthful execution location, and Windows resume UX are judging-visible.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and `StateDirectoryLease` single-owner browser-state protection. Browser ownership was moved before `Playwright.CreateAsync`; cross-process contention/crash/stale-file fixtures exist.

Representative commits: `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `79b9338dceb55470f775d5467fd8b5ad2461ea29`, `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily evidence quality and resumable research
Added query-focused advanced Tavily Extract enrichment, fail-soft extraction fallback, exact credit accounting, deterministic authority/freshness/diversity ranking, stale/unknown-current-event warnings, deceptive-subdomain hardening, staged `ResearchEngine` boundaries, `ResearchPreparedEvidence`, versioned durable research checkpoints, and resume-from-evidence without repeat Search/Extract/reranking.

Representative commits: `9b45da151dedd946625962f485713a4543543cd6`, `cb521700f2edd588be5385b33c72f2275d7aea3e`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `5affb9d37266b3d137fe2adb4c84dc367bdfcadb`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`.

### 2026-09-09 — Production durable research Windows UX
Added `ResearchJobStatus`, `ResearchJobRuntime`, composition-root wiring, protected local research persistence, truthful execution-location projection, restart recovery from saved evidence, Windows create/resume/cancel/report UI, emergency-stop cancellation, busy-state integration, and README documentation. A regression fixture proves restart from an evidence checkpoint completes synthesis with zero repeated provider Search calls.

Representative commits: `e6409e2260deb7f9938596d372e89adc7bee5978`, `ac305dd8158518274de5779c681f6f29c6ed5b71`, `3cc1b1820307f2cc0f37bc725edc249f915d99fe`, `9152ae71fa700356547332943c28e243b9946725`, `eec2cdb014494b1129373eae0f95d65190a2eb4b`, `7dc10b7293769ff4b89c3886a8a0ca13d4e47a86`.

### 2026-09-09 — Explicit interrupted research recovery
Added `ResearchJobStage.Interrupted`, `CanRecoverInterrupted`, a 30-second recovery grace period, known-checkpoint validation, privacy-safe duplicate-work/cost warnings, and research-only `RecoverInterruptedAsync`. Re-arm preserves checkpoint/attempt, clears retry/error scheduling, emits `research.interrupted_rearmed`, performs zero provider work, and requires a second explicit UI action before retry. Generic Running jobs remain fail-closed and covered by a non-research regression fixture.

Representative commits: `94f10003a112146cb6b56a650ed8c2467b0b4ca3`, `6c3d86d8637f5dae55784b140ba046a0dc5a0d2b`, `8c1ab61290adfc2a8b3e487cfaa572a4aeca02b7`, `99689293856567cf40e551a849cc58f721d99f87`, `2a967f27fb0da901630a574d0c835ea71ae409e1`, `93cd4c84faa5c7aee528357dca0ee702b9f17907`, `8227c91c4affceb0333f525f6da571a1347c1441`.

### 2026-09-09 — Cross-process research-state mutation ownership
Completed this run:
- Re-read `progress.md` completely and inspected `ResearchJobRuntime`, `NvideaCompositionRoot`, `StateDirectoryLease`, research runtime tests, and current repo tree before mutation.
- Initial review considered a lifetime-held research lease, then rejected that design because it would unnecessarily block legitimate restart/reconstruction scenarios and complicate teardown. The implementation was corrected to a mutation-scoped lease instead (`43712cf72dc673775620f8b93046cba9ff01c54d` superseded by `98d3d4355028f0bc541b75aca1905bcbb456f661`).
- `ResearchJobRuntime` now serializes local mutations through `_mutationGate`, then acquires `StateDirectoryLease` before any durable mutation. The lease is held for the whole operation, including Nemotron/Tavily provider execution and final checkpoint write, and released in `finally` after the operation completes/fails/cancels.
- Protected mutations: research creation, next-stage execution, interrupted-stage re-arm, and cancellation. Read-only `ListAsync`, `GetStatusAsync`, and completed-report recovery intentionally do not acquire mutation authority.
- Added `ResearchJobStateLeaseTests` (`771656a5c8300707cdbd45ad82809a0bdde59875`). One test blocks a real research planning inference while the first runtime owns the lease, confirms a second runtime can still read status, confirms its cancellation mutation fails closed with `StateDirectoryLeaseUnavailableException`, then confirms the second runtime can mutate successfully after the first operation releases the lease. A second test checks ordinary completed mutation releases ownership for another runtime.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Static design review found and corrected the initial over-broad lifetime-lease approach before finalization.
- Existing `StateDirectoryLease` already has independent process-level plus OS-lock coverage, including true child-process contention/crash/stale-file fixtures; this run adds the research-runtime integration layer on top of that primitive.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- This execution environment still exposes no `dotnet`, `msbuild`, or `csc`; therefore compilation, unit-test execution, WPF/XAML load, Windows DPAPI execution, and live Nemotron/Tavily execution are **not claimed**.
- No other repository was mutated.

Security / privacy / permission / cost review:
- The lease prevents a stale second NVIDEA process from re-arming/cancelling/starting provider work while another live process owns the same research state.
- The lease is acquired before provider execution and retained until persistence finishes, so the protection covers the expensive/side-effect-relevant remote-work window, not only the final JSON write.
- Read-only status/report access remains available without mutation authority and does not render hidden checkpoint/source payloads.
- Recovery remains research-only, local-only, approval-free, non-automatic, and requires explicit two-step user intent.
- If another process owns the state, mutation fails closed rather than waiting indefinitely or guessing whether the other process is alive.

## Known Blockers / Risks
- This environment still lacks a verified .NET 8/Windows execution signal. Source/tests are not substitutes for `dotnet build`, `dotnet test`, XAML load, DPAPI execution, and real WPF interaction.
- The new research mutation lease is statically integrated but has not yet been executed on Windows/.NET in this environment. True cross-process behavior of the underlying lease has fixtures already, but the new research-runtime integration test has not been executed.
- A stale `Running` stage may already have reached the remote provider before a crash, so explicit retry can duplicate Nemotron/Tavily cost/latency. The UI warns about this and never auto-replays.
- The WPF research panel surfaces the newest nonterminal job rather than offering a full multi-job history/selector.
- Real Nebius Serverless dispatch is not connected; all durable research execution is correctly local today.
- Local voice/transcription is absent; a verified production embedding adapter remains absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every compile/XAML/runtime issue found. If that execution signal remains unavailable, return to hackathon-scoring functionality: implement a **real Nebius Serverless dispatch boundary for cloud-safe long research** while preserving local-only private OS actions, truthful execution-location reporting, durable checkpoint provenance, cancellation/recovery semantics, and no hidden fallback to non-NVIDIA primary models. After that, build a polished multi-job research history/demo surface.
