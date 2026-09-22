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
Found a first-call concurrency race in `NvideaCompositionRoot.GetBrowserProductAsync`: host creation was serialized, but product publication occurred after the host gate was released, allowing simultaneous first callers to construct distinct product facade instances. Product publication was serialized and composition-boundary tests added to keep browser lifetime synchronization, raw host and canonical product private and reject privileged browser authority from public composition APIs.

### 2026-09-22 — deterministic composition lifetime primitive
Added assembly-internal `CompositionLifetimeGate` with deterministic Chromium-free tests. It gives acquisition and shutdown one explicit linearization point; once disposal crosses the gate, queued/new acquisitions fail closed, cancellation while waiting does not poison the gate, disposal is idempotent, and lease release is idempotent.

## Latest run — composition lifetime integration
Files changed:
- Updated `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`.
- Updated `tests/Nvidea.Core.Tests/NvideaCompositionRootBrowserLifetimeBoundaryTests.cs`.
- Updated `progress.md`.

Validation/evidence:
- Re-read `progress.md` completely and inspected the current composition root, lifetime primitive, repository tree, and lifetime-boundary tests before implementation.
- Replaced the split `_disposed` + raw `_browserGate` scheme with the qualified `CompositionLifetimeGate` as the single browser/composition lifetime linearization authority.
- `GetBrowserProductAsync`, browser-goal-agent creation, browser-goal-session listing, and ambiguous-recovery-service creation now acquire a lifetime lease before touching composition-owned browser/session authority. Cached host/product paths no longer bypass disposal synchronization.
- Host lazy startup and canonical product publication now occur under the same lifetime lease. There is no nested gate acquisition, eliminating the prior deadlock risk while ensuring disposal cannot cross the lifetime boundary during first publication.
- `DisposeAsync` now obtains the non-cancellable disposal lease before disposing the browser and remaining composition-owned resources. Once disposal wins the gate, queued/future acquisitions fail closed; concurrent disposal remains idempotent.
- Updated reflection regression coverage requires the private `CompositionLifetimeGate`, rejects resurrection of `_browserGate`/`_disposed`, and continues to reject raw browser authority from public composition APIs.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The lifetime gate carries no browser/provider payloads or credentials and remains assembly-internal/private to the composition root.
- Acquisition leases protect only authority acquisition/publication and short session-store reads; they are released before returned product/agent facades perform normal long-running work. Browser startup may hold the lease, intentionally forcing shutdown to wait until startup either publishes a valid host or fails/cancels rather than disposing a half-created authority graph.
- Disposal is deliberately non-cancellable once it wins the lifetime gate, preventing half-disposed provider/browser authority. Browser disposal happens before provider/memory resources are torn down.
- Cancellation remains honored while waiting for acquisition and during browser startup. Failed/cancelled startup does not publish `_browser`; the lease is released by `await using`.
- Public APIs still return least-authority product/agent/recovery abstractions rather than the raw durable runtime, receipt store, verification publisher, or host.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- Returned browser product/goal/recovery facades can outlive their acquisition lease; disposal can therefore race with operations invoked later on those facades. Existing host/runtime cancellation and disposal behavior must be qualified explicitly rather than assuming composition acquisition synchronization covers operation lifetime.
- Browser startup is intentionally inside the lifetime lease, so shutdown waits for startup cancellation/completion. This is safer than half-disposal but needs executable timing qualification on Windows/Playwright.
- `MainWindow.Readiness` initializes the browser product when Judge Evidence is opened; this can incur Playwright startup latency. Any future cached/non-starting reader must preserve the same durable serialization boundary.
- Provider-live proof remains independently typed and freshness-checked; session milestone completion alone is insufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add deterministic operation-vs-root-disposal qualification for already-issued `BrowserProductRuntime`/goal/recovery facades. Verify that an operation started after root disposal fails closed and that an in-flight operation is cancelled or settles safely without stale VERIFIED evidence or use-after-dispose behavior. Prefer a narrow assembly-internal host/factory seam over launching Chromium; only change production lifetime ownership further if those tests expose a real gap. Then run the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
