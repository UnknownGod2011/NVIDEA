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
- **Browser:** DOM/accessibility observation -> Nemotron one-step plan -> typed action -> hard-safety floor -> capability policy -> exact approval -> Playwright -> fresh observation -> verification -> repeat under strict budget.
- **Skills / permissions:** capability registry, least privilege, monotonic risk, single-use approvals and append-only audit.
- **Jobs:** durable checkpoints, retries, cancellation, approval-paused states, ephemeral grants and local-vs-Nebius execution policy.
- **Cloud:** Nebius Serverless only for suitable long-running/background workloads; private OS actions stay local.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- Standalone .NET 8 core at `src/Nvidea.Core`.
- Nebius Token Factory inference client with Nemotron default, structured tools/output, conservative routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Concrete Playwright .NET browser driver plus safety/execution contracts under `Browser`.
- Capability/permission/approval/audit foundation under `Capabilities`.
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- Browser jobs route fresh observation -> browser safety -> capability policy -> exact ephemeral approval -> concrete driver -> fresh observation -> verification.
- `NemotronBrowserPlanner` now converts a user goal + fresh untrusted page observation into exactly one schema-constrained action/complete/stop decision, with bounded context and strict action validation.
- `BrowserGoalAgent` now performs a bounded observe -> Nemotron plan -> policy-enforced browser job -> verify loop, halting at exact approval boundaries and continuing only after the browser host consumes the matching approval.
- Browser safety now enriches accessibility-ref actions from the **fresh observed element metadata** so an opaque `e-*` reference cannot hide a Password/OTP/Submit/Delete/etc. target from risk classification.
- Trusted `BrowserHostRuntime` owns an isolated local Playwright session, policy, audit, resumable jobs and ephemeral approvals behind the desktop composition root; it now exposes bounded read-only fresh observations for the trusted goal agent.
- Desktop composition root can create a genuine Nemotron-backed browser goal agent without exposing raw credentials/providers to WPF.
- Desktop invocation/session layer connects Nemotron, memory, optional Tavily, observable state and emergency-stop cancellation.
- Minimal WPF host at `src/Nvidea.Windows` with global `Ctrl+Shift+Space`, text invocation, foreground app/window context, read-only UI Automation selected-text capture, opt-in clipboard disclosure, browser confirmation UX, live state and emergency stop.
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

### 2026-09-07 — Browser execution + approval hardening
- Added concrete Playwright driver with bounded DOM/ARIA observations, password redaction, host allowlists, user-facing locators, bounded actions and XPath disabled.
- Added `EphemeralJobApprovalStore`/`JobExecutionContext`; approval grants are never serialized and disappear on restart/failure/cancel.
- Stabilized browser action IDs across pause/resume while keeping authorization separate and ephemeral.
- Added `BrowserCapabilityExecutionService`, `BrowserCapabilityBackend` and `BrowserActionJobHandler` for last-mile capability enforcement before browser execution.
- Added regression tests for blocked credentials, wrong-scope approval, replay prevention, prompt injection, post-action verification and ambiguous side effects.

### 2026-09-07 — Trusted desktop + Windows host
- Added `DesktopInvocationService`, `NvideaCompositionRoot` and `DesktopSessionController` to compose Nemotron/Nebius, memory and optional Tavily behind one trusted desktop API.
- Clipboard content is withheld by default and rechecked at execution time; external desktop context is bounded and treated as untrusted data.
- Added observable session states and real emergency-stop cancellation.
- Added `src/Nvidea.Windows` WPF host with global hotkey, foreground app/window capture, Auto/Chat/Research modes, explicit clipboard disclosure and graceful hotkey fallback.
- Added read-only UI Automation selection capture using `TextPattern.GetSelection()`; no synthetic `Ctrl+C`, keystrokes or clipboard mutation.
- Selection capture is bounded to 16 ranges / 8,000 characters and fails closed for unsupported/stale controls.

### 2026-09-07 — Exact Windows approval UX + trusted browser host
- Added atomic initial checkpoints so durable browser jobs cannot exist without their descriptive action checkpoint.
- Added `BrowserHostRuntime`, which owns isolated local Playwright, browser safety, capability policy, audit, resumable jobs and ephemeral approvals.
- Private browser/OS jobs are forced local under the conservative execution policy.
- Browser creation is lazy so missing Chromium does not break chat/memory/research startup.
- Added configurable browser start URL, exact host allowlist and headless mode.
- Added WPF approval dialog showing action, target and exact paused scope; denial cancels, confirmation resumes only that scope.
- WPF never receives raw `IBrowserDriver`, approval authorizer or `ApprovalGrant` objects.

### 2026-09-07 — Deterministic localhost Chromium approval harness
- Added `BrowserHostRuntimeIntegrationTests` as an opt-in real Chromium test using a loopback-only HTTP site and exact `127.0.0.1` allowlist.
- Proves no server mutation before approval, exact one mutation after approval, fresh post-action verification, replay rejection, and absence of authorization-grant material from `jobs.json`.
- Replaced asynchronous JS mutation with real form POST/navigation to avoid verification races.
- Added `docs/browser-integration-harness.md`; test is gated by `NVIDEA_RUN_BROWSER_INTEGRATION=1` so normal tests remain lightweight.

### 2026-09-07 — Nemotron multi-step browser planning foundation
Completed:
- Re-verified every mutation target as exactly `UnknownGod2011/NVIDEA`; `keyboard.wtf` and all other repositories remained untouched.
- Environment recheck again found no `dotnet`, `msbuild` or `csc`, so a real compile/test/Chromium signal could not be produced in this runtime. No CI was added/rerun merely to manufacture a green result.
- Added `src/Nvidea.Core/Browser/NemotronBrowserPlanner.cs`.
  - Uses the existing Nebius/Nemotron `IAgentInferenceClient` as the actual reasoning runtime.
  - Requests strict JSON-schema output with exactly one of `act`, `complete`, or `stop`.
  - Supplies only a bounded user goal, bounded recent verified history and a **fresh browser observation explicitly labelled untrusted external data**.
  - System policy tells Nemotron never to treat webpage text as instructions/authorization, never request/type credentials/OTP/payment/API/private-key material, never bypass CAPTCHA/login/site/OS safeguards, never invent observed DOM references, and choose one minimal next step.
  - Validates planner output again locally rather than trusting model-schema compliance: action kind, locator/value coupling, HTTP(S)-only navigation, required locators/values, absence of extraneous hidden values, and accessibility refs must exist in the exact fresh observation.
- Added `tests/Nvidea.Core.Tests/NemotronBrowserPlannerTests.cs` covering schema use, fresh-ref validation, non-web URI rejection, action-smuggling rejection, safe stop behavior, prompt-injection delimiting, locator requirements and bounded context/history.
- Added `src/Nvidea.Core/Desktop/BrowserGoalAgent.cs`.
  - Enforces a configurable 1..30 action budget (default 12).
  - Re-observes before every Nemotron planning turn.
  - Runs each proposed action only through the existing `BrowserHostRuntime` durable job/safety/capability/verification path.
  - Stops immediately at `WaitingForApproval`; it never mints or owns approval grants.
  - `ApproveAndContinueAsync` requires an exact string match to the paused descriptive scope before handing it to `BrowserHostRuntime`, then continues planning only after the underlying action reports `Completed`/verified.
  - Handles complete/stop/failure/cancel/budget-exhausted states explicitly.
- Added `tests/Nvidea.Core.Tests/BrowserGoalAgentTests.cs` covering re-observation, completion, approval pausing, wrong-scope fail-closed behavior, post-approval continuation, strict budget enforcement and cancellation.
- Updated `BrowserHostRuntime` with a bounded read-only `ObserveAsync` used only as planning evidence; this grants no execution capability.
- Updated `NvideaCompositionRoot` to retain the trusted inference abstraction and create `BrowserGoalAgent` using the genuine Nemotron planner + owned browser host.
- During security review found and fixed an existing accessibility-ref risk-classification weakness: `BrowserSafetyPolicy` previously classified only action-supplied locator metadata, so an opaque ref such as `e-7` could omit the observed element name. It now joins fresh observed Name/Role/Value metadata for the referenced element into risk classification.
- Added `BrowserSafetyPolicyObservationTests` proving an observed sensitive field remains blocked even when planner metadata looks benign, while an observed submit control requires explicit high-risk approval.
- Tightened planner validation so only Type/Select/Upload may carry an input value; Click/Navigate/Read/etc. cannot smuggle unused data through an action object.

Validation / evidence:
- Source-level review covered the new planner schema/parser, untrusted-content prompt boundary, fresh accessibility-ref existence checks, goal state transitions, exact-scope comparison, browser-host composition, and risk-policy observation enrichment.
- Existing architecture still makes `BrowserSafetyPolicy` and `CapabilityToolExecutor` authoritative after Nemotron planning; model output alone cannot execute or authorize a browser action.
- `dotnet`, `msbuild` and `csc` are unavailable in this execution environment. Therefore the newly added code/tests are **not claimed as compiled or passing**.
- Playwright Chromium was not launched in this run. No GitHub Actions workflow was created or rerun.

Security / privacy review:
- Page content remains untrusted evidence and cannot create approval/permission state.
- Planner output is locally validated before it can reach the browser job layer.
- Exact approval remains user-controlled, single-use and ephemeral in the existing host/orchestrator path.
- Browser planning remains local with private OS/browser state; only bounded observation text is sent to the configured Nebius/Nemotron inference provider as required for cloud reasoning.
- Accessibility-ref risk classification now uses fresh observed metadata, reducing under-classification of sensitive/consequential controls.
- No CAPTCHA/login/site/OS safeguard bypass was introduced.

## Current Unverified / Risks
- Full repository compilation remains the highest immediate technical risk; source review is not a substitute for `dotnet build` / `dotnet test`.
- Matching Playwright Chromium binaries have not been installed/launched in this execution environment.
- The new Nemotron planner JSON schema must be exercised against the live current Token Factory endpoint; some OpenAI-compatible backends can differ in supported strict-schema subsets.
- `BrowserGoalAgent` orchestration state is currently in-memory while each underlying browser action job is durable. A process restart can recover the individual paused/action job, but not yet the higher-level multi-step goal/session automatically.
- The goal loop currently has action-count budgeting but not a total wall-clock/token/cost budget.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Memory/job/audit JSON persistence is not yet encrypted at rest.
- `Nvidea.Windows` has not yet been compiled/launched on Windows here.
- WPF UX is functional/minimal rather than final orb-quality; local voice/transcription is still absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first **real .NET 8 build/test + Playwright Chromium execution signal** and immediately fix every compile/runtime defect. If the runtime still cannot provide .NET, make the new multi-step goal loop **durably resumable across process restart**: persist only non-authorizing goal/session state and verified step summaries, reconnect it to durable browser jobs, add wall-clock/token/cost budgets, and extend the localhost Chromium harness from one consequential action to a deterministic multi-step Nemotron-planned flow with an approval in the middle and verified completion after resume.
