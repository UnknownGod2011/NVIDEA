# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Dispatch-binding V2 signs authoritative remote id + canonical SHA-256 of the exact encrypted work-item envelope; worker verifies and pins the envelope before execution.
- Protected local CAS state supports durable binding-publication, audit and protected-payload-cleanup obligations with independent restart recovery.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-12
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment preflight and judging/evaluator tooling.

### 2026-09-13 to 2026-09-15
Hardened exact-once browser behavior and remote dispatch: durable external-action/cleanup/audit intents, exact remote provenance, crash-resumable cancellation, envelope commitment, V2 sender authenticity, pinned-envelope worker execution, bounded worker transport retry/SIGTERM, atomic reservation + audit + digest CAS, provider-delivery ambiguity reconciliation, shared reservation trust validation and final pre-Create durable authority revalidation.

### 2026-09-15 to 2026-09-16 — durability composition
Added durable V2 binding obligations, restart reconciliation, result/audit/cleanup crash recovery, ambiguous-delete handling, bounded cleanup CAS convergence, multi-artifact cleanup races, and a composed production-path regression covering V2 publication + result CAS/audit + encrypted deletion + concurrent terminal progress + final binding reconciliation without replay.

### 2026-09-16 — judge-visible runtime evidence
Added `JudgeEvidenceDialog` backed by real `DesktopResearchReadiness`, plus `SessionEvidenceLedger`, a closed process-local kind+timestamp proof boundary. Desktop chat/research record successful Nemotron inference, actual memory influence, and Tavily research only when a validated citation was used. Added fail-closed `BrowserSessionEvidenceRecorder` so only completed browser jobs with a trusted verified-step checkpoint qualify, and accepted consequential approvals are recorded only after the exact-scope host boundary returns.

### 2026-09-16 — shared production session evidence (latest run)
Completed:
- Re-read this ledger completely and inspected the current composition root, desktop invocation evidence, browser product runtime, browser host execution/reconciliation boundaries, and recent commits before mutation.
- Explicitly verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `SessionEvidenceLedger.ProcessLocal`, one payload-free process-lifetime production ledger. Explicitly injected ledgers remain supported for isolated tests.
- `DesktopInvocationService` now defaults to that process ledger rather than silently allocating its own private ledger.
- `BrowserProductRuntime` now defaults to the same process ledger and always constructs its fail-closed `BrowserSessionEvidenceRecorder`.
- This closes the previous default-composition gap: browser actions reached through the product-facing runtime now feed the same snapshot returned by `DesktopInvocationService.SessionEvidenceSnapshot()`, which is what the Windows judge-evidence dialog displays.
- No provider payload, URL, approval scope, site text, memory content, prompt, tool argument, credential, provider id, or raw error was added to the ledger.

Files changed:
- `src/Nvidea.Core/Desktop/SessionEvidenceLedger.cs`
- `src/Nvidea.Core/Desktop/DesktopInvocation.cs`
- `src/Nvidea.Core/Desktop/BrowserProductRuntime.cs`
- `progress.md`

Commits this run before ledger:
- `c576827f950eacc1ca8bcadbf05a7a9b09b03031` — add the shared process-local evidence ledger.
- `38f348466f8992e791964a307b3baa770391947c` — make desktop invocation use the shared production ledger by default.
- `7794f55b9d321d3791ec30aa1dde0ed42f01e747` — feed browser product proof into the same shared ledger.

Validation/evidence:
- Static composition review: `NvideaCompositionRoot` constructs `DesktopInvocationService` without an explicit ledger and constructs `BrowserProductRuntime` without an explicit ledger; both now resolve to `SessionEvidenceLedger.ProcessLocal`, so the existing judge dialog snapshot and product browser evidence share identity without widening the composition root's public API.
- Browser qualification remains fail-closed in `BrowserSessionEvidenceRecorder`: completed + trusted `VerifiedStep` is required for browser proof; accepted approval remains downstream of the exact-scope trusted host boundary.
- Explicit ledger injection remains available, preserving deterministic isolated unit-test construction rather than forcing tests onto global state.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Session proof remains process-local and intentionally non-durable; it is judge evidence, not an audit replacement.
- First-observation ledger semantics prevent retries from inflating proof.
- The shared singleton contains only the closed enum + timestamp projection. Sharing it does not create a cross-component payload channel.
- Browser proof cannot be inferred from driver-reported success alone: the durable completed verified-step checkpoint is required.
- Waiting-for-approval is descriptive only and cannot become approval proof. A mismatched exact scope fails before the product evidence record point.
- Existing browser authorization, durable job, audit, quarantine, emergency-stop and ambiguous-side-effect recovery authority is unchanged; evidence remains observation-only.
- Existing remote result/audit/cleanup/binding authority boundaries are unchanged.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Multi-step `BrowserGoalAgent` and `BrowserAmbiguousRecoveryService` still call the trusted `BrowserHostRuntime` directly rather than the product wrapper. Their verified outcomes therefore do not yet feed session proof. Central host-level observation is still required to cover every browser path without duplicating authority.
- The process ledger intentionally spans the lifetime of one desktop process. A future in-app "new demo session" UX must explicitly clear it rather than accidentally carrying proof between logical sessions.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- `NebiusBackgroundExecutionObserved` is defined but not yet wired.

## Single Best Next Task
Move browser evidence observation to the trusted `BrowserHostRuntime` success/reconciliation boundaries using the same `SessionEvidenceLedger.ProcessLocal`, then remove duplicate product-level observation once covered. Prove direct product actions, multi-step `BrowserGoalAgent`, and ambiguous crash reconciliation all record verified browser proof, while rejected/mismatched approvals, failed/cancelled/ambiguous actions and unverified completions never do. After that, wire `NebiusBackgroundExecutionObserved` only from authenticated/reconciled remote lifecycle evidence.
