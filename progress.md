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
- `StateDirectoryLease` combines process-local ownership with OS-backed `FileStream.Lock(0, 1)` on `.nvidea-state.lock`; stale lock files are not ownership.
- Browser-state ownership is now acquired by `BrowserHostRuntime.CreateAsync` **before `Playwright.CreateAsync`**. A contending NVIDEA instance therefore fails closed before browser transport startup.
- `BrowserHostRuntime` transfers that exact lease once into `PersistentBrowserContextFactory.LaunchOwnedAsync`; the persistent context owns it through Chromium `Close`, while validation/startup failures release it idempotently.
- Direct `PersistentBrowserContextFactory.LaunchAsync` callers still get intrinsic single-owner protection because that wrapper acquires and transfers its own lease.
- `NvideaCompositionRoot` does not double-lease browser state; it relies on the browser-host/context ownership boundary and keeps only its in-process lazy-creation semaphore.
- Cross-process lease regression coverage includes an independent `dotnet test` child process that owns the kernel lock, parent-process contention, simulated process-tree crash, and stale lock-file reacquisition.
- No-browser regression coverage now proves BrowserHost contention does not invoke the Playwright transport factory and transport-creation failure releases the pre-transport lease for immediate reacquisition.
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

### 2026-09-08 — Trusted download recovery UX
Added deliberate **Recover safely** UX, trusted quarantine reconciliation only after user action, sanitized pending-recovery telemetry, and emergency-stop cancellation for browser startup/recovery.

Representative commits: `ac374dee484eb51b8c92e96b48b5b6d58c0200a9`, `a3617ce95cb3a7e41f593dba5888cf8bb63d51d7`, `1d8a164add9606af5ae5640eaad36ffbdd2499db`.

### 2026-09-08 — Single-owner durable browser state
Added `StateDirectoryLease`, combining a process-local path guard with an OS-backed kernel region lock. Added same-state exclusivity/reacquisition tests plus a true independent-child-process fixture covering contention, abrupt process death and stale lock-file reacquisition. Moved lease ownership into the persistent browser boundary and removed redundant composition-root double leasing.

Representative commits: `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `e8abde4be63d434079f1fcc427182f61418fef46`, `00549921b78e0aff0862d0ddfbebd87df0a3ca6c`, `97cb35651728f91cb7be6d9fda47e80e259bd92e`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `2ed36aa814793f33d81cf824befc0b1fc857ddfc`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `d8922aa915d6f63fcd06327c55db7cd0afa0737e`, `9a2afc79fbcc09ad89f47f5e30be187b5b47f51e`, `832accb9e4aedc9d5b5224f3a43f00d1d53fcc95`.

### 2026-09-08 — Pre-Playwright durable-state ownership
Completed this run:
- Re-read `progress.md`, recent commits, `BrowserHostRuntime`, `PersistentBrowserContextFactory`, `StateDirectoryLease`, existing lease tests and current repository tree before implementation.
- Changed `BrowserHostRuntime.CreateAsync` so `StateDirectoryLease.Acquire(fullStateDirectory)` happens before the Playwright transport factory. The public path still uses `Playwright.CreateAsync`, but it now runs only after ownership is secured.
- Added an internal injectable Playwright transport factory seam used only for deterministic ordering/failure tests; production behavior remains the public `Playwright.CreateAsync` path.
- Added `PersistentBrowserContextFactory.LaunchOwnedAsync`, which takes ownership of one already-acquired lease instead of acquiring a second lease. Direct factory callers retain the existing intrinsic `LaunchAsync` wrapper that acquires and transfers a lease itself.
- Added exact requested-vs-owned state-directory validation before profile/quarantine/staging work.
- Hardened all validation/startup failure paths so transferred or wrapper-acquired leases are disposed idempotently, including failures before Chromium context creation.
- Hardened nullable transfer under warnings-as-errors by requiring a non-null lease before ownership is cleared from `BrowserHostRuntime`.
- Added tests proving same-state BrowserHost contention fails before the injected Playwright factory executes, and synthetic Playwright transport failure releases ownership so the same state directory can immediately be reacquired.
- Current implementation/test commits before this progress update: `b840a2220353d035eca1d1c13de6cb15b8875c85`, `79b9338dceb55470f775d5467fd8b5ad2461ea29`, `0a4658deb4d78e0d747663fe9f80b9e0a47dca4a`, `1e7438cb9bdd80b3b5d5bdda10971033af650947`, `bd96428dae7222d702429a42ecaf1e337a88bcfa`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation.
- Commit diffs were re-read after mutation; the BrowserHost change is limited to pre-transport lease acquisition/transfer plus the internal test seam, and the persistent factory change is limited to explicit lease transfer/failure cleanup.
- Static review found and corrected two issues during the run: possible nullable-lease warnings at transfer, and a lease leak if transferred-factory validation threw before entering cleanup.
- `dotnet`, `msbuild`, `csc` and `mcs` are unavailable in this execution environment (`command -v` returned no tool), so compilation/tests/WPF/Chromium/Windows DPAPI execution are not claimed.
- No GitHub Actions workflow was triggered merely to obtain a green signal.
- No other repository was mutated.

Security / privacy review:
- Same-state contention now prevents Playwright transport startup as well as all persistent profile/download/audit mutation.
- Lease transfer carries only the existing low-sensitivity owner metadata; no prompts, URLs, credentials, cookies, browser payloads or approval data are added.
- The change does not widen model/UI authority or alter approval, export/discard, audit, login/CAPTCHA, site safeguards, or browser-action policy.
- Failed transport creation, invalid transfer state, cancellation, context startup failure, normal close and browser crash all have explicit lease-release paths; disposal remains idempotent.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The new pre-transport tests, intrinsic-lease tests and earlier two-process fixture are implemented but unexecuted here; especially verify Playwright 1.62 event binding, Windows `FileStream.Lock`, nested filtered `dotnet test --no-build`, process-tree termination and lock-release timing on a real Windows runner.
- New recovery UX is statically reviewed but unexecuted; WPF binding/event behavior still needs real Windows evidence.
- Passive snapshots intentionally verify retained payload length, not SHA-256, on every four-second poll. Trusted export/discard performs full hash verification before consequential mutation.
- Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer source authority/freshness work and a verified embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, add explicit transferred-lease misuse regression coverage (wrong state directory, cancellation immediately after transfer, and factory validation failure) and then shift back to product score: strengthen Tavily research with current Extract support, richer source-authority/freshness scoring, provenance-preserving synthesis, and resumable research evidence suitable for the <=3 minute demo.
