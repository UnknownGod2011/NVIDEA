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

### 2026-09-20 — Tavily/research provenance
Added payload-free `DesktopResearchEvidence`, SHA-256 lineage across Nemotron plan -> Tavily prepared evidence -> cited synthesis, restart-stable `DurableResearchReceipt`, no-repeat-Tavily restart simulation, authoritative receipt reading, and Core-owned judge presentation that fails closed for legacy/corrupt/inconsistent evidence.

### 2026-09-20 — browser judge evidence and durable receipts
Added explicit `ApprovalGranted` evidence, Core-owned `DesktopBrowserVerificationPresentation`, and WPF browser verification panel. Added restart-stable non-authorizing `DurableBrowserActionEvidence`/`DurableBrowserVerificationReceipt` with canonical SHA-256 commitment and a protected atomic store. Receipt/store exclude URLs, locators, typed values, page content, approval scopes/tokens, diagnostics, rationale and verification details. Plaintext downgrade, wrong protection context and integrity-invalid state fail closed.

### 2026-09-20 — production approval and checkpoint correctness
Audited `BrowserHostRuntime -> BrowserActionJobHandler -> BrowserCapabilityExecutionService -> CapabilityToolExecutor`; `ApprovalGranted` becomes true only downstream of successful last-mile authorization/consumption. Successful terminal checkpoints now include payload-free `durableEvidence`; failed or side-effect-ambiguous attempts cannot create it. `DurableBrowserVerificationReceipt.CreateFromEvidence` creates the committed receipt without reconstructing raw browser data or approval authority.

### 2026-09-20 — authoritative browser evidence publication boundary
Added stale-evidence clearing and `DurableBrowserVerificationPublisher`. Publication accepts only authoritative Completed `browser.action.verified` records, binds evidence ActionId to durable JobId, and requires allowed execution, driver success, verified post-state, plus historical approval evidence when required. Legacy/reconciled checkpoints without structural evidence do not overwrite the store.

### 2026-09-21 — publisher contract/adversarial validation
Added deterministic `DurableBrowserVerificationPublisherTests` covering successful approved publication, fail-closed gating for Pending/Running/WaitingForApproval/Failed/Cancelled jobs, cross-job evidence rejection, consequential completion without observed approval, and stale-receipt clearing before a new action. These tests encode the intended no-false-green semantics before production host composition.

### 2026-09-21 — product-facing durable browser evidence read boundary
`BrowserProductRuntime` now exposes only `DesktopBrowserVerificationPresentation` through `ReadVerificationPresentationAsync`. The publisher is an internal optional dependency; until trusted host composition supplies it, the product API deterministically returns NOT VERIFIED instead of inferring success from provider readiness, session milestones, or raw browser outcomes. This keeps URLs, locators, typed values, page content, approval authority and diagnostics outside the product/judge evidence API.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/BrowserProductRuntime.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the current default-branch tree, `BrowserHostRuntime`, `BrowserProductRuntime`, `DurableBrowserVerificationPublisher`, protected receipt store, and CurrentUser DPAPI protection boundary before changing code.
- Added the product-facing durable browser presentation read path without manufacturing a production green state before host publication is wired.
- Static review confirms the new API returns only the closed Core presentation type and fails closed when no trusted publisher is composed.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- `ApprovalGranted`/`ApprovalObserved` is historical evidence only; it contains no exact scope/token/reusable authority and is never accepted as authorization.
- Old judge evidence must be cleared before a new action is admitted; clear failure must abort admission before browser side effects.
- Post-commit publication must never be interpreted as browser-action failure: the durable job may already be Completed. Host wiring must fail the evidence surface closed without encouraging replay of a completed side effect.
- Failed, denied, cancelled and ambiguous executions cannot create the normal structural terminal checkpoint.
- Existing verified action checkpoints retain URL/verification detail for goal recovery; protected judge receipts are a separate least-authority artifact.
- Receipt SHA-256 is tamper evidence, not authenticity by itself; authenticity inherits protected local state. Windows production uses CurrentUser DPAPI with purpose-derived entropy.
- Missing, plaintext, corrupt, wrong-context, malformed or integrity-invalid protected judge evidence remains NOT VERIFIED.
- Product callers cannot read the receipt or raw durable evidence directly; the new read API exposes only the closed `DesktopBrowserVerificationPresentation` projection.

## Known blockers / risks
- New Core changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- `DurableBrowserVerificationPublisher` is still not composed into `BrowserHostRuntime`; production browser actions therefore do not yet publish the protected receipt and the new product read API correctly remains NOT VERIFIED.
- Host wiring must call `BeginActionAsync` before durable action admission, then attempt publication only after `RunNextStepAsync` returns Completed. Publication failure after completion must not make callers replay a side effect.
- WPF Judge Evidence still needs to consume `BrowserProductRuntime.ReadVerificationPresentationAsync` once production composition supplies the publisher.
- A production-path test is still needed to prove publication failure after durable completion cannot be surfaced as replayable browser failure.
- Cross-process coordination for the receipt file is not needed by the single Windows host today, but must be revisited if a second local writer is introduced.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Compose `DurableBrowserVerificationPublisher` into `BrowserHostRuntime` with CurrentUser DPAPI: clear stale evidence before new action admission; after `RunNextStepAsync` returns authoritative Completed, publish structural evidence while swallowing/reporting evidence-publication failure as NOT VERIFIED rather than converting a completed browser side effect into a replayable action failure. Then inject the same publisher into `BrowserProductRuntime`, feed `ReadVerificationPresentationAsync` into WPF Judge Evidence, and add the production-path no-replay test.
