# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI for Windows for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest interaction concepts from keyboard.wtf while replacing the intelligence/runtime with an NVIDIA/Nebius-first agent architecture that materially improves memory, research, browser automation, long-running work, safety, verification and personal-AI UX.

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
- **Browser:** DOM/accessibility observation -> typed action -> browser hard-safety floor -> capability policy -> exact approval -> Playwright -> fresh observation -> verification -> receipt.
- **Skills / permissions:** capability registry, least-privilege permissions, monotonic risk, single-use approvals and append-only audit.
- **Jobs:** durable checkpoints, retries, cancellation, approval-paused states, ephemeral grants and local-vs-Nebius execution policy.
- **Cloud:** Nebius Serverless only for suitable long-running/background workloads; private OS actions stay local.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- Standalone .NET 8 core at `src/Nvidea.Core`.
- Nebius Token Factory inference client with verified Nemotron default, structured tools/output, model routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Browser safety/execution foundation plus concrete Playwright .NET driver under `Browser`.
- System-wide capability/permission/approval/audit foundation under `Capabilities`.
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- End-to-end browser jobs route through fresh observation -> browser safety -> capability policy -> exact ephemeral approval -> concrete driver -> fresh observation -> verification.
- New trusted desktop-facing composition layer under `Desktop` now gives the Windows shell one safe API for Nemotron, memory and optional Tavily research.
- New desktop session controller exposes observable agent states and a real emergency-stop cancellation path suitable for orb/hotkey UX.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 — Core foundations
- Added Nebius/Nemotron inference abstraction and Token Factory client with retries, structured tools/output, endpoint validation and conservative routing.
- Added typed layered personal memory with provenance, confidence, importance, sensitivity, retention, secret detection, privacy-aware writes and hybrid semantic/lexical/recency retrieval.
- Added Tavily research planning/search/deduplication/provenance plus Nemotron synthesis with untrusted-web boundaries and machine-checkable source IDs.
- Added provider-neutral browser contracts, browser hard-safety policy, bounded observe-act-observe-verify execution and action receipts.
- Added capability registry, declared permissions, monotonic risk, exact single-use approvals and append-only audit events.
- Added durable jobs with checkpoints, retries/backoff, cancellation, approval pauses and local-vs-Nebius execution selection.
- Added `CapabilityToolExecutor` for last-mile authorization immediately before concrete tool execution.
- Added Nebius Serverless create/list/cancel REST contract with endpoint validation, retries, project scoping and plaintext-secret environment rejection.

### 2026-09-07 — Browser execution and resumable approval hardening
- Added concrete Playwright .NET driver with bounded DOM/ARIA observations, password redaction, exact-host allowlists, user-facing locators and bounded navigation/click/type/select/upload/download-trigger actions. XPath remains intentionally disabled.
- Added in-memory `EphemeralJobApprovalStore` and `JobExecutionContext`; approval grants are never serialized and disappear on restart/failure/cancel.
- Stabilized browser action IDs across pause/resume while preserving exact ephemeral authorization semantics.
- Added `BrowserCapabilityExecutionService`, `BrowserCapabilityBackend` and `BrowserActionJobHandler` to create the first end-to-end permissioned browser job path.
- Added regression tests for blocked credentials, wrong-scope approval, approval replay prevention, prompt injection, post-action verification and failed/ambiguous side effects.

### 2026-09-07 — Trusted desktop invocation/composition layer
Completed:
- Re-verified `UnknownGod2011/NVIDEA` before every GitHub mutation; no writes occurred anywhere else.
- Added `Desktop/DesktopInvocation.cs` with `DesktopInvocationService`, the trusted high-level API intended for Windows shell code.
- Desktop invocation now:
  - accepts active application, window title, selected text and clipboard context;
  - labels selected text, clipboard content and memory excerpts as untrusted data before sending them to Nemotron;
  - withholds clipboard contents by default and only discloses them when the local shell explicitly sets `AllowClipboardContext`;
  - retrieves only Public/Personal memory by default and passes bounded relevant memory to Nemotron as potentially stale/untrusted context;
  - keeps automatic current-information requests on the Tavily/Nemotron research path when research is configured;
  - fails closed if research is requested but Tavily is unavailable rather than silently falling back to unsupported model knowledge;
  - applies conservative fast/standard/deep workload selection based on prompt/context size;
  - never claims tool execution in its system policy unless a verified receipt exists.
- Added `Desktop/NvideaCompositionRoot.cs`:
  - provider credentials/raw clients remain owned by one trusted root rather than UI/plugin code;
  - creates Nebius inference + local memory from environment/configuration;
  - adds Tavily research only when `TAVILY_API_KEY` exists, allowing ordinary local desktop chat to remain usable without Tavily;
  - defaults local state to `%LOCALAPPDATA%/NVIDEA` without committing secrets.
- Added `Desktop/DesktopSessionController.cs`:
  - one active invocation at a time;
  - status states suitable for orb UI (`Idle`, `Thinking`, `Researching`, `WaitingForApproval`, `Acting`, `Completed`, `Cancelled`, `Failed`, etc.);
  - real linked cancellation so emergency stop terminates the active provider call rather than merely changing UI state;
  - status-change event for future WPF/WinUI binding.
- Added `DesktopInvocationTests.cs` covering clipboard non-disclosure by default, explicit clipboard disclosure, relevant-memory injection as untrusted data and fail-closed research when Tavily is absent.
- Added `DesktopSessionControllerTests.cs` covering emergency-stop cancellation and concurrent-invocation rejection.

Security / privacy review:
- Clipboard disclosure is local-policy opt-in per invocation; presence can be acknowledged without sending contents.
- Context strings are bounded before provider disclosure to control cost and reduce prompt-injection blast radius.
- Selected text/memory/clipboard are explicitly data, never policy or authorization.
- Composition root does not expose raw provider clients or API keys to shell code.
- Emergency stop propagates through a linked `CancellationToken` to provider operations.
- This layer intentionally does not expose browser-driver construction; browser tools remain behind their capability/approval boundary.

Validation / evidence:
- Source inspection covered existing inference, memory, Tavily and browser/job contracts before composing them.
- Environment check found no `dotnet`, `msbuild` or `csc`; therefore new desktop code/tests are NOT claimed as compiled or passing.
- No GitHub Actions workflow was added, triggered or rerun, avoiding unnecessary CI/storage usage.

Unverified / risks:
- Full .NET compilation remains the highest immediate technical risk across the repository.
- The current composition root wires Nemotron, memory and research but does not yet instantiate the trusted Playwright/browser-job/capability graph; that remains deliberately isolated until a concrete Windows host owns browser lifetime/session boundaries.
- No actual WPF/WinUI shell exists yet; current desktop layer is the production-facing core contract the shell should bind to.
- Active-app/selected-text/clipboard capture providers and global hotkey implementation remain Windows-host work.
- Voice/local transcription and orb rendering are not yet ported/adapted.
- Memory/job/audit JSON persistence is not yet encrypted at rest.
- Authenticated Playwright session ownership, popup/new-tab lifecycle and durable download handling remain incomplete.
- A deterministic real Chromium integration harness still does not exist.

## Single Best Next Task
Build a minimal **Windows desktop host project** that binds global-hotkey/text invocation + active-app/selected-text/clipboard capture + orb/status UI to `NvideaCompositionRoot.Session`, with emergency stop and explicit clipboard disclosure UI. Keep browser construction inside the trusted host and route browser work only through existing capability/job APIs. In the same run, add a credential-free deterministic Playwright local-page integration harness if the project can remain buildable without secrets. Then obtain the first real `dotnet test`/Chromium execution signal as soon as a .NET-capable environment is available.
