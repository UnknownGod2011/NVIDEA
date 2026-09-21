# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, browser-verification state, and production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17 — product foundation
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-20 — browser, memory, startup and research hardening
Hardened browser transport, redirects/WebSockets, Service Worker blocking, page admission, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart behavior, emergency stop and real-Chromium qualification tooling. Persisted memory embeddings are treated as untrusted. Startup/shutdown gained cancellation-safe cleanup and reverse-order partial-construction ownership. Tavily research gained payload-free evidence and SHA-256 lineage from Nemotron plan through Tavily evidence to cited synthesis, plus restart-stable durable receipts.

### 2026-09-20 to 2026-09-21 — authoritative browser judge evidence
Added historical `ApprovalGranted` evidence, Core-owned `DesktopBrowserVerificationPresentation`, WPF browser verification panel, payload-free `DurableBrowserActionEvidence`/`DurableBrowserVerificationReceipt`, canonical SHA-256 commitments and a protected atomic receipt store. Production approval evidence is set only downstream of successful last-mile authorization consumption. Successful terminal checkpoints carry payload-free durable evidence; failed/ambiguous attempts cannot create it.

Added `DurableBrowserVerificationPublisher`, stale-evidence clearing, adversarial publisher tests, product-facing payload-free reads, `BrowserVerificationPublicationBoundary`, `BrowserVerificationRuntime`, and `BrowserVerificationActionLifecycle`. Publication accepts only authoritative Completed verified records, binds evidence to durable JobId, requires verified post-state and historical approval where required, and cannot convert post-commit evidence failure into a replayable browser-action failure.

### 2026-09-21 — durable browser host/product facades
Added internal `BrowserDurableActionRuntime`, pairing durable orchestration with the verification lifecycle. It owns Windows-DPAPI verification composition, exact approval resume + execution, rearm, cancellation, and explicitly non-verifying ambiguous crash reconciliation. Rearm/cancel/ambiguous reconciliation clear protected evidence first. A private `SemaphoreSlim` linearizes durable/evidence mutations and payload-free reads across jobs so clear/execute/publish phases cannot reorder or be observed half-settled. Verification is also cleared before every execution attempt, protecting resumed/migrated/pre-verification jobs from stale receipts.

Production `BrowserHostRuntime` now owns one `BrowserDurableActionRuntime.CreateWindows(orchestrator, fullStateDirectory)` and routes durable create, ordinary execution, approval-resume execution, rearm, cancellation, and ambiguous completion through that facade instead of retaining raw orchestrator authority. The host has an assembly-internal `CreateProductRuntime` factory that binds product verification reads to the exact execution-owned durable runtime.

Production `NvideaCompositionRoot.GetBrowserProductAsync` now uses `host.CreateProductRuntime()`. WPF/product browser verification therefore resolves through the same protected receipt lifecycle used by production browser execution. `BrowserProductRuntime` now has only the trusted constructor requiring both the host and its execution-owned `BrowserDurableActionRuntime`; the legacy host-only and direct-publisher constructors were removed so future internal composition cannot silently create a verification-disconnected browser product runtime.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/BrowserProductRuntime.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, inspected recent commits, current desktop composition files, the browser product runtime, and the Core test tree before implementation.
- Removed the legacy constructor that accepted only `BrowserHostRuntime` plus an optional `DurableBrowserVerificationPublisher`.
- Made the durable-runtime verification reader mandatory/non-null and simplified `ReadVerificationPresentationAsync` to always use that execution-owned reader.
- Re-fetched the mutated file and confirmed the only remaining constructor requires `BrowserDurableActionRuntime` and no nullable publisher/read fallback remains.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed. Removing the legacy constructor is intentionally compile-enforcing: any overlooked internal call site will fail the next executable build rather than silently weakening verification.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- Browser product composition can no longer omit authoritative durable verification or inject publisher authority directly.
- Product/WPF code receives only `BrowserProductRuntime`; raw durable orchestration, receipt storage, publisher authority, approval authority and browser payloads remain behind trusted Core composition.
- Browser execution continues through clear-before-admission/execution, serialized durable/evidence transitions, exact approval scope checks, and observational post-completion publication.
- `ApprovalGranted`/`ApprovalObserved` remains historical evidence only; it contains no exact scope/token/reusable authority and is never accepted as authorization.
- Old judge evidence is cleared before new admission and before every execution attempt. Clear failure prevents browser execution.
- After authoritative durable completion, judge-evidence publication remains observational and must never encourage replay of a completed side effect.
- Rearm, cancellation and ambiguous reconciliation cannot publish verification and invalidate protected evidence before their durable transition.
- Ambiguous crash reconciliation remains deliberately NOT VERIFIED in the protected judge-evidence channel even when legacy goal-recovery state is reconciled.
- Protected receipt authenticity inherits CurrentUser DPAPI protection with purpose-derived entropy; malformed/corrupt/wrong-context/missing evidence remains NOT VERIFIED.

## Known blockers / risks
- New Core changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- Production-host integration coverage is still needed to prove clear-before-admission/execution, completed-side-effect/no-replay behavior, cancellation/rearm invalidation, ambiguous-reconciliation stale-receipt invalidation, cross-job serialization, settled verification reads, and product composition end to end.
- Because executable compilation is unavailable here, an overlooked test/internal caller of the removed constructor is possible; this is preferable to retaining an unsafe fallback and must be resolved by the next .NET-capable validation run.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add production-host/composition integration tests around the exact `BrowserHostRuntime` → `BrowserDurableActionRuntime` → `BrowserProductRuntime` graph, especially verified completion, approval-resumed consequential actions, stale-evidence invalidation, cancellation/rearm, ambiguous reconciliation, cross-job serialization, settled reads, and completed-side-effect/no-replay semantics. Run the full .NET suite in the first capable environment and repair any compile-time callers exposed by removal of the unsafe legacy constructor rather than restoring it.
