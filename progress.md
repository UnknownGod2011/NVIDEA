# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest interaction concepts from keyboard.wtf while replacing the intelligence/runtime with an NVIDIA/Nebius-first architecture that materially improves memory, research, browser automation, long-running work, safety, verification and personal-AI UX.

Target track: **Personal AI**. Secondary target: **Best Use of Tavily**. Overall ambition: top-three / Grand Prize quality as a complete product rather than a model wrapper.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working NVIDEA functionality merely to simplify implementation.

## Target Architecture
- **Desktop shell:** Windows global hotkeys, voice/text invocation, orb/status, active-app/selected-text/clipboard context, local speech where useful, permission UX and emergency stop.
- **Agent core:** Nemotron/Nebius reasoning, structured tools, bounded execution, verification, retries/cancellation and approvals.
- **Memory:** typed working/episodic/semantic/project/skill memory with privacy-aware writes, hybrid retrieval, provenance/confidence/sensitivity/retention and deletion controls.
- **Research:** Nemotron planning -> Tavily evidence -> untrusted-content boundary -> Nemotron synthesis -> validated citations.
- **Browser:** DOM/accessibility observation -> Nemotron one-step plan -> typed action + typed postconditions -> hard-safety floor -> capability policy -> exact approval -> Playwright -> fresh observation -> deterministic verification -> repeat under strict budgets.
- **Skills / permissions:** capability registry, least privilege, monotonic risk, single-use approvals and append-only audit.
- **Jobs:** durable checkpoints, retries, cancellation, approval-paused states, ephemeral grants and local-vs-Nebius execution policy.
- **Cloud:** Nebius Serverless only for suitable long-running/background workloads; private OS actions stay local.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- Standalone .NET 8 core at `src/Nvidea.Core` plus WPF host at `src/Nvidea.Windows`.
- Nebius Token Factory inference client with Nemotron default, structured output/tools, conservative routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Concrete Playwright .NET browser driver, hard safety policy, capability execution boundary and deterministic verifier under `Browser`.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer and append-only audit under `Capabilities`.
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- `BrowserHostRuntime` owns isolated local Playwright + safety + capability + audit + child-job orchestration and exposes only bounded observations/high-level outcomes.
- `NemotronBrowserPlanner` turns fresh untrusted observations into validated one-step decisions using typed postconditions rather than free-text `expected_state`.
- `BrowserGoalAgent` runs the bounded observe -> plan -> job -> verify loop, halts at approval boundaries, and independently rejects legacy/unverifiable autonomous action contracts before child reservation.
- Browser goal sessions persist separately from approval state with privacy-minimized verified history and action/planner/context/wall-clock budgets.
- Parent/child browser orchestration persists a reserved child ID before creation/execution and reconciles that exact durable child after restart.
- Durable `Running` child jobs are never automatically replayed. Evidence-only reconciliation may complete them only when deterministic fresh evidence proves success; otherwise human resolution is required.
- WPF host has global `Ctrl+Shift+Space`, foreground app/window context, read-only selected-text capture, opt-in clipboard disclosure, confirmation UX, live status, emergency stop and interrupted-work evidence inspection.
- Typed browser postconditions support exact URL, title/text presence, element existence/value, checked state and enabled state; the same evaluator is shared by normal execution and crash reconciliation.
- Durable browser action checkpoints now carry verification-contract version 2 at the top level. `JsonAgentJobStore` rewrites safe unversioned records under its exclusive lock before exposing them and quarantines ambiguous legacy mutations without execution.
- `BrowserActionJobHandler` accepts only current versioned typed-verification checkpoints.
- Opt-in localhost Chromium integration harness covers both the approval boundary and deterministic Nemotron planner -> durable child -> Playwright -> typed verifier path.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 — Core foundations
- Added Nebius/Nemotron inference abstraction and Token Factory client with structured tools/output, retries, timeout/cancellation, endpoint validation and conservative model routing.
- Added typed layered memory with provenance/confidence/importance/sensitivity/retention, secret detection, privacy-aware writes and hybrid retrieval.
- Added Tavily research planning/search/deduplication/provenance plus Nemotron synthesis with explicit untrusted-web boundaries and validated source IDs.
- Added provider-neutral browser contracts, hard-safety policy, bounded observe-act-observe-verify execution and receipts.
- Added capability registry, least privilege, exact single-use approvals, append-only audit, durable jobs and Nebius Serverless execution contracts.

### 2026-09-07 — Concrete browser, desktop and multi-step agent
- Added Playwright driver with bounded DOM/ARIA observations, password redaction, host allowlists, user-facing locators, bounded actions and XPath disabled.
- Added ephemeral approval handoff, last-mile browser capability enforcement, durable browser child jobs and `BrowserHostRuntime`.
- Added trusted desktop composition root, WPF global hotkey/context capture/clipboard disclosure, cancellation and emergency stop.
- Added `NemotronBrowserPlanner`, bounded `BrowserGoalAgent`, privacy-minimized durable goal sessions and action/planner/context/wall-clock budgets.

### 2026-09-07 — Crash consistency and evidence-only recovery
- Added caller-supplied child IDs, idempotent creation and structural permission comparison.
- Split child creation from execution so the parent persists exact child identity before any browser side effect.
- Recovery handles missing/created/approval-paused/completed/lost-grant/ambiguous-running child states without blind replay.
- Added deterministic no-model ambiguous-side-effect reconciliation; uploads/downloads are never auto-reconciled from DOM evidence.
- Added Windows **Inspect evidence** UX with no retry-anyway affordance.

### 2026-09-07 — Typed verification and Nemotron migration
- Added typed browser postconditions capped at eight predicates and shared deterministic evaluation.
- Replaced autonomous planner `expected_state` with bounded `postconditions[]` and local shape validation.
- Added real-Chromium typed planner contract harness proving planner -> durable child -> approval -> Playwright -> fresh observation -> verifier -> durable history.
- Added conservative `BrowserLegacyActionMigration`: typed/redundant legacy records can be normalized; exact navigation can become `UrlEquals(destination)`; arbitrary free-text mutations require human review.
- Added an independent `BrowserGoalAgent` guard rejecting legacy or unverifiable autonomous actions before durable child reservation.

### 2026-09-07 — Durable browser verification contract v2
Completed:
- Added `BrowserActionCheckpointCodec` with explicit `verificationContractVersion = 2` persisted at the top level of the existing `BrowserAction` JSON shape. Keeping the marker top-level preserves existing descriptive recovery/approval readers while making execution schema explicit.
- Verification contract v2 rejects any non-empty legacy `ExpectedState`; state-changing browser actions must carry typed postconditions.
- Added `BrowserActionCheckpointMigrationService` plus a pure `MigrateRecord` transform. It never invokes Playwright, Nemotron, approval, capability execution, or any external side effect.
- Safe legacy records are rewritten deterministically: existing typed actions receive the v2 marker; legacy navigation may derive exact `UrlEquals(destination)`; redundant free text is removed when typed predicates already exist.
- Ambiguous legacy click/type/select/etc. records are quarantined as `Failed`, approval scope is cleared, retry timing is cleared, and the original action payload is replaced by a sanitized quarantine record so it cannot later be replayed accidentally.
- `JsonAgentJobStore` now performs the migration under its existing exclusive gate before returning persisted records. Any changed records are atomically rewritten through the existing temp-file + move path.
- `BrowserActionJobHandler` now deserializes only current v2 checkpoints at the execution boundary; an unversioned action cannot silently reach browser execution.
- Migrated the handler test scenarios themselves away from `ExpectedState` to typed postconditions.
- Added `BrowserActionCheckpointMigrationTests` covering top-level v2 shape, deterministic navigation migration, ambiguous mutation quarantine/payload removal, migration idempotency, and rejection of unversioned typed writes by the execution codec.

Validation / evidence:
- Reviewed the repository delta from `a090be372d731f09b5a4d4d7882f93251db1e166` through the migration work: changes are limited to the checkpoint migration/codec, browser job handler, JSON job store, and focused tests.
- Re-probed the execution environment on 2026-09-07: `dotnet`, `msbuild`, and `csc` are still unavailable, so **no compile/test/Chromium success is claimed**.
- Direct GitHub cloning also remains unavailable in the execution container because `github.com` DNS resolution fails; repository work is persisted through the authenticated GitHub connector instead.
- No GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- Version migration treats legacy free text as data, never as authority, and never asks a model to reinterpret it.
- Quarantine clears persisted approval scope and removes the original ambiguous action payload from the durable checkpoint.
- Load-time migration occurs before callers can retrieve/resume a legacy record from `JsonAgentJobStore`, reducing upgrade-time replay risk.
- The version marker itself is descriptive metadata and grants no authority.
- Existing exact single-use approval, capability policy, safety floor, cancellation and audit boundaries remain unchanged.
- No credentials, approval grants, bearer tokens, page bodies, CAPTCHA/login bypasses, or new cloud disclosures were introduced.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- Matching Playwright Chromium binaries have not been installed/launched in this environment, and WPF has not been compiled/launched on Windows here.
- The strict planner postcondition schema still needs a live Nebius Token Factory / Nemotron exercise; OpenAI-compatible backends can differ in strict-schema subsets.
- `JsonAgentJobStore` now owns browser checkpoint schema migration because it must transform records before exposure. This is safe but creates some persistence-to-browser coupling; if job types expand substantially, migrate to a generic registered job-schema migration pipeline.
- The recovery card discovers sessions already marked failed/ambiguous; a process dying while the parent still says `Running` may require goal resume before the child is classified.
- `JsonAgentJobStore` and `JsonBrowserGoalSessionStore` are individually atomic files, not a cross-file transaction; reserved-child-ID ordering remains the cross-file safety mechanism.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration signal** if a capable runtime becomes available and fix all compile/runtime defects immediately. If compilation remains unavailable, add a one-command **live Nebius Token Factory strict-schema contract probe** that exercises the production `NemotronBrowserPlanner` schema against the currently configured Nemotron model without executing browser actions, records only sanitized contract diagnostics, and fails clearly when the backend rejects the structured schema. After that, prioritize encrypted local state and authenticated browser-profile ownership.
