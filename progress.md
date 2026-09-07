# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest interaction ideas from keyboard.wtf while making NVIDEA independently stronger in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy and safety.

Target: **Personal AI**. Secondary target: **Best Use of Tavily**. Ambition: top-three / Grand Prize quality as a complete product rather than a model wrapper.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working NVIDEA functionality merely to simplify implementation.

## Target Architecture
- **Desktop shell:** Windows global hotkeys, voice/text invocation, orb/status, active-app/selected-text/clipboard context, local speech where useful, permission UX and emergency stop.
- **Agent core:** Nemotron through Nebius Token Factory, structured tools/output, bounded execution, verification, retries/cancellation and approvals.
- **Memory:** typed working/episodic/semantic/project/skill memory with privacy-aware writes, hybrid retrieval, provenance/confidence/sensitivity/retention and deletion controls.
- **Research:** Nemotron planning -> Tavily evidence -> untrusted-content boundary -> Nemotron synthesis -> validated citations.
- **Browser:** DOM/accessibility observation -> Nemotron one-step plan -> typed action + typed postconditions -> hard-safety floor -> capability policy -> exact approval -> Playwright -> fresh observation -> deterministic verification -> repeat under strict budgets.
- **Skills / permissions:** capability registry, least privilege, monotonic risk, single-use approvals and append-only audit.
- **Jobs:** durable checkpoints, retries, cancellation, approval-paused states, ephemeral grants and local-vs-Nebius execution policy.
- **Cloud:** Nebius Serverless only for suitable long-running/background workloads; private OS actions stay local.
- **Local security:** high-sensitivity durable Windows state protected with a CurrentUser DPAPI boundary, versioned formats, purpose binding, conservative migration and fail-closed reads.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- .NET 8 core at `src/Nvidea.Core` plus WPF host at `src/Nvidea.Windows`.
- Nebius Token Factory inference client with Nemotron default, structured output/tools, conservative routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Concrete Playwright .NET browser driver, hard safety policy, capability execution boundary and deterministic verifier under `Browser`.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer and privacy-minimized audit under `Capabilities`.
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- `BrowserHostRuntime` owns local Playwright + safety + capability + audit + child-job orchestration and exposes only bounded observations/high-level outcomes.
- `NemotronBrowserPlanner` turns fresh untrusted observations into validated one-step decisions using typed postconditions.
- `BrowserGoalAgent` runs a bounded observe -> plan -> durable child -> verify loop, halts at approval boundaries, and independently rejects legacy/unverifiable autonomous action contracts before child reservation.
- Browser goal sessions persist separately from approval state with privacy-minimized verified history and action/planner/context/wall-clock budgets.
- Parent/child browser orchestration persists a reserved child ID before creation/execution and reconciles that exact child after restart.
- Durable `Running` child jobs are never blindly replayed. Fresh deterministic evidence may reconcile them; otherwise human resolution is required.
- WPF host has global `Ctrl+Shift+Space`, foreground app/window context, read-only selected-text capture, opt-in clipboard disclosure, confirmation UX, live status, emergency stop and interrupted-work evidence inspection.
- Typed browser postconditions support exact URL, title/text presence, element existence/value, checked state and enabled state; normal execution and crash reconciliation share the evaluator.
- Durable browser-action checkpoints carry verification contract v2. Safe legacy records are migrated; ambiguous legacy mutations are quarantined without execution.
- `BrowserActionJobHandler` accepts only current typed-verification checkpoints.
- Opt-in localhost Chromium integration harness covers approval boundaries and deterministic Nemotron planner -> durable child -> Playwright -> typed verifier behavior.
- Live Nebius strict-schema contract probe exists under `tools/Nvidea.NebiusContractProbe`.
- Memory, durable jobs and browser-goal sessions use versioned protected local-state envelopes; on Windows the default protector is CurrentUser DPAPI.
- Local capability audit records use per-event protection on Windows plus a versioned append-only hash chain, crash-safe protected tail seals, and segmented cross-segment anchors.
- Production browser orchestration uses `SegmentedAuditTrail`; active audit segments rotate after 1,000 events by default.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform milestones
- Added Nebius/Nemotron inference abstraction, layered memory, Tavily research, provider-neutral browser contracts, capability registry, approval boundary, durable jobs and Nebius Serverless contracts.
- Added Playwright browser execution, Windows desktop shell, durable parent/child browser orchestration, deterministic crash reconciliation, typed postconditions, verification-contract v2 migration, and an end-to-end Chromium contract harness.
- Added live Nebius strict-schema probe using the production `NebiusTokenFactoryClient` and `NemotronBrowserPlanner`.
- Added CurrentUser DPAPI-backed local-state protection for memory/jobs/browser-goal sessions.
- Reworked local audit storage into encrypted per-event hash chains with crash-safe protected tail seals, then added bounded segmented rotation with protected cross-segment manifests and deterministic recovery.

### 2026-09-07 — Explicit browser-profile ownership + safe popup/new-tab tracking
Completed:
- Added `src/Nvidea.Core/Browser/BrowserProfileOwnership.cs`.
- The profile boundary reserves a dedicated `browser-profile` directory beneath the NVIDEA state directory; arbitrary external Chrome/Edge user-data paths are never accepted by this component.
- A versioned `.nvidea-profile.json` ownership marker is written atomically on first creation. Existing unmarked directories fail closed instead of being silently adopted.
- Corrupt/unsupported ownership markers fail closed and existing browser data is never deleted during validation.
- Added `src/Nvidea.Core/Browser/PlaywrightBrowserSessionDriver.cs`, which composes the existing per-page `PlaywrightBrowserDriver` rather than duplicating action logic.
- The session-aware driver listens to Playwright `BrowserContext.Page` events and tracks newly-created tabs/popups without assuming an undocumented ordering for `BrowserContext.Pages`.
- Newly-created pages become active only after their URL is absolute HTTP(S) and satisfies the configured allowed-host boundary.
- Cross-boundary HTTP(S) popups are closed without being observed or acted upon by the agent. `about:blank`/not-yet-navigated pages remain pending until a later browser boundary call can classify their destination.
- After click actions, the driver resolves pending pages before returning so the next verifier observation can attach to the permitted popup/new tab when that is the action result.
- Added a read-only session snapshot model for diagnostics without exposing cookies, local storage, authorization headers or other browser credentials.
- Added `tests/Nvidea.Core.Tests/BrowserProfileOwnershipTests.cs` covering dedicated-profile creation/reuse, refusal to adopt unmarked directories, corrupt-marker fail-closed behavior while preserving state, and rejection of profile paths outside the NVIDEA state root.

Validation / evidence:
- Profile ownership commit: `f3335f59111de2928cc6e6c186370e25d8b29241`.
- Initial session driver commit: `43cfb4ffb19d5c9a07d72887fa429353bbe656cf`.
- Profile-boundary tests commit: `384d136338f26eb9d17f61d4eb4f81c7c9a092a5`.
- Session-driver event-order hardening commit: `970b40bce228adfe97cfeacf34422b01e7887de8`.
- Current official Playwright .NET docs were checked during this run. They confirm that persistent contexts use a dedicated user-data directory and that `BrowserContext.Page` is emitted for new pages/popups; the implementation deliberately follows those documented primitives rather than automating a user's default Chrome profile.
- The execution container was probed again for `dotnet`, `msbuild` and `csc`; none is available. Therefore **no compilation or test execution success is claimed**.
- No GitHub Actions workflow was created or rerun merely to manufacture a green signal.

Security / privacy review:
- Profile ownership is local-only metadata and contains only a random profile ID, format version and creation timestamp; no cookies, credentials or session contents are copied into NVIDEA state metadata.
- The design intentionally creates a NVIDEA-owned profile instead of taking control of the user's default browser profile, reducing accidental credential/session scope.
- Popup adoption is constrained by the same allowed-host boundary used by direct navigation. A newly-created disallowed HTTP(S) page is closed instead of becoming agent-visible context.
- The session snapshot surfaces only page URLs and boundary classification, not cookies/local-storage data.
- The new session-aware driver is not yet the `BrowserHostRuntime` production driver, so these protections are implemented and reviewable but **not yet active in the desktop composition root**.
- Persistent browser profile data itself will be managed by Chromium once production wiring lands; it is local but not yet wrapped by the NVIDEA DPAPI envelope. Do not claim full encrypted-at-rest browser credential storage.

## Current Unverified / Risks
- **Highest risk remains compilation/runtime validation:** source review is not a substitute for `dotnet build`, `dotnet test`, a Windows WPF launch and a real Playwright Chromium launch.
- The live strict-schema probe still has not been compiled or run against Token Factory here because .NET and a Nebius API key are unavailable.
- DPAPI P/Invoke, protected stores, protected audit events/tail seals and protected segment manifests have not executed on a real Windows runner in this environment.
- `BrowserHostRuntime` still launches a non-persistent context and still uses the single-page `PlaywrightBrowserDriver`; the new ownership/session components must be wired into production before authenticated-session persistence or popup tracking can be claimed end to end.
- Persistent Chromium profile contents are local but are not application-encrypted by NVIDEA. Threat-model/user-disclosure language must distinguish local browser-managed secrets from DPAPI-wrapped NVIDEA state.
- Segmentation bounds active audit files by event count, but total archive retention and byte-size quotas are not yet implemented.
- Durable download lifecycle remains incomplete.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.
- Cross-file browser parent/child state is still separate atomic files; reserved-child ordering remains the crash-safety mechanism.

## Single Best Next Task
Wire the new browser ownership/session layer into `BrowserHostRuntime` using Playwright `LaunchPersistentContextAsync` with the dedicated NVIDEA-owned profile directory, make the browser field/context shutdown logic correct for persistent-context semantics, expose a safe session diagnostic if useful, and add an opt-in localhost Chromium integration test proving: authenticated/session state survives a runtime restart, an allowed popup becomes the verified active page, and a cross-boundary popup is never adopted. Then obtain the first real .NET 8 build/test/Chromium/Windows-DPAPI/live-Nebius signal as soon as an executable environment is available.
