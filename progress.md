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
- **Production browser composition now uses `BoundedSegmentedAuditTrail` instead of constructing `SegmentedAuditTrail` directly**, so browser actions, download handoff/discard, capability execution and resumable-job auditing all share the bounded protected audit boundary.
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
- Oldest archived segments are pruned only at immutable segment boundaries.
- Protected manifest stores `PrunedThroughIndex`, cumulative pruned-event count, SHA-256 chain over removed anchors, and exact `PendingDeleteIndices`.
- Manifest-first/delete-second ordering makes retention pruning crash-recoverable and prevents unexplained silent truncation.
- Added regression tests for pruning, byte-quota failure before rotation and simulated post-manifest/pre-delete crash recovery.
- Commits: `467f6f1a4846fc1f88f76e59ee29111e5e070bb4`, `b74c884107056b175d9afbf773dc0bc5d7dc9125`.

### 2026-09-08 — Bounded active audit payloads
- Added `AuditPayloadPolicy` with conservative defaults: 64 KiB maximum serialized logical payload per event and 4 MiB maximum logical payload in the mutable active segment.
- Added `BoundedSegmentedAuditTrail`, a real `IAuditTrail` facade over `SegmentedAuditTrail`. It measures UTF-8 JSON before protected persistence and rejects oversized events before any audit side effect.
- Active-segment accounting remains correct after retention because pruning removes complete full segments; retained-count modulo the configured segment size identifies the current active suffix. A full active segment is treated as rotating before the next event's byte budget is evaluated.
- Added regression tests proving single-event rejection leaves no audit data file, active-segment exhaustion leaves the prior record intact, a full segment rotates and accepts the next bounded event, and invalid payload policy fails during construction.
- Commits: `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `933a0fa5455b47b7da14ae2fe375c9ecd9c8b243`.

### 2026-09-08 — Production bounded-audit composition
Completed:
- Re-read the full progress source of truth, current repository tree, `BoundedSegmentedAuditTrail`, `SegmentedAuditTrail`, and `BrowserHostRuntime` before mutating production composition.
- Replaced the direct `SegmentedAuditTrail` construction in `BrowserHostRuntime.CreateAsync` with `BoundedSegmentedAuditTrail`.
- This is intentionally one shared instance: browser download handoff, browser download discard, capability-tool execution and `ResumableJobOrchestrator` all receive the same bounded audit object, preserving one protected ordering/retention boundary rather than creating per-subsystem logs.
- No behavior or data permissions were removed; this closes the documented production escape hatch and makes the existing payload ceilings effective on the browser-runtime path.
- Commit: `58ad476cba7059220f9fd0129a086a2a661deb7c`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before each GitHub mutation.
- Commit diff was re-read after mutation and confirms exactly one production code change: `new SegmentedAuditTrail(...)` -> `new BoundedSegmentedAuditTrail(...)` in `BrowserHostRuntime`.
- Existing `BoundedSegmentedAuditTrailTests` cover oversized event rejection, active-segment exhaustion, rollover semantics and invalid policy configuration; those tests remain the relevant contract for the newly wired production type.
- No GitHub Actions workflow was rerun merely to obtain a green signal.
- This automation environment still does not provide a usable .NET build/test execution path, so compilation/unit-test/Windows DPAPI/WPF/Chromium execution is NOT claimed.

Security / privacy review:
- Browser-originated or tool-originated audit metadata can no longer bypass the logical payload ceiling through the production browser composition root.
- Existing protected segmented hash chains, retention tombstones, DPAPI behavior, crash recovery and deletion authority are unchanged because `BoundedSegmentedAuditTrail` delegates persistence to `SegmentedAuditTrail` after pre-append validation.
- All browser-runtime audit consumers share the same bounded object, avoiding inconsistent enforcement across approval, download and job paths.
- Rejected oversized audit events fail before persistence; prior audit evidence remains intact.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The bounded-audit implementation/tests and the newly changed production composition are statically reviewed but unexecuted; constructor overload resolution and runtime behavior still require a real .NET 8 signal.
- Audit protected-manifest bytes themselves are not quota-bounded, though their shape is small and bounded primarily by retained anchors/pending deletes.
- Retention duplicate-event detection covers the retained window only; intentionally pruned random GUID event IDs are no longer available for historical duplicate checks.
- Users currently cannot see retained/pruned audit status; deliberate retention pruning is cryptographically represented but not yet surfaced through a privacy-safe status API/UI.
- The Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.
- WPF download polling can initialize the browser runtime at window render time even when browser work was not requested.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, add a privacy-safe audit retention-status API exposing only retained archived/active counts and bytes, configured quotas, pruned-through segment/event counts and the protected pruning digest (never audit payloads), delegate it through `BoundedSegmentedAuditTrail`, expose it from `BrowserHostRuntime`, add regression tests, and surface the status in the trusted Windows UI.
