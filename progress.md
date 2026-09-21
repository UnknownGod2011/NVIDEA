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
Real-Chromium production-host coverage now proves consequential actions remain NOT VERIFIED before approval, become VERIFIED only after exact-scope approval + real mutation + typed postcondition + durable completion, and cannot replay. Additional coverage proves a prior green receipt is invalidated on newer action admission and remains absent through approval rearm and cancellation without extra browser mutation.

Added crash-ambiguous reconciliation coverage through the production host/product composition. The test establishes a genuine green receipt, reproduces a persisted Running crash shape via the trusted job-store test seam, reconciles from fresh observed expected-state evidence, and requires durable completion while judge verification remains NOT VERIFIED and the controlled mutation count remains unchanged. This proves ambiguous recovery can restore workflow progress without minting replacement green evidence or retaining an unrelated stale receipt.

## Latest run
Files changed:
- `tests/Nvidea.Core.Tests/BrowserHostVerificationInvalidationIntegrationTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, recent commits, `BrowserHostRuntime`, `BrowserDurableActionRuntime`, `BrowserVerificationRuntime`, ambiguous recovery tests and durable job storage before implementation.
- Added an opt-in real-Chromium production-host scenario using `runtime.CreateProductRuntime()` and the actual protected receipt path.
- Scenario establishes genuine VERIFIED evidence through exact approval and one controlled mutation, then creates a second browser job and uses the internal durable-store test seam to reproduce a process-crash `Running` record.
- Fresh browser observation already contains the second action's deterministic expected state; production `TryReconcileAmbiguousAsync` therefore completes the durable job without replaying the browser mutation.
- Assertions require the reconciled durable record to be Completed with a verified-step checkpoint while product/judge evidence is NOT VERIFIED with zero action/approval counts; controlled mutation count remains exactly one.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed. The test remains opt-in under the existing `BrowserIntegrationFact` gate.
- No live/paid Nebius, Object Storage, Serverless, Tavily, browser or inference operation was triggered.

## Security / privacy / failure review
- Ambiguous crash reconciliation is intentionally weaker evidence than normal last-mile execution: it can restore durable workflow state from deterministic post-state but cannot create judge-green evidence.
- The trusted-store seam exists only in Core tests to reproduce persisted crash state; product composition still cannot manufacture arbitrary Running records or access receipt publication authority.
- Reconciliation does not replay the side effect; the local controlled site's mutation counter explicitly guards that invariant.
- Product evidence remains payload-free and contains no approval tokens, locators, URLs, typed values, page content or protected receipt material.
- Existing clear-before-execution, serialized durable/evidence transitions, completed-side-effect/no-replay behavior, and fail-closed protected receipt handling remain unchanged.

## Known blockers / risks
- New Core/test changes require executable .NET 8 validation; accumulated Windows/Chromium suites remain pending environment validation.
- Production-host integration coverage is still needed for cross-job serialization and settled verification reads under concurrent transitions. Ambiguous reconciliation, cancellation and rearm invalidation are now covered structurally but remain unexecuted here.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add a deterministic cross-job concurrency/settled-read test proving the execution-owned `BrowserDurableActionRuntime` serializes competing clear/execute/publish transitions and blocks judge reads until the winning transition is settled. Then run the full .NET suite in the first capable environment and fix any compile/runtime findings without restoring weaker composition paths.
