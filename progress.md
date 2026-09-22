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
Added internal `BrowserDurableActionRuntime`, pairing durable orchestration with verification lifecycle. It owns Windows-DPAPI verification composition, exact approval resume + execution, rearm, cancellation, and explicitly non-verifying ambiguous crash reconciliation. Rearm/cancel/ambiguous reconciliation clear protected evidence first. A private `SemaphoreSlim` linearizes durable/evidence mutations and payload-free reads across jobs; verification is also cleared before every execution attempt. Production `BrowserHostRuntime` owns one execution-scoped durable runtime and `NvideaCompositionRoot.GetBrowserProductAsync` binds WPF/product verification to that same protected receipt lifecycle. Legacy verification-disconnected product constructors were removed.

### 2026-09-21 — production browser evidence integration coverage
Real-Chromium production-host coverage proves consequential actions remain NOT VERIFIED before approval, become VERIFIED only after exact-scope approval + real mutation + typed postcondition + durable completion, and cannot replay. Additional coverage proves a prior green receipt is invalidated on newer action admission and remains absent through approval rearm and cancellation without extra browser mutation. Crash-ambiguous reconciliation coverage establishes a genuine green receipt, reproduces a persisted Running crash shape, reconciles from fresh expected-state evidence, and requires durable completion while judge verification remains NOT VERIFIED and mutation count remains unchanged.

### 2026-09-21 to 2026-09-22 — serialization and lifecycle race hardening
Added structural and behavioral Core coverage for `BrowserDurableActionRuntime`'s cross-job serialization boundary. The private instance `SemaphoreSlim` covers receipt-mutating operations and judge reads. A paused transition blocks competitors until settlement, and a cancelled waiter never enters the critical section. Added internal payload-free `IBrowserVerificationLifecycleObserver` stages for admission/execution evidence clear; production composition installs no observer. `CreateForTesting` injects it only into the exact verification lifecycle protected by the transition gate. Tests can pause after protected receipt invalidation without receiving job, receipt, URL, locator, approval, typed value, browser handle, publisher or semaphore authority.

### 2026-09-22 — exact clear/execute/publish/new-admission race qualification
Upgraded `BrowserDurableActionRuntimeLifecycleRaceTests` from a non-browser completing fixture to a production-valid payload-free browser terminal checkpoint shape accepted by `DurableBrowserVerificationPublisher`. Job A pauses exactly after execution evidence clear while holding the runtime transition gate. A queued judge read and newer job-B admission must both remain blocked and B must remain absent from durable storage. After release, A completes exactly once and publishes VERIFIED evidence tied to A's durable JobId; the already-queued judge read observes that settled green receipt. B then enters the same gate, clears A's receipt before durable admission, and a final judge read is NOT VERIFIED with zero actions. This proves neither a half-settled read nor stale-green carryover across newer admission.

### 2026-09-22 — demo evidence contract bound to production enum
Found a judging-readiness defect: `docs/demo-package.json` and `Nvidea.DemoPackageValidator` used friendly milestone names that did not exist in production `SessionEvidenceKind`, despite the runbook claiming exact names. Bound the validator project directly to `Nvidea.Core` and changed its milestone table to `nameof(SessionEvidenceKind...)`, so future production enum renames become compile-time/demo-validation failures instead of silently validating fictional evidence names. Updated the machine-checkable manifest to the six actual production names: `NemotronInferenceCompleted`, `MemoryInfluencedInvocation`, `TavilyResearchCompletedWithCitations`, `BrowserPostStateVerified`, `ConsequentialApprovalGateExercised`, and `NebiusBackgroundExecutionObserved`.

### 2026-09-22 — operator runbook aligned to production evidence
Aligned `docs/judge-demo-runbook.md` to the same six production `SessionEvidenceKind` values used by the machine-checkable manifest and validator. Removed the four stale friendly labels from per-beat judge proof and the final expected sequence. Reviewed `JudgeEvidenceDialog.xaml.cs`; its human-readable WPF labels already switch directly on the correct production enum values, so no WPF mutation was necessary.

## Latest run
Files changed:
- `docs/judge-demo-runbook.md`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the latest repository commits before changing anything.
- Confirmed the runbook still contradicted its own exact-production-name claim in four places plus the final sequence.
- Replaced those names with `MemoryInfluencedInvocation`, `TavilyResearchCompletedWithCitations`, `BrowserPostStateVerified`, and `ConsequentialApprovalGateExercised`, preserving the existing fail-closed claim boundaries and clarifying the Tavily/browser wording to match what those production milestones actually prove.
- Inspected `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs`; its presentation labels already map directly from all six current production enum members and require no correction.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment still cannot execute .NET 8, so compile/test/validator PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- Documentation-only runbook changes add no runtime authority, provider calls, browser access, credentials, private payloads, persistence, or CI load.
- The operator instructions remain fail-closed: missing live evidence means the beat failed, synthetic evidence cannot be upgraded to live proof, and login/CAPTCHA/MFA safeguards may not be bypassed.
- The WPF evidence view remains payload-free and projects human-readable labels from the production enum rather than accepting arbitrary milestone strings.

## Known blockers / risks
- The validator and accumulated suite require executable .NET 8 validation; compile/test/validator PASS remains unverified in this connector environment.
- The manifest, validator and operator runbook are now name-aligned, but there is not yet a regression test that loads the checked-in `docs/demo-package.json` and proves every declared milestone parses as `SessionEvidenceKind` and matches a projected six-beat `SessionEvidenceSnapshot`.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add a regression test that loads the checked-in `docs/demo-package.json`, parses every declared milestone as a production `SessionEvidenceKind`, and proves the expected six-beat sequence can be projected from a `SessionEvidenceSnapshot` without duplicated friendly-name mappings. Then run the demo validator and full .NET/Windows/Chromium qualification suite in the first capable environment and fix any compile/runtime findings without weakening authority boundaries.
