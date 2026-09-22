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
`BrowserDurableActionRuntime` owns one private `SemaphoreSlim` covering evidence-affecting transitions and judge reads. Payload-free internal lifecycle observers permit deterministic tests without production authority. Race coverage proves judge reads/new admissions cannot overtake settlement and genuine VERIFIED evidence is invalidated by newer admission. No stale green or half-settled judge state is allowed.

### 2026-09-22 — demo evidence, judge integration and build contracts
Bound demo validation to production `SessionEvidenceKind`, aligned manifest/runbook to the canonical six milestones, added payload-free `DemoRecordingGate` plus `DemoRecordingContract.RequiredSequence`, and wired WPF Judge Evidence to real session/browser evidence. Removed a stale duplicate validator test type, aligned the manifest directly to the production recording contract, and repaired browser API-surface contracts for the sanctioned verification reader.

### 2026-09-22 — composition lifetime hardening
Serialized canonical browser product publication, added composition-boundary regressions, introduced deterministic `CompositionLifetimeGate`, and integrated it into `NvideaCompositionRoot` as the single acquisition/disposal linearization authority. Cached host/product acquisition no longer bypasses shutdown synchronization; lazy startup and product publication occur under one lifetime lease; disposal is non-cancellable after winning the gate.

## Latest run — issued facade operation lifetime qualification
Files changed:
- Added `tests/Nvidea.Core.Tests/CompositionIssuedFacadeLifetimeTests.cs`.
- Updated `progress.md`.

Validation/evidence:
- Re-read `progress.md` completely, inspected current commits/tree, `NvideaCompositionRoot`, `BrowserProductRuntime`, `BrowserHostRuntime`, and existing lifetime tests before changing anything.
- Qualified the exact shared-gate pattern needed for already-issued facades without launching Chromium: an operation that acquires a composition lifetime lease before touching host authority keeps disposal behind it; an operation attempted after disposal fails closed before invoking its body; cancellation while waiting does not invoke the operation.
- This closes an important design uncertainty from the prior run: the existing `CompositionLifetimeGate` can safely protect operation lifetime as well as acquisition lifetime without a second semaphore or nested root acquisition.
- I intentionally did not wire the pattern into `BrowserProductRuntime`/goal/recovery facades in this run because that requires coordinated constructor/composition changes across the privileged host boundary. A partial edit was immediately restored to the exact prior blobs before the qualification test was committed; the final default branch retains the original host/product production files and therefore does not contain the transient incomplete edit.
- Repository identity was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- The qualified operation lease contains no provider/browser payload and adds no new persisted state, credentials, URLs, locators, prompts, or evidence content.
- The pattern fails closed before a stale facade can invoke its operation after root disposal has crossed the lifetime gate.
- An already-running operation holds the lease until settlement, so root disposal cannot tear down browser/provider authority underneath that operation; cancellation while waiting remains honored.
- The test deliberately uses a narrow fake issued facade over the real `CompositionLifetimeGate`, avoiding fake browser success claims and avoiding Chromium dependency.
- Production browser product/goal/recovery facades are not yet wired to acquire operation leases, so the identified use-after-dispose race remains a known production risk rather than being falsely marked fixed.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation; compile/test/validator PASS remains unverified in this connector environment.
- Already-issued `BrowserProductRuntime`, `BrowserGoalAgent`, and `BrowserAmbiguousRecoveryService` instances can currently outlive their root acquisition lease. Host `ThrowIfDisposed` rejects operations after host disposal, but disposal can still race an operation that passed that check and is awaiting Playwright/durable work.
- Wiring operation leases must avoid double-acquiring the non-reentrant composition gate while root factory methods already hold acquisition leases.
- Browser startup remains intentionally inside the lifetime lease, so shutdown waits for startup cancellation/completion; executable Windows timing qualification is still required.
- `MainWindow.Readiness` initializes browser product authority when Judge Evidence is opened, which can incur Playwright startup latency.
- Provider-live proof remains independently typed/freshness-checked; session milestone completion alone is insufficient to claim live provider readiness.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Integrate the now-qualified shared `CompositionLifetimeGate` into already-issued browser facades at the operation boundary. Start with `BrowserProductRuntime`: trusted composition should inject the root gate, every public product operation should acquire one lease before touching host/durable authority, and root disposal should wait for in-flight product work while stale product calls fail closed. Then extend the same least-authority lifetime wrapper to goal-agent and ambiguous-recovery operations without nested gate acquisition. Add deterministic post-disposal/in-flight regressions first; run the full .NET/Windows/Chromium qualification suite in the first capable environment and fix findings without weakening authority boundaries.
