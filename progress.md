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
`MainWindow.Readiness` now obtains browser verification only through `BrowserProductRuntime.ReadVerificationPresentationAsync`, the payload-free reader bound to the execution-owned `BrowserDurableActionRuntime`. The read therefore participates in the same serialization boundary as evidence-affecting browser transitions. Browser startup/read/protected-receipt failure is caught and fails closed to NOT VERIFIED rather than preventing provider/research/session evidence from opening. The dialog now receives this authoritative projection instead of silently omitting browser evidence.

## Latest run
Files changed:
- `src/Nvidea.Windows/MainWindow.Readiness.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected repository tree/current head, `MainWindow.Readiness`, `NvideaCompositionRoot`, and `BrowserProductRuntime` before mutation.
- Confirmed trusted composition exposes browser judge evidence through `BrowserProductRuntime.ReadVerificationPresentationAsync`; it does not expose the publisher, protected receipt store, durable evidence, raw browser host, approval authority, URL, locator, typed value, or browser payload.
- The Judge Evidence click path now awaits that authoritative read before constructing the dialog. Failure leaves the optional presentation null, which preserves fail-closed NOT VERIFIED behavior while keeping the rest of the judge surface usable.
- Existing durable-runtime race coverage remains the authority for serialization/invalidation behavior; this run did not weaken or bypass that boundary.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- New WPF integration consumes only `DesktopBrowserVerificationPresentation`, a Core-owned payload-free projection. No provider credentials, prompts, memory contents, source bodies, URLs, browser locators/typed values, raw durable checkpoints, protected receipts, or mutation authority cross into the dialog.
- Browser verification read is downstream of the product runtime's durable serialized reader; the UI does not read receipt files directly and cannot race around the transition gate.
- Missing Playwright/browser startup, corrupt evidence, cancellation, or read failure cannot produce green state. They degrade to an absent presentation/NOT VERIFIED.
- The judge dialog remains available when browser evidence is unavailable, avoiding a browser installation problem masking independent provider/research/session proof.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- `MainWindow.Readiness` now initializes the browser product when Judge Evidence is opened. This is correct for authoritative browser evidence but can incur Playwright startup latency; failure is fail-closed and non-fatal. A future cached/non-starting authoritative reader could improve UX only if it preserves the same durable serialization boundary.
- The canonical Core demo contract and checked-in demo manifest are enforced by separate regression tests but are not yet compared to each other from one shared assertion; future drift should be made impossible with a validator test comparing manifest order directly to `DemoRecordingContract.RequiredSequence`.
- Provider-live proof must remain independently typed and freshness-checked; session milestone completion alone is not sufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Tighten the demo-package validator regression so the checked-in manifest milestone order is compared directly with `DemoRecordingContract.RequiredSequence`, eliminating the last duplicated six-beat contract. Then add a narrow product/composition regression proving the authoritative browser presentation path exposed to Windows is VERIFIED only for a protected durable receipt and returns NOT VERIFIED after newer admission/cancellation/rearm/ambiguous reconciliation. Execute the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
