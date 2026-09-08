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
- Production browser composition uses `BoundedSegmentedAuditTrail` for browser actions, download handoff/discard, capability execution and resumable-job auditing.
- Privacy-safe audit retention status exposes retained counts/bytes, quotas and protected pruning evidence without audit payloads.
- `LocalStateRuntime` exposes audit retention status without Playwright/Chromium, browser actions, approval grants, audit append, download export/discard, repair or delete methods.
- WPF `Audit status` uses `NvideaCompositionRoot.LocalState` instead of `GetBrowserAsync`.
- Same-path bounded audit instances and local audit status use one process-wide in-process synchronization gate.
- Passive WPF browser-download polling now also uses `LocalStateRuntime` and no longer initializes Chromium merely because the window rendered.
- Root README + MIT license.

## Persistent Progress History

### 2026-09-06 to 2026-09-07 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, browser contracts/execution, capability registry, approval boundary, durable jobs, Nebius Serverless contracts, Playwright execution, Windows shell, deterministic recovery, typed postconditions, verification migration, live Nebius strict-schema probe, DPAPI-backed state protection, protected hash-chained audit and segmented rotation.

### 2026-09-07 to 2026-09-08 — Persistent browser and safe downloads
Added owned persistent Chromium profile/session state, popup/new-tab tracking, durable download quarantine, verified payload identity, exact-scope export/discard, single-use grants, WPF confirmation, crash-recoverable discard, retained-byte quotas, bounded in-progress staging, pre-launch stale-staging reclamation and deterministic opt-in Chromium cancellation fixture.

Representative commits: `86ce7ccfdfd09ad27fdb129c6220fe4deff02633`, `74b1c00ea306a825486a32d55be26dfca8bbf3fd`, `5baf8569512ea91cbadaaa8d543a18dc18447878`, `2a0bd1d556d26329b46b6043c31ee90ddc4111e2`, `96adbcf0b8d00257207d282a01aef9cf78f64e94`, `5ed0e2753ad9d46610200303fc7b3418158d0d52`, `994234746644826d2f98a9ca3cd1974f85b1cce1`.

### 2026-09-08 — Bounded protected audit
Added crash-safe archived retention, protected pruning tombstones/digests, 64 KiB per-event and 4 MiB active logical payload ceilings, production bounded-audit composition, privacy-safe retention telemetry, and browser-free read-only audit status. Representative commits: `467f6f1a4846fc1f88f76e59ee29111e5e070bb4`, `b74c884107056b175d9afbf773dc0bc5d7dc9125`, `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `58ad476cba7059220f9fd0129a086a2a661deb7c`, `6d547d218e03da473e552d69cfb02765a91407f2`, `99f04fe3bb7e6b2a42ec1e3fe89ce656206cedac`, `668d7b97f3a8c0ce0c195a08e628e0077b75fadf`, `6569c4dd4876a2d8969b3aee2e33c480e76abb62`, `366d8e317497a6800879d02c2379c0f078361a7f`, `3007792b6eab6402320d245d387e05b0456c658a`, `d55c87a4333bfa1a48aa2c1fe87398371d4b180f`, `00b11aebf3e099ab5c66fd6e4f5f99002c332c89`, `80994a4a6732e0c7d28efe13fc15770b1bdebd2d`.

### 2026-09-08 — Browser-free passive download snapshots
Completed this run:
- Added `src/Nvidea.Core/Browser/BrowserDownloadSnapshotReader.cs`, a strictly read-only view over protected download metadata and retained quarantine payload accounting.
- Snapshot output intentionally contains only download id, source host, sanitized filename, stable Ready/Exported state, verified metadata length/SHA-256, created time and aggregate quota usage. It does **not** expose full source URLs/query strings, exported paths, failure strings, payload bytes, approval objects or filesystem mutation methods.
- Receiving records are not promoted or repaired by the passive reader. They are counted only as `PendingRecoveryCount`; `.partial` bytes and metadata remain untouched for the existing higher-authority `BrowserHostRuntime` recovery path.
- Stable Ready/Exported entries fail closed if their payload is missing, their on-disk length differs from protected metadata, SHA-256 metadata is malformed, a per-file/aggregate quota is impossible, ids are duplicated, filenames/URIs are invalid, or metadata contains an unknown state.
- Passive metadata reads are bounded to 4 MiB encoded/decoded state and 4096 records to prevent the 4-second WPF poller from becoming an unbounded local allocation/iteration path.
- `LocalStateRuntime` now exposes `GetBrowserDownloadSnapshotAsync(CancellationToken)` in addition to audit status. Its public authority remains read-only telemetry: there is still no browser action, approval, append, export, discard, delete, recover or repair API.
- `src/Nvidea.Windows/MainWindow.Downloads.cs` now polls `_root.LocalState.GetBrowserDownloadSnapshotAsync()` on content render/timer ticks instead of `_root.GetBrowserAsync()`. Passive window rendering therefore no longer launches Chromium just to discover retained downloads.
- Export/discard remain behind `BrowserHostRuntime`. When the user actually requests one, WPF initializes the trusted runtime, reloads/reconciles the selected download by exact id, and compares state, length, SHA-256, filename and source host against the passive snapshot before preparing approval. Any change fails closed and requires a fresh review.
- Added/updated regression coverage for the LocalState authority surface, sanitized snapshot output, retained-payload length tampering, quota telemetry, and proving a Receiving record/partial file is not mutated by passive inspection.
- Commits: `e09cdfe6c90db5e330abbc9b020a92ae642a72d7`, `11a63aef7e0cc0b3c8f853ce5de3c26b28d5110e`, `44228a3ca42d3d06d9c0184a47a0d07efcdb56d9`, `4288b7ebee3a331e036b7b6af7696abcb8778e5d`, `2cbd1a7bc87123ceca1a8634a128e7152b619d59`, `159e68fa309989f215fe4be6bc2a6b8b3cc3d112`, `42b15e34cf5ff29a74ac28994d5f277527254e6e`, `537c3df7fdf10d46ccb7dde7b3bbed0615f4f32a`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation in this run.
- Re-read `progress.md`, `BrowserDownloadQuarantine`, `BrowserHostRuntime`, `NvideaCompositionRoot`, `LocalStateRuntime`, WPF download polling and existing local-state/download tests before changing behavior.
- Re-read the new/updated files after mutation and reviewed the actual commit sequence.
- Rechecked this execution environment for `dotnet`, `msbuild`, `csc` and `mcs`; none is available.
- Tests were added but cannot be truthfully reported as executed here.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy review:
- Merely opening/rendering the WPF window no longer initializes Playwright/Chromium for download discovery.
- Passive snapshots cannot turn Receiving into Interrupted, remove `.partial` bytes, export/discard payloads, grant approvals, or append audit events.
- Full URL paths/query strings and exported destinations are omitted from the passive UI contract.
- Consequential export/discard still requires the higher-authority browser runtime plus exact single-use approval.
- Snapshot-to-action handoff revalidates trusted runtime identity before approval preparation, reducing stale-read/TOCTOU risk.
- No other repository was mutated.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- New snapshot reader, LocalState overloads, WPF handler and tests are statically reviewed but unexecuted; .NET 8 overload/xUnit/WPF behavior still needs real evidence.
- The passive download reader has its own same-path in-process synchronization registry, but `BrowserDownloadQuarantine` still uses its existing instance-local gate. Atomic metadata persistence plus fail-closed payload/metadata validation should turn overlap into a transient unavailable snapshot rather than unsafe promotion, but there is not yet a shared read/write gate or deterministic concurrent-race test.
- Cross-process synchronization is still absent for both audit and download state; a second NVIDEA process can access the same state directory concurrently.
- Passive snapshot checks retained payload length but intentionally does not recompute SHA-256 every four seconds. The trusted export/discard path still performs full payload verification before consequential mutation.
- Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, unify `BrowserDownloadQuarantine` and `BrowserDownloadSnapshotReader` on a same-path synchronization primitive (without widening the public authority surface), then add deterministic concurrent capture/list/export/discard snapshot race tests and a lightweight explicit "recovery needed" UX that invokes the trusted browser runtime only on user request rather than during passive polling.
