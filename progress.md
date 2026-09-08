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
- **New:** `BoundedSegmentedAuditTrail` adds conservative logical payload limits (64 KiB/event, 4 MiB/current active segment by default) before any audit append side effect. It preserves the underlying segmented trail's protected chain/rotation/retention behavior and has regression coverage for oversized events, active-segment exhaustion, correct full-segment rollover accounting and invalid policy configuration.
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
Completed:
- Added `AuditPayloadPolicy` with conservative defaults: 64 KiB maximum serialized logical payload per event and 4 MiB maximum logical payload in the mutable active segment.
- Added `BoundedSegmentedAuditTrail`, a real `IAuditTrail` facade over `SegmentedAuditTrail`. It measures UTF-8 JSON before protected persistence and rejects oversized events before any audit side effect.
- Active-segment accounting remains correct after retention because pruning removes complete full segments; retained-count modulo the configured segment size identifies the current active suffix. A full active segment is treated as rotating before the next event's byte budget is evaluated.
- Added regression tests proving single-event rejection leaves no audit data file, active-segment exhaustion leaves the prior record intact, a full segment rotates and accepts the next bounded event, and invalid payload policy fails during construction.
- Commits: `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `933a0fa5455b47b7da14ae2fe375c9ecd9c8b243`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Re-read `progress.md`, current repository tree, `AuditTrail.cs`, `SegmentedAuditTrail.cs`, existing segmented-audit retention tests and `BrowserHostRuntime` audit composition before implementing the new guard.
- Static review confirms the facade does not alter audit cryptography, retention manifests, deletion authority or existing on-disk format; it gates only pre-append logical payload size.
- No GitHub Actions workflow was rerun merely to obtain a green signal.
- This automation environment still does not provide a usable .NET build/test execution path, so compilation/unit-test/Windows DPAPI/WPF/Chromium execution is NOT claimed.

Security / privacy review:
- Oversized metadata/summary content is rejected before it can become a protected/base64 audit record, preventing attacker-controlled logical payload growth from bypassing archived retention simply by staying in the active segment.
- The guard serializes only in memory for byte measurement; it does not create a second plaintext audit log or persist diagnostic payload content.
- Failure is fail-closed: the oversized event is not partially appended and prior audit records remain intact.
- Rotation semantics are preserved rather than silently dropping the event that would start a new segment.
- Archived physical byte quotas, protected retention tombstones and crash-safe deletion remain owned by the existing `SegmentedAuditTrail`.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The new bounded-audit facade/tests are statically reviewed but unexecuted; constructor overload resolution and test compilation require a real .NET 8 signal.
- **Production composition still instantiates `SegmentedAuditTrail` directly in `BrowserHostRuntime`; `BoundedSegmentedAuditTrail` must replace that construction after/with compile validation so the new limits become the default browser-runtime boundary.** Until then this run provides the tested implementation layer but does not claim the production runtime is already protected by it.
- Audit protected-manifest bytes themselves are not quota-bounded, though their shape is small and bounded primarily by retained anchors/pending deletes.
- Retention duplicate-event detection covers the retained window only; intentionally pruned random GUID event IDs are no longer available for historical duplicate checks.
- The Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.
- WPF download polling can initialize the browser runtime at window render time even when browser work was not requested.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, wire `BoundedSegmentedAuditTrail` into `BrowserHostRuntime` (and other production composition roots using protected audit), add a retention-status API exposing pruned-through index/event count/digest plus current retained/active usage without leaking audit payloads, and then surface that status in the trusted Windows UI.
