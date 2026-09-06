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
- Trusted desktop-facing composition layer under `Desktop` gives Windows code one safe API for Nemotron, memory and optional Tavily research.
- Desktop session controller exposes observable agent states and real emergency-stop cancellation.
- Minimal WPF Windows host now exists at `src/Nvidea.Windows` with global `Ctrl+Shift+Space`, text invocation, foreground-app/window context, explicit clipboard disclosure, state/status binding and emergency stop.
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
- Added `DesktopInvocationService`, `NvideaCompositionRoot` and `DesktopSessionController`.
- Desktop invocation accepts app/window/selection/clipboard context, marks external data untrusted, retrieves bounded relevant memory, routes current research through Tavily, and keeps raw provider credentials behind the composition root.
- Clipboard contents are withheld by default and only disclosed when explicitly allowed per invocation.
- Session controller serializes foreground invocations, emits orb-friendly states and propagates emergency stop through linked cancellation tokens.
- Added desktop privacy, memory, research-failure and cancellation tests.

### 2026-09-07 — Minimal Windows WPF host
Completed:
- Re-verified `UnknownGod2011/NVIDEA` immediately before every GitHub mutation; no writes occurred anywhere else.
- Added `src/Nvidea.Windows/Nvidea.Windows.csproj` targeting `net8.0-windows` + WPF and referencing only `Nvidea.Core`.
- Added application bootstrap that creates `NvideaCompositionRoot` from environment configuration and fails visibly if required Nebius configuration is absent.
- Added `WindowsContextCapture` using bounded Win32 foreground-window/process metadata and optional clipboard capture.
- Clipboard capture is opt-in and catches transient clipboard lock failures instead of failing the entire invocation.
- Added a compact WPF host with:
  - global `Ctrl+Shift+Space` registration via `RegisterHotKey`;
  - foreground context capture before NVIDEA activates itself, preserving the invoking application/window;
  - Auto/Chat/Research mode selection;
  - explicit `Allow clipboard context` control;
  - live session status binding;
  - emergency-stop button wired to `DesktopSessionController.EmergencyStop()`;
  - bounded error display rather than fake success states.
- Hotkey registration failure degrades to manual Run-button invocation rather than preventing startup.
- Clipboard disclosure is re-checked at execution time; a previous hotkey capture cannot cause clipboard contents to be sent after the user disables disclosure.

Security / privacy review:
- Provider keys remain behind `NvideaCompositionRoot`; the WPF host never receives raw inference/Tavily clients.
- Clipboard is neither read nor sent by default.
- Active window/process context is bounded and treated by the existing desktop service as untrusted data.
- Global hotkey only summons/captures context; it does not directly execute consequential browser/OS actions.
- Emergency stop cancels the active provider operation rather than only changing UI state.
- No browser safety, permission, approval or audit floor was bypassed or weakened.

Validation / evidence:
- Source review covered the new WPF project reference, bootstrap, Win32 P/Invoke signatures, clipboard failure handling, hotkey lifecycle, context timing and session integration.
- This execution environment still does not expose a usable Windows/.NET build runtime, so `Nvidea.Windows` is NOT claimed as compiled or launched.
- No GitHub Actions workflow was added, triggered or rerun, avoiding CI/storage spam.

Unverified / risks:
- Full repository compilation remains the highest immediate technical risk.
- Selected-text capture is not yet implemented in the Windows host; the core contract supports it, but the host currently supplies `null` rather than using unsafe clipboard-stealing tricks.
- The WPF shell is functional/minimal rather than polished orb UX; voice/local transcription remains absent.
- The trusted Windows composition root still does not instantiate browser lifetime/session ownership or permission-approval UI.
- Memory/job/audit JSON persistence is not yet encrypted at rest.
- Authenticated Playwright session ownership, popup/new-tab lifecycle and durable download handling remain incomplete.
- A deterministic real Chromium integration harness still does not exist.

## Single Best Next Task
Add **safe selected-text capture + explicit approval UX + browser host composition**: use Windows accessibility/UI Automation for read-only selection retrieval where supported (without synthesizing Ctrl+C or mutating the clipboard), add a concrete approval dialog/view-model that can mint only the exact ephemeral grant requested by a paused capability/job, and instantiate Playwright/browser-job lifetime inside the trusted Windows host so a deterministic local browser scenario can demonstrate observe -> plan -> approval -> action -> verification. Pair this with the first credential-free Chromium integration harness and obtain real `dotnet test`/Windows launch evidence as soon as a .NET-capable environment is available.
