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

## Latest run
Files changed:
- `tests/Nvidea.Core.Tests/BrowserDurableActionRuntimeConcurrencyTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, recent commits, repository tree, `BrowserDurableActionRuntime`, and existing verification lifecycle tests before implementation.
- Confirmed the runtime's single private `_transitionGate` currently covers create/admission, ordinary execution, approval-resume execution, rearm, cancellation, ambiguous reconciliation, and payload-free verification reads.
- Added deterministic structural tests that do not require Chromium or DPAPI and therefore complement the existing opt-in real-browser scenarios.
- The new tests assert the gate is private/non-static, the serialization helper remains private generic instance state, all evidence-affecting/read operations remain non-public instance operations, and the durable runtime declares no public product-facing instance API.
- Repository metadata was explicitly reverified immediately before each GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment still cannot execute .NET 8, so compile/test PASS is not claimed. No live/paid Nebius, Tavily, browser, Object Storage, Serverless, or inference operation was triggered.

## Security / privacy / failure review
- Serialization remains execution-owned rather than product-owned; UI receives only the narrower product runtime and payload-free presentation.
- The regression guard deliberately tests architecture, not timing assumptions: it prevents a future refactor from making the semaphore static/public or exposing durable/evidence mutation authority as public product API.
- Existing clear-before-execution, completed-side-effect/no-replay behavior, fail-closed receipt handling, cancellation semantics, and ambiguous-recovery non-verification remain unchanged.
- No URL, locator, typed value, approval token, page content, secret, or protected receipt material is added to the new test surface.

## Known blockers / risks
- New Core/test changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- A behavioral cross-job concurrency test is still desirable to prove a blocked judge read cannot sample the transient clear state while another transition is in-flight. The structural guard added here prevents architectural drift but does not itself execute the semaphore timing path.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add a deterministic behavioral concurrency seam/test around `BrowserDurableActionRuntime` that pauses a transition after protected-evidence clear but before settlement, starts a judge read plus a competing transition, and proves both remain blocked until the first transition settles and that the final presentation belongs to the latest admitted transition. Keep the seam internal/test-only and do not weaken receipt or browser authority. Then run the full .NET suite in the first capable environment and fix compile/runtime findings without restoring weaker composition paths.
