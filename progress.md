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

### 2026-09-17 to 2026-09-21 — security, browser and durable evidence hardening
Hardened browser transport, redirects/WebSockets, Service Worker blocking, page admission, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart behavior, emergency stop and real-Chromium qualification tooling. Persisted memory embeddings are untrusted. Startup/shutdown gained cancellation-safe cleanup. Tavily research gained payload-free SHA-256 lineage and restart-stable durable receipts. Browser judge evidence gained historical approval evidence, canonical commitments, protected atomic receipts, durable host/product facades and production-host coverage for approval, mutation, post-state verification, stale-receipt invalidation, cancellation/rearm and crash-ambiguous reconciliation.

### 2026-09-21 to 2026-09-22 — browser serialization/race qualification
`BrowserDurableActionRuntime` owns one private instance `SemaphoreSlim` covering evidence-affecting transitions and judge reads. Payload-free internal lifecycle observers permit deterministic tests without production authority. Race coverage pauses after receipt invalidation, proves judge reads/new admissions cannot overtake settlement, then proves a genuine VERIFIED receipt from job A is invalidated by newer job B admission. No stale green or half-settled judge state is allowed.

### 2026-09-22 — demo evidence contract alignment
Bound `Nvidea.DemoPackageValidator` and its tests to production `SessionEvidenceKind`, replacing obsolete friendly names. `docs/demo-package.json` and the operator runbook now use the six production milestones: `NemotronInferenceCompleted`, `MemoryInfluencedInvocation`, `TavilyResearchCompletedWithCitations`, `BrowserPostStateVerified`, `ConsequentialApprovalGateExercised`, and `NebiusBackgroundExecutionObserved`. Regression coverage reads the checked-in manifest, requires canonical case-sensitive enum names and exact six-beat ordering, and projects them through `SessionEvidenceSnapshot`.

### 2026-09-22 — fail-closed recording evidence gate
Added Core `DemoRecordingGate`, a payload-free production-evidence gate intended for the pre-recording judge path. It accepts only a fresh `SessionEvidenceSnapshot` plus the validated required milestone sequence. It fails closed for an empty contract, undefined/duplicate contract kinds, missing production milestones, or milestones observed out of contract order; only the complete ordered production sequence opens the gate. Added Core tests for success, missing evidence, out-of-order evidence and invalid contracts. This prevents synthetic evaluator metadata from satisfying production session proof because the gate consumes only `SessionEvidenceSnapshot` entries.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/DemoRecordingGate.cs`
- `tests/Nvidea.Core.Tests/DemoRecordingGateTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely; inspected recent commits, Windows readiness/judge evidence surfaces, and production `SessionEvidenceLedger` before changing anything.
- Added a fail-closed Core gate over the real production snapshot type rather than inventing a second evidence ledger.
- Added deterministic unit coverage for complete ordered evidence, a missing browser milestone, complete-but-out-of-order evidence, empty contract and duplicate contract.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The gate is payload-free: it sees only enum kinds and first-observed timestamps already present in `SessionEvidenceSnapshot`; it adds no provider credentials, browser authority, private payloads, persistence or network access.
- Evaluation is fail-closed. Missing, malformed, duplicate or out-of-order requirements cannot become a green recording decision.
- The gate deliberately does not accept synthetic evaluator evidence or provider-live claims as substitutes for production session milestones.
- Provider-live freshness/typing remains a separate concern and must not be collapsed into this session gate.

## Known blockers / risks
- New Core code and accumulated suite still require executable .NET 8 validation; compile/test/validator PASS remains unverified in this connector environment.
- `DemoRecordingGate` is implemented and tested but is not yet wired into the WPF judge evidence/recording UX, and the Windows host does not yet load the validated checked-in manifest sequence into that gate.
- Provider-live proof must remain independently typed and freshness-checked; session milestone completion alone is not sufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Wire `DemoRecordingGate` into the WPF judge evidence/pre-recording surface using the validated `docs/demo-package.json` milestone sequence (or a build-produced canonical contract derived from it), display a clear fail-closed READY/NOT READY result with missing/out-of-order reasons, and keep provider-live proof independently typed/freshness-checked. Add Windows/Core integration tests so synthetic evaluator evidence cannot open the recording gate. Then execute the validator and full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
