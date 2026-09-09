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
- `ResearchJobRuntime` is the trusted production host for durable research. It creates protected job/audit stores, registers only `ResearchJobHandler`, and deliberately forces `JobExecutionLocation.Local` until a real Nebius Serverless dispatcher exists.
- The WPF shell surfaces durable Nemotron + Tavily research: create a protected job from the prompt, execute one persisted remote boundary per explicit Start/Resume action, recover unfinished jobs after restart, show privacy-safe stage telemetry, cancel work, and recover completed report text.
- Research now has an explicit interrupted-Running recovery path. A stale local `Running` research record with a known research checkpoint can be explicitly re-armed to `Pending`; the re-arm itself performs no Nemotron/Tavily call, preserves the checkpoint, emits an audit event, and requires a second deliberate UI action before retrying provider work. Status text warns that retry may repeat provider work/cost without exposing query/source payloads.
- Generic `ResumableJobOrchestrator` behavior remains fail-closed for `Running` jobs. Browser/consequential jobs are not automatically replayed or re-armed by the research-specific recovery API.
- Global Emergency stop cancels an in-flight durable research stage; research also participates in desktop busy-state gating so browser/chat actions cannot be started concurrently from the same foreground shell.
- Research execution is truthfully displayed as local today. No serverless execution claim is made merely because an execution-location contract exists.
- Playwright browser agent includes persistent Chromium profile, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit download recovery, emergency stop, and crash recovery.
- Browser download metadata and audit state are protected/bounded; read-only local telemetry does not initialize Playwright/Chromium or expose action authority.
- `StateDirectoryLease` provides single-owner durable browser-state protection with process-local ownership plus OS-backed file locking. Browser ownership is acquired before Playwright transport startup and transferred through persistent-context lifetime; cross-process contention/crash/stale-file fixtures exist.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Root README + MIT license exist; Tavily Search/Extract, evidence-quality ranking, durable research checkpoints, truthful execution location, and Windows resume UX are judging-visible.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and `StateDirectoryLease` single-owner browser-state protection. Browser ownership was moved before `Playwright.CreateAsync`; cross-process contention/crash/stale-file fixtures exist.

Representative commits: `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `79b9338dceb55470f775d5467fd8b5ad2461ea29`, `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily Extract, evidence quality, and resumable research
Added query-focused advanced Tavily Extract enrichment, fail-soft extraction fallback, exact credit accounting, deterministic authority/freshness/diversity ranking, stale/unknown-current-event warnings, deceptive-subdomain hardening, staged `ResearchEngine` boundaries, `ResearchPreparedEvidence`, versioned durable research checkpoints, and resume-from-evidence without repeat Search/Extract/reranking.

Representative commits: `9b45da151dedd946625962f485713a4543543cd6`, `cb521700f2edd588be5385b33c72f2275d7aea3e`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `5affb9d37266b3d137fe2adb4c84dc367bdfcadb`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`.

### 2026-09-09 — Production durable research Windows UX
Added `ResearchJobStatus`, `ResearchJobRuntime`, composition-root wiring, protected local research persistence, truthful execution-location projection, restart recovery from saved evidence, Windows create/resume/cancel/report UI, emergency-stop cancellation, busy-state integration, and README documentation. A regression fixture proves restart from an evidence checkpoint completes synthesis with zero repeated provider Search calls.

Representative commits: `e6409e2260deb7f9938596d372e89adc7bee5978`, `ac305dd8158518274de5779c681f6f29c6ed5b71`, `3cc1b1820307f2cc0f37bc725edc249f915d99fe`, `9152ae71fa700356547332943c28e243b9946725`, `eec2cdb014494b1129373eae0f95d65190a2eb4b`, `7dc10b7293769ff4b89c3886a8a0ca13d4e47a86`.

### 2026-09-09 — Explicit interrupted research recovery
Completed this run:
- Re-read `progress.md` completely and inspected current research runtime/status/orchestrator/job-store/handler/WPF/test code before mutation.
- Added `ResearchJobStage.Interrupted`, `CanRecoverInterrupted`, a 30-second recovery grace period, known-checkpoint validation, and privacy-safe stage-specific duplicate-work/cost warnings in `ResearchJobStatus` (`94f10003a112146cb6b56a650ed8c2467b0b4ca3`).
- Added `ResearchJobRuntime.RecoverInterruptedAsync` (`6c3d86d8637f5dae55784b140ba046a0dc5a0d2b`, corrected in `888113272c69ee4511baba280ac64920a82990bb`). Recovery is restricted to local research jobs in durable `Running`, requires a recognized research checkpoint, no approval scope, an unexhausted retry budget, and a stale-enough `UpdatedAt`.
- Recovery preserves the existing checkpoint/attempt, clears retry/error scheduling, changes only the durable state back to `Pending`, and appends a `research.interrupted_rearmed` audit event. It does **not** call Nemotron, Tavily, browser tools, or any consequential action.
- Wired WPF to make recovery explicitly two-phase (`8c1ab61290adfc2a8b3e487cfaa572a4aeca02b7`): the first click is labeled `Re-arm interrupted stage` and only re-arms; a second explicit click is required to retry the remote stage. The visible status warns that provider work/cost may repeat while still hiding checkpoint/query/source contents.
- Added status regression coverage for stale vs fresh Running records and payload non-disclosure (`99689293856567cf40e551a849cc58f721d99f87`).
- Added runtime regression coverage proving explicit recovery performs no provider Search work, preserves the research checkpoint, leaves the attempt unchanged, emits the recovery audit event, and rejects a fresh Running record (`2a967f27fb0da901630a574d0c835ea71ae409e1`).
- Added `GenericRunningFailClosedTests` (`93cd4c84faa5c7aee528357dca0ee702b9f17907`) proving the generic orchestrator returns a non-research/browser-style `Running` record unchanged and invokes its handler zero times. This guards against accidentally broadening replay semantics while adding research-only recovery.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Static review caught and repaired an invalid named-argument construction of `AuditEvent` before finalization.
- Existing generic `RunNextStepAsync` remains unchanged: `Running` returns immediately and cannot auto-replay.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- This execution environment still does not expose `dotnet`, `msbuild`, `csc`, or `mcs`; therefore compilation, unit-test execution, WPF/XAML load, Windows DPAPI execution, and real Nemotron/Tavily calls are **not claimed**.
- No other repository was mutated.

Security / privacy / permission / cost review:
- Recovery is research-only and local-only; it cannot re-arm a browser/consequential job through `ResearchJobRuntime`.
- No approval scope is accepted or recreated during recovery, and no ephemeral approval grant is minted.
- The persisted checkpoint is reused exactly; the recovery transition does not deserialize/render its question/source payload into UI status.
- Retry is not automatic. The user sees the duplicate provider-work/cost warning, explicitly re-arms, then must explicitly run the stage again.
- A fresh `Running` record is not recoverable until the grace interval passes, reducing accidental re-arm of work that may still be active.
- Generic Running semantics remain fail-closed and covered by a non-research regression fixture.

## Known Blockers / Risks
- This environment still lacks a verified .NET 8/Windows execution signal. Source/tests are not substitutes for `dotnet build`, `dotnet test`, XAML load, DPAPI execution, and real WPF interaction.
- Research recovery uses the job store's existing per-instance synchronization plus a runtime recovery semaphore. It is not yet protected by a cross-process single-owner research-state lease/CAS boundary. Two concurrently running NVIDEA processes targeting the same research state could still race durable job mutations; the 30-second grace period reduces but does not eliminate that class of risk.
- A stale `Running` stage may already have reached the remote provider before a crash, so explicit retry can duplicate Nemotron/Tavily cost/latency. The UI now warns about this and never auto-replays.
- The WPF research panel surfaces the newest nonterminal job rather than offering a full multi-job history/selector.
- Real Nebius Serverless dispatch is not connected; all durable research execution is correctly local today.
- Local voice/transcription is absent; a verified production embedding adapter remains absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every compile/XAML/runtime issue found. If that execution signal remains unavailable, add a **single-owner durable research-state lease or equivalent cross-process compare-and-swap boundary** around research job mutations/recovery so explicit interrupted-stage re-arm cannot race another live NVIDEA process. After that, return to hackathon-scoring work: real Nebius Serverless dispatch for cloud-safe long research and a polished multi-job research history/demo surface.
