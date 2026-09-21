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
Added historical `ApprovalGranted` evidence, Core-owned `DesktopBrowserVerificationPresentation`, WPF browser verification panel, payload-free durable browser evidence/receipts, canonical SHA-256 commitments and protected atomic receipt storage. Publication accepts only authoritative Completed verified records, binds evidence to durable JobId, requires verified post-state and historical approval where required, and cannot convert post-commit evidence failure into a replayable browser-action failure.

### 2026-09-21 — durable browser host/product facades
Added internal `BrowserDurableActionRuntime`, pairing durable orchestration with verification lifecycle. It owns Windows-DPAPI verification composition, exact approval resume + execution, rearm, cancellation, and explicitly non-verifying ambiguous crash reconciliation. Rearm/cancel/ambiguous reconciliation clear protected evidence first. A private `SemaphoreSlim` linearizes durable/evidence mutations and payload-free reads across jobs; verification is also cleared before every execution attempt.

Production `BrowserHostRuntime` owns one execution-scoped durable runtime and routes durable create, ordinary execution, approval-resume execution, rearm, cancellation, and ambiguous completion through it. `NvideaCompositionRoot.GetBrowserProductAsync` uses `host.CreateProductRuntime()`, binding WPF/product verification to the same protected receipt lifecycle. Legacy verification-disconnected product constructors were removed.

### 2026-09-21 — production browser evidence integration coverage
Real-Chromium production-host coverage proves consequential actions remain NOT VERIFIED before approval, become VERIFIED only after exact-scope approval + real mutation + typed postcondition + durable completion, and cannot replay. Additional coverage proves a prior green receipt is invalidated on newer action admission and remains absent through approval rearm and cancellation without extra browser mutation.

Crash-ambiguous reconciliation coverage establishes a genuine green receipt, reproduces a persisted Running crash shape via the trusted job-store test seam, reconciles from fresh observed expected-state evidence, and requires durable completion while judge verification remains NOT VERIFIED and the controlled mutation count remains unchanged.

### 2026-09-21 to 2026-09-22 — serialization boundary regression guard
Added structural and behavioral Core coverage for `BrowserDurableActionRuntime`'s cross-job serialization boundary. The private instance `SemaphoreSlim` must cover every receipt-mutating operation and judge read. A paused transition blocks a competitor until settlement, and a cancelled waiter never enters the critical section.

### 2026-09-22 — payload-free lifecycle observation seam
Added an internal optional `IBrowserVerificationLifecycleObserver` with only two payload-free stages: admission evidence cleared and execution evidence cleared. Production composition installs no observer. Deterministic tests can pause precisely after protected receipt invalidation and before durable create/browser execution without receiving a job, receipt, URL, locator, approval, typed value, browser handle, or publisher authority. Lifecycle tests prove the execution-stage callback occurs after stale receipt removal and before execution, and cancellation while paused prevents execution entirely.

### 2026-09-22 — observer threaded through durable runtime test composition
`BrowserDurableActionRuntime` now has a narrowly scoped internal `CreateForTesting` factory that injects the payload-free lifecycle observer into the exact verification lifecycle protected by the runtime transition gate. Production `Create` and `CreateWindows` remain observer-free. API-surface regression coverage requires the factory and observer contract to stay non-public and prevents the test seam from becoming product authority.

### 2026-09-22 — exact durable clear/execute race coverage
Added `BrowserDurableActionRuntimeLifecycleRaceTests` using the real durable runtime, real orchestrator, shared verification runtime and payload-free observer. The test pauses job A exactly after execution evidence is cleared, queues both a judge verification read and a newer job-B admission, and requires both to remain outside the critical section until A settles. It also requires job B to remain absent from durable storage while blocked and final evidence to remain NOT VERIFIED after the newer admission.

## Latest run
Files changed:
- `tests/Nvidea.Core.Tests/BrowserDurableActionRuntimeLifecycleRaceTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the current durable runtime, verification runtime, lifecycle tests, concurrency tests, orchestrator behavior, and current repository tree before implementation.
- Added a deterministic in-memory orchestrator fixture and exercised `BrowserDurableActionRuntime.CreateForTesting` rather than reflecting directly into its semaphore.
- Seeded stale receipt bytes, paused at `ExecutionEvidenceCleared`, and asserted receipt invalidation happens before handler execution.
- While A owns the transition gate, started both `ReadVerificationPresentationAsync` and a newer `CreateAsync` admission; asserted neither completes, job B is not persisted, and the handler has not executed.
- After release, A must complete exactly once; queued operations then settle and B admission leaves the shared verification store clear/NOT VERIFIED.
- The fixture deliberately uses a non-browser completing handler, so it cannot mint a browser verification receipt and does not weaken production publication validation merely to make the race test convenient.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment still cannot execute .NET 8, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The new test uses only internal test composition and an in-memory job/audit fixture; production factories and runtime authority are unchanged.
- The lifecycle observer remains payload-free and sees only the enum stage. It receives no URL, locator, typed value, job, approval, receipt, browser handle, publisher, or semaphore.
- The test explicitly proves a judge read cannot inspect the transient cleared-but-unsettled state and a newer admission cannot overtake the active transition.
- Job B is checked at the durable store boundary while blocked, guarding against future refactors that might move job persistence outside the verification serialization gate.
- No test hook can manufacture VERIFIED evidence: the non-browser fixture intentionally exercises only serialization and invalidation semantics.

## Known blockers / risks
- The new Core race test and accumulated suite require executable .NET 8 validation; compile/test PASS remains unverified in this connector environment.
- The strongest remaining variant is the same race with a genuine browser-action terminal checkpoint that successfully publishes a green receipt for A before B invalidates it; existing production Chromium tests separately cover genuine publication but not this exact interleaving.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Upgrade the deterministic durable-runtime race fixture to use a genuine browser-action terminal checkpoint/evidence shape accepted by `DurableBrowserVerificationPublisher`: pause A after clear, prove judge read and B admission block, let A publish VERIFIED evidence, then prove B admission immediately invalidates A's newly published receipt and the final judge presentation is NOT VERIFIED. Reuse existing browser checkpoint/receipt test builders rather than weakening validation. Then run the full .NET suite in the first capable environment and fix any compile/runtime findings without weakening the authority boundary.
