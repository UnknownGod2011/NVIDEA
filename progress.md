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

### 2026-09-15 to 2026-09-16
Added durable V2 binding obligations, restart reconciliation, result/audit/cleanup crash recovery, ambiguous-delete handling, bounded cleanup CAS convergence, multi-artifact cleanup races, and a composed production-path regression covering V2 publication + result CAS/audit + encrypted deletion + concurrent terminal progress + final binding reconciliation without replay.

### 2026-09-16 — judge-visible runtime evidence
Added `JudgeEvidenceDialog` backed by real `DesktopResearchReadiness`, plus `SessionEvidenceLedger`, a closed process-local kind+timestamp proof boundary. Desktop chat/research record successful Nemotron inference, actual memory influence, and Tavily research only when a validated citation was used. Added fail-closed browser evidence for completed jobs with trusted verified-step checkpoints; consequential approval proof is recorded only after the exact-scope trusted-host boundary returns.

### 2026-09-16 — browser evidence composition
- Shared `SessionEvidenceLedger.ProcessLocal` across desktop and browser product runtime.
- Added `EvidenceObservingBrowserGoalHost` for normal multi-step goal execution.
- Hardened regressions so pending/running/waiting/retry/failed/cancelled/unverified browser outcomes never become proof.
- Routed `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` through the evidence decorator.
- Reused one `JsonBrowserGoalSessionStore` per composition-root lifetime across goal agent, listing, and ambiguous recovery.
- Added a composition regression proving the internal factory returns the evidence decorator rather than raw browser authority without starting Playwright.

### 2026-09-16 — trusted Nebius background evidence
- Added `EvidenceObservingRemoteResearchClientRuntime`, an observation-only decorator over `IRemoteResearchClientRuntime`, composed immediately after the real Nebius runtime and before `ResearchCloudExecutionCoordinator`.
- Dispatch and cancellation requests remain evidence-free; reconciliation/recovery can record `NebiusBackgroundExecutionObserved` only after authenticated result ingestion returns durable `ResultApplied` provenance locally.
- Added an internal composition seam for isolated testing without provider/network activity.

### 2026-09-16 — audited Nebius evidence boundary (latest run)
Completed:
- Re-read `progress.md` completely and inspected the remote evidence observer plus `ResearchCloudExecutionCoordinator` reconciliation/audit-recovery semantics before mutation.
- Explicitly verified immediately before every GitHub mutation that repository metadata reported `repository_full_name` exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Hardened `EvidenceObservingRemoteResearchClientRuntime.Observe` so `ResultApplied + Local` is no longer sufficient when a durable `PendingAuditEvent` remains.
- `NebiusBackgroundExecutionObserved` now requires all three facts: local execution location, trusted `ResultApplied` remote provenance, and a cleared durable audit outbox.
- This closes the crash window where authenticated result CAS succeeded but its accountability/audit append had not yet completed. `RecoverPendingAuditAsync` can establish evidence only after the underlying runtime actually clears that obligation and returns the recovered record.
- Deliberately did not require the whole research job to be terminal `Completed`: remote execution is per-stage, and an authenticated Nebius-produced stage can legitimately return the durable job to local `Pending` for later stages. Requiring terminal completion would under-report genuine background execution.

Files changed this run:
- `src/Nvidea.Core/Desktop/EvidenceObservingRemoteResearchClientRuntime.cs`
- `progress.md`

Validation/evidence:
- Static inspection of `ResearchCloudExecutionCoordinator.ReconcileAsync` confirms a pending audit marker is recovered before normal remote-state reconciliation and that a locally applied result with no unresolved marker is a valid return boundary.
- Static inspection confirms `DispatchAsync` and `RequestCancellationAsync` remain pure delegation and cannot record evidence.
- Static inspection confirms an authenticated result stranded before audit append can no longer become judge-visible proof through the observer.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Session proof remains process-local and intentionally non-durable; it is judge evidence, not an audit replacement.
- First-observation ledger semantics prevent retries, repeated approvals, repeated reconciliation or repeated recovery from inflating proof.
- The shared ledger contains only a closed enum + timestamp projection; no prompts, URLs, browser text, memory content, approval scope, filenames, provider ids, remote job ids, checkpoint payloads or raw errors are retained.
- Browser proof cannot be inferred from driver-reported success alone: terminal completion plus a trusted verified-step checkpoint is required.
- Nebius background proof cannot be inferred from dispatch, a remote-looking Running state, cancellation request, provider error, ambiguous delivery, or a result CAS whose durable audit obligation is still unresolved.
- The remote decorator is observation-only and cannot dispatch, approve, retry, reconcile, cancel, decrypt, mutate provider state, clear audit obligations, or alter durable provenance beyond delegating to the existing trusted runtime.
- Existing browser authorization, durable job, audit, quarantine, emergency-stop and ambiguous-side-effect recovery authority is unchanged.
- Existing remote result/audit/cleanup/binding authority boundaries are unchanged.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- The remote evidence decorator still needs isolated contract regressions proving dispatch/running/failure/cancellation/exception/pending-audit paths remain non-evidence and audited `ResultApplied` records exactly once.
- Product-level and ambiguous-recovery browser evidence observation remain separate; ledger idempotence prevents proof inflation, but redundant observation should be removed only after equivalent host-level coverage is proven.
- The process ledger intentionally spans one desktop-process lifetime; a future in-app "new demo session" UX must explicitly clear it.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add isolated contract regressions for `EvidenceObservingRemoteResearchClientRuntime` using a passive fake remote runtime and isolated ledger. Explicitly cover the newly hardened pending-audit crash window: `ResultApplied + Local + PendingAuditEvent` must remain non-evidence, while the same record after successful audit recovery must record `NebiusBackgroundExecutionObserved` idempotently. Also prove dispatch, dispatched/running, failure, cancellation and thrown reconciliation remain non-evidence.
