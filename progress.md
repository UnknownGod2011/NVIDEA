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
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- `BrowserHostRuntime` owns isolated local Playwright + safety + capability + audit + child-job orchestration and exposes only bounded observations/high-level outcomes.
- `NemotronBrowserPlanner` turns a fresh untrusted observation into one validated step/complete/stop decision. `BrowserGoalAgent` runs the bounded observe -> plan -> job -> verify loop and halts at approval boundaries.
- Browser goal sessions persist separately from approval state, with restart recovery, privacy-minimized verified history, action/planner/context/wall-clock budgets, and no persisted raw typed browser values.
- Parent/child browser orchestration persists a reserved child ID before child creation/execution, then reconciles that exact durable child on restart instead of blindly re-planning or replaying it.
- Durable `Running` child jobs are never automatically replayed. A new evidence-only reconciliation path can mark them completed only when fresh URL/DOM state deterministically proves the intended end state; otherwise they remain stopped for human resolution.
- A lost ephemeral approval grant between approval and execution is restored only as a non-authorizing `WaitingForApproval` state; explicit user approval is required again.
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

### 2026-09-07 — Concrete browser, desktop and multi-step agent
- Added concrete Playwright driver with bounded DOM/ARIA observations, password redaction, host allowlists, user-facing locators, bounded actions and XPath disabled.
- Added `EphemeralJobApprovalStore`/`JobExecutionContext`; approval grants are never serialized and disappear on restart/failure/cancel.
- Added `BrowserCapabilityExecutionService`, `BrowserCapabilityBackend` and `BrowserActionJobHandler` for last-mile capability enforcement before browser execution.
- Added `BrowserHostRuntime` + exact WPF confirmation UX and localhost Chromium approval harness.
- Added `DesktopInvocationService`, `NvideaCompositionRoot`, WPF host/global hotkey, context capture, explicit clipboard disclosure and genuine cancellation/emergency stop.
- Added `NemotronBrowserPlanner`, strict action schema validation, bounded `BrowserGoalAgent`, observation-enriched risk classification and durable multi-step goal sessions.
- Added privacy-minimized verified browser history and planner/action/context/wall-clock budgets.

### 2026-09-07 — Crash-consistent parent/child browser execution
- Added caller-supplied durable job IDs to `ResumableJobOrchestrator.CreateAsync` with equivalent-definition/checkpoint idempotency checks. Permission sets are compared structurally rather than by object identity.
- Split `BrowserHostRuntime` browser child creation from execution into `CreateActionAsync` and `AdvanceActionAsync`; legacy `StartActionAsync` remains a compatibility wrapper.
- Added `ICrashConsistentBrowserGoalHost`; browser goal ordering is now Nemotron decision -> reserve child GUID -> persist parent -> create exact durable child -> advance exact child -> reconcile result.
- Recovery handles missing child, created-but-not-run child, approval-paused child, child completed before parent update, lost ephemeral approval, and durable `Running` ambiguity.
- Durable `Running` jobs fail closed rather than replaying because execution may have crossed an external side-effect boundary before the process died.
- Added `BrowserGoalCrashConsistencyTests` for the crash windows above.
- Added `job.approval_rearmed` audit event; re-arming revokes ephemeral grants and creates no authorization.

### 2026-09-07 — Evidence-based ambiguous side-effect reconciliation
Completed:
- Re-verified every GitHub mutation target as exactly `UnknownGod2011/NVIDEA`; `keyboard.wtf` and all other repositories remained untouched.
- Added `BrowserAmbiguousStateReconciler`, a deterministic no-model verifier for durable `Running` browser jobs after crash.
- Reconciliation rules deliberately use only fresh browser evidence:
  - read-only steps may be recovered from a fresh observation;
  - navigation requires the current normalized URL to exactly match the intended destination;
  - click/type/select/back/refresh require an explicit `ExpectedState` that is visibly present in fresh title/text/element evidence;
  - upload/download never auto-reconcile from DOM evidence and require human resolution.
- Added `BrowserHostRuntime.TryReconcileAmbiguousAsync`. It performs observe -> deterministic proof -> audited mark-complete and **never calls the browser action executor**.
- Added internal `ResumableJobOrchestrator.CompleteAmbiguousRunningAsync` transition. It only accepts a durable `Running` job, revokes any ephemeral approval residue, writes a verified checkpoint, clears approval/error/retry state and records `job.reconciled_completed` in the audit trail. It never invokes a handler.
- Added `BrowserAmbiguousRecoveryService` for parent goal sessions. Positive evidence restores the parent to `Running`, clears the exact pending child link and appends privacy-minimized verified history. Inconclusive evidence preserves the pending child and keeps the goal stopped with a clear human-resolution message.
- Added `IBrowserAmbiguousRecoveryHost` so the recovery coordinator is testable without Playwright.
- `NvideaCompositionRoot` now exposes `CreateBrowserAmbiguousRecoveryServiceAsync`, sharing the exact browser host and `goal-sessions.json` store used by the goal agent.
- Added `BrowserAmbiguousRecoveryTests` covering exact navigation proof, expected-state proof, file-transfer refusal, successful parent restoration without replay and inconclusive evidence preserving the ambiguous child for human resolution.

Validation / evidence:
- GitHub compare from prior head `2cc1853a40682d7d7c30d9dead4a17e357114bf0` shows only five NVIDEA files changed before this ledger update: new recovery implementation/tests plus targeted browser host, composition-root and orchestrator changes.
- Source review confirms the recovery path contains no call to `AdvanceActionAsync`, `RunNextStepAsync` or `IBrowserDriver.ExecuteAsync`; the only browser operation in reconciliation is a fresh bounded `ObserveAsync`.
- Source review confirms upload/download are explicitly excluded from automatic reconciliation even when DOM text appears to say completion occurred.
- `Nvidea.Core.csproj` targets `net8.0`, has nullable enabled and treats warnings as errors; source-level validation was performed with those constraints in mind.
- Runtime tool check again found no `dotnet`, `msbuild` or `csc`. **No compile/test success is claimed.**
- Matching Playwright Chromium binaries still cannot be launched in this execution environment.
- No GitHub Actions workflow was created or rerun just to manufacture a green signal.

Security / privacy review:
- Ambiguous recovery is evidence-only. It does not re-execute the action, call Nemotron, mint/recreate approval, or translate descriptive scope into authorization.
- Reconciled completion requires positive end-state evidence; absence of evidence never becomes inferred success.
- File upload/download remain human-only after ambiguity because DOM state cannot reliably establish external file-transfer side effects.
- Parent sessions continue to persist descriptive pending data but no grant IDs, bearer tokens or execution authority.
- Reconciliation audit records that completion was inferred from post-crash evidence rather than normal execution.
- No CAPTCHA/login/site/OS safeguard bypass was introduced.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- Matching Playwright Chromium binaries have not been installed/launched in this environment.
- New ambiguous-recovery tests have not executed here because no .NET SDK/compiler exists.
- `BrowserGoalAgent` still surfaces a durable `Running` child as failed/ambiguous first; the explicit recovery service must then be invoked by the Windows UX. A polished WPF recovery card/dialog is still needed.
- `ExpectedState` is planner-supplied and may be broad. Reconciliation is intentionally safer than replay, but future hardening should support structured state predicates (URL/element/value assertions) rather than free-text matching alone.
- `JsonAgentJobStore` and `JsonBrowserGoalSessionStore` are individually atomic files, not a cross-file transaction; the reserved-child-ID protocol remains the safety mechanism across that boundary.
- Nemotron strict JSON schema still needs live Token Factory exercise; OpenAI-compatible backends can differ in supported strict-schema subsets.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- WPF has not been compiled/launched on Windows here; UX is functional/minimal and local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real **.NET 8 build + unit test + localhost Chromium integration signal** and fix every compile/runtime defect immediately. If the execution environment still cannot provide .NET, wire `BrowserAmbiguousRecoveryService` into the WPF browser UX so an ambiguous crash displays fresh evidence and a clear `reconciled automatically` versus `human resolution required` state without ever offering an automatic retry. After that, strengthen `ExpectedState` into typed postcondition predicates for more rigorous crash reconciliation and ordinary post-action verification.
