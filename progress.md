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
- Added `ResearchProductRuntime.ReadCompletedReceiptAsync`, a narrow read path that re-reads the authoritative durable job and returns only the completed receipt. It does not require the local Tavily executor, so a receipt remains inspectable after restart/configuration loss.
- The WPF Judge Evidence action now attempts to read the active completed receipt and fails closed: incomplete, legacy-unproven, corrupt, or concurrently changing research renders no receipt rather than green evidence.
- Judge Evidence now renders concise `Nemotron plan -> Tavily evidence -> cited synthesis -> restart lineage` state with structural query/source/citation counts.
- The dialog deliberately does not render receipt hashes or any question, query, URL, title, snippet, source text, answer, provider ID, credential, or raw provider error.
- Existing ephemeral session evidence remains independently resettable; the new durable receipt is not cleared by “New demo session.”

Files changed in latest run:
- `src/Nvidea.Core/Jobs/ResearchProductRuntime.cs`
- `src/Nvidea.Windows/MainWindow.Readiness.cs`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the current WPF Judge Evidence dialog, research UI path, durable receipt projection and product research runtime before implementation.
- Static review confirms the judge path reads the durable store at dialog-open time rather than trusting cached demo state.
- Receipt read delegates to `ResearchJobHandler.ReadCompletedReceipt`, retaining the production completion/lineage validation boundary.
- Receipt acquisition catches failures only at the judge projection boundary and degrades to explicit absence; it cannot turn invalid state into success.
- UI text renders structural counts/verification state only. Hash commitments remain available in the Core projection for programmatic verification but are not shown in WPF.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Durable research receipt fingerprints are one-way commitments, not authentication signatures; integrity inherits the protected durable store and must not be described as third-party attestation.
- The judge UI receives a payload-free receipt projection and renders only counts/boolean integrity state. Research payloads never cross this new WPF rendering path.
- Incomplete/corrupt/legacy-unproven receipts fail closed to “No completed durable research receipt” instead of optimistic evidence.
- Browser transport, credential authority rejection, consequential-action approvals, sensitive typing blocks, prompt-injection boundaries and emergency cancellation remain intact.
- Memory recovery remains explicit, bounded, protection-context validated, fail closed and non-mutating during eligibility checks.

## Known blockers / risks
- Latest Core/WPF changes require compile/runtime execution under .NET 8/Windows; all accumulated Windows suites remain pending executable-environment validation.
- No dedicated WPF unit test currently pins `BuildDurableResearchSummary`; static review is the strongest available evidence in this connector environment.
- Receipt commitments prove persisted-stage consistency, not truth of web sources and not cryptographic third-party attestation.
- Real-Chromium fixtures and release/judge qualification scripts still need execution on Windows with .NET 8, PowerShell 7 and matching Playwright Chromium.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add a Core-owned judge-safe research-lineage presentation model (rather than formatting in WPF) with deterministic tests proving incomplete/legacy/tampered receipts never become green and private marker strings cannot enter any rendered evidence field; then have WPF bind only that closed projection.
