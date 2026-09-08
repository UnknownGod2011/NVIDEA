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

## Current Architecture / Product State
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
- Browser-download metadata mutation and passive snapshot reads share one same-path in-process synchronization gate.
- WPF surfaces an explicit **Download recovery needed** state for passive `Receiving` records. Recovery occurs only after deliberate user action and remains emergency-stop cancellable.
- Production browser acquisition through `NvideaCompositionRoot` now takes a process-local + OS-backed durable-state lease before Playwright/profile/download/audit initialization. A second NVIDEA process targeting the same browser state fails closed rather than concurrently mutating it.
- Root README + MIT license.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, deterministic recovery, typed postconditions, verification migration, live Nebius strict-schema probe, DPAPI-backed state protection, protected hash-chained audit and segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent browser and safe downloads
Added owned persistent Chromium profile/session state, popup/new-tab tracking, durable download quarantine, verified payload identity, exact-scope export/discard, single-use grants, WPF confirmation, crash-recoverable discard, retained-byte quotas, bounded in-progress staging, pre-launch stale-staging reclamation and deterministic opt-in Chromium cancellation fixture.

Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `5baf8569512ea91cbadaaa8d543a18dc18447878`, `2a0bd1d556d26329b46b6043c31ee90ddc4111e2`, `96adbcf0b8d00257207d282a01aef9cf78f64e94`, `5ed0e2753ad9d46610200303fc7b3418158d0d52`, `994234746644826d2f98a9ca3cd1974f85b1cce1`.

### 2026-09-08 — Bounded audit + browser-free telemetry
Added crash-safe archived audit retention, protected pruning tombstones/digests, per-event/active-segment ceilings, production bounded-audit composition, privacy-safe retention telemetry, browser-free read-only audit status/download snapshots, bounded metadata reads, trusted snapshot-to-action revalidation, and same-path audit/download synchronization.

Representative commits: `467f6f1a4846fc1f88f76e59ee29111e5e070bb4`, `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `58ad476cba7059220f9fd0129a086a2a661deb7c`, `6d547d218e03da473e552d69cfb02765a91407f2`, `d55c87a4333bfa1a48aa2c1fe87398371d4b180f`, `e09cdfe6c90db5e330abbc9b020a92ae642a72d7`, `44228a3ca42d3d06d9c0184a47a0d07efcdb56d9`, `537c3df7fdf10d46ccb7dde7b3bbed0615f4f32a`, `148962033b88a4b54b58346e7c9ee0b89ebfafbf`, `5a4fe4ab5660679bf24bad889cdfcd83473fe693`.

### 2026-09-08 — Explicit trusted download recovery UX
Added deliberate **Recover safely** UX, trusted quarantine reconciliation only after user action, sanitized pending-recovery telemetry, and emergency-stop cancellation for browser startup/recovery.

Representative commits: `ac374dee484eb51b8c92e96b48b5b6d58c0200a9`, `a3617ce95cb3a7e41f593dba5888cf8bb63d51d7`, `1d8a164add9606af5ae5640eaad36ffbdd2499db`.

### 2026-09-08 — Single-owner browser durable-state lease
Completed this run:
- Re-read `progress.md`, browser profile ownership, `BrowserHostRuntime`, `NvideaCompositionRoot`, and existing test patterns before implementation.
- Added `StateDirectoryLease`, which combines a process-local path ownership guard with an OS-backed `FileStream.Lock(0, 1)` on `.nvidea-state.lock`.
- The lease file intentionally remains after shutdown/crash, but ownership is the live kernel lock rather than file existence; stale lock files therefore do not permanently brick startup.
- Lease metadata is bounded and contains only format version, random instance id, local process id and acquisition timestamp; no prompts, URLs, filenames, credentials or browser state are written.
- Existing lease files that are reparse points are rejected rather than followed.
- `NvideaCompositionRoot.GetBrowserAsync()` acquires the lease before Playwright/profile/download/audit initialization and retains it for the complete production browser-runtime lifetime.
- Browser startup failure/cancellation releases the lease immediately; normal disposal closes the browser before releasing state ownership.
- Added regression tests for same-state exclusivity, release/reacquisition, stale-file recovery and different-state isolation.
- During review, added the process-local guard because Unix byte-range locking behavior can differ for multiple handles in one process; the OS lock remains the cross-process authority.
- Current Microsoft documentation was checked for `FileStream.Lock`; .NET uses region locking on Windows and Unix, with Unix lock behavior depending on stream access mode.
- Files changed: `src/Nvidea.Core/Desktop/StateDirectoryLease.cs`, `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`, `tests/Nvidea.Core.Tests/StateDirectoryLeaseTests.cs`, and this progress file.
- Commits before this progress update: `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `e8abde4be63d434079f1fcc427182f61418fef46`, `00549921b78e0aff0862d0ddfbebd87df0a3ca6c`, `97cb35651728f91cb7be6d9fda47e80e259bd92e`.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation.
- Static review confirms the production composition root acquires state ownership before `BrowserHostRuntime.CreateAsync` and releases it on failure or disposal.
- Tests were added but not executed in this environment.
- `dotnet`, `msbuild`, `csc` and `mcs` remain unavailable here, so compilation/unit tests/WPF/Chromium/DPAPI execution cannot truthfully be reported as successful.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No other repository was mutated.

Security / privacy review:
- A second production NVIDEA composition root cannot intentionally start a second browser mutator over the same durable browser directory while the first lease is alive.
- The lease does not bypass Chromium's own profile safeguards; it adds an earlier NVIDEA ownership boundary.
- Read-only `LocalStateRuntime` remains browser-free and does not acquire mutation authority.
- No lock-file deletion is required for recovery, eliminating stale-file orphaning as a permanent-denial mechanism.
- Lease metadata is intentionally low-sensitivity and bounded to 4 KiB.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The cross-process lease is statically reviewed but has not yet been validated by two real independent Windows processes.
- `BrowserHostRuntime.CreateAsync()` is public and can still be called directly by code that bypasses `NvideaCompositionRoot`; the production Windows composition path is protected, but the stronger end state is to make lease ownership intrinsic to every mutating browser-host construction path.
- New recovery UX is statically reviewed but unexecuted; WPF binding/event behavior still needs real Windows evidence.
- Passive snapshots intentionally verify retained payload length, not SHA-256, on every four-second poll. Trusted export/discard performs full hash verification before consequential mutation.
- Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, move the state lease into the lowest mutating browser-host composition boundary so direct `BrowserHostRuntime.CreateAsync()` callers cannot bypass it, and add a true two-process lease integration fixture proving contention, crash release and stale-file reacquisition.
