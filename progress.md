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

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/BrowserDurableActionRuntime.cs`
- `tests/Nvidea.Core.Tests/BrowserDurableActionRuntimeApiSurfaceTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the current durable runtime, verification runtime, lifecycle observer, concurrency tests and API-surface tests before implementation.
- Threaded the observer through the private runtime constructor and added only an internal `CreateForTesting(jobs, verificationRuntime, observer)` composition seam.
- Production factories still call the constructor without an observer, so shipping behavior is unchanged.
- Added reflection regression coverage requiring `CreateForTesting` to remain internal, to take exactly the orchestrator + shared verification runtime + internal payload-free observer, and requiring the observer type itself to remain non-public.
- Existing private instance `SemaphoreSlim` remains the sole transition serialization authority; the observer is invoked from inside the lifecycle that is already executed under that gate.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment still cannot execute .NET 8, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The new factory is internal and test-oriented; it does not expose the receipt store, publisher, browser, approval capability, transition semaphore, or any payload-bearing object.
- The observer contract remains payload-free and internal. Production factories install no observer, avoiding accidental timing callbacks or side-channel expansion in shipping composition.
- Observer failure/cancellation remains fail-closed after receipt invalidation and before execution; old green evidence cannot survive a failed observation callback.
- Shared `BrowserVerificationRuntime` remains mandatory for the test seam, so publication and judge reads cannot be accidentally wired to different receipt stores during the forthcoming race test.

## Known blockers / risks
- New Core/test changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- The exact race can now be constructed, but the behavioral test still needs a deterministic in-memory/local orchestrator fixture capable of completing a verified browser-action record while paused at `ExecutionEvidenceCleared`.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Use `CreateForTesting` to add the full durable-runtime race regression: seed prior verified evidence, pause job A at `ExecutionEvidenceCleared`, start a judge read and newer job-B admission, prove neither can enter while A owns the transition gate, release A and prove its verified receipt settles, then allow B admission and prove B invalidates A's just-published receipt so final judge presentation is NOT VERIFIED. Keep the fixture payload-free outside the trusted test composition and verify cancellation of either waiter cannot bypass the gate. Then execute the full .NET suite in the first capable environment and fix any compile/runtime findings without weakening the authority boundary.
