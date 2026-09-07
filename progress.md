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
- **Browser:** DOM/accessibility observation -> typed action -> hard-safety floor -> capability policy -> exact approval -> Playwright -> fresh observation -> verification -> receipt.
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
- Trusted `BrowserHostRuntime` owns an isolated local Playwright session, policy, audit, resumable jobs and ephemeral approvals behind the desktop composition root.
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
- Browser writes retain the full path: fresh observation -> hard-safety floor -> capability policy -> exact approval -> `CapabilityToolExecutor` -> Playwright -> fresh observation -> verification.

### 2026-09-07 — Deterministic localhost Chromium approval harness
Completed:
- Re-verified the write target as exactly `UnknownGod2011/NVIDEA` before every mutation; `keyboard.wtf` and every other repository remained untouched.
- Added `tests/Nvidea.Core.Tests/BrowserHostRuntimeIntegrationTests.cs` as a credential-free, opt-in real Chromium integration test.
- The test starts a loopback-only `TcpListener` HTTP site on an ephemeral port and launches the real `BrowserHostRuntime` headlessly with an exact `127.0.0.1` host allowlist.
- The controlled page exposes one consequential **Submit demo mutation** form action. The server owns an atomic mutation counter, giving an external side-effect signal independent of browser observation.
- The test proves the job enters `WaitingForApproval` and server mutation count remains zero before approval.
- It verifies persisted `jobs.json` does not contain grant/token authorization material while paused or after completion.
- It resumes using only the exact paused approval scope, requires the job to reach `Completed`, and verifies the server mutation count becomes exactly one.
- It then attempts to replay the same approval scope and requires `InvalidOperationException`, with the mutation count remaining exactly one.
- Initial async JavaScript/fetch-based mutation was rejected during review because Playwright could observe before the async handler completed. The harness was changed to a real form POST/navigation so Playwright's click auto-wait + fresh observation produce a deterministic verification boundary.
- Added `docs/browser-integration-harness.md` with setup/run commands, security assertions and failure interpretation.
- The integration test is opt-in via `NVIDEA_RUN_BROWSER_INTEGRATION=1`, so ordinary unit tests stay lightweight and do not download/launch Chromium.

Security / privacy review:
- Harness binds only `IPAddress.Loopback`, uses no external site or credentials, and grants the runtime access only to the exact loopback host.
- No CAPTCHA/login/site/OS safeguard bypass exists.
- Approval remains exact-scope, ephemeral and single-use; the durable checkpoint/job file never becomes authorization.
- Server-side mutation count directly detects any execution before approval or approval replay.
- No CI workflow was added or rerun, avoiding browser-binary/artifact storage spam.

Validation / evidence:
- Source review covered `BrowserHostRuntime`, browser safety classification, exact-scope resume behavior, Playwright role locator behavior, deterministic form navigation, loopback server lifecycle, state-file assertions and cleanup.
- Current official Playwright .NET documentation confirms browser binaries are version-coupled to Playwright and should be installed from the generated `playwright.ps1` CLI after build; the runbook follows that flow.
- Environment check in this run again found no `dotnet`, `msbuild` or `csc`. Therefore the new integration test is **not claimed as compiled or passing**, and Chromium was not launched here.

## Current Unverified / Risks
- Full repository compilation remains the highest immediate technical risk; repeated source review is not a substitute for `dotnet build` / `dotnet test`.
- Matching Playwright Chromium binaries have not been installed/launched in this execution environment.
- `Nvidea.Windows` has not yet been compiled/launched on Windows here.
- Nemotron-driven multi-action browser planning is not yet connected to the browser host UX; the current host exposes typed browser actions.
- Authenticated persistent browser-profile ownership, popup/new-tab tracking and durable download lifecycle remain incomplete.
- Memory/job/audit JSON persistence is not yet encrypted at rest.
- WPF UX is functional/minimal rather than final orb-quality; local voice/transcription is still absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first **real .NET 8 compile/test + Playwright Chromium execution signal** for the repository using the documented localhost harness, and immediately fix every compile/runtime defect found. Once the trusted browser path is actually green, connect **Nemotron structured multi-step browser planning** to `BrowserHostRuntime` with a strict action budget, observe-plan-act-verify loop, per-step policy re-evaluation, resumable checkpoints and exact approvals for consequential steps; this will turn the current typed browser demo into a competition-grade agent flow.
