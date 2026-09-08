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
- A privacy-safe audit retention status API/UI now exposes retained segment counts/bytes, configured quotas, active-segment usage, pruned-through segment/event counts and the protected pruning digest without exposing audit event payloads.
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
- Added regression tests proving single-event rejection leaves no audit data file, active-segment exhaustion leaves the prior record intact, a full segment rotates and accepts the next bounded event, and invalid payload policy fails during construction.
- Production browser composition now uses `BoundedSegmentedAuditTrail` directly.
- Commits: `ab9285e84f13520fbce37ec9b8436371c568bf0e`, `933a0fa5455b47b7da14ae2fe375c9ecd9c8b243`, `58ad476cba7059220f9fd0129a086a2a661deb7c`.

### 2026-09-08 — Privacy-safe audit retention status
Completed:
- Added `AuditRetentionStatus`, a payload-free public contract exposing active segment index/event count/on-disk bytes, retained archived segment/byte usage, configured retention quotas, pruned-through segment index, cumulative pruned-event count and the protected pruning digest.
- Added an internal `AuditRetentionStatusReader` that reads the same protected `audit-segment-manifest-v1` state as the segmented audit trail. On Windows it uses the same CurrentUser DPAPI protection semantics; on non-Windows test paths it preserves the existing protector behavior.
- `BoundedSegmentedAuditTrail.GetRetentionStatusAsync` serializes status reads with bounded audit writes, first runs the underlying trail through its normal verification/recovery/retention path, then returns only accounting/tombstone data.
- `BoundedSegmentedAuditTrail.ReadAllAsync` now also uses the outer gate, avoiding races between payload-budget accounting, retention status reads and direct retained-event reads through the facade.
- Added `AuditRetentionStatusTests`: normal retained usage must not serialize a sensitive audit summary or expose `summary`/`metadata` fields; pruning with a one-segment retention window reports the expected protected tombstone and 64-character SHA-256 digest.
- `BrowserHostRuntime` now retains the single shared bounded audit instance and exposes `GetAuditRetentionStatusAsync`; it does not expose raw `SegmentedAuditTrail` or audit payloads.
- Added a trusted Windows `Audit status` control. It shows local active/archived usage, quotas, explicit intentional-pruning status, pruned-event count and protected pruning digest, and states that prompts, URLs, filenames, tool arguments, summaries and metadata are intentionally omitted.
- Commits: `6d547d218e03da473e552d69cfb02765a91407f2`, `99f04fe3bb7e6b2a42ec1e3fe89ce656206cedac`, `668d7b97f3a8c0ce0c195a08e628e0077b75fadf`, `6569c4dd4876a2d8969b3aee2e33c480e76abb62`, `366d8e317497a6800879d02c2379c0f078361a7f`, `3007792b6eab6402320d245d387e05b0456c658a`.

Validation / evidence:
- Repository identity was explicitly re-verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation in this run.
- Re-read `progress.md`, `SegmentedAuditTrail`, `BoundedSegmentedAuditTrail`, `BrowserHostRuntime`, `JsonLinesAuditTrail`, Windows `MainWindow.xaml`, and current audit tests before/after changes.
- The status path intentionally uses file length and current-segment JSONL line count only after the real segmented trail has validated/recovered its protected state; no audit event payload is returned by the public status contract.
- Tests were added but not executed because this environment has no `dotnet`, `msbuild`, `csc`, or `mcs` executable.
- No GitHub Actions workflow was triggered merely to obtain a green signal.

Security / privacy review:
- Status is read-only and has no deletion, approval, browser-action or audit-append authority.
- It does not expose prompts, browser targets, URLs, filenames, action summaries, tool arguments or event metadata.
- The pruning digest is intentionally visible because it is tamper-evident retention evidence, not user content.
- Status reads force the protected trail through existing anchor verification and pending-retention recovery first; corrupted or ambiguous state fails closed instead of presenting a reassuring but stale status.
- Existing single-use approvals, audit hash chains, retention tombstones, DPAPI handling and exact delete/export scopes are unchanged.

## Current Unverified / Risks
- Highest risk remains executable validation: no real `dotnet build`, `dotnet test`, Windows WPF launch, persistent Chromium launch or DPAPI round-trip has run in this environment.
- The new status reader/API/tests/WPF handler are statically reviewed but unexecuted; .NET 8 overload resolution, XAML event binding and Windows DPAPI behavior still need real executable evidence.
- The trusted `Audit status` button currently obtains `BrowserHostRuntime` through the existing composition root, which can initialize Playwright/Chromium merely to read local audit status. This is functionally correct but wasteful and couples a local privacy/status view to browser availability.
- Audit protected-manifest bytes themselves are not quota-bounded, though their shape is small and bounded primarily by retained anchors/pending deletes.
- Retention duplicate-event detection covers the retained window only; intentionally pruned random GUID event IDs are no longer available for historical duplicate checks.
- The Chromium oversized-download fixture and Windows staging/reparse behavior remain unexecuted here.
- Persistent Chromium profile contents and quarantined payload bytes rely on the OS user-profile boundary rather than application-level encryption.
- Local voice/transcription is absent.
- Tavily Extract/richer authority/freshness work and a verified embedding adapter remain opportunities.
- Existing WPF download polling can initialize the browser runtime at window render time even when browser work was not requested.

## Single Best Next Task
Obtain the first real Windows/.NET 8 build + unit tests + WPF launch + persistent Chromium + DPAPI signal and repair any compile/runtime issues. If executable validation remains unavailable, decouple audit-status reading (and ideally passive download/recovery discovery) from Playwright startup by introducing a lightweight local-state runtime/composition service, so opening trusted local status never launches Chromium; add API-surface/regression coverage proving the read-only status service has no browser-action, approval-grant, audit-append or deletion authority.
