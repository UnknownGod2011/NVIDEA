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

### 2026-09-22 — validator compile blocker and manifest/production contract alignment
Repository inspection found two source files in `tests/Nvidea.DemoPackageValidator.Tests` both declaring the same `DemoPackageValidatorTests` type in the same namespace. The stale copy also still encoded four obsolete session milestone names, so it was both a likely C# duplicate-type compile blocker and a source of contract drift. Removed only that obsolete duplicate source; retained the newer production-enum-aware suite. Added `DemoRecordingContractAlignmentTests`, which reads the checked-in `docs/demo-package.json` and compares its flattened milestone sequence directly to `DemoRecordingContract.RequiredSequence`, eliminating the last independent six-beat assertion as an authority for manifest alignment.

## Latest run
Files changed:
- Removed stale `tests/Nvidea.DemoPackageValidator.Tests/DemoPackageValidatorTests.cs` (obsolete duplicate test type; newer test source retained).
- Added `tests/Nvidea.DemoPackageValidator.Tests/DemoRecordingContractAlignmentTests.cs`.
- Updated `progress.md`.

Validation/evidence:
- Re-read `progress.md` completely and inspected current head, test project contents, production `DemoRecordingGate`, and `DemoRecordingContract` before mutation.
- Confirmed the test directory contained two compile-included `.cs` files declaring the same fully-qualified `Nvidea.DemoPackageValidator.Tests.DemoPackageValidatorTests` class. The removed file used obsolete milestones (`MemoryInfluencedResponse`, `TavilyValidatedCitationUsed`, `BrowserVerifiedGoalCompleted`, `ConsequentialApprovalGranted`); the retained suite uses current production enum names and checks the checked-in manifest.
- Added a narrow repository-artifact regression that derives expected names from `DemoRecordingContract.RequiredSequence` and compares them exactly, in order, to every checked-in manifest `expectedSessionMilestones` entry.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- This run changes tests/docs only; no runtime authority, credentials, prompts, memory contents, browser payloads, protected receipts, or provider calls were added or exposed.
- The new alignment test reads only the checked-in public demo manifest and production enum contract; it cannot create evidence or grant recording readiness.
- Removing the stale duplicate does not remove unique coverage: its scenarios are represented in the retained newer suite, which additionally uses production enum names and repository-artifact checks.
- Manifest drift now fails the regression whenever its flattened production milestone sequence differs from the Windows recording gate's canonical contract.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- `MainWindow.Readiness` initializes the browser product when Judge Evidence is opened. This is correct for authoritative browser evidence but can incur Playwright startup latency; failure is fail-closed and non-fatal. A future cached/non-starting authoritative reader could improve UX only if it preserves the same durable serialization boundary.
- Provider-live proof must remain independently typed and freshness-checked; session milestone completion alone is not sufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add a narrow product/composition regression proving the authoritative browser presentation path exposed to Windows is VERIFIED only for a protected durable receipt and returns NOT VERIFIED after newer admission, cancellation/rearm, and ambiguous reconciliation. Then execute the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
