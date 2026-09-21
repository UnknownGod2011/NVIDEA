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

### 2026-09-21 — durable browser host facade
Added internal `BrowserDurableActionRuntime`, a narrow host-facing facade that owns the `ResumableJobOrchestrator` + verification lifecycle pairing. Its create path structurally clears stale evidence before durable admission; its advance path returns the authoritative durable job even when observational evidence publication fails, preventing evidence-store faults from encouraging replay of completed side effects. Added API-surface regression coverage so the facade exposes only create/advance operations and no raw orchestrator, publisher, receipt store, or approval authority.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/BrowserDurableActionRuntime.cs`
- `tests/Nvidea.Core.Tests/BrowserDurableActionRuntimeApiSurfaceTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected current repository tree, `BrowserHostRuntime`, `BrowserVerificationRuntime`, `BrowserVerificationActionLifecycle`, and `BrowserVerificationPublicationBoundary` before implementation.
- Confirmed production `BrowserHostRuntime` still directly calls `_jobs.CreateAsync` and `_jobs.RunNextStepAsync`; the new facade is deliberately shaped as a drop-in durable create/advance boundary for those paths.
- Added reflection-based least-authority API coverage: the facade is internal, exposes only `CreateAsync`/`RunNextStepAsync`, and has no public fields/properties carrying privileged internals.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- `ApprovalGranted`/`ApprovalObserved` remains historical evidence only; it contains no exact scope/token/reusable authority and is never accepted as authorization.
- Old judge evidence must be cleared before a new action is admitted; clear failure must abort admission before browser side effects.
- After authoritative durable completion, judge-evidence publication is observational. Publication failure/cancellation returns NOT VERIFIED semantics and must never encourage replay of a completed side effect.
- The new durable facade returns `publication.AuthoritativeJob`, never a synthetic failure based on evidence persistence.
- Failed, denied, cancelled and ambiguous executions cannot create the normal structural terminal checkpoint.
- Existing verified action checkpoints retain URL/verification detail for goal recovery; protected judge receipts are a separate least-authority artifact.
- Receipt SHA-256 is tamper evidence, not authenticity by itself; authenticity inherits protected local state. Windows production uses CurrentUser DPAPI with purpose-derived entropy.
- Missing, plaintext, corrupt, wrong-context, malformed or integrity-invalid protected judge evidence remains NOT VERIFIED.

## Known blockers / risks
- New Core/test changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- `BrowserDurableActionRuntime` is not yet injected into `BrowserHostRuntime`; production browser actions therefore still call the raw orchestrator and do not publish the protected receipt.
- Host wiring must construct exactly one `BrowserVerificationRuntime.CreateWindows(fullStateDirectory)`, one lifecycle, and one durable facade, then route create and every run-next-step path through it.
- `BrowserProductRuntime` must receive the exact publisher/read path owned by the same `BrowserVerificationRuntime`; WPF Judge Evidence still needs to consume that presentation.
- Ambiguous crash reconciliation intentionally uses a legacy verified checkpoint that lacks normal durable structural evidence; it must remain NOT VERIFIED unless a separately trustworthy evidence model is designed.
- A production-host integration test is still needed after composition to prove clear-before-admission and completed-side-effect/no-replay behavior end to end.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Replace `BrowserHostRuntime`'s direct durable create/run-next-step calls with one composed `BrowserDurableActionRuntime` backed by one `BrowserVerificationRuntime.CreateWindows(fullStateDirectory)`. Preserve raw orchestrator access only for non-execution operations such as approval resume/rearm/cancel/reconciliation. Then pass the same verification runtime read path into `BrowserProductRuntime`/WPF Judge Evidence and add production-host integration coverage proving stale-clear ordering and completed-side-effect/no-replay behavior.
