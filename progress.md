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
Removed a stale duplicate demo-validator test type that was a likely C# compile blocker and encoded obsolete milestone names. Added manifest-to-production recording-contract alignment coverage. Repaired `NvideaCompositionRootBrowserApiSurfaceTests` after the sanctioned payload-free browser verification reader was added: the product API now explicitly allows that read while rejecting raw host/durable-runtime/receipt-store/publisher authority in its signature.

### 2026-09-22 — canonical browser product publication hardening
Found a real first-call concurrency race in `NvideaCompositionRoot.GetBrowserProductAsync`: host creation was serialized, but `_browserProduct ??= host.CreateProductRuntime()` occurred after the host gate was released, so simultaneous first callers could construct and receive different product facade instances. Product publication is now serialized by the same composition `_browserGate`, rechecks disposal while holding the gate, and publishes exactly one canonical least-authority product facade. This also closes the race where disposal could occur between host acquisition and product publication.

## Latest run
Files changed:
- Updated `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`.
- Updated `progress.md`.

Validation/evidence:
- Re-read `progress.md` completely, inspected recent commits, the Core browser API-surface contract, `NvideaCompositionRoot`, `BrowserHostRuntime.CreateProductRuntime`, and the execution-owned durable verification binding before mutation.
- Identified that `GetBrowserHostAsync` serialized only host creation; after it returned, concurrent first callers executed `_browserProduct ??= host.CreateProductRuntime()` without synchronization. `??=` is not an atomic singleton-publication primitive, so more than one caller could receive a distinct facade.
- Hardened publication with `_browserGate`: after obtaining the canonical host, callers acquire the composition gate, recheck `_disposed`, and only then publish/read `_browserProduct`. The product still wraps the same execution-owned durable runtime and no raw browser authority is exposed.
- The disposal interleaving is now fail-closed: if disposal wins the gate after host acquisition, the waiting product request observes `_disposed` and throws instead of constructing a facade over a disposed host.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The change narrows authority publication rather than expanding it: all Windows/judge callers converge on one product facade backed by the trusted host's exact durable runtime.
- No credentials, prompts, browser URLs/locators/typed values, memory contents, protected receipts, or provider payloads were exposed.
- Cancellation while waiting for product publication propagates normally. Disposal racing publication fails closed under the same gate.
- Browser evidence serialization inside `BrowserDurableActionRuntime` remains unchanged; this composition gate only protects host/product lifetime and canonical facade publication.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- `MainWindow.Readiness` initializes the browser product when Judge Evidence is opened. This preserves authoritative browser evidence but can incur Playwright startup latency; failure is fail-closed and non-fatal. A cached/non-starting authoritative reader is only acceptable if it preserves the same durable serialization boundary.
- Provider-live proof must remain independently typed and freshness-checked; session milestone completion alone is not sufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add deterministic composition-level concurrency coverage for canonical `GetBrowserProductAsync` publication without requiring real Chromium (likely via a narrow internal browser-host/product factory seam), including simultaneous first callers and disposal-vs-publication. Keep the seam assembly-internal and incapable of exposing raw browser authority. Then run the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
