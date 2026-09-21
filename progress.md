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

Production `NvideaCompositionRoot.GetBrowserProductAsync` now uses `host.CreateProductRuntime()` instead of the legacy host-only `new BrowserProductRuntime(host)` path. WPF/product browser verification therefore resolves through the same protected receipt lifecycle used by production browser execution rather than failing closed because it was bound to no verification source.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected recent commits, `NvideaCompositionRoot`, `BrowserHostRuntime`, and the current test tree before implementation.
- Switched the sole production browser-product composition call site from `new BrowserProductRuntime(host)` to `host.CreateProductRuntime()`.
- Re-fetched the mutated composition-root region and confirmed `GetBrowserProductAsync` now delegates to the host factory.
- This closes the production composition gap recorded in the prior run: execution and judge-visible browser verification now share the exact host-owned `BrowserDurableActionRuntime` and protected DPAPI receipt lifecycle.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- Browser product composition no longer has a production path that silently omits the authoritative verification source.
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
- The legacy publisher/host-only constructors on `BrowserProductRuntime` should be removed only after call-site coverage proves no tests or internal composition paths still depend on them.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add production-host/composition integration tests around the exact `BrowserHostRuntime` → `BrowserDurableActionRuntime` → `BrowserProductRuntime` graph, including completed verified actions, approval-resumed consequential actions, stale-evidence invalidation, cancellation/rearm, ambiguous reconciliation, and completed-side-effect/no-replay semantics. Once call-site coverage proves the old composition routes are unused, remove or narrow the legacy `BrowserProductRuntime` constructors so future code cannot regress to a verification-disconnected product runtime.
