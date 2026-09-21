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

Production `BrowserHostRuntime` now owns one `BrowserDurableActionRuntime.CreateWindows(orchestrator, fullStateDirectory)` and routes durable create, ordinary execution, approval-resume execution, rearm, cancellation, and ambiguous completion through that facade instead of retaining raw orchestrator authority. The host also has an assembly-internal `CreateProductRuntime` factory that binds product verification reads to the exact execution-owned durable runtime.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/BrowserHostRuntime.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected recent commits plus `BrowserHostRuntime`, `BrowserDurableActionRuntime`, `BrowserProductRuntime`, and `NvideaCompositionRoot` before implementation.
- Replaced the host's retained `ResumableJobOrchestrator` with `BrowserDurableActionRuntime`; the raw orchestrator is now a local construction detail only.
- Production host construction creates exactly one Windows-DPAPI durable browser runtime using the same full browser state directory as jobs/audit/browser state.
- `CreateActionAsync` now goes through clear-before-admission; `AdvanceActionAsync` through clear-before-execution + authoritative publication; `ApproveAndResumeAsync` through exact-scope resume+execution in one serialized boundary; rearm/cancel through stale-receipt invalidation; ambiguous reconciliation through the explicitly non-verifying completion path.
- Added assembly-internal `BrowserHostRuntime.CreateProductRuntime(...)`, which constructs `BrowserProductRuntime(this, _durableActions, ...)`; this avoids exposing the raw receipt store/publisher/runtime outside trusted Core composition while guaranteeing execution and judge reads share one protected receipt lifecycle.
- Re-fetched the mutated host and confirmed the retained field/constructor authority is `BrowserDurableActionRuntime`, not `ResumableJobOrchestrator`.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- Browser execution now traverses the same protected verification lifecycle in the production host rather than only in test/composition helpers.
- `ApprovalGranted`/`ApprovalObserved` remains historical evidence only; it contains no exact scope/token/reusable authority and is never accepted as authorization.
- Old judge evidence is cleared before new admission and before every execution attempt. Clear failure prevents browser execution, so stale judge-green evidence cannot coexist with a newly-attempted restored job.
- Verification receipt mutation and reads are serialized with browser durable transitions, preventing cross-job clear/publish reordering and transient mid-transition presentation.
- After authoritative durable completion, judge-evidence publication is observational. Publication failure/cancellation returns NOT VERIFIED semantics and must never encourage replay of a completed side effect.
- Exact approval scope is validated before resume; approval resume and execution share the same verification boundary.
- Rearm, cancellation and ambiguous reconciliation execute no browser side effect and cannot publish verification; they invalidate existing protected verification before their durable transition.
- Ambiguous crash reconciliation can still produce a legacy verified-step checkpoint for goal recovery, but deliberately cannot create protected judge-green evidence.
- Durable reads continue through `IAgentJobStore`; that dependency is read-only and does not confer execution authority.
- Protected receipt authenticity inherits CurrentUser DPAPI protection with purpose-derived entropy; malformed/corrupt/wrong-context/missing evidence remains NOT VERIFIED.

## Known blockers / risks
- New Core changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- `NvideaCompositionRoot.GetBrowserProductAsync` still calls the legacy host-only `new BrowserProductRuntime(host)` constructor. Therefore production browser actions now generate protected receipts, but current product/WPF composition still fails closed instead of reading them. It should call `host.CreateProductRuntime()` next.
- The legacy publisher constructor on `BrowserProductRuntime` should be removed only after production composition and tests prove no caller depends on it.
- Production-host integration coverage is still needed to prove clear-before-admission/execution, completed-side-effect/no-replay behavior, cancellation/rearm invalidation, ambiguous-reconciliation stale-receipt invalidation, cross-job serialization, and settled verification reads end to end.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Switch `NvideaCompositionRoot.GetBrowserProductAsync` from `new BrowserProductRuntime(host)` to `host.CreateProductRuntime()` so WPF judge evidence reads the exact protected receipts now generated by production execution. Then add/adjust composition and host integration tests proving the execution-owned runtime is the sole browser verification source, and remove the legacy publisher composition path only after call-site coverage confirms it is unused.
