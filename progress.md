# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI for Windows for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest interaction concepts from keyboard.wtf while replacing the intelligence/runtime with an NVIDIA/Nebius-first architecture that materially improves memory, research, browser automation, long-running work, safety, verification and personal-AI UX.

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
- **Browser:** DOM/accessibility observation -> Nemotron one-step plan -> typed action -> hard-safety floor -> capability policy -> exact approval -> Playwright -> fresh observation -> verification -> repeat under strict budgets.
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
- Concrete Playwright .NET browser driver, hard safety policy, verifier and capability execution boundary under `Browser`.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer and append-only audit under `Capabilities`.
- Durable resumable child jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- `BrowserHostRuntime` owns isolated local Playwright + safety + capability + audit + child-job orchestration and exposes only bounded observations/high-level outcomes.
- `NemotronBrowserPlanner` turns a fresh untrusted observation into one validated step/complete/stop decision. `BrowserGoalAgent` runs the bounded observe -> plan -> job -> verify loop and halts at approval boundaries.
- Multi-step browser goal sessions are now durably persisted separately from approval state, with restart recovery, privacy-minimized verified history, action/planner/context/wall-clock budgets, and no persisted raw typed browser values.
- Browser completed-job outcomes now expose a privacy-minimized `BrowserGoalVerifiedStep` parsed from the verified child-job checkpoint so verified evidence can survive process restart without persisting page bodies or typed values.
- Desktop composition root wires browser goal sessions to `browser/goal-sessions.json` while BrowserHostRuntime retains ownership of ephemeral grants.
- WPF host has global `Ctrl+Shift+Space`, foreground app/window context, read-only UI Automation selected-text capture, opt-in clipboard disclosure, browser confirmation UX, live status and emergency stop.
- Opt-in localhost real-Chromium integration harness exists for the trusted browser approval path.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 — Core foundations
- Added Nebius/Nemotron inference abstraction and Token Factory client with structured tools/output, retries, timeout/cancellation, endpoint validation and conservative model routing.
- Added typed layered personal memory with provenance, confidence, importance, sensitivity, retention, secret detection, privacy-aware writes and hybrid semantic/lexical/recency retrieval.
- Added Tavily research planning/search/deduplication/provenance plus Nemotron synthesis with explicit untrusted-web boundaries and validated source IDs.
- Added provider-neutral browser contracts, browser hard-safety policy, bounded observe-act-observe-verify execution and receipts.
- Added capability registry, declared permissions, monotonic risk, exact single-use approvals and append-only audit.
- Added durable jobs with checkpoints, retry/backoff, cancellation, approval pauses, local-vs-Nebius execution selection and Nebius Serverless REST create/list/cancel contract.

### 2026-09-07 — Concrete browser + approval hardening
- Added concrete Playwright driver with bounded DOM/ARIA observations, password redaction, host allowlists, user-facing locators, bounded actions and XPath disabled.
- Added `EphemeralJobApprovalStore`/`JobExecutionContext`; approval grants are never serialized and disappear on restart/failure/cancel.
- Stabilized browser action IDs across pause/resume while keeping authorization separate and ephemeral.
- Added `BrowserCapabilityExecutionService`, `BrowserCapabilityBackend` and `BrowserActionJobHandler` for last-mile capability enforcement before browser execution.
- Added regression coverage for blocked credentials, wrong-scope approval, replay prevention, prompt injection, post-action verification and ambiguous side effects.
- Added `BrowserHostRuntime` + exact WPF confirmation UX. Private browser/OS work is forced local.
- Added deterministic opt-in localhost Chromium harness proving no mutation before approval, exactly one mutation after approval, fresh verification and approval replay rejection.

### 2026-09-07 — Trusted desktop + Nemotron browser goal loop
- Added `DesktopInvocationService`, `NvideaCompositionRoot` and `DesktopSessionController` to compose Nemotron/Nebius, memory and optional Tavily behind a trusted desktop API.
- Added WPF host, global hotkey, bounded active-app/selection context, explicit clipboard disclosure and genuine cancellation/emergency stop.
- Added `NemotronBrowserPlanner` with strict schema-constrained one-step planning and local validation of action/locator/value/URL consistency.
- Added bounded `BrowserGoalAgent` and fixed accessibility-ref risk under-classification by enriching policy with fresh observed element metadata.
- Planner cannot self-authorize; every action still routes through browser safety -> capability policy -> exact approval -> concrete driver -> fresh verification.

### 2026-09-07 — Durable multi-step browser goal sessions
Completed:
- Re-verified every GitHub mutation target as exactly `UnknownGod2011/NVIDEA`; `keyboard.wtf` and every other repository remained untouched.
- Rechecked execution tooling: `dotnet`, `msbuild`, and `csc` are still unavailable; direct GitHub clone also still fails because the container cannot resolve `github.com`. No CI workflow was added/rerun just to manufacture a green signal.
- Added `src/Nvidea.Core/Desktop/BrowserGoalSessionStore.cs` with `IBrowserGoalSessionStore` and atomic JSON persistence.
  - Persists goal/session id, action/planner/context/wall-clock budgets, status, pending child job id, descriptive exact scope, timestamps and privacy-minimized verified-step summaries.
  - Deliberately strips `PendingAction`, including any typed value/file path, before writing to disk.
  - Never persists `ApprovalGrant`, grant ids, bearer tokens, credentials or any object capable of authorizing execution.
  - Added listing support so a future/restarted host can discover recoverable sessions instead of requiring an in-memory session id.
- Extended `BrowserGoalSession` with durable `StartedAt`/`UpdatedAt`, planner-turn count/budget, cumulative planner-context-character budget, wall-clock budget, and bounded verified history.
- Added `BrowserGoalAgent.ResumeAsync(sessionId)`.
  - Restarted `WaitingForApproval` sessions remain paused and do not call Observe, StartAction, approval minting or Nemotron.
  - A persisted descriptive approval scope is not authorization; the user must explicitly approve again after restart and BrowserHostRuntime must mint a new ephemeral exact single-use grant.
- Added strict multi-dimensional execution budgets:
  - existing max browser actions (default 12),
  - max planner turns (default 20),
  - cumulative estimated planner context (default 120k characters),
  - wall-clock lifetime (default 600 seconds).
  - Budget exhaustion fails closed before the next prohibited observation/planner/action boundary where applicable.
- Added privacy-minimized `BrowserGoalVerifiedStep` containing only child job id, action kind, before/after URL, verification detail and completion time. It excludes typed values and page bodies.
- Updated `BrowserHostRuntime` to parse the existing `browser.action.verified` durable child checkpoint into `BrowserGoalVerifiedStep` and attach it to completed `BrowserJobOutcome`s.
- Goal-agent verified history is capped at 50 items and converted back into bounded textual planner history for Nemotron; it cannot grant capability or approval.
- Updated `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` to use `browser/goal-sessions.json`, separate from child `jobs.json` and audit data.
- Added `BrowserGoalSessionStoreTests` covering stripped typed values, absence of grant/bearer material, descriptive paused scope round-trip, budgets, timestamps and verified history.
- Added `BrowserGoalDurabilityTests` covering restart of a paused session without any approval recreation/execution, wall-clock fail-closed behavior before observation/inference, and planner-context exhaustion before Nemotron/action execution.

Validation / evidence:
- Git compare from prior head `8034b6c` through this run shows only NVIDEA changes: `BrowserGoalAgent.cs`, new `BrowserGoalSessionStore.cs`, `BrowserHostRuntime.cs`, `NvideaCompositionRoot.cs`, and the two new test files.
- Source-level review caught and fixed a constructor-argument ordering issue in the initial store implementation before this ledger update.
- The new host/session contract is backward-compatible for existing `BrowserJobOutcome` call sites because verified-step metadata is an optional trailing field.
- The project targets .NET 8 with nullable and `TreatWarningsAsErrors=true`; `TimeProvider` usage is therefore intentional and framework-native.
- **No compile/test success is claimed:** this runtime still has no .NET SDK/compiler and cannot directly clone GitHub. Matching Playwright Chromium was not launched here.

Security / privacy review:
- Approval grants remain exclusively ephemeral in BrowserHostRuntime/ResumableJobOrchestrator and are never placed in goal-session persistence.
- Pending browser action payload is intentionally not persisted by the goal-session store; the durable child job remains the source of truth for the concrete action.
- Persisted exact scope is descriptive matching data only and cannot execute anything without a freshly minted matching grant.
- Verified history excludes page bodies and typed values and is used only as planning evidence.
- Existing prompt-injection boundary and downstream browser/capability policy remain authoritative after Nemotron planning.
- No CAPTCHA/login/site/OS safeguard bypass was introduced.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- Matching Playwright Chromium binaries have not been installed/launched in this environment.
- The Nemotron planner strict JSON schema still needs live Token Factory exercise; OpenAI-compatible backends can differ in supported strict-schema subsets.
- **Crash-consistency gap:** `IBrowserGoalHost.StartActionAsync` currently creates *and advances* a child browser job before returning its job id to the goal agent. A process crash in the narrow interval after a child action executes/pauses but before the parent goal session persists that child id/history could make the restarted parent re-plan the step. Consequential child actions are still protected by their own exact-approval/job state, but this parent-child linkage should become atomic before claiming robust long-running browser resumability.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- WPF has not been compiled/launched on Windows here; its UX is functional/minimal rather than final orb quality and local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
Make parent/child browser execution **crash-consistent** by splitting browser child-job creation from advancement: persist the child job id into the durable `BrowserGoalSession` before any action can execute, then advance/reconcile that exact child job after restart. Add recovery tests for crashes at (1) child created but not run, (2) waiting for approval, and (3) child completed but parent history not yet updated, proving no duplicate consequential action and no stale authorization replay. If a .NET-capable runtime becomes available first, run `dotnet build`/`dotnet test` plus the localhost Chromium harness immediately and fix every compile/runtime defect before further feature work.
