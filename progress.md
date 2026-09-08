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
- Nemotron through Nebius as the primary reasoning runtime with structured tools, routing, retries, cancellation and bounded execution.
- Layered privacy-aware memory, Tavily-backed research, safe browser automation, capability registry, single-use exact approvals, durable jobs and Nebius background execution.
- Private OS actions remain local; high-sensitivity Windows durable state uses CurrentUser DPAPI where implemented.

## Current State
- .NET 8 core at `src/Nvidea.Core`; WPF host at `src/Nvidea.Windows`.
- Nebius/Nemotron inference abstraction with structured output/tools, retries, timeout/cancellation and conservative routing.
- Layered personal memory, Tavily research engine, Playwright browser agent, deterministic verification, prompt-injection/safety boundaries, resumable browser-goal sessions and durable jobs.
- Capability registry, least-privilege permission policy, exact single-use approval authorizer, protected segmented audit trail and emergency-stop plumbing.
- Production browser runtime uses an NVIDEA-owned persistent Chromium profile with popup/new-tab tracking and durable download quarantine.
- Browser downloads use Receiving -> Ready/Interrupted with verified length/SHA-256 and DPAPI-protected metadata on Windows.
- Exact-scope download handoff/discard require short-lived single-use approvals; WPF never receives `ApprovalGrant`.
- Retained download quarantine defaults to 512 MiB total / 128 MiB per file. In-progress Playwright staging and quarantine `.partial` copies are bounded; oversized transfers are cancelled.
- Crash-leftover browser staging cleanup is bounded, top-level only and fail-closed around unexpected directories/reparse points.
- Deterministic opt-in real-Chromium coverage exists for oversized download cancellation/cleanup using a throttled localhost fixture.
- Protected segmented audit retention defaults to 32 archived segments / 64 MiB archived segment+seal bytes with crash-safe protected prune tombstones and exact pending-delete recovery.
- `BoundedSegmentedAuditTrail` enforces 64 KiB/event and 4 MiB/current-active-segment logical payload ceilings before append side effects, and production browser composition uses it.
- Privacy-safe audit retention status exposes retained counts/bytes, quotas and protected pruning evidence without audit payloads.
- `LocalStateRuntime` exposes browser-free read-only audit/download telemetry without browser actions, approval grants, audit append, export/discard, repair or delete methods.
- WPF `Audit status` and passive browser-download polling use `NvideaCompositionRoot.LocalState`, so simply rendering local state does not initialize Playwright/Chromium.
- Passive download snapshots omit full URLs/query strings, exported paths, failure strings, payload bytes and approval state; stable entries are fail-closed on malformed metadata/missing or wrong-length payloads.
- Browser-download metadata mutation and passive snapshot reads now share one same-path in-process synchronization gate, removing the previous instance-lock race between `BrowserDownloadQuarantine` and `BrowserDownloadSnapshotReader`.
- Root README + MIT license.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, deterministic recovery, typed postconditions, verification migration, live Nebius strict-schema probe, DPAPI-backed state protection, protected hash-chained audit and segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent browser and safe downloads
Added owned persistent Chromium profile/session state, popup/new-tab tracking, durable download quarantine, verified payload identity, exact-scope export/discard, single-use grants, WPF confirmation, crash-recoverable discard, retained-byte quotas, bounded in-progress staging, pre-launch stale-staging reclamation and deterministic opt-in Chromium cancellation fixture.

Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `5baf8569512ea91cbadaaa8d543a18dc18447878`, `2a0bd1d556d26329b46b6043c31ee90ddc4111e2`, `96adbcf0b8d00257207d282a01aef9cf78f64e94`, `5ed0e2753ad9d46610200303fc7b3418158d0d52`, `994234746644826d2f98a9ca3cd1974f85b1cce1`.

### 2026-09-08 — Bounded protected audit and browser-free local telemetry
Added crash-safe archived retention, protected pruning tombstones/digests, per-event/active-segment audit ceilings, production bounded-audit composition, privacy-safe retention telemetry, browser-free read-only audit status, browser-free passive download snapshots, bounded snapshot metadata reads, and trusted snapshot-to-action revalidation.

Representative commits: `467f6f1a4846fc1f88f76e59ee29111e5e070bb4`, `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `58ad476cba7059220f9fd0129a086a2a661deb7c`, `6d547d218e03da473e552d69cfb02765a91407f2`, `d55c87a4333bfa1a48aa2c1fe87398371d4b180f`, `00b11aebf3e099ab5c66fd6e4f5f99002c332c89`, `e09cdfe6c90db5e330abbc9b020a92ae642a72d7`, `44228a3ca42d3d06d9c0184a47a0d07efcdb56d9`, `537c3df7fdf10d46ccb7dde7b3bbed0615f4f32a`.

### 2026-09-08 — Unified download-state synchronization
Completed this run:
- Re-read `progress.md`, `BrowserDownloadQuarantine`, `BrowserDownloadSnapshotReader`, and existing snapshot tests before changing behavior.
- Replaced `BrowserDownloadQuarantine`'s instance-local semaphore with `BrowserDownloadStateSynchronization.GetGate(_metadataPath)`, the same same-path registry already used by `BrowserDownloadSnapshotReader`.
- Added an internal `SynchronizationGate` test seam only; no new public browser, mutation, approval, export, discard, delete, recovery or repair authority was introduced.
- Same-process passive snapshots can no longer read protected download metadata while a quarantine metadata transition owns the same-path gate. Different state roots still receive different gates.
- Added `BrowserDownloadSynchronizationTests.cs` proving exact gate identity, isolation between different roots, deterministic passive-reader blocking while the quarantine gate is held, and stable Ready identity after capture.
- Commits: `148962033b88a4b54b58346e7c9ee0b89ebfafbf`, `5a4fe4ab5660679bf24bad889cdfcd83473fe693`.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation.
- Reviewed the actual synchronization commit diff after mutation; intended semantic change is gate ownership plus the internal test seam. No authorization policy or public authority was widened.
- `dotnet`, `msbuild`, `csc` and `mcs` are not present in this execution environment, so the new tests cannot truthfully be reported as executed here.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy review:
- Passive snapshots remain read-only and cannot perform Receiving recovery, delete `.partial` bytes, export/discard payloads, issue approvals or append audit events.
- Export/discard still require the higher-authority browser runtime, exact trusted identity revalidation and single-use approval.
- The shared gate narrows same-process TOCTOU exposure around metadata/payload state transitions without granting the passive reader mutation capability.
- Full URL paths/query strings and exported destinations remain absent from passive snapshot output.
- No other repository was mutated.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- New synchronization tests are statically reviewed but unexecuted; .NET 8/xUnit behavior still needs real evidence.
- Cross-process synchronization is still absent for both audit and download state; a second NVIDEA process can access the same state directory concurrently.
- Passive snapshots intentionally verify retained payload length, not SHA-256, on every four-second poll. Trusted export/discard performs full hash verification before consequential mutation.
- Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, add a lightweight explicit **Recovery needed** UX for passive `Receiving` records: show only a sanitized count/status in `LocalStateRuntime`, require a deliberate user action to initialize the trusted browser runtime, and keep recovery/deletion authority out of passive polling. Then design a safe cross-process ownership/locking strategy for browser-download and audit durable state before allowing multiple NVIDEA processes to share one profile/state directory.
