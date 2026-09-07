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
- `NemotronBrowserPlanner` turns a fresh untrusted observation into one validated step/complete/stop decision. Autonomous actions use bounded typed postconditions rather than free-text `expected_state`.
- `BrowserGoalAgent` runs the bounded observe -> plan -> job -> verify loop, halts at approval boundaries, and now independently rejects any autonomous action that reintroduces legacy `ExpectedState` or omits typed postconditions for write-like actions before a durable child is reserved.
- Browser goal sessions persist separately from approval state, with privacy-minimized verified history, action/planner/context/wall-clock budgets, and no persisted raw typed browser values.
- Parent/child browser orchestration persists a reserved child ID before child creation/execution and reconciles that exact durable child on restart instead of blindly re-planning or replaying it.
- Durable `Running` child jobs are never automatically replayed. Evidence-only reconciliation may mark them complete only when deterministic fresh evidence proves the intended end state; otherwise they stop for human resolution.
- Lost ephemeral approval after restart is restored only as a non-authorizing approval wait; explicit approval is required again.
- WPF host has global `Ctrl+Shift+Space`, foreground app/window context, read-only UI Automation selected-text capture, opt-in clipboard disclosure, browser confirmation UX, live status, emergency stop and evidence-only interrupted-work recovery UX.
- Typed browser postconditions support exact URL, title/text presence, element existence/value, checked state and enabled state. The same evaluator is used by normal post-action verification and ambiguous crash reconciliation.
- `BrowserLegacyActionMigration` conservatively upgrades only deterministic legacy cases: redundant `ExpectedState` is removed when typed postconditions already exist; legacy navigation can be converted to exact `UrlEquals(destination)`; arbitrary free-text click/type/select/etc. mutations are classified as requiring human review rather than guessed.
- Opt-in localhost Chromium integration harness covers both the exact approval boundary and deterministic Nemotron-planner -> durable child -> Playwright -> typed-verifier path.
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

### 2026-09-07 — Typed browser verification and Nemotron migration
- Added `BrowserPostconditionKind`, `BrowserPostcondition` and `BrowserPostconditionEvaluator` for exact URL, title/text, element existence/value, checked-state and enabled-state predicates, capped at 8 per action.
- `ConservativeBrowserVerifier` and `BrowserAmbiguousStateReconciler` share the same typed evaluator and require all predicates to succeed.
- Upload/download actions remain excluded from automatic crash reconciliation even when DOM predicates appear successful.
- Replaced autonomous planner `expected_state` schema with bounded `postconditions[]`; planner-created `BrowserAction.ExpectedState` is always null.
- Added local postcondition shape validation independent of model/schema compliance; malformed URL/boolean/locator assertions fail closed.
- Added contract tests for valid typed mapping and malformed/unsafe planner outputs.

### 2026-09-07 — Real Chromium typed planner contract harness
- Migrated the real-Chromium approval integration action from legacy `ExpectedState` to typed verification.
- Added deterministic inference fixture through the production `NemotronBrowserPlanner` and `BrowserGoalAgent`.
- Harness proves planner schema -> typed action -> durable parent/child linkage -> approval pause -> Playwright form submission -> fresh observation -> typed verifier -> verified-history persistence -> planner completion.
- Durable `jobs.json` is checked for typed postconditions and absence of grant/bearer material while approval is pending.
- Planner fixture records response schemas and asserts `postconditions` is present while autonomous `expected_state` is absent.

### 2026-09-07 — Legacy browser verification retirement guard
Completed:
- Added `BrowserLegacyActionMigration` as a conservative compatibility boundary for already-existing `BrowserAction.ExpectedState` records.
- If typed postconditions already exist, migration removes only the redundant free-text field.
- Legacy `Navigate` can be upgraded without guessing by replacing free text with exact `UrlEquals(Destination)` when the destination is absolute HTTP(S).
- Legacy write-like actions whose success condition exists only as arbitrary free text are not auto-converted and return `RequiresHumanReview`; no replay or guessed verification is authorized.
- Added `EnsureAutonomousActionUsesTypedVerification`: autonomous write/navigation/back/refresh actions require at least one typed postcondition, and any non-empty `ExpectedState` is rejected even if typed predicates are also present.
- Wired that guard into `BrowserGoalAgent` immediately after the Nemotron decision and before child-ID reservation, durable child creation, approval, or execution. Invalid contracts stop the goal with a descriptive reason and execute nothing.
- Added `BrowserLegacyActionMigrationTests` covering exact-navigation migration, unsafe mutation quarantine, removal of redundant legacy free text, rejection of legacy autonomous contracts, rejection of unverifiable writes, and allowance of read-only actions.

Validation / evidence:
- Reviewed the resulting `BrowserGoalAgent` commit diff and confirmed the behavioral change is confined to the new pre-execution autonomous verification guard; no execution/approval path was broadened.
- Re-probed the execution environment: `dotnet`, `msbuild`, and `csc` are still unavailable, so **no compile/test/Chromium success is claimed**.
- No GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- Migration never treats legacy free text as authority and never calls a model to reinterpret it.
- Only destination-derived exact URL verification is auto-generated, because it is independently present in the trusted persisted action structure.
- Ambiguous legacy mutations require human review rather than silent conversion or replay.
- The autonomous guard executes before durable child reservation, so a malformed Nemotron verification contract cannot create an executable job or approval prompt.
- No credentials, approval grants, bearer tokens, page bodies, CAPTCHA/login bypasses, or new cloud disclosures were introduced.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- Matching Playwright Chromium binaries have not been installed/launched in this execution environment.
- WPF has not been compiled/launched on Windows here.
- The strict planner postcondition schema still needs a live Nebius Token Factory / Nemotron exercise; OpenAI-compatible backends can differ in supported strict-schema subsets.
- The new migration helper is not yet a durable `jobs.json` rewrite/versioning pass. Direct non-goal legacy jobs can still reach the compatibility branch in `BrowserActionJobHandler`; autonomous Nemotron goal execution is now protected independently.
- The recovery card currently discovers sessions already marked failed/ambiguous. A process that dies while the parent still says `Running` may require goal resume to classify the child first.
- `JsonAgentJobStore` and `JsonBrowserGoalSessionStore` are individually atomic files, not a cross-file transaction; the reserved-child-ID protocol remains the safety mechanism across that boundary.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
First obtain a real **.NET 8 build + unit-test + localhost Chromium integration signal** if a capable runtime becomes available and fix all compile/runtime defects immediately. If compilation remains unavailable, implement the durable legacy checkpoint versioning/rewrite pass over `jobs.json`: explicitly mark verification-contract version, safely rewrite deterministic legacy navigation/redundant typed cases, quarantine ambiguous legacy mutations without executing them, and make `BrowserActionJobHandler` reject unversioned legacy writes after migration. Then add a one-command live Nebius strict-schema contract probe for Token Factory.
