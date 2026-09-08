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
- Windows-first desktop shell with global invocation, voice/text, context capture, permissions and emergency stop.
- Nemotron via Nebius Token Factory as the primary reasoning runtime with structured tools, routing, retries, cancellation and bounded execution.
- Layered privacy-aware memory, Tavily-backed research, safe browser automation, capability registry, single-use exact approvals, durable jobs and Nebius background execution.
- Private OS actions remain local; high-sensitivity Windows durable state uses CurrentUser DPAPI where implemented.

## Current State
- .NET 8 core at `src/Nvidea.Core`; WPF host at `src/Nvidea.Windows`.
- Nebius/Nemotron inference abstraction with structured output/tools, retries, timeout/cancellation and conservative routing.
- Layered personal memory, Tavily research engine, Playwright browser agent, deterministic verification, prompt-injection/safety boundaries, resumable browser-goal sessions and durable jobs.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer, protected segmented audit trail and emergency-stop plumbing.
- Production browser runtime uses an NVIDEA-owned persistent Chromium profile, popup/new-tab tracking and durable download quarantine.
- Browser downloads are captured as Receiving -> Ready/Interrupted with verified length/SHA-256 and DPAPI-protected metadata on Windows.
- `BrowserDownloadHandoffService` binds a specific download + exact canonical destination to a high-risk `FilesWrite` approval scope, revalidates immediately before export, consumes a short-lived single-use grant before side effects and audits the handoff without persisting the raw destination path.
- `BrowserHostRuntime` exposes `ListDownloadsAsync`, `PrepareDownloadHandoffAsync`, and `ApproveAndExportDownloadAsync`; production browser code has no boolean export API.
- **Trusted WPF download handoff UI now exists.** `DownloadHandoffDialog` displays filename, source host, verified size/SHA-256 and exact destination. `MainWindow.Downloads.cs` shows Ready quarantine entries, uses the .NET 8 `OpenFolderDialog`, prepares the exact runtime scope, requires a fresh explicit confirmation click, then calls `ApproveAndExportDownloadAsync` without ever exposing or persisting `ApprovalGrant`.
- Root README + MIT license.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform milestones
Added Nebius/Nemotron inference, layered memory, Tavily research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, deterministic recovery, typed postconditions, verification-contract migration, live Nebius strict-schema probe, DPAPI-backed state protection, protected hash-chained audit with tail seals and segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent authenticated browser boundary
Added owned browser-profile markers, persistent Playwright startup, popup/new-tab tracking, privacy-minimized session snapshots, Chromium persistence/boundary tests and DPAPI-safe integration assertions.

### 2026-09-08 — Durable permissioned browser downloads
- Added `BrowserDownloadQuarantine` with durable Receiving/Ready/Interrupted/Exported lifecycle, `.partial` capture, SHA-256/length verification, atomic `.payload`, restart recovery, protected metadata, filename sanitization, no-overwrite and destination-boundary checks.
- Wired Playwright `Page.Download` correlation so a `Download` action cannot complete before verified quarantine capture.
- Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `588fdee148fca3c98c5ed90ac758aa899e689f69`, `58374c4126288b5bfffd62e343667f7d1ce746e9`, `2bd59d1aa21e2a246a36e2ea65e834d5db279ca0`.

### 2026-09-08 — Exact-scope handoff and production wiring
- Added `BrowserDownloadHandoffService`; scope binds download id + destination fingerprint to high-risk `FilesWrite`; fresh revalidation + single-use grant + audit.
- Runtime registers `browser.download.handoff` and shares the same quarantine/policy/authorizer/audit as browser capture.
- Removed `PlaywrightBrowserSessionDriver.ExportDownloadAsync(..., bool)`.
- Representative commits: `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `ec2eca992d867d27193e527fb9667b77f817de71`, `6bbd177297126a237017cbea2435455472ec5870`, `e7dfbaac046d6fbd9f51cd3c8f1f6e3b1993b12c`, `bcca57e7c4577ac7bf118329fe3fd9ac4d2e20ab`.

### 2026-09-08 — Trusted WPF download approval flow
Completed:
- `src/Nvidea.Windows/DownloadHandoffDialog.xaml` / `.xaml.cs`
  - trusted confirmation surface with filename, source host, verified byte count/SHA-256, exact destination and explicit explanation that approval is single-use and exact-scope.
  - confirmation requires a fresh `Export file` click; no approval token/grant is surfaced.
  - commits: `06af85bc690e7eecf4f640a7f74f272453a5325b`, `0042bc349ba6f3e582f31b597a0a3424b872ac63`.
- `src/Nvidea.Windows/MainWindow.xaml`
  - added a visible Ready-download quarantine panel with `Review & export` action and no automatic handoff.
  - commit: `d19278ed0e3e4b0d5089b71b61942f0f20a0f5f9`.
- `src/Nvidea.Windows/MainWindow.Downloads.cs`
  - read-only polling discovers the newest Ready artifact.
  - uses `Microsoft.Win32.OpenFolderDialog` to select an existing destination folder.
  - calls `PrepareDownloadHandoffAsync` before confirmation, passes the exact prepared scope back only after the human confirms, and then calls `ApproveAndExportDownloadAsync`.
  - grant creation remains exclusively inside `BrowserHostRuntime`; UI never receives or persists `ApprovalGrant`.
  - successful export reports exact destination + SHA-256; failures remain fail-closed.
  - commit: `3009d0702c5953bb3c3f4800ba627d93ad40c1c2`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- Re-read `progress.md`, current WPF shell, runtime handoff API, quarantine records and handoff plan before implementation.
- Environment check again found no `dotnet`, `msbuild` or `csc`; therefore compilation/WPF launch/Chromium/DPAPI execution is NOT claimed.
- No GitHub Actions workflow was rerun merely to obtain a green signal.

Security / privacy review:
- The UI only sees read-only metadata and the exact prepared destination; it never sees `ApprovalGrant`.
- Destination selection happens before exact-scope preparation; confirmation happens after the destination path is fixed.
- A changed download/destination still fails runtime handoff revalidation and requires a new approval.
- Quarantined payload bytes remain local until explicit export.
- The UI displays source host rather than arbitrary source-page content, minimizing prompt-injection influence in the approval surface.
- Polling is read-only, but currently causes browser-runtime initialization when the main window first renders; this should be revisited after executable validation to avoid unnecessary Chromium startup if desired.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The new WPF flow depends on .NET 8 `Microsoft.Win32.OpenFolderDialog`; source-level compatibility is expected but must be compiled on Windows before claiming success.
- `BrowserDownloadQuarantine.ExportAsync(..., bool userApproved)` remains public at the low-level storage layer for tests, though no production driver/runtime caller exposes it.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Unsolicited/background downloads can still be quarantined; cleanup/retention policy remains future work.
- Audit lifetime retention/byte quotas remain absent.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and immediately repair any compile/runtime issues. If executable validation remains unavailable, internalize the low-level `BrowserDownloadQuarantine.ExportAsync(..., bool)` primitive using a test-only friend assembly or equivalent test seam, then add bounded quarantine retention/cleanup with explicit user controls so unsolicited artifacts cannot accumulate indefinitely.
