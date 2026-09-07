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
- **Local security:** high-sensitivity durable Windows state protected with CurrentUser DPAPI, purpose binding, conservative migration and fail-closed reads.

## Hackathon Demo Bar
The <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory changing later behavior, Tavily research with sources, complex browser work with visible verification, approval before consequential actions, meaningful Nebius background work, and an architecture view proving Nemotron/Nebius/Tavily are core.

## Current State
- .NET 8 core at `src/Nvidea.Core` plus WPF host at `src/Nvidea.Windows`.
- Nebius Token Factory inference client with Nemotron default, structured output/tools, conservative routing, retries, timeout/cancellation and endpoint validation.
- Layered privacy-aware personal memory under `Memory`.
- Tavily provider + Nemotron research engine under `Research`.
- Concrete Playwright browser driver, hard safety policy, capability execution boundary and deterministic verifier under `Browser`.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer and privacy-minimized audit under `Capabilities`.
- Durable resumable jobs, ephemeral approval handoff and Nebius Serverless REST contract under `Jobs`.
- `BrowserHostRuntime` owns local Playwright + safety + capability + audit + child-job orchestration and exposes only bounded observations/high-level outcomes.
- `NemotronBrowserPlanner` turns fresh untrusted observations into validated one-step decisions using typed postconditions.
- `BrowserGoalAgent` runs a bounded observe -> plan -> durable child -> verify loop, halts at approval boundaries, and independently rejects legacy/unverifiable autonomous action contracts before child reservation.
- Typed browser postconditions and verification-contract v2 are enforced for durable browser actions; safe legacy records migrate and ambiguous legacy mutations quarantine without execution.
- Memory, durable jobs and browser-goal sessions use versioned protected local-state envelopes; on Windows the default protector is CurrentUser DPAPI.
- Local capability audit uses protected per-event payloads, append-only hash chaining, crash-safe protected tail seals, segmented rotation and protected cross-segment manifests.
- Production browser orchestration uses `SegmentedAuditTrail`.
- Production browser runtime uses an NVIDEA-owned persistent Chromium profile and session-aware popup/new-tab tracking.
- Browser downloads are captured into an NVIDEA-owned durable quarantine before a `Download` action may return successfully; quarantine metadata is DPAPI-protected by default on Windows.
- `BrowserDownloadHandoffService` provides exact-scope, single-use approval and audit for releasing quarantined artifacts.
- **Production runtime now composes that handoff service directly.** The handoff capability is registered in the same capability registry, uses the same `ScopedApprovalAuthorizer` and segmented audit trail, and the persistent browser session exposes the exact quarantine instance used by Playwright capture.
- `BrowserHostRuntime` now exposes trusted-UI APIs to list quarantined downloads, prepare an exact handoff plan, and approve/export only when the UI echoes the exact scope shown to the human. It mints a 2-minute single-use `ApprovalGrant` only after that equality check; `BrowserDownloadHandoffService` consumes it before the copy.
- The old `PlaywrightBrowserSessionDriver.ExportDownloadAsync(..., bool userApproved)` escape hatch has been removed. The driver can list/capture downloads but cannot release them.
- Root README + MIT license.
- No repository other than NVIDEA has been mutated.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform milestones
- Added Nebius/Nemotron inference abstraction, layered memory, Tavily research, browser contracts, capability registry, approval boundary, durable jobs and Nebius Serverless contracts.
- Added Playwright execution, Windows shell, durable parent/child browser orchestration, deterministic crash reconciliation, typed postconditions, verification-contract v2 migration and end-to-end Chromium contract harnesses.
- Added live Nebius strict-schema probe using the production `NebiusTokenFactoryClient` and `NemotronBrowserPlanner`.
- Added CurrentUser DPAPI-backed local-state protection for memory/jobs/browser-goal sessions.
- Reworked audit persistence into protected hash-chained records, tail seals and bounded segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent authenticated browser boundary
- Added NVIDEA-owned browser-profile ownership markers, persistent Playwright context startup, safe popup/new-tab tracking, privacy-minimized session snapshots, and opt-in Chromium persistence/boundary tests.
- Production `BrowserHostRuntime` uses the persistent Chromium composition.
- Fixed Windows integration tests to inspect logical DPAPI-decrypted job records instead of raw encrypted `jobs.json` text.

### 2026-09-08 — Durable permissioned browser downloads
- Added `BrowserDownloadQuarantine`: durable Receiving/Ready/Interrupted/Exported lifecycle; `.partial` -> SHA-256/length verification -> atomic `.payload`; restart interruption recovery; DPAPI-protected metadata; sanitized filenames; no-overwrite and destination-boundary checks; atomic verified export copy.
- `PlaywrightBrowserSessionDriver` correlates `Page.Download` events to the active page/action sequence and does not complete `BrowserActionKind.Download` until quarantine capture succeeds.
- Production persistent browser composition injects the download quarantine.
- Added quarantine regression tests for capture/hash persistence, tampering, cancellation, protected metadata and no-overwrite behavior.
- Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `588fdee148fca3c98c5ed90ac758aa899e689f69`, `58374c4126288b5bfffd62e343667f7d1ce746e9`, `2bd59d1aa21e2a246a36e2ea65e834d5db279ca0`.

### 2026-09-08 — Exact-scope download handoff approval boundary
- Added `BrowserDownloadHandoffService` and regression tests.
- Scope binds download id + canonical destination fingerprint to `FilesWrite`; policy requires high-risk exact approval.
- Re-derives the plan immediately before export, consumes a short-lived single-use grant before side effects, and audits waiting/scope-changed/start/success/cancel/failure without persisting the raw destination path.
- Representative commits: `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `ec2eca992d867d27193e527fb9667b77f817de71`.

### 2026-09-08 — Production download handoff wiring
Completed:
- `src/Nvidea.Core/Browser/PersistentBrowserContextFactory.cs`
  - `PersistentBrowserContextSession` now returns the exact `BrowserDownloadQuarantine` instance used by the Playwright session, so production authorization and capture operate on one durable store rather than parallel instances.
  - Commit: `6bbd177297126a237017cbea2435455472ec5870`.
- `src/Nvidea.Core/Browser/PlaywrightBrowserSessionDriver.cs`
  - removed `ExportDownloadAsync(Guid, string, bool, ...)`; the browser driver no longer has any boolean-based release API.
  - capture and read-only listing remain intact.
  - Commit: `e7dfbaac046d6fbd9f51cd3c8f1f6e3b1993b12c`.
- `src/Nvidea.Core/Desktop/BrowserHostRuntime.cs`
  - registered `browser.download.handoff` as a distinct high-risk, confirmation-required `FilesWrite` capability. This fixes a real production composition bug: without the descriptor, `BrowserDownloadHandoffService.PrepareAsync` would fail capability lookup if wired into the runtime.
  - composes `BrowserDownloadHandoffService` with the same capability policy, `ScopedApprovalAuthorizer`, quarantine and `SegmentedAuditTrail` used by the production browser host.
  - added `ListDownloadsAsync`, `PrepareDownloadHandoffAsync`, and `ApproveAndExportDownloadAsync` as the trusted runtime boundary.
  - approval requires an exact scope echo matching the prepared plan. Only then does the runtime mint a 2-minute grant from `ScopedApprovalAuthorizer`; the handoff service re-prepares the plan and consumes the grant before export.
  - Commit: `bcca57e7c4577ac7bf118329fe3fd9ac4d2e20ab`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- Re-fetched the modified runtime after commit and confirmed the handoff descriptor, shared policy/authorizer/audit composition and trusted runtime APIs are present.
- Git tree at `bcca57e7c4577ac7bf118329fe3fd9ac4d2e20ab` confirms only NVIDEA files changed and shows the expected new blobs for `PersistentBrowserContextFactory.cs`, `PlaywrightBrowserSessionDriver.cs`, and `BrowserHostRuntime.cs`.
- Searched the repository for `ExportDownloadAsync`; no default-branch result remains.
- The execution environment was checked again for `dotnet`, `msbuild`, `csc`, and `mcs`; none is available. No compile/test/Chromium/DPAPI success is claimed and no GitHub Actions run was triggered merely to create a green signal.

Security / privacy review:
- Browser code that observes/clicks/downloads no longer possesses a direct boolean release method; release authority is concentrated in the runtime handoff boundary.
- The exact quarantine instance is shared between capture and handoff, avoiding state desynchronization or a second unverified download store.
- Handoff remains least-privilege `FilesWrite`, high-risk, consequential and human-confirmed.
- Destination paths are shown only to the trusted UI/runtime plan; the durable audit stores only a fingerprint.
- Grant material remains ephemeral and short-lived. A scope mismatch fails before minting the grant; handoff revalidation can still reject mutation after approval preparation.
- The low-level `BrowserDownloadQuarantine.ExportAsync(..., bool userApproved)` primitive remains public for now because existing unit tests directly exercise it and executable validation is unavailable. It is no longer reachable from `PlaywrightBrowserSessionDriver` or `BrowserHostRuntime`. Make it `internal` only after compiling the test assembly boundary or adding an explicit test-only friend assembly.

## Current Unverified / Risks
- **Highest risk remains executable validation:** source review is not a substitute for `dotnet build`, `dotnet test`, Windows WPF launch and a real Playwright Chromium launch.
- Persistent Chromium, browser download capture, runtime handoff wiring and DPAPI-protected stores have not compiled/executed in this environment.
- Live Token Factory strict-schema probe remains unexecuted because .NET and a Nebius API key are unavailable here.
- The trusted WPF download list/destination confirmation UI is still absent; the runtime APIs now exist for it.
- `BrowserDownloadQuarantine.ExportAsync(..., bool userApproved)` remains public at the low-level storage layer although production driver/runtime callers no longer expose it.
- Persistent Chromium profile contents and quarantined payload bytes are local but not application-encrypted by NVIDEA; OS/user-profile protections remain their confidentiality boundary.
- Unsolicited/background page downloads can be quarantined; cleanup/retention policy remains future work.
- Segmentation bounds active audit files by event count, but lifetime archive retention and byte-size quotas remain absent.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified production embedding adapter remain opportunities.

## Single Best Next Task
First obtain a real .NET 8 build/test/Chromium/Windows signal and immediately fix compile/runtime issues. If executable validation remains unavailable, build the minimal trusted WPF Ready-download list and destination-picker confirmation flow on top of `BrowserHostRuntime.ListDownloadsAsync` / `PrepareDownloadHandoffAsync` / `ApproveAndExportDownloadAsync`. The UI must display filename, source host, size/hash and exact destination, require a fresh explicit click, and never expose or persist `ApprovalGrant`. After that, make the quarantine boolean export primitive internal (with an explicit test-only friend assembly if needed) so no public API can release a file using a bare boolean.
