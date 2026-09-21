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
Added internal `BrowserDurableActionRuntime`, pairing durable orchestration with verification lifecycle. It owns Windows-DPAPI verification composition, exact approval resume + execution, rearm, cancellation, and explicitly non-verifying ambiguous crash reconciliation. Rearm/cancel/ambiguous reconciliation clear protected evidence first. A private `SemaphoreSlim` linearizes durable/evidence mutations and payload-free reads across jobs; verification is also cleared before every execution attempt.

Production `BrowserHostRuntime` now owns one execution-scoped durable runtime and routes durable create, ordinary execution, approval-resume execution, rearm, cancellation, and ambiguous completion through it. `NvideaCompositionRoot.GetBrowserProductAsync` uses `host.CreateProductRuntime()`, binding WPF/product verification to the same protected receipt lifecycle. Legacy verification-disconnected product constructors were removed.

### 2026-09-21 — production browser evidence integration coverage
Real-Chromium production-host coverage proves consequential actions remain NOT VERIFIED before approval, become VERIFIED only after exact-scope approval + real mutation + typed postcondition + durable completion, and cannot replay. Additional coverage proves a prior green receipt is invalidated on newer action admission and remains absent through approval rearm and cancellation without extra browser mutation.

Crash-ambiguous reconciliation coverage establishes a genuine green receipt, reproduces a persisted Running crash shape via the trusted job-store test seam, reconciles from fresh observed expected-state evidence, and requires durable completion while judge verification remains NOT VERIFIED and the controlled mutation count remains unchanged. Ambiguous recovery can therefore restore workflow progress without minting replacement green evidence or retaining an unrelated stale receipt.

### 2026-09-21 — serialization boundary regression guard
Added Core-level structural regression tests for `BrowserDurableActionRuntime`'s cross-job serialization boundary. The tests require the transition gate to remain private, instance-scoped `SemaphoreSlim` state; require every receipt-mutating durable operation plus the judge-facing verification read to remain on the same internal runtime surface; and forbid public instance methods declared by the runtime. This protects the least-authority composition and makes accidental exposure/removal of the serialization boundary fail loudly in the test suite.

### 2026-09-22 — behavioral serialization regression coverage
Added dependency-free behavioral coverage for the private transition serialization helper. A deliberately paused first transition must prevent a competing transition from entering until settlement, with deterministic ordering asserted after release. A second scenario cancels a waiter while the gate is held and requires that cancelled waiter never enter the critical section. The test seam constructs no orchestrator, browser, DPAPI, provider, receipt store, or approval authority; it invokes only the private synchronization helper and installs only its private semaphore on an uninitialized test instance.

## Latest run
Files changed:
- `tests/Nvidea.Core.Tests/BrowserDurableActionRuntimeConcurrencyTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, current repository tree, `BrowserDurableActionRuntime`, and existing concurrency regression tests before implementation.
- Confirmed the runtime's single private `_transitionGate` still covers create/admission, ordinary execution, approval-resume execution, rearm, cancellation, ambiguous reconciliation, and payload-free verification reads.
- Added a deterministic behavioral test that pauses one serialized transition, starts a competitor, proves the competitor has not entered, releases the first transition, then requires exact `first-enter -> first-exit -> second-enter` ordering.
- Added cancellation coverage proving a waiter cancelled while another transition owns the gate never enters the critical section and does not disturb the current owner.
- The behavioral seam intentionally avoids constructing browser/DPAPI/job/provider authority and therefore cannot mutate product state or leak payloads.
- Repository metadata was explicitly reverified immediately before each GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment still cannot execute .NET 8, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- Serialization remains execution-owned rather than product-owned; UI receives only the narrower product runtime and payload-free presentation.
- Structural coverage prevents the semaphore from becoming static/public or durable/evidence mutation authority from becoming public product API; behavioral coverage now also guards mutual exclusion and cancelled-waiter semantics.
- Existing clear-before-execution, completed-side-effect/no-replay behavior, fail-closed receipt handling, cancellation semantics, and ambiguous-recovery non-verification remain unchanged.
- No URL, locator, typed value, approval token, page content, secret, or protected receipt material is introduced by the test seam.
- The test uses reflection only inside the friend test assembly to invoke the private serialization helper on a dependency-free instance; production code and production authority surfaces are unchanged.

## Known blockers / risks
- New Core/test changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- The behavioral test proves the actual private semaphore helper blocks competitors and cancelled waiters, while the structural test proves all evidence-affecting/read operations remain on that runtime surface. A full behavioral production-path race test with a pause specifically between receipt clear and publication remains desirable once an injectable test-only lifecycle seam can be introduced without weakening authority.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Introduce the narrowest internal/test-only verification-lifecycle observation hook that can pause after protected evidence is cleared but before execution/publication, then add a production-runtime behavioral race test proving a judge read and a newer competing admission both remain blocked until settlement and that final presentation belongs to the newest admitted transition. The hook must carry no receipt/browser payload and must be inert/absent in production composition. Then run the full .NET suite in the first capable environment and fix compile/runtime findings without restoring weaker composition paths.
