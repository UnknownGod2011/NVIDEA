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
- Parent/child browser orchestration now persists a reserved child ID before child creation/execution, then reconciles the exact durable child on restart instead of blindly re-planning or replaying it.
- Durable `Running` child jobs are now treated as ambiguous after crash and are not automatically replayed.
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
Completed:
- Re-verified every GitHub mutation target as exactly `UnknownGod2011/NVIDEA`; `keyboard.wtf` and every other repository remained untouched.
- Added caller-supplied durable job IDs to `ResumableJobOrchestrator.CreateAsync` with equivalent-definition/checkpoint idempotency checks. Permission sets are compared structurally rather than by object identity.
- Split `BrowserHostRuntime` browser child creation from execution:
  - `CreateActionAsync(jobId, action)` creates the durable child and checkpoint without executing it.
  - `AdvanceActionAsync(jobId)` advances only that exact existing child.
  - legacy `StartActionAsync` remains as a compatibility wrapper around create + advance.
- Added `ICrashConsistentBrowserGoalHost` so `BrowserGoalAgent` can use the stronger protocol without breaking existing `IBrowserGoalHost` test doubles/callers.
- New browser goal ordering is: Nemotron decision -> reserve child GUID -> persist parent session with that GUID -> create durable child -> advance exact child -> reconcile result.
- Recovery behavior now covers the critical crash windows:
  1. **Parent reserved ID, child never created:** child lookup returns missing, action reservation is released, action budget is restored, and the agent safely re-plans. No browser side effect could have happened.
  2. **Child created but never advanced:** restart advances that exact child ID; it does not create a replacement.
  3. **Child waiting for approval:** restart preserves the same descriptive exact scope and performs no approval or execution.
  4. **Child completed before parent update:** restart imports the verified child result into privacy-minimized parent history and continues without replay.
  5. **Approval consumed but process died before execution:** the child may be durably `Pending` while the grant is gone. `RearmApprovalAsync` restores only `WaitingForApproval`; no grant is minted and the user must approve again.
  6. **Child durably `Running` after process failure:** state is considered side-effect-ambiguous and fails closed. `RunNextStepAsync` no longer automatically replays durable `Running` jobs.
- Added `BrowserGoalCrashConsistencyTests` for missing child, created-but-not-run child, waiting child, lost-grant re-arm, completed-before-parent-update, and ambiguous-running fail-closed behavior.
- Added explicit audit event `job.approval_rearmed` for restart recovery. Re-arming revokes any ephemeral grant and creates no authorization.

Validation / evidence:
- Repository compare from prior head `a3e7c458eaa0908bdcf46e36af1a16546b5e7a76` shows only NVIDEA changes in `BrowserGoalAgent.cs`, `BrowserHostRuntime.cs`, `ResumableJobOrchestrator.cs`, the new crash-consistency tests, and this progress ledger.
- Source review caught and fixed a logical-idempotency bug where record equality would not safely compare independently-created `IReadOnlySet<DataPermission>` instances.
- Source review also identified and closed the approval-loss window where `ResumeAfterApprovalAsync` can persist `Pending` before the ephemeral grant reaches execution.
- `dotnet`, `msbuild`, and `csc` are still unavailable in this execution environment. **No compile/test success is claimed.**
- No GitHub Actions workflow was created or rerun just to manufacture a green signal.

Security / privacy review:
- Parent sessions persist child IDs and descriptive exact scopes only; they do not persist `ApprovalGrant`, grant IDs, bearer tokens or other execution authority.
- Raw pending browser action payload remains stripped from parent goal-session persistence; the durable child checkpoint remains the concrete action source of truth.
- Missing child recovery cannot replay an action because no durable child ever existed.
- Existing child recovery is ID-addressed; it does not ask Nemotron to reconstruct a possibly different action before reconciliation.
- `Running` after restart is deliberately fail-closed because execution may have crossed an external side-effect boundary before the process died.
- Lost ephemeral approvals never regenerate themselves. Restart can restore only a waiting state and requires a new explicit user approval.
- Existing prompt-injection, capability, exact-scope, browser safety and verification boundaries remain downstream and authoritative.
- No CAPTCHA/login/site/OS safeguard bypass was introduced.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- Matching Playwright Chromium binaries have not been installed/launched in this environment.
- The new crash-consistency tests have not been executed here because no .NET SDK/compiler exists.
- Durable `Running` jobs now fail closed rather than replay, but the product still needs a user-facing/manual reconciliation path that can inspect fresh browser evidence and decide whether an ambiguous action already took effect.
- `JsonAgentJobStore` and `JsonBrowserGoalSessionStore` are individually atomic files, but there is no cross-file transaction. The reserved-child-ID protocol is designed to remain safe across that boundary rather than pretending a distributed transaction exists.
- The Nemotron planner strict JSON schema still needs live Token Factory exercise; OpenAI-compatible backends can differ in supported strict-schema subsets.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- WPF has not been compiled/launched on Windows here; UX is functional/minimal and local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real **.NET 8 build + unit test + localhost Chromium integration signal** and fix every compile/runtime defect immediately. Once that is green, implement an explicit **ambiguous side-effect reconciliation flow** for crashed `Running` browser jobs: inspect fresh DOM/URL evidence, prove whether the expected state already exists, mark the child verified without replay when evidence is sufficient, otherwise stop for human resolution. This is the remaining reliability gap before claiming robust long-running browser automation.
