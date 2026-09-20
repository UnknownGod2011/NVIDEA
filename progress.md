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

### 2026-09-20 — least-authority durable browser receipts and protected store
- Added `DurableBrowserActionEvidence` and `DurableBrowserVerificationReceipt` as restart-stable, non-authorizing structural evidence with canonical SHA-256 integrity commitment.
- Durable evidence contains only action id/kind, risk/policy outcome, whether approval was required and historically observed, driver success, post-state verification and timestamps. It excludes URLs, locators, typed values, page content, approval scopes/tokens, diagnostics, policy rationale and verification detail.
- Added `DurableBrowserVerificationReceiptStore`: receipt JSON is protected through the existing `ILocalStateProtector`/`LocalStateEnvelope` boundary with a store-specific purpose and persisted by same-directory atomic replace. Plaintext downgrade is rejected; read requires both protected envelope and valid receipt commitment.
- Store serialisation buffers and persisted read buffers are zeroed best-effort after use. Writes reject invalid receipts before touching durable state and serialize under a process-local gate.
- Added deterministic store tests for protected round-trip, private marker non-disclosure, plaintext downgrade rejection and integrity-tampered protected payload rejection.

Files changed in latest run:
- `src/Nvidea.Core/Browser/DurableBrowserVerificationReceiptStore.cs`
- `tests/Nvidea.Core.Tests/DurableBrowserVerificationReceiptStoreTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely; inspected recent commits, `BrowserProductRuntime`, durable browser receipt model, local-state protection envelope and atomic writer before implementation.
- Static review confirms the store never receives or serializes raw browser actions; its persistence input is the already payload-free durable receipt.
- Protection purpose is store-specific (`browser-verification-receipt/v1`), preventing cross-store ciphertext transplant under DPAPI purpose-derived entropy.
- Plaintext legacy/downgrade files fail closed rather than being silently migrated into judge evidence.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- `ApprovalObserved` remains historical evidence only; it is never an approval grant and carries no exact scope/token/reusable authority.
- The protected receipt store is separate from browser recovery checkpoints and approval authority, avoiding accidental authority resurrection after restart.
- Browser judge projection and durable receipt remain payload-free. Private marker tests guard against accidental persistence of typed/locator/URL/detail fields.
- Receipt SHA-256 is tamper evidence, not authenticity by itself; authenticity inherits the local protected-state boundary. On Windows production this is CurrentUser DPAPI with purpose-derived entropy.
- Atomic replace prevents a normal crash during write from exposing a partially serialized final receipt; a process-local semaphore prevents concurrent readers/writers in the same runtime.
- Missing, plaintext, corrupt, wrong-protection-context, malformed JSON or integrity-invalid evidence fails closed and must remain NOT VERIFIED.

## Known blockers / risks
- New Core changes require compile/runtime execution under .NET 8; accumulated Windows/Chromium suites remain pending executable-environment validation.
- Protected receipt persistence now exists, but the trusted terminal browser execution path does not yet write it and `BrowserProductRuntime` does not yet expose a read-only latest-completed presentation. WPF therefore still correctly shows NOT VERIFIED rather than manufacturing evidence.
- Cross-process coordination for the receipt file is not yet needed by the single Windows host, but should be revisited if a second local writer is introduced.
- Existing browser action verified checkpoints retain URL/verification detail for goal recovery; judge receipt remains deliberately separate so planner recovery is not weakened.
- Receipt evidence demonstrates execution-policy consistency, not truth of remote page content or cryptographic third-party attestation.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Wire `DurableBrowserVerificationReceiptStore` into the trusted browser terminal-completion path so only a fully verified completed run can replace the latest receipt, then add a read-only `BrowserProductRuntime` method that returns only `DesktopBrowserVerificationPresentation` and feed it into WPF Judge Evidence. Add restart/tamper/failure tests proving failed, cancelled, denied, ambiguous or partially verified runs cannot overwrite the last valid receipt.
