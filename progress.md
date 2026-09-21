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

Added `DurableBrowserVerificationPublisher`, stale-evidence clearing, adversarial publisher tests, product-facing payload-free reads, and `BrowserVerificationPublicationBoundary`. Publication accepts only authoritative Completed verified records, binds evidence to durable JobId, requires verified post-state and historical approval where required, and cannot convert post-commit evidence failure into a replayable browser-action failure.

Added `BrowserVerificationRuntime`, a single-owner composition object using Windows CurrentUser DPAPI in production, plus `BrowserVerificationActionLifecycle`. The lifecycle structurally clears stale evidence before durable admission and observes only the authoritative record returned by durable execution before attempting publication.

### 2026-09-21 — lifecycle ordering regression coverage
Added deterministic `BrowserVerificationActionLifecycleTests` covering three host-critical invariants: stale evidence is absent before the durable-create delegate runs; a receipt-clear failure prevents durable admission entirely; and non-Completed authoritative records are returned unchanged without evidence publication. These tests are intended to protect the upcoming `BrowserHostRuntime` wiring from ordering regressions.

## Latest run
Files changed:
- `tests/Nvidea.Core.Tests/BrowserVerificationActionLifecycleTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the current repository tree, `BrowserHostRuntime`, `BrowserVerificationRuntime`, `BrowserVerificationActionLifecycle`, `BrowserVerificationPublicationBoundary`, existing runtime tests and current `AgentJobRecord` contract before implementation.
- Added deterministic lifecycle tests for clear-before-create, clear-failure-before-admission, and unchanged nonterminal authoritative records.
- The clear-failure test deliberately makes the receipt path a directory so deletion fails before the create delegate can run; it asserts the durable-create delegate remains uncalled.
- The nonterminal advance test asserts reference identity of the returned authoritative record and `Published=false`, `PublicationFailed=false`, `EvidenceVerified=false`.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- `ApprovalGranted`/`ApprovalObserved` remains historical evidence only; it contains no exact scope/token/reusable authority and is never accepted as authorization.
- Old judge evidence must be cleared before a new action is admitted; clear failure must abort admission before browser side effects. This ordering now has a direct regression test.
- After authoritative durable completion, judge-evidence publication is observational. Publication failure/cancellation returns NOT VERIFIED semantics and must never encourage replay of a completed side effect.
- Failed, denied, cancelled and ambiguous executions cannot create the normal structural terminal checkpoint.
- Existing verified action checkpoints retain URL/verification detail for goal recovery; protected judge receipts are a separate least-authority artifact.
- Receipt SHA-256 is tamper evidence, not authenticity by itself; authenticity inherits protected local state. Windows production uses CurrentUser DPAPI with purpose-derived entropy.
- Missing, plaintext, corrupt, wrong-context, malformed or integrity-invalid protected judge evidence remains NOT VERIFIED.

## Known blockers / risks
- New Core/test changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- `BrowserVerificationRuntime` and `BrowserVerificationActionLifecycle` are not yet injected into `BrowserHostRuntime`; production browser actions therefore still do not publish the protected receipt.
- Host wiring must use one lifecycle instance around durable `_jobs.CreateAsync` and every `_jobs.RunNextStepAsync` path rather than calling publication primitives independently.
- `BrowserProductRuntime` must receive the exact publisher/read path owned by the same `BrowserVerificationRuntime`; WPF Judge Evidence still needs to consume that presentation.
- A production-host integration test is still needed after composition to prove clear-before-admission and completed-side-effect/no-replay behavior end to end.
- Cross-process coordination for the receipt file is not needed by the single Windows host today, but must be revisited if a second local writer is introduced.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Inject one `BrowserVerificationRuntime.CreateWindows(fullStateDirectory)` plus one `BrowserVerificationActionLifecycle` into `BrowserHostRuntime`, routing durable `_jobs.CreateAsync` through `AdmitAsync` and every `_jobs.RunNextStepAsync` path through `AdvanceAsync`. Then pass the same runtime read path into `BrowserProductRuntime`/WPF Judge Evidence and add production-host integration coverage proving stale-clear ordering and completed-side-effect/no-replay behavior.
