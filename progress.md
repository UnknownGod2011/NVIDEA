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

Added `DurableBrowserVerificationPublisher`, stale-evidence clearing, adversarial publisher tests, product-facing payload-free reads, `BrowserVerificationPublicationBoundary`, `BrowserVerificationRuntime`, and `BrowserVerificationActionLifecycle`. Publication accepts only authoritative Completed verified records, binds evidence to durable JobId, requires verified post-state and historical approval where required, and cannot convert post-commit evidence failure into a replayable browser-action failure. Lifecycle regression tests protect clear-before-admission and authoritative-record ordering.

### 2026-09-21 — durable browser host/product facades
Added internal `BrowserDurableActionRuntime`, a narrow host-facing facade pairing durable orchestration with verification lifecycle. Its create path clears stale evidence before durable admission; its advance path returns the authoritative durable job even if observational evidence publication fails. API-surface regression coverage prevents exposure of raw orchestrator, publisher, receipt store, or approval authority.

The facade owns production verification composition through `CreateWindows(orchestrator, stateDirectory)`: exactly one Windows-DPAPI `BrowserVerificationRuntime` is paired with the durable orchestrator, and the same instance supplies a payload-free `ReadVerificationPresentationAsync` path.

`BrowserProductRuntime` now has a dedicated trusted composition constructor accepting `BrowserDurableActionRuntime` and captures only its payload-free presentation read delegate. This lets the final product/WPF path consume the execution-owned verification store without receiving the publisher, receipt store, raw durable evidence, or approval authority. The legacy internal publisher constructor remains temporarily supported for migration; absent either source the product read still fails closed to NOT VERIFIED.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/BrowserProductRuntime.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected current `BrowserHostRuntime`, `BrowserDurableActionRuntime`, `BrowserProductRuntime`, and composition root before implementation.
- Confirmed `BrowserHostRuntime` still directly calls `_jobs.CreateAsync` and `_jobs.RunNextStepAsync`; host execution integration remains the primary production gap.
- Confirmed `NvideaCompositionRoot.GetBrowserProductAsync` currently constructs `BrowserProductRuntime(host)` and therefore still receives fail-closed NOT VERIFIED browser evidence until host durable-runtime wiring is completed.
- Added a least-authority product composition path that can consume the exact execution-owned durable verification reader without exposing privileged verification internals.
- Existing constructor behavior is preserved to avoid deleting working functionality during the migration.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- `ApprovalGranted`/`ApprovalObserved` remains historical evidence only; it contains no exact scope/token/reusable authority and is never accepted as authorization.
- Old judge evidence must be cleared before a new action is admitted; clear failure must abort admission before browser side effects.
- After authoritative durable completion, judge-evidence publication is observational. Publication failure/cancellation returns NOT VERIFIED semantics and must never encourage replay of a completed side effect.
- The durable facade returns the authoritative job, never a synthetic failure based on evidence persistence.
- Product verification reads can now be sourced from the execution-owned durable facade without exposing raw receipt/publisher authority.
- Failed, denied, cancelled and ambiguous executions cannot create the normal structural terminal checkpoint.
- Existing verified action checkpoints retain URL/verification detail for goal recovery; protected judge receipts remain a separate least-authority artifact.
- Receipt SHA-256 is tamper evidence, not authenticity by itself; authenticity inherits protected local state. Windows production uses CurrentUser DPAPI with purpose-derived entropy.
- Missing, plaintext, corrupt, wrong-context, malformed or integrity-invalid protected judge evidence remains NOT VERIFIED.

## Known blockers / risks
- New Core changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- `BrowserDurableActionRuntime` is not yet injected into `BrowserHostRuntime`; production browser actions therefore still call the raw orchestrator and do not publish the protected receipt.
- Host wiring should instantiate one `BrowserDurableActionRuntime.CreateWindows(orchestrator, fullStateDirectory)`, route create and every execution `RunNextStepAsync` path through it, and retain raw orchestrator access only for non-execution transitions such as approval resume/rearm/cancel/reconciliation.
- `NvideaCompositionRoot` must construct `BrowserProductRuntime` with the exact durable runtime owned by the host rather than the current host-only constructor.
- The legacy publisher constructor on `BrowserProductRuntime` should be removed only after production composition and tests prove no caller depends on it.
- Ambiguous crash reconciliation intentionally uses a legacy verified checkpoint lacking normal durable structural evidence; it must remain NOT VERIFIED unless a separately trustworthy evidence model is designed.
- A production-host integration test is still needed after composition to prove clear-before-admission and completed-side-effect/no-replay behavior end to end.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Inject one `BrowserDurableActionRuntime.CreateWindows(orchestrator, fullStateDirectory)` into `BrowserHostRuntime`; replace direct durable create and every execution `RunNextStepAsync` call with the facade while retaining raw orchestrator access only for non-execution state transitions. Then expose that exact durable runtime to trusted composition so `NvideaCompositionRoot` constructs `BrowserProductRuntime(host, durableActions)`, and add production-host integration coverage for stale-clear ordering and completed-side-effect/no-replay semantics.
