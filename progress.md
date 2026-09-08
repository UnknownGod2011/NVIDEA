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
- Trusted WPF download handoff UI displays filename, source host, verified size/SHA-256 and exact destination, then requires a fresh explicit confirmation before exact-scope export.
- The low-level `BrowserDownloadQuarantine.ExportAsync(..., bool)` storage primitive is assembly-internal. Tests retain access only through `InternalsVisibleTo("Nvidea.Core.Tests")`, and a reflection regression test guards against accidental public re-exposure.
- Download quarantine now has fail-closed byte quotas: default 512 MiB retained total / 128 MiB per file. Quota checks never silently evict Ready or Exported artifacts.
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
- Added `DownloadHandoffDialog` trusted confirmation surface with filename, source host, verified byte count/SHA-256, exact destination and explicit single-use/exact-scope explanation.
- Added Ready-download panel and `.NET 8` `OpenFolderDialog` destination picker.
- UI prepares exact runtime scope before confirmation and never receives or persists `ApprovalGrant`.
- Representative commits: `06af85bc690e7eecf4f640a7f74f272453a5325b`, `0042bc349ba6f3e582f31b597a0a3424b872ac63`, `d19278ed0e3e4b0d5089b71b61942f0f20a0f5f9`, `3009d0702c5953bb3c3f4800ba627d93ad40c1c2`.

### 2026-09-08 — Low-level export API containment
- Added test-only friend assembly access and internalized `BrowserDownloadQuarantine.ExportAsync(..., bool userApproved)`.
- Added reflection guard preventing accidental public API re-exposure.
- Representative commits: `49b8fea298a772f0345d05c9ef1072aeb32d22de`, `f1f85339558965c57f031368a855237dc6502001`, `277c175c0ebe6ed0c8a85a0aa35d7ddf5c9734eb`.

### 2026-09-08 — Fail-closed browser-download quarantine quota
Completed:
- Added `BrowserDownloadQuarantineOptions` with explicit `MaxRetainedBytes` and `MaxSingleDownloadBytes`; defaults are 512 MiB retained total and 128 MiB per file.
- Added `BrowserDownloadQuotaExceededException` for a typed fail-closed storage refusal.
- Capture now refuses to start another browser save when retained quota is already full.
- After transfer, per-file quota is checked before hashing/promotion. Final Ready promotion is serialized under the quarantine gate and atomically re-checks aggregate retained bytes, preventing concurrent captures from oversubscribing the configured retained quota.
- Quota refusal deletes only the current `.partial`/uncommitted payload and records the current item as `Interrupted`; it never silently deletes or demotes an existing Ready/Exported artifact.
- Added `BrowserDownloadQuotaTests` covering oversized single files, aggregate quota preservation, pre-save rejection at full capacity, and invalid quota configuration.
- Commits: `5baf8569512ea91cbadaaa8d543a18dc18447878`, `eecad36143ca10d4687f9c36f0c5975afaf76c28`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every attempted GitHub mutation, including retry after a stale content SHA conflict.
- Re-read `progress.md`, `BrowserDownloadQuarantine`, its existing tests, recent commits, and the exact-scope handoff service before implementation.
- Source-level review confirms existing constructor call sites remain compatible because the original `(stateDirectory, protector?)` constructor is preserved and delegates to default quota options.
- Re-checked the execution environment for `dotnet`, `msbuild`, `csc`, and `mcs`; none is available, so compilation/test/WPF/Chromium/DPAPI execution is NOT claimed.
- No GitHub Actions workflow was rerun merely to obtain a green signal.

Security / privacy review:
- Quota exhaustion is fail-closed: no user-visible export occurs and no existing trusted Ready artifact is automatically destroyed to make space.
- Concurrent completed downloads cannot both independently pass a stale aggregate quota check because final promotion and aggregate accounting happen while holding the quarantine gate.
- Quota failure metadata uses a generic local-storage explanation and does not persist payload contents.
- A file can temporarily occupy `.partial` space while its final size is unknown; once transfer completes, an oversized current partial is deleted rather than promoted. A streaming hard cap during Playwright transfer remains a possible future hardening step.
- Export authorization, exact destination binding, SHA-256/length verification, no-overwrite semantics, and audit behavior remain unchanged.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The WPF flow depends on .NET 8 `Microsoft.Win32.OpenFolderDialog`; source-level compatibility is expected but must be compiled on Windows before claiming success.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Quota protects retained completed bytes, but an unknown-size in-progress `.partial` can transiently exceed the final per-file limit before Playwright finishes saving it.
- Explicit audited discard controls are still absent, so a user currently cannot intentionally reclaim quarantine capacity through a trusted NVIDEA deletion flow.
- Audit lifetime retention/byte quotas remain absent.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and immediately repair any compile/runtime issues. If executable validation remains unavailable, add explicit exact-scope/audited discard controls for quarantined downloads (with a trusted WPF confirmation surface) so users can reclaim quota intentionally without silent retention pruning or model-controlled deletion.
