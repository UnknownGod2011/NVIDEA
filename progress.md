# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness, payload-free durable research lineage, browser-verification state, and production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17 — product foundation
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-19 — browser safety and qualification
Hardened browser transport, redirects/WebSockets, Service Worker blocking, page admission, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart behavior, emergency stop, real-Chromium qualification and SHA-256 release/judge evidence tooling.

### 2026-09-19 to 2026-09-20 — memory/startup integrity
Persisted embedding state is untrusted and malformed vector/provenance data is stripped while user memory survives. Recovery is explicit, bounded and protection-context validated. Startup/shutdown gained cancellation-safe cleanup and `StartupResourceLease` reverse-order exactly-once partial-construction ownership.

### 2026-09-20 — Tavily and research provenance
Added payload-free `DesktopResearchEvidence`, SHA-256 lineage across Nemotron plan -> Tavily prepared evidence -> cited synthesis, restart-stable `DurableResearchReceipt`, no-repeat-Tavily restart simulation, authoritative receipt reading, and Core-owned judge presentation that fails closed for legacy/corrupt/inconsistent evidence.

### 2026-09-20 — browser judge-verification boundary and UI
Added explicit `ApprovalGranted` action evidence and Core-owned `DesktopBrowserVerificationPresentation`. It fails closed for empty/invalid/blocked/failed/unverified actions and consequential actions without explicit approval evidence. WPF Judge Evidence has a dedicated browser panel and cannot receive raw URLs, locators, typed values, page text, verification details, errors or rationale. Missing authoritative evidence remains visibly NOT VERIFIED.

### 2026-09-20 — least-authority durable browser receipt model
- Added `DurableBrowserActionEvidence` and `DurableBrowserVerificationReceipt` as restart-stable, non-authorizing structural evidence.
- Durable evidence contains only action id/kind, risk/policy outcome, whether approval was required and historically observed, driver success, post-state verification and timestamps. It excludes URLs, locators, typed values, page content, approval scopes/tokens, diagnostics, policy rationale and verification detail.
- Receipt SHA-256 commitment binds ordered structural evidence; read-side integrity validation uses fixed-time comparison and fails closed on version mismatch, duplicate/empty action identity, invalid timestamps, inconsistent approval metadata, changed completion time or commitment mismatch.
- `DesktopBrowserVerificationProjector` now shares one validation path for live receipts and durable structural evidence, preventing durable/demo semantics from drifting from the live judge projection.
- Added deterministic tests for verified approved evidence, private marker/URL/detail non-disclosure, structural tampering rejection and impossible approval metadata.

Files changed in latest run:
- `src/Nvidea.Core/Browser/DurableBrowserVerificationReceipt.cs`
- `src/Nvidea.Core/Browser/DesktopBrowserVerificationPresentation.cs`
- `tests/Nvidea.Core.Tests/DurableBrowserVerificationReceiptTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely; inspected recent commits, browser product runtime, durable goal store, browser contracts, browser job checkpoint behavior and current judge projection before implementation.
- Static review confirms the durable receipt type has no fields capable of carrying URL/locator/value/page text/approval scope/raw diagnostic payloads.
- Tamper detection covers all persisted structural fields through a canonical SHA-256 commitment and fixed-time comparison.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- `ApprovalObserved` is explicitly historical evidence only; it is never an approval grant and carries no exact scope/token/reusable authority.
- Browser judge projection and durable receipt remain payload-free. Private marker tests guard against accidental persistence of typed/locator/URL/detail fields.
- Integrity commitment is local tamper evidence, not a signature or third-party attestation; authenticity still inherits the protected durable state boundary that will own the store.
- Missing/corrupt durable evidence must remain NOT VERIFIED and cannot be substituted by provider readiness or session milestones.
- Existing browser transport, credential authority rejection, consequential-action approvals, sensitive typing blocks, prompt-injection boundaries and emergency cancellation remain intact.

## Known blockers / risks
- New Core changes require compile/runtime execution under .NET 8; accumulated Windows/Chromium suites remain pending executable-environment validation.
- The least-authority durable receipt model and integrity validator now exist, but production persistence/reader wiring is not complete. WPF therefore still correctly shows NOT VERIFIED rather than manufacturing evidence.
- Existing browser action verified checkpoints retain URL/verification detail for goal recovery; the new judge receipt must be stored separately or those checkpoints must be migrated carefully without breaking planner recovery.
- Receipt evidence demonstrates execution-policy consistency, not truth of remote page content or cryptographic third-party attestation.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add a protected atomic `DurableBrowserVerificationReceipt` store under the existing local-state protection boundary, record receipts from the trusted browser execution path only after terminal verified execution, and expose a read-only latest-completed presentation through `BrowserProductRuntime` to WPF Judge Evidence. Keep the store separate from approval authority and never persist exact scopes, grants, URLs, locators, values, page content or raw diagnostics.
