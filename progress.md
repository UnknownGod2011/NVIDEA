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
Added `JudgeEvidenceDialog` reachable from Research readiness. It derives Tavily/Nebius/Serverless readiness from production `DesktopResearchReadiness`, stays payload/secret-free, performs no provider/browser side effect, and never promotes unavailable capabilities to simulated success. Added `SessionEvidenceLedger`, a closed, process-local kind+timestamp proof boundary.

### 2026-09-16 — production session evidence integration (latest run)
Completed:
- Re-read this ledger completely and inspected current composition, desktop invocation, research synthesis/citation validation, judge dialog, readiness flow, and desktop tests before mutation.
- Explicitly verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- `DesktopInvocationService` now owns one process-session evidence ledger by default and exposes only an immutable payload-free snapshot.
- Chat records `NemotronInferenceCompleted` only after a non-empty successful completion. If retrieved personal memory was actually carried into that successful invocation, it also records `MemoryInfluencedInvocation`.
- Research records Nemotron proof only after the complete research invocation returns. `TavilyResearchCompletedWithCitations` is stricter: it is recorded only when `ResearchReport.UsedCitations` contains at least one source id that survived synthesis citation validation.
- Failed inference, unavailable Tavily research, cancellation/exception paths and uncited reports cannot cross these record points.
- `JudgeEvidenceDialog` now receives a snapshot at open time and renders only friendly milestone labels plus first-observed local timestamps. Empty sessions explicitly say no verified production milestones have been observed.
- The dialog continues to distinguish provider readiness, implemented architecture and actually observed session proof; none can manufacture another.
- Added desktop tests asserting successful Nemotron proof, memory-influence proof, failed-inference no-proof, and unavailable-research no-proof.

Files changed:
- `src/Nvidea.Core/Desktop/DesktopInvocation.cs`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml`
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs`
- `src/Nvidea.Windows/MainWindow.Readiness.cs`
- `tests/Nvidea.Core.Tests/DesktopInvocationTests.cs`
- `progress.md`

Commits this run before ledger:
- `51440443e548f35111665432c9a71e831a4555b6` — initial verified desktop success-boundary wiring.
- `85750c153ab216ce17ac6034e89cfe88d7d74bc7` — own/expose payload-free desktop evidence snapshot.
- `3f8309e00ecbca1e313d0b6ea518cdf506c41060` — render verified session milestones.
- `2cc10ae2ba77420b158a1189477f5955158ac969` — add judge-dialog session proof section.
- `ac166d61da3090a1d2b5e86943244f8b82bb029f` — pass live snapshot from production desktop root.
- `cf6cdd6ff3c211a5cdfe9a6119211f1771dd63ff` — test success and fail-closed evidence boundaries.

Validation/evidence:
- Static control-flow review confirms all new `Record` calls occur after their relevant success conditions, never before provider/research completion.
- Tavily proof depends on `ResearchReport.UsedCitations`, which `ResearchEngine.SynthesizeAsync` builds only from source markers present in the answer and found in the provider batch citation dictionary; unknown source ids do not qualify.
- UI receives `SessionEvidenceSnapshot`, whose entries contain only closed `SessionEvidenceKind` + `DateTimeOffset`; no prompt, memory content, source URL/text, provider id, model output, tool arguments, secrets or raw errors can enter this projection.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Session proof remains process-local and intentionally non-durable; it is not an audit replacement and restart clears judge-session evidence rather than retaining potentially misleading history.
- First-observation semantics prevent retries/duplicate notifications from inflating proof.
- Failed/empty Nemotron output cannot record inference proof; failed/unavailable research cannot record research proof; Tavily results without a validated used citation cannot record Tavily proof.
- Memory proof means retrieved memory was included in a successful invocation, not merely that memory exists on disk.
- The judge view snapshots state and causes no inference, Tavily, browser, credential or cloud operation.
- Existing durable result/audit/cleanup/binding authority boundaries remain unchanged.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Browser verified-post-state, consequential approval-gate and Nebius-background milestones are defined but not yet wired to their production success boundaries.
- Tavily evidence has targeted semantic review but does not yet have a dedicated end-to-end unit test proving cited vs uncited ResearchReport behavior through `DesktopInvocationService`.

## Single Best Next Task
Wire `BrowserPostStateVerified` and `ConsequentialApprovalGateExercised` to the existing trusted browser outcome/approval boundaries without broadening authorization authority, then add tests proving ambiguous/failed/unverified actions never record them. After that, wire `NebiusBackgroundExecutionObserved` only from authenticated/reconciled remote lifecycle evidence. This will make the <=3-minute judge screen visibly prove the browser safety and cloud-background parts of the demo from real production events rather than architecture claims.