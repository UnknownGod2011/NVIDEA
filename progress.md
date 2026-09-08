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
- `BrowserDownloadDiscardService` binds a specific retained payload identity (download id + verified SHA-256) to a separate high-risk `FilesWrite` approval scope. It revalidates immediately before deletion, consumes a single-use grant before the mutation and audits the result.
- `BrowserHostRuntime` exposes trusted prepare/approve boundaries for both export and discard; WPF never receives or persists `ApprovalGrant`.
- Trusted WPF download controls display sanitized filename, source host, verified size/SHA-256 and exact destination/operation before explicit human confirmation.
- Low-level `BrowserDownloadQuarantine.ExportAsync(..., bool)` and `DiscardAsync(..., bool)` storage primitives are assembly-internal; reflection regression tests guard against accidental public exposure.
- Download quarantine has fail-closed byte quotas: default 512 MiB retained total / 128 MiB per file. Quota checks never silently evict Ready or Exported artifacts.
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

### 2026-09-08 — Exact-scope handoff, WPF approval and quota
- Added `BrowserDownloadHandoffService`, exact destination binding, single-use grant consumption, audit, runtime wiring and trusted WPF confirmation.
- Internalized the low-level boolean export primitive and added an API-surface regression guard.
- Added `BrowserDownloadQuarantineOptions` with fail-closed 512 MiB retained / 128 MiB per-file defaults and concurrency-safe final promotion checks.
- Representative commits: `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `bcca57e7c4577ac7bf118329fe3fd9ac4d2e20ab`, `3009d0702c5953bb3c3f4800ba627d93ad40c1c2`, `f1f85339558965c57f031368a855237dc6502001`, `5baf8569512ea91cbadaaa8d543a18dc18447878`, `eecad36143ca10d4687f9c36f0c5975afaf76c28`.

### 2026-09-08 — Exact-scope audited quarantine discard
Completed:
- Added `BrowserDownloadDiscardService` and `BrowserDownloadDiscardPlan`; discard approval scope is bound to the exact download id + current verified SHA-256 identity and uses high-risk `FilesWrite` policy.
- Added a durable `Discarded` state and assembly-internal `BrowserDownloadQuarantine.DiscardAsync(..., bool)` primitive.
- Discard verifies the retained payload length/SHA-256 immediately before mutation.
- Implemented a crash-recoverable two-phase local deletion transition: `.payload -> .discarding`, persist the `Discarded` tombstone, then delete `.discarding`. If metadata persistence fails, the payload is restored. On restart, Ready/Exported metadata restores a pre-tombstone `.discarding` file, while durable Discarded metadata removes leftover payload/discarding files.
- `BrowserHostRuntime` now registers `browser.download.discard`, composes it against the same quarantine/policy/authorizer/segmented audit trail, and exposes `PrepareDownloadDiscardAsync` + `ApproveAndDiscardDownloadAsync`. Grants remain runtime-local, short-lived and single-use.
- WPF now shows Ready and Exported retained quarantine entries, adds a separate `Discard copy` control, and displays sanitized filename, source host, verified size/SHA-256 plus a warning that already-exported user files are untouched.
- Expanded API-surface protection so both low-level export and discard primitives must remain assembly-internal.
- Added discard tests covering approval requirement, scope mutation rejection, quota reclamation, exported-copy preservation, restart recovery before tombstone commit, and cleanup after a durable tombstone.
- Commits: `494fb682f42d445d0ef50aca6f9d07c33ce8ca9c`, `81fef085e8fc0d8986a7b68eb4c0a53b94cda97b`, `fcc554101e4838a6a3142ff48ae5bcda3c090b6b`, `81e1612c4ec33a10a197e648532975adc27d22dd`, `2a0bd1d556d26329b46b6043c31ee90ddc4111e2`, `a4c408196763e9ee33b831da0a1b98f3d29606ef`, `26d8275339db8a5bd18a6fc5eab52d743b682a66`, `64826c0dc6c29f768541024422bf62da287e4791`, `32217fcf12a7273927dcc8bb01bcf4d66231c6d8`, `4a18a2baa7ab562d23ba2364907c1e28402a7e28`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Re-read `progress.md`, current quarantine implementation, exact-scope handoff service, capability policy, runtime composition, WPF download flow and existing tests before changing them.
- Re-read the new discard service and recent commit chain after wiring it into production.
- Source-level review caught and repaired a discard crash-consistency flaw before completion; the final implementation uses reversible `.discarding` state until the durable tombstone succeeds.
- Re-checked the execution environment for `dotnet`, `msbuild`, `csc`, and `mcs`; none is available, so compilation/test/WPF/Chromium/DPAPI execution is NOT claimed.
- No GitHub Actions workflow was rerun merely to obtain a green signal.

Security / privacy review:
- Quota reclamation is never automatic and cannot be initiated by browser/model code through a public low-level API.
- Export and discard are separate capabilities with separate exact scopes and separate fresh confirmations.
- The UI does not receive `ApprovalGrant`; it only echoes the exact prepared scope after the human click.
- Discarded quarantine bytes are removed without deleting any previously exported user file.
- A stale or forged discard plan fails revalidation before a grant can authorize deletion.
- Crash before tombstone durability restores the retained payload; crash after tombstone durability treats deletion as authoritative and finishes cleanup.
- Audit records operation identity/verified hash/size but not payload contents.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The WPF flow depends on .NET 8 `Microsoft.Win32.OpenFolderDialog`; source-level compatibility is expected but must be compiled on Windows before claiming success.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Quota protects retained completed bytes, but an unknown-size in-progress `.partial` can transiently exceed the final per-file limit before Playwright finishes saving it.
- Audit lifetime retention/byte quotas remain absent.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.
- The WPF download polling path can initialize the browser runtime at window render time even when the user has not requested browser work, which is safe but suboptimal for startup latency/resources.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and immediately repair any compile/runtime issues. If executable validation remains unavailable, harden the remaining unbounded in-progress download path so a hostile/accidental large transfer cannot transiently consume arbitrary local disk before final quota rejection, while preserving Playwright cancellation and the existing quarantine verification model.
