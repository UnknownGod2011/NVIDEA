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
Added internal `BrowserDurableActionRuntime`, pairing durable orchestration with the verification lifecycle. It owns Windows-DPAPI verification composition, exact approval resume + execution, rearm, cancellation, and explicitly non-verifying ambiguous crash reconciliation. Rearm/cancel/ambiguous reconciliation clear protected evidence first. A private `SemaphoreSlim` linearizes durable/evidence mutations across jobs so clear/execute/publish phases cannot reorder and misidentify the latest judge-green action. Verification is also cleared before every execution attempt, protecting resumed/migrated/pre-verification jobs from stale receipts.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/BrowserDurableActionRuntime.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the current repository tree, recent commits, `BrowserHostRuntime`, `BrowserDurableActionRuntime`, `BrowserProductRuntime`, and `NvideaCompositionRoot` before implementation.
- Confirmed production `BrowserHostRuntime` still directly uses `ResumableJobOrchestrator`; host/product wiring remains the highest-value integration task.
- Found an observational consistency race: durable/evidence mutations were serialized, but `ReadVerificationPresentationAsync` bypassed the gate. Judge UI could therefore sample protected evidence between the clear and publish phases of one action and display transient NOT VERIFIED even though the same transition subsequently settled as verified.
- Hardened `BrowserDurableActionRuntime.ReadVerificationPresentationAsync` to use the same cancellation-aware transition gate as receipt mutations. Judge-facing reads now observe a settled durable/evidence transition, never an intermediate clear/execute/publish state.
- This does not expand authority: the read still returns only `DesktopBrowserVerificationPresentation`; raw receipts, publishers, URLs, locators, typed values, approval grants and browser payloads remain inaccessible.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- `ApprovalGranted`/`ApprovalObserved` remains historical evidence only; it contains no exact scope/token/reusable authority and is never accepted as authorization.
- Old judge evidence is cleared before new admission and before every execution attempt. Clear failure prevents browser execution, so stale judge-green evidence cannot coexist with a newly-attempted restored job.
- Verification receipt mutation is serialized with browser durable transitions, preventing cross-job clear/publish reordering from misrepresenting which action is judge-green.
- Judge verification reads now share that serialization boundary, preventing transient mid-transition receipt state from being presented as a settled result.
- After authoritative durable completion, judge-evidence publication is observational. Publication failure/cancellation returns NOT VERIFIED semantics and must never encourage replay of a completed side effect.
- Exact approval scope is validated before resume; approval resume and execution share the same verification boundary.
- Rearm, cancellation and ambiguous reconciliation execute no browser side effect and cannot publish verification; they invalidate existing protected verification before their durable transition.
- Product verification reads can be sourced from the execution-owned durable facade without exposing raw receipt/publisher authority.
- Failed, denied, cancelled and ambiguous executions cannot create the normal structural terminal checkpoint.
- Existing verified action checkpoints retain URL/verification detail for goal recovery; protected judge receipts remain a separate least-authority artifact.
- Receipt SHA-256 is tamper evidence, not authenticity by itself; authenticity inherits protected local state. Windows production uses CurrentUser DPAPI with purpose-derived entropy.
- Missing, plaintext, corrupt, wrong-context, malformed or integrity-invalid protected judge evidence remains NOT VERIFIED.

## Known blockers / risks
- New Core changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- `BrowserDurableActionRuntime` is not yet injected into `BrowserHostRuntime`; production browser actions therefore still call the raw orchestrator and do not publish the protected receipt.
- Host wiring should instantiate one `BrowserDurableActionRuntime.CreateWindows(orchestrator, fullStateDirectory)`, route create, ordinary execution, approval-resumed execution, rearm, cancellation and ambiguous completion through it. Durable reads may continue through the existing `IAgentJobStore`; that read-only dependency does not confer execution authority.
- `NvideaCompositionRoot` must construct `BrowserProductRuntime` with the exact durable runtime owned by the host rather than the current host-only constructor.
- The legacy publisher constructor on `BrowserProductRuntime` should be removed only after production composition and tests prove no caller depends on it.
- Production-host integration coverage is still needed to prove clear-before-admission/execution, completed-side-effect/no-replay behavior, cancellation/rearm invalidation, ambiguous-reconciliation stale-receipt invalidation, cross-job serialization, and settled verification reads end to end.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Inject one `BrowserDurableActionRuntime.CreateWindows(orchestrator, fullStateDirectory)` into `BrowserHostRuntime`; replace direct durable create, ordinary `RunNextStepAsync`, approval resume+execution, rearm, cancellation and ambiguous completion with the facade. Expose that exact durable runtime to trusted composition so `NvideaCompositionRoot` constructs `BrowserProductRuntime(host, durableActions)`, then add production-host integration coverage for stale-clear ordering, completed-side-effect/no-replay semantics, cancellation/rearm invalidation, ambiguous-reconciliation NOT VERIFIED behavior, cross-job receipt serialization, and settled verification reads.
