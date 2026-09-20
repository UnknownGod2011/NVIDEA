# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness, payload-free durable research lineage, and production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17 — product foundation
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-19 — browser safety and qualification
Hardened browser transport to HTTPS or loopback HTTP and WSS or loopback WS; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Added real-Chromium qualification, TRX validation, SHA-256 evidence receipts, independent verification, clean exact-HEAD provenance, release gate and judge-recording gate.

### 2026-09-19 to 2026-09-20 — memory integrity and explicit recovery
Persisted embedding state is treated as untrusted; malformed vector/provenance state is stripped while user-authored memory survives. Migration remains local-provider-only, preserves Sensitive/Restricted opt-ins, revalidates candidates, validates vectors/provenance, skips concurrent edits, and has invalid-vector/failure/cancellation coverage. JSON persistence serializes access, uses same-directory write-through replacement, and maintains one bounded `.bak` last-known-good generation. Recovery is explicit, protection-context validated, fail-closed and never automatic. Startup recovery eligibility uses non-mutating primary validation and cannot be authorized by unrelated startup failure.

### 2026-09-20 — startup/shutdown and partial-construction ownership
WPF cancellation is distinct from corruption/configuration failure. Exit cleanup is contained and idempotent. `StartupResourceLease` provides reverse-order, exactly-once best-effort cleanup until a complete composition root assumes ownership. Deterministic failure coverage exercises failures after memory, Tavily, cloud transfer and pre-release, including cleanup-failure and cancellation semantics.

### 2026-09-20 — Tavily evidence and restart-stable research provenance
Added `DesktopResearchEvidence` as a payload-free evidence boundary over completed reports. Durable research checkpoints carry SHA-256 lineage from the Nemotron plan into Tavily prepared evidence and final synthesis. Completed jobs expose `DurableResearchReceipt`; legacy evidence remains resumable but cannot claim historical plan provenance. Deterministic restart simulation proves synthesis can resume from persisted evidence without another Tavily provider call and tampered evidence is rejected before synthesis.

### 2026-09-20 — judge-visible durable research lineage
Added `ResearchProductRuntime.ReadCompletedReceiptAsync`; WPF Judge Evidence re-reads the authoritative durable store and fails closed for incomplete, legacy-unproven, corrupt or concurrently changing research. `DesktopResearchLineagePresentation` moved verification copy into Core so WPF only renders a closed payload-free projection; malformed or structurally inconsistent receipts cannot become green/verified.

### 2026-09-20 — browser judge-verification boundary
- Added explicit `ApprovalGranted` evidence to `BrowserActionReceipt`; the executor records it only after the approval gate returns true. Existing/legacy receipts default false, so they cannot retroactively claim approval.
- Added Core-owned `DesktopBrowserVerificationPresentation` / projector. Judge/demo surfaces can now consume fixed payload-free browser evidence without receiving URLs, locators, typed values, page text, verification details, errors or rationale.
- Projection fails closed for empty evidence, invalid action IDs/timestamps, disallowed or blocked decisions, driver failure, unverified post-state, and any approval-required action lacking explicit approval evidence.
- Added deterministic tests for verified approved actions, approval bypass, unverified/blocked actions, empty evidence, and private-marker/URL non-disclosure.

Files changed in latest run:
- `src/Nvidea.Core/Browser/BrowserContracts.cs`
- `src/Nvidea.Core/Browser/BrowserAgentExecutor.cs`
- `src/Nvidea.Core/Browser/DesktopBrowserVerificationPresentation.cs`
- `tests/Nvidea.Core.Tests/DesktopBrowserVerificationPresentationTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected browser contracts/executor and existing browser test inventory before implementation.
- Approval evidence is now an explicit receipt fact rather than inferred from `RequiresApproval && success`; legacy/default receipts therefore fail closed for consequential actions.
- Presentation accepts only receipts and emits fixed structural text/counts; no browser payload fields are copied into the presentation.
- Tests deliberately place a private marker in a typed value/rationale and private URL state in the receipt and require neither to render.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- Browser approval is positive evidence only when the approval gate actually returned true. Denial, unavailable approval and legacy receipts remain false.
- Browser judge projection is deliberately payload-free and cannot leak typed secrets, page content, URLs or raw diagnostics through its public fields.
- The projection proves the local guarded execution/verification path represented by the supplied receipts; it is not third-party attestation and does not prove website truth.
- Durable research receipt fingerprints are one-way commitments, not authentication signatures; integrity inherits the protected durable store.
- Browser transport, credential authority rejection, consequential-action approvals, sensitive typing blocks, prompt-injection boundaries and emergency cancellation remain intact.
- Memory recovery remains explicit, bounded, protection-context validated, fail closed and non-mutating during eligibility checks.

## Known blockers / risks
- Latest Core changes require compile/runtime execution under .NET 8/Windows; accumulated Windows suites remain pending executable-environment validation.
- Real-Chromium fixtures and release/judge qualification scripts still need execution on Windows with .NET 8, PowerShell 7 and matching Playwright Chromium.
- The new browser presentation is not yet wired into the WPF Judge Evidence dialog or a durable/restart-stable browser receipt store; current projection is over in-memory action receipts.
- Receipt evidence demonstrates execution-policy consistency, not truth of remote page content or cryptographic third-party attestation.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Wire `DesktopBrowserVerificationPresentation` into the WPF Judge Evidence surface through an authoritative browser-session evidence boundary, then bind the presentation to durable/audit-backed browser receipts so restart-stable requested-action -> approval -> observed-post-state lineage can be demonstrated without exposing browser payloads.
