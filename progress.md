# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, browser-verification state, production-observed session milestones, and a fail-closed recording gate.

## Persistent history
### 2026-09-06 to 2026-09-17 — product foundation
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-21 — security, browser and durable evidence hardening
Hardened browser transport, redirects/WebSockets, Service Worker blocking, page admission, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart behavior, emergency stop and real-Chromium qualification tooling. Persisted memory embeddings are untrusted. Startup/shutdown gained cancellation-safe cleanup. Tavily research gained payload-free SHA-256 lineage and restart-stable durable receipts. Browser judge evidence gained historical approval evidence, canonical commitments, protected atomic receipts, durable host/product facades and production-host coverage for approval, mutation, post-state verification, stale-receipt invalidation, cancellation/rearm and crash-ambiguous reconciliation.

### 2026-09-21 to 2026-09-22 — browser serialization/race qualification
`BrowserDurableActionRuntime` owns one private instance `SemaphoreSlim` covering evidence-affecting transitions and judge reads. Payload-free internal lifecycle observers permit deterministic tests without production authority. Race coverage pauses after receipt invalidation, proves judge reads/new admissions cannot overtake settlement, then proves a genuine VERIFIED receipt from job A is invalidated by newer job B admission. No stale green or half-settled judge state is allowed.

### 2026-09-22 — demo evidence contract alignment
Bound `Nvidea.DemoPackageValidator` and its tests to production `SessionEvidenceKind`, replacing obsolete friendly names. `docs/demo-package.json` and the operator runbook use the six production milestones: `NemotronInferenceCompleted`, `MemoryInfluencedInvocation`, `TavilyResearchCompletedWithCitations`, `BrowserPostStateVerified`, `ConsequentialApprovalGateExercised`, and `NebiusBackgroundExecutionObserved`. Regression coverage reads the checked-in manifest, requires canonical case-sensitive enum names and exact six-beat ordering, and projects them through `SessionEvidenceSnapshot`.

### 2026-09-22 — fail-closed recording evidence gate
Added Core `DemoRecordingGate`, a payload-free production-evidence gate. It fails closed for an empty contract, undefined/duplicate contract kinds, missing production milestones, or milestones observed out of contract order; only the complete ordered production sequence opens the gate. Core tests cover success and failure cases. Synthetic evaluator metadata cannot satisfy the gate because it consumes only `SessionEvidenceSnapshot` entries.

### 2026-09-22 — Windows recording gate integration
Added `DemoRecordingContract.RequiredSequence` as the build-time canonical six-beat production contract, avoiding runtime trust in mutable documentation. Wired `JudgeEvidenceDialog` to evaluate the live `SessionEvidenceSnapshot` on open and after session reset and render explicit `READY TO RECORD` / `NOT READY TO RECORD` state, including payload-free missing milestone labels and out-of-order reason. Provider readiness remains visibly separate and the UI explicitly states that recording readiness does not imply provider-live readiness. Added Core regression coverage locking completeness, uniqueness and ordering of the canonical sequence.

## Latest run
Files changed:
- `src/Nvidea.Core/Desktop/DemoRecordingContract.cs`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml`
- `tests/Nvidea.Core.Tests/DemoRecordingContractTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely; inspected recent commits, `DemoRecordingGate`, WPF judge evidence UI, readiness composition, and current test layout before mutation.
- Canonical contract uses the same six production enum values already enforced by the checked-in demo-package regression tests.
- Judge UI now evaluates the real production snapshot through Core `DemoRecordingGate`; no synthetic evaluator evidence path was added.
- New session reset immediately re-evaluates the gate, so cleared evidence cannot leave a stale green recording status.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The canonical contract and gate are payload-free; the WPF projection sees only enum kinds, first-observed timestamps, and gate reasons/missing kinds. It adds no provider credentials, prompts, memory contents, sources, URLs, browser locators/typed values, persistence, or network authority.
- Evaluation remains fail-closed. Session reset cannot preserve a stale READY state because both evidence text and gate result are rebuilt from the post-reset snapshot.
- Runtime does not parse `docs/demo-package.json`; mutable documentation therefore cannot change production recording authority. Existing validator regression coverage is responsible for keeping the checked-in manifest aligned with production enum names/order.
- Provider-live readiness remains independently displayed and is explicitly not implied by `READY TO RECORD`.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- The canonical Core contract and checked-in demo manifest are enforced by separate regression tests but are not yet compared to each other from one shared assertion; future drift should be made impossible with a validator test comparing manifest order directly to `DemoRecordingContract.RequiredSequence`.
- The judge dialog receives browser verification only when its caller supplies it; current `MainWindow.Readiness` construction still omits that optional presentation, so browser receipt projection in this dialog can remain NOT VERIFIED even when separate browser evidence exists. This is now a higher-value integration gap than further gate UI work.
- Provider-live proof must remain independently typed and freshness-checked; session milestone completion alone is not sufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Wire the authoritative `DesktopBrowserVerificationPresentation` into the `JudgeEvidenceDialog` call path from the existing browser durable runtime without exposing browser payloads or bypassing its serialized judge-read boundary. Add integration/regression coverage proving the dialog gets VERIFIED only from the protected receipt projection and fails closed after newer admission/cancellation/rearm/ambiguous reconciliation. Also tighten the demo-package validator regression to compare manifest milestone order directly with `DemoRecordingContract.RequiredSequence`. Then execute the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
