# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest interaction concepts from keyboard.wtf while replacing the intelligence/runtime with an NVIDIA/Nebius-first architecture that materially improves memory, research, browser automation, long-running work, safety, verification and personal-AI UX.

Target track: **Personal AI**. Secondary target: **Best Use of Tavily**. Overall ambition: top-three / Grand Prize quality as a complete product rather than a model wrapper.

## HARD REPOSITORY BOUNDARY
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material. Never mutate it in any way.
- Never write to ANY other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working NVIDEA functionality merely to simplify implementation.

## Required Work Loop
Every run must read this file first, inspect current NVIDEA state, choose the highest-value unfinished engineering task, verify current platform/API assumptions where needed, implement real code/tests/docs only in NVIDEA, validate as far as tooling permits, review security/correctness, update this ledger, and continue while meaningful work remains.

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
- `NemotronBrowserPlanner` turns a fresh untrusted observation into one validated step/complete/stop decision. Planner autonomous actions now use bounded typed postconditions rather than free-text `expected_state`.
- `BrowserGoalAgent` runs the bounded observe -> plan -> job -> verify loop and halts at approval boundaries.
- Browser goal sessions persist separately from approval state, with privacy-minimized verified history, action/planner/context/wall-clock budgets, and no persisted raw typed browser values.
- Parent/child browser orchestration persists a reserved child ID before child creation/execution and reconciles that exact durable child on restart instead of blindly re-planning or replaying it.
- Durable `Running` child jobs are never automatically replayed. Evidence-only reconciliation may mark them complete only when deterministic fresh evidence proves the intended end state; otherwise they stop for human resolution.
- Lost ephemeral approval after restart is restored only as a non-authorizing approval wait; explicit approval is required again.
- WPF host has global `Ctrl+Shift+Space`, foreground app/window context, read-only UI Automation selected-text capture, opt-in clipboard disclosure, browser confirmation UX, live status, emergency stop and evidence-only interrupted-work recovery UX.
- Opt-in localhost real-Chromium integration harness exists for the trusted browser approval path.
- Typed browser postconditions support exact URL, title/text presence, element existence/value, checked state and enabled state. The same evaluator is used by normal post-action verification and ambiguous crash reconciliation.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 — Core foundations
- Added Nebius/Nemotron inference abstraction and Token Factory client with structured tools/output, retries, timeout/cancellation, endpoint validation and conservative model routing.
- Added typed layered personal memory with provenance, confidence, importance, sensitivity, retention, secret detection, privacy-aware writes and hybrid retrieval.
- Added Tavily research planning/search/deduplication/provenance plus Nemotron synthesis with explicit untrusted-web boundaries and validated source IDs.
- Added provider-neutral browser contracts, hard-safety policy, bounded observe-act-observe-verify execution and receipts.
- Added capability registry, declared permissions, monotonic risk, exact single-use approvals and append-only audit.
- Added durable jobs with checkpoints, retry/backoff, cancellation, approval pauses, local-vs-Nebius execution selection and Nebius Serverless REST create/list/cancel contract.

### 2026-09-07 — Concrete browser, desktop and multi-step agent
- Added concrete Playwright driver with bounded DOM/ARIA observations, password redaction, host allowlists, user-facing locators, bounded actions and XPath disabled.
- Added ephemeral job approval handoff; grants are never serialized and disappear on restart/failure/cancel.
- Added last-mile browser capability enforcement and durable browser action handler.
- Added `BrowserHostRuntime`, exact WPF confirmation UX and localhost Chromium approval harness.
- Added `DesktopInvocationService`, trusted composition root, WPF host/global hotkey, context capture, explicit clipboard disclosure and genuine cancellation/emergency stop.
- Added `NemotronBrowserPlanner`, bounded `BrowserGoalAgent`, observation-enriched risk classification and durable multi-step goal sessions.
- Added privacy-minimized verified browser history and planner/action/context/wall-clock budgets.

### 2026-09-07 — Crash consistency and evidence-only recovery
- Added caller-supplied durable child IDs with idempotency checks and structural permission comparison.
- Split browser child creation from execution so the parent persists exact child identity before anything can execute.
- Recovery handles missing child, created-but-not-run child, approval-paused child, completion before parent update, lost ephemeral approval and durable `Running` ambiguity.
- Durable `Running` jobs fail closed rather than replaying because execution may already have crossed an external side-effect boundary.
- Added deterministic no-model ambiguous-side-effect reconciliation using fresh browser evidence; uploads/downloads are never auto-reconciled from DOM evidence.
- Added `BrowserAmbiguousRecoveryService`, audited completion of proven ambiguous jobs and Windows **Inspect evidence** UX with no retry/execute-anyway affordance.

### 2026-09-07 — Typed browser verification
- Added `BrowserPostconditionKind`, `BrowserPostcondition` and `BrowserPostconditionEvaluator`.
- Added exact URL, title/text, element existence/value, checked-state and enabled-state predicates, capped at 8 per action.
- `ConservativeBrowserVerifier` and `BrowserAmbiguousStateReconciler` share the same typed evaluator and require all predicates to succeed.
- Upload/download actions remain excluded from automatic crash reconciliation even when DOM predicates appear successful.
- Kept legacy `ExpectedState` only for backwards compatibility with already-durable jobs.

### 2026-09-07 — Nemotron planner migrated to typed postconditions
Completed:
- Re-verified every GitHub write target as exactly `UnknownGod2011/NVIDEA`; no other repository was written.
- Replaced autonomous planner `expected_state` schema with a bounded `postconditions[]` schema covering exact URL, title/visible text, element existence/value, checked state and enabled state.
- Added planner limits for postcondition count/text size and local shape validation independent of model/schema compliance.
- Mutating/navigation/back/refresh planner actions now fail closed unless at least one typed postcondition is supplied.
- Typed verification locators intentionally reject CSS and test-id because the sanitized observation/evaluator cannot prove those selectors deterministically.
- Accessibility-ref postconditions must reference an element present in the fresh pre-action observation; future-state assertions must use semantic role/name, label or text locators instead of invented refs.
- `url_equals` rejects non-HTTP(S) expected values.
- Boolean predicates require `expected_boolean`; predicates reject irrelevant text/boolean/locator fields instead of silently ignoring malformed model output.
- Planner-created `BrowserAction.ExpectedState` is now always null; typed `Postconditions` are the normal autonomous path.
- Set planner JSON deserialization to snake_case naming so structured fields such as `locator_kind`, `expected_boolean` and `postconditions` map explicitly rather than relying on underscore-insensitive assumptions.
- Added `NemotronBrowserPlannerPostconditionTests` covering successful typed mapping, missing verification, unsupported CSS verification, invented accessibility refs, malformed boolean predicates and non-HTTP URL assertions.

Validation / evidence:
- Source-level consistency review completed across planner schema, conversion, observation validation and existing evaluator contracts.
- Confirmed the existing Nebius client transmits `ResponseJsonSchema` as strict `response_format.type=json_schema`; no production endpoint/API key was available for live schema exercise in this run.
- Local runtime probe still found no `dotnet`, `msbuild` or `csc`; **no compile/test/Windows/Chromium success is claimed**.
- No GitHub Actions workflow was created or rerun merely to obtain a green signal.

Security / privacy review:
- Planner postconditions are declarative evidence requirements only; they grant no execution authority and do not weaken capability/approval checks.
- Model-supplied CSS/test-id postconditions are rejected because current privacy-bounded observations cannot verify them safely.
- Future-state assertions cannot fabricate accessibility refs.
- All write-like autonomous steps must provide deterministic post-action evidence or planning fails closed.
- No CAPTCHA/login/site/OS safeguard bypass, secret disclosure path or automatic approval path was introduced.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- Matching Playwright Chromium binaries have not been installed/launched in this execution environment.
- WPF has not been compiled/launched on Windows here.
- The new strict planner postcondition schema still needs a live Nebius Token Factory / Nemotron exercise; OpenAI-compatible backends can differ in supported strict-schema subsets.
- Existing persisted jobs with legacy `ExpectedState` still use the old permissive compatibility branch; this should be retired after a persistence migration window.
- The recovery card currently discovers sessions already marked failed/ambiguous. A process that dies while the parent still says `Running` may require goal resume to classify the child first.
- `JsonAgentJobStore` and `JsonBrowserGoalSessionStore` are individually atomic files, not a cross-file transaction; the reserved-child-ID protocol remains the safety mechanism across that boundary.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration signal** if a capable runtime becomes available and fix all compile/runtime defects immediately. If it remains unavailable, add an end-to-end planner-to-verifier contract harness using a deterministic fake Nemotron response plus the localhost browser page so typed postconditions are proven to survive planner -> durable child job -> Playwright -> fresh observation -> verifier -> crash-recovery serialization without falling back to legacy `ExpectedState`. Then begin the persisted-legacy migration/retirement path.
