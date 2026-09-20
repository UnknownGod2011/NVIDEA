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
Added `ResearchProductRuntime.ReadCompletedReceiptAsync`, a narrow read path that re-reads the authoritative durable job and returns only the completed receipt without requiring the local Tavily executor. WPF Judge Evidence reads the durable store at dialog-open time and fails closed for incomplete, legacy-unproven, corrupt or concurrently changing research. The UI shows structural Nemotron-plan/Tavily-evidence/cited-synthesis/restart-lineage state only; payloads and commitment hashes are not rendered.

### 2026-09-20 — Core-owned closed judge presentation
- Added `DesktopResearchLineagePresentation` and projector in Core so WPF no longer decides whether a durable receipt deserves green/verified copy.
- Presentation is closed to fixed product text plus structural counts. It never carries receipt commitments, question/query/URL/title/snippet/source/answer/provider payload fields.
- The projector fails closed for null/incomplete receipts, missing citation verification, non-positive structural counts, malformed SHA-256 commitment shape, restart instability, and inconsistent multi-query metadata.
- WPF `BuildDurableResearchSummary` now only joins Core-owned rendered fields; verification semantics are no longer duplicated in the UI layer.
- Added deterministic Core tests for verified presentation, null/incomplete/legacy cases, malformed commitment shapes, private-marker non-disclosure and absence of URL-like content.

Files changed in latest run:
- `src/Nvidea.Core/Desktop/DesktopResearchLineagePresentation.cs`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs`
- `tests/Nvidea.Core.Tests/DesktopResearchLineagePresentationTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, inspected the current durable receipt projection, judge dialog, existing research evidence tests, and recent commits before implementation.
- Core presentation has no research-payload inputs other than the already payload-free `DesktopDurableResearchReceipt`; rendered fields exclude all three commitments.
- Defensive validation prevents arbitrary malformed or internally inconsistent receipt projections from becoming green. Cryptographic lineage validation remains authoritative upstream in `ResearchJobHandler.ReadCompletedReceipt`; presentation does not claim to authenticate a syntactically valid but fabricated hash.
- Tests deliberately place a private marker in a malformed commitment and require it never to appear in any rendered field.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Durable research receipt fingerprints are one-way commitments, not authentication signatures; integrity inherits the protected durable store and must not be described as third-party attestation.
- Judge rendering is now a closed Core projection. Malformed, incomplete, legacy-unproven or structurally inconsistent input yields explicit NOT VERIFIED copy rather than optimistic evidence.
- Syntactically valid commitment substitution is detected at the authoritative durable-job lineage boundary, not by the presentation layer; this separation is intentional.
- Browser transport, credential authority rejection, consequential-action approvals, sensitive typing blocks, prompt-injection boundaries and emergency cancellation remain intact.
- Memory recovery remains explicit, bounded, protection-context validated, fail closed and non-mutating during eligibility checks.

## Known blockers / risks
- Latest Core/WPF changes require compile/runtime execution under .NET 8/Windows; all accumulated Windows suites remain pending executable-environment validation.
- Real-Chromium fixtures and release/judge qualification scripts still need execution on Windows with .NET 8, PowerShell 7 and matching Playwright Chromium.
- Receipt commitments prove persisted-stage consistency, not truth of web sources and not cryptographic third-party attestation.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Move the same closed-evidence pattern onto the browser demo path: create a Core-owned, payload-free browser verification presentation/receipt that binds requested action -> observed post-state -> permission/consequential-action decision -> restart/audit evidence, with adversarial tests proving unverified, prompt-injected, cancelled or approval-bypassed runs can never render as judge-verified.
