# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, authoritative serialized browser-verification state, production-observed session milestones, and a fail-closed recording gate.

## Persistent history
### 2026-09-06 to 2026-09-17 — product foundation
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-21 — security, browser and durable evidence hardening
Hardened browser transport, redirects/WebSockets, Service Worker blocking, page admission, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart behavior, emergency stop and real-Chromium qualification tooling. Persisted memory embeddings are untrusted. Startup/shutdown gained cancellation-safe cleanup. Tavily research gained payload-free SHA-256 lineage and restart-stable durable receipts. Browser judge evidence gained historical approval evidence, canonical commitments, protected atomic receipts, durable host/product facades and production-host coverage for approval, mutation, post-state verification, stale-receipt invalidation, cancellation/rearm and crash-ambiguous reconciliation.

### 2026-09-21 to 2026-09-22 — browser serialization/race qualification
`BrowserDurableActionRuntime` owns one private `SemaphoreSlim` covering evidence-affecting transitions and judge reads. Payload-free internal lifecycle observers permit deterministic tests without production authority. Race coverage pauses after receipt invalidation, proves judge reads/new admissions cannot overtake settlement, then proves a genuine VERIFIED receipt from job A is invalidated by newer job B admission. No stale green or half-settled judge state is allowed.

### 2026-09-22 — demo evidence and judge integration
Bound demo validation to production `SessionEvidenceKind`, aligned manifest/runbook to the canonical six milestones, and added payload-free `DemoRecordingGate` plus `DemoRecordingContract.RequiredSequence`. WPF Judge Evidence renders explicit READY/NOT READY from the real session snapshot and resets fail closed. Provider-live readiness remains independently typed. Windows obtains browser verification only through `BrowserProductRuntime.ReadVerificationPresentationAsync`, bound to the execution-owned durable runtime. Browser startup/read/protected-receipt failure fails closed without hiding independent provider/research/session evidence.

### 2026-09-22 — validator/build and API contract cleanup
Removed a stale duplicate demo-validator test type that was a likely C# compile blocker and encoded obsolete milestone names. Added manifest-to-production recording-contract alignment coverage. Repaired `NvideaCompositionRootBrowserApiSurfaceTests` after the sanctioned payload-free browser verification reader was added: the product API explicitly allows that read while rejecting raw host/durable-runtime/receipt-store/publisher authority in its signature.

### 2026-09-22 — canonical browser product publication hardening
Found a first-call concurrency race in `NvideaCompositionRoot.GetBrowserProductAsync`: host creation was serialized, but product publication occurred after the host gate was released, allowing simultaneous first callers to construct distinct product facade instances. Product publication is now serialized by the composition `_browserGate`, rechecks disposal while holding the gate, and publishes one canonical least-authority product facade backed by the execution-owned durable runtime.

## Latest run — browser composition lifetime contract coverage
Files changed:
- Added `tests/Nvidea.Core.Tests/NvideaCompositionRootBrowserLifetimeBoundaryTests.cs`.
- Updated `progress.md`.

Validation/evidence:
- Re-read `progress.md` completely and inspected the latest commits, `NvideaCompositionRoot`, the existing browser API-surface regression suite, and the current test project layout before mutation.
- Added a regression contract that locks the composition root's browser lifetime synchronization primitive, raw host, and canonical product facade as private implementation details.
- Added a public-surface scan rejecting accidental exposure of `BrowserHostRuntime`, `BrowserDurableActionRuntime`, `DurableBrowserVerificationReceiptStore`, or `DurableBrowserVerificationPublisher` through public fields/properties/method signatures.
- Added a contract for `GetBrowserProductAsync` requiring an asynchronous `Task<BrowserProductRuntime>` return and cancellation-aware `CancellationToken` input, protecting the cancellation/lifetime semantics needed by Windows judge reads.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The new tests add no runtime authority and no provider/browser data path. They enforce that privileged browser host/runtime/receipt/publisher types stay behind the composition boundary.
- No credentials, prompts, URLs, locators, typed values, memory contents, protected receipts, or provider payloads are captured by the tests.
- Cancellation remains part of the product-acquisition contract; the test does not weaken fail-closed disposal or durable browser evidence serialization.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- A remaining lifetime edge deserves deterministic executable qualification: a caller that passes the initial `_disposed` check can race with disposal when a cached browser product/host already exists. The strongest fix should ensure acquisition and disposal linearize on the same gate without introducing nested-gate deadlock or Chromium-dependent tests.
- `MainWindow.Readiness` initializes the browser product when Judge Evidence is opened; this can incur Playwright startup latency. Any future cached/non-starting reader must preserve the same durable serialization boundary.
- Provider-live proof remains independently typed and freshness-checked; session milestone completion alone is insufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Refactor browser host/product acquisition so cached fast paths and disposal linearize under one composition lifetime gate, then add deterministic concurrency coverage for simultaneous first callers and disposal-vs-cached-acquisition without launching Chromium. Keep any test seam assembly-internal and incapable of exposing raw browser authority. Then run the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
