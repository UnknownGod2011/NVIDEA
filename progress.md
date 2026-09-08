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
- Exact-scope download handoff and discard services require short-lived single-use approvals; WPF displays trusted decision data but never receives `ApprovalGrant`.
- Retained download quarantine defaults to 512 MiB total / 128 MiB per file. In-progress Playwright staging and quarantine `.partial` copies are guarded and oversized transfers are cancelled.
- Crash-leftover browser staging cleanup is bounded, top-level only and fail-closed around unexpected directories/reparse points.
- Deterministic opt-in real-Chromium coverage exists for oversized download cancellation/cleanup using a throttled localhost fixture.
- Protected segmented audit retention defaults to 32 archived segments / 64 MiB archived segment+seal bytes with crash-safe protected prune tombstones and exact pending-delete recovery.
- `BoundedSegmentedAuditTrail` enforces conservative logical payload limits (64 KiB/event and 4 MiB/current active segment by default) before any audit append side effect.
- Production browser composition uses `BoundedSegmentedAuditTrail`, so browser actions, download handoff/discard, capability execution and resumable-job auditing share one bounded protected audit boundary.
- Privacy-safe audit retention status exposes retained counts/bytes, quotas and protected pruning evidence without audit payloads.
- `LocalStateRuntime` now exposes that status without Playwright/Chromium, browser actions, approval grants, audit append, download export/discard, repair or delete methods.
- WPF `Audit status` now uses `NvideaCompositionRoot.LocalState` instead of `GetBrowserAsync`, so opening the trusted local status view no longer launches Chromium.
- Same-path `BoundedSegmentedAuditTrail` instances use one process-wide synchronization gate, and `LocalStateRuntime` uses that same gate for status reads so browser audit rotation/pruning cannot race the local read in-process.
- Root README + MIT license.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, deterministic recovery, typed postconditions, verification migration, live Nebius strict-schema probe, DPAPI-backed state protection, protected hash-chained audit and segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent browser and safe downloads
Added owned persistent Chromium profile/session state, popup/new-tab tracking, durable download quarantine, verified payload identity, exact-scope export/discard, single-use grants, WPF confirmation, crash-recoverable discard, retained-byte quotas, bounded in-progress staging, pre-launch stale-staging reclamation and deterministic opt-in Chromium cancellation fixture.

Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `5baf8569512ea91cbadaaa8d543a18dc18447878`, `2a0bd1d556d26329b46b6043c31ee90ddc4111e2`, `96adbcf0b8d00257207d282a01aef9cf78f64e94`, `5ed0e2753ad9d46610200303fc7b3418158d0d52`, `994234746644826d2f98a9ca3cd1974f85b1cce1`.

### 2026-09-08 — Crash-safe bounded audit retention
- Added `AuditRetentionPolicy` with default 32 archived segments / 64 MiB archived bytes.
- Rotation reserves exact current segment+seal bytes before archiving; impossible retention fails closed before the new event is recorded.
- Protected manifest stores the pruned-through boundary, cumulative retired-event count, SHA-256 chain over removed anchors and exact pending-delete indices.
- Manifest-first/delete-second ordering makes pruning crash-recoverable.
- Added retention, byte-quota and crash-recovery regression tests.
- Commits: `467f6f1a4846fc1f88f76e59ee29111e5e070bb4`, `b74c884107056b175d9afbf773dc0bc5d7dc9125`.

### 2026-09-08 — Bounded active audit payloads
- Added `AuditPayloadPolicy`: 64 KiB maximum logical payload per event and 4 MiB maximum mutable active-segment logical payload by default.
- Added `BoundedSegmentedAuditTrail`; oversized audit records fail before any persistence side effect.
- Production browser composition uses the bounded trail.
- Added tests for event rejection, active-segment exhaustion, rollover and invalid configuration.
- Commits: `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `933a0fa5455b47b7da14ae2fe375c9ecd9c8b243`, `58ad476cba7059220f9fd0129a086a2a661deb7c`.

### 2026-09-08 — Privacy-safe audit retention status
- Added `AuditRetentionStatus` and internal protected-manifest reader.
- Bounded audit status reads serialize with audit writes and, on the full audit-runtime path, first run normal protected verification/recovery.
- Added status regression tests proving sensitive summaries/metadata do not appear in serialized status and protected pruning evidence is surfaced.
- Added `BrowserHostRuntime.GetAuditRetentionStatusAsync` and trusted WPF Audit status UI.
- Commits: `6d547d218e03da473e552d69cfb02765a91407f2`, `99f04fe3bb7e6b2a42ec1e3fe89ce656206cedac`, `668d7b97f3a8c0ce0c195a08e628e0077b75fadf`, `6569c4dd4876a2d8969b3aee2e33c480e76abb62`, `366d8e317497a6800879d02c2379c0f078361a7f`, `3007792b6eab6402320d245d387e05b0456c658a`.

### 2026-09-08 — Browser-free read-only local-state status
Completed:
- Added `src/Nvidea.Core/Desktop/LocalStateRuntime.cs` as a deliberately narrow trusted local-state surface.
- Its public API contains only `GetAuditRetentionStatusAsync(CancellationToken) -> Task<AuditRetentionStatus>`; it does not publicly expose browser actions, `BrowserHostRuntime`, approval grants/authorizers, `IAuditTrail`, append, export, discard or delete operations.
- The first implementation reused `BoundedSegmentedAuditTrail`; review caught that its status method can complete already-authorized audit retention recovery, so that design was superseded in the same run.
- Final `LocalStateRuntime` directly owns only `AuditRetentionStatusReader` plus a synchronization semaphore. It performs the status snapshot through read operations only and therefore does not trigger audit append/rotation/recovery/deletion merely because the user inspected status.
- `NvideaCompositionRoot` now creates/exposes `LocalState` eagerly from the browser state directory while keeping Playwright browser creation lazy.
- `src/Nvidea.Windows/MainWindow.Audit.cs` now calls `_root.LocalState.GetAuditRetentionStatusAsync(...)` and never calls `_root.GetBrowserAsync(...)` for audit status.
- WPF copy explicitly describes this as a read-only retention snapshot and no longer claims the browser-free path performed audit repair/full runtime recovery.
- `BoundedSegmentedAuditTrail` now derives its gate from a same-path process-wide registry. `LocalStateRuntime` obtains that exact gate, preventing a read snapshot from racing an in-process browser audit append/rotation/prune while still exposing no mutation method.
- Added `tests/Nvidea.Core.Tests/LocalStateRuntimeTests.cs` covering the narrow public API surface, functional payload-free status reading with an existing protected audit, and shared-gate identity between local status and browser audit composition.
- Commits: `914d2e8dfbb62342ad6502431812b4797994352b`, `e7918276691484832016155949baeb204d4ed16c`, `39b734ce7dd7ec3e3853b60d6a04aab3e6695c91`, `783dcc7279ca448bfb16bb6ddac5325c8b0532ab`, `94839b601148a827995b5bb4cf6d11cec05b7a56`, `d55c87a4333bfa1a48aa2c1fe87398371d4b180f`, `8f07aaca46b805d4020e569e6373376c18e06c3d`, `6c4c3907a8f4fecf95aec752274473fbeb50b756`, `00b11aebf3e099ab5c66fd6e4f5f99002c332c89`, `80994a4a6732e0c7d28efe13fc15770b1bdebd2d`, `32060ae0cd3984668f03a6731d436d7e9fa1a19c`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation.
- Re-read `progress.md`, current repository history/tree, `BrowserHostRuntime`, `NvideaCompositionRoot`, `BoundedSegmentedAuditTrail`, `AuditRetentionStatusReader`, `MainWindow.Audit.cs` and existing audit tests before/after changes.
- Rechecked this execution environment for `dotnet`, `msbuild`, `csc` and `mcs`; none is available.
- New tests were written but cannot be truthfully reported as executed here.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy review:
- Inspecting audit retention status no longer initializes Playwright or Chromium.
- The final local-state service has no public browser action, approval-grant, audit append, export, discard, delete or repair capability.
- The service holds only the payload-free status reader and shared semaphore; the semaphore serializes access but cannot mutate audit state.
- Local status still omits prompts, URLs, filenames, summaries, tool arguments and audit metadata.
- Browser execution, exact approvals, protected audit writes, retention recovery and download mutation remain behind their existing higher-authority runtimes.
- No other repository was mutated.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- `LocalStateRuntime`, its internal-constructor test path, the process-wide gate sharing and updated WPF handler are statically reviewed but unexecuted; .NET 8 overload resolution and WPF event behavior still need real evidence.
- The browser-free `AuditRetentionStatusReader` validates protected manifest shape and referenced-file accounting, but unlike `BoundedSegmentedAuditTrail.GetRetentionStatusAsync` it intentionally does not execute the full segmented audit recovery path; this is the tradeoff required to keep the local surface strictly non-mutating. UI wording now calls it a read-only snapshot rather than full audit-runtime verification.
- Process synchronization is in-process only; a second NVIDEA process could still read/write the same audit directory concurrently because there is no cross-process file lock yet.
- Audit protected-manifest bytes themselves are not separately quota-bounded, though their shape is small and bounded primarily by retained anchors/pending deletes.
- Retention duplicate-event detection covers the retained window only; intentionally pruned random GUID event IDs are no longer available for historical duplicate checks.
- Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.
- Existing WPF download polling can still initialize the browser runtime at window render time even when browser work was not requested.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, decouple passive browser-download discovery from Playwright startup **without** giving the passive local-state surface delete/recovery authority: add a strictly read-only quarantine snapshot reader that reports sanitized retained-download metadata/quota usage, switch WPF polling to it, keep actual Receiving->Interrupted crash recovery and discard/export behind `BrowserHostRuntime`, and add authority-surface plus race/tamper regression tests.
