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
- Browser goal sessions persist separately from approval state, with privacy-minimized verified history, action/planner/context/wall-clock budgets, and no persisted raw typed browser values.
- Parent/child browser orchestration persists a reserved child ID before child creation/execution and reconciles that exact durable child on restart instead of blindly re-planning or replaying it.
- Durable `Running` child jobs are never automatically replayed. Evidence-only reconciliation may mark them complete only when fresh URL/DOM state deterministically proves the intended end state; otherwise they stop for human resolution.
- Lost ephemeral approval after restart is restored only as a non-authorizing approval wait; explicit approval is required again.
- WPF host has global `Ctrl+Shift+Space`, foreground app/window context, read-only UI Automation selected-text capture, opt-in clipboard disclosure, browser confirmation UX, live status and emergency stop.
- WPF now also surfaces interrupted ambiguous browser work and exposes an **Inspect evidence** recovery flow that never offers an automatic retry.
- Opt-in localhost real-Chromium integration harness exists for the trusted browser approval path.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 — Core foundations
- Added Nebius/Nemotron inference abstraction and Token Factory client with structured tools/output, retries, timeout/cancellation, endpoint validation and conservative model routing.
- Added typed layered personal memory with provenance, confidence, importance, sensitivity, retention, secret detection, privacy-aware writes and hybrid semantic/lexical/recency retrieval.
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
- Added `NemotronBrowserPlanner`, strict action schema validation, bounded `BrowserGoalAgent`, observation-enriched risk classification and durable multi-step goal sessions.
- Added privacy-minimized verified browser history and planner/action/context/wall-clock budgets.

### 2026-09-07 — Crash-consistent parent/child browser execution
- Added caller-supplied durable job IDs with equivalent-definition/checkpoint idempotency checks and structural permission-set comparison.
- Split browser child creation from execution so parent session can persist the exact child identity before anything executes.
- Recovery handles missing child, created-but-not-run child, approval-paused child, child completed before parent update, lost ephemeral approval and durable `Running` ambiguity.
- Durable `Running` jobs fail closed rather than replaying because execution may have crossed an external side-effect boundary before the process died.
- Added crash-window regression tests and non-authorizing approval re-arm audit flow.

### 2026-09-07 — Evidence-based ambiguous side-effect reconciliation
- Added deterministic no-model reconciliation for durable `Running` browser jobs after crash.
- Read-only steps can recover from a fresh observation; navigation requires exact normalized destination URL; click/type/select/back/refresh require explicit expected-state evidence; upload/download never auto-reconcile from DOM evidence.
- Added `BrowserHostRuntime.TryReconcileAmbiguousAsync`, audited `CompleteAmbiguousRunningAsync`, `BrowserAmbiguousRecoveryService` and `IBrowserAmbiguousRecoveryHost`.
- Positive evidence restores the parent without replay; inconclusive evidence preserves the exact pending child and requires human resolution.
- Added tests for exact navigation proof, expected-state proof, file-transfer refusal, successful parent restoration without replay and inconclusive evidence preservation.

### 2026-09-07 — Windows evidence-only recovery UX
Completed:
- Re-verified the repository before every mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was written.
- Added `NvideaCompositionRoot.ListBrowserGoalSessionsAsync`. It exposes only the already privacy-minimized durable session records to local desktop UX and does **not** initialize Playwright or confer execution authority.
- Added a collapsed-by-default WPF recovery card for the newest failed ambiguous goal with a pending child. It shows a bounded goal summary and descriptive session/child identifiers.
- Added **Inspect evidence** instead of any retry button. The copy explicitly tells the user that NVIDEA will never auto-retry an ambiguous side effect.
- Wired `BrowserAmbiguousRecoveryService` into `MainWindow` lazily. Recovery initializes the trusted browser only when the user asks to inspect evidence.
- Positive reconciliation displays `reconciled automatically from fresh evidence` and, when available, the fresh evidence URL.
- Inconclusive recovery displays `human resolution required; no automatic retry` and preserves the existing fail-closed core behavior.
- Cancellation/error handling explicitly states that the interrupted action was not retried.
- Startup recovery discovery is read-only and isolated: malformed/unavailable recovery state cannot prevent normal chat/research/memory startup.
- Busy/emergency-stop UX now includes recovery inspection; the same cancellation token can stop the observation/reconciliation operation.

Validation / evidence:
- GitHub compare from prior head `e9d1ba524ce9058f1db7baae4eb9ff12c8db7f65` to implementation head `8a137fcdb1924a9f15896bdf342409cf6fa22cd2` shows exactly three implementation files changed: `NvideaCompositionRoot.cs`, `MainWindow.xaml`, and `MainWindow.xaml.cs`.
- Source review confirms the new WPF path calls `BrowserAmbiguousRecoveryService.RecoverAsync` only; the recovery core remains evidence-only and contains no browser execution, model inference or approval-minting call.
- Recovery discovery reads `goal-sessions.json` through the existing privacy-minimized store and does not create the Playwright host.
- No GitHub Actions workflow was created or rerun merely to obtain a signal.
- This execution environment still does not provide a usable `dotnet`, `msbuild` or `csc`; **no compile/test/Windows-launch success is claimed**.

Security / privacy review:
- The WPF surface cannot convert persisted descriptive scope into authorization.
- There is deliberately no `Retry`, `Execute anyway`, grant creation, or approval reconstruction control in the recovery card.
- Fresh browser evidence is only displayed after the trusted recovery service returns it.
- Goal text shown in recovery UX is bounded; raw typed browser values were never persisted by the session store.
- Browser recovery remains local; no extra cloud disclosure was introduced.
- No CAPTCHA/login/site/OS safeguard bypass was introduced.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test` and a real Playwright Chromium launch.
- Matching Playwright Chromium binaries have not been installed/launched in this execution environment.
- WPF has not been compiled/launched on Windows here, including the new recovery card.
- The recovery card currently discovers sessions already marked failed/ambiguous. A process that dies while the parent still says `Running` may require the goal-resume path to classify the child as ambiguous before the card appears; a future recovery inbox should detect durable child state without initializing Playwright.
- `ExpectedState` remains planner-supplied free text. Typed postcondition predicates are needed for stronger ordinary verification and crash reconciliation.
- `JsonAgentJobStore` and `JsonBrowserGoalSessionStore` are individually atomic files, not a cross-file transaction; the reserved-child-ID protocol remains the safety mechanism across that boundary.
- Nemotron strict JSON schema still needs live Token Factory exercise; OpenAI-compatible backends can differ in strict-schema subsets.
- Goal-session JSON, child jobs, audit JSONL and memory JSON are not encrypted at rest yet.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real **.NET 8 build + unit test + localhost Chromium integration signal** and fix every compile/runtime defect immediately. If the execution environment still cannot provide .NET, replace free-text `ExpectedState` with **typed browser postcondition predicates** (URL, title/text, element existence/value/state) and use the same predicates in normal post-action verification plus ambiguous crash reconciliation. This will materially reduce false-positive verification and make the agent’s safety story stronger for judges.
