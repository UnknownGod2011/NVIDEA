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
`BrowserDurableActionRuntime` owns one private instance `SemaphoreSlim` covering evidence-affecting transitions and judge reads. Payload-free internal lifecycle observers permit deterministic tests without production authority. Race coverage pauses after receipt invalidation, proves judge reads/new admissions cannot overtake settlement, then proves a genuine VERIFIED receipt from job A is invalidated by newer job B admission. No stale green or half-settled judge state is allowed.

### 2026-09-22 — demo evidence contract and recording gate
Bound `Nvidea.DemoPackageValidator` and tests to production `SessionEvidenceKind`; manifest/runbook use the canonical six milestones. Added payload-free `DemoRecordingGate` and build-time `DemoRecordingContract.RequiredSequence`; WPF Judge Evidence renders explicit READY/NOT READY from the real session snapshot and resets fail closed. Provider-live readiness remains independently typed.

### 2026-09-22 — authoritative browser evidence reaches Judge Evidence UI
`MainWindow.Readiness` obtains browser verification only through `BrowserProductRuntime.ReadVerificationPresentationAsync`, the payload-free reader bound to the execution-owned `BrowserDurableActionRuntime`. The read participates in the same serialization boundary as evidence-affecting browser transitions. Browser startup/read/protected-receipt failure is caught and fails closed to NOT VERIFIED rather than preventing provider/research/session evidence from opening.

### 2026-09-22 — validator/build contract cleanup
Removed a stale duplicate demo-validator test type that was a likely C# compile blocker and still encoded obsolete milestone names. Added repository-artifact alignment coverage comparing the checked-in demo manifest directly to `DemoRecordingContract.RequiredSequence`.

### 2026-09-22 — browser product API regression repaired
Repository review found `NvideaCompositionRootBrowserApiSurfaceTests` had not been updated when `BrowserProductRuntime.ReadVerificationPresentationAsync` became the sanctioned judge-facing read. Its allow-list therefore rejected the new production method, creating a deterministic test failure in the Core suite. Updated the contract test to explicitly allow that method, require its return path to contain only `DesktopBrowserVerificationPresentation`, and reject raw `BrowserHostRuntime`, `BrowserDurableActionRuntime`, receipt-store, or publisher authority in its parameters. Existing real-Chromium host integration already exercises the product reader through verified publication, newer admission/rearm/cancellation invalidation, and ambiguous recovery.

## Latest run
Files changed:
- Updated `tests/Nvidea.Core.Tests/NvideaCompositionRootBrowserApiSurfaceTests.cs`.
- Updated `progress.md`.

Validation/evidence:
- Re-read `progress.md` completely and inspected the current repository tree, `BrowserProductRuntime`, composition API-surface tests, and real-Chromium verification-invalidation integration tests before mutation.
- Found a concrete deterministic regression: `BrowserProductRuntime` publicly declares `ReadVerificationPresentationAsync`, while the API-surface test asserted every declared public method belonged to an allow-list that omitted it.
- Repaired the test contract rather than weakening production: the authoritative payload-free read is now explicitly sanctioned, while raw host/durable-runtime/receipt-store/publisher authority remains prohibited from the product read signature.
- Confirmed existing `BrowserHostVerificationInvalidationIntegrationTests` already read verification through `BrowserProductRuntime` and cover genuine VERIFIED publication followed by newer admission, rearm, cancellation, and ambiguous reconciliation invalidation, so no redundant second integration harness was added.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- Production runtime code is unchanged. The test now documents the intended least-authority boundary instead of incorrectly rejecting it.
- Judge-facing browser verification remains payload-free and cannot accept the raw host, durable action runtime, protected receipt store, or publisher as call-time authority.
- No credentials, prompts, browser URLs/locators/typed values, memory contents, protected receipts, or provider payloads were exposed.
- Existing fail-closed behavior remains: newer browser work, cancellation/rearm, and ambiguous recovery cannot leave stale green verification visible through the product reader.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- `MainWindow.Readiness` initializes the browser product when Judge Evidence is opened. This preserves authoritative browser evidence but can incur Playwright startup latency; failure is fail-closed and non-fatal. A cached/non-starting authoritative reader is only acceptable if it preserves the same durable serialization boundary.
- Provider-live proof must remain independently typed and freshness-checked; session milestone completion alone is not sufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Inspect the complete Core test surface for other deterministic contract drift introduced by the recent judge-evidence APIs, then add a narrow composition-level regression proving `NvideaCompositionRoot.GetBrowserProductAsync()` returns the same least-authority product reader used by Windows without exposing raw browser authority. After that, execute the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
