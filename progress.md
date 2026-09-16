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

### 2026-09-16 — trusted Nebius background evidence (latest run)
Completed:
- Re-read `progress.md` completely and inspected the current repository tree, composition root, remote-research coordinator/runtime, job status projection, and result protocol before mutation.
- Explicitly verified immediately before every GitHub mutation that repository metadata reported `repository_full_name` exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `EvidenceObservingRemoteResearchClientRuntime`, an observation-only decorator over `IRemoteResearchClientRuntime`.
- Dispatch and cancellation-request calls deliberately remain evidence-free: a successful Create/dispatch receipt or cancellation request does not prove a Nebius worker executed.
- Reconciliation and audit-recovery returns record `NebiusBackgroundExecutionObserved` only when durable trusted provenance is `ResultApplied` and execution has returned to `Local`.
- This boundary intentionally relies on the existing remote result-ingestion trust path: `ResultApplied` is produced only after the protected result envelope is authenticated/decrypted, bound to the exact local job/checkpoint/remote id, and durably applied.
- Wired the decorator in `NvideaCompositionRoot` immediately after `NebiusResearchLiveRuntimeFactory.Create` and before `ResearchCloudExecutionCoordinator`, using `SessionEvidenceLedger.ProcessLocal` by default.
- Added the narrow internal `CreateObservedRemoteResearchRuntime` composition seam so isolated tests can inject a ledger later without provider/network activity.

Files changed this run:
- `src/Nvidea.Core/Desktop/EvidenceObservingRemoteResearchClientRuntime.cs`
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms optimistic dispatch cannot record the background milestone because `DispatchAsync` is pure delegation.
- Static inspection confirms `RequestCancellationAsync` is also pure delegation.
- Static inspection confirms only `ReconcileReservedAsync`, `ReconcileDispatchedAsync`, `ReconcileCancellationAsync`, and `RecoverPendingAuditAsync` can reach the observer, and even those require `ExecutionLocation.Local + RemoteResearch.State.ResultApplied`.
- Existing `NebiusResearchClientRuntime` shows result recovery/ingestion is upstream of this observer; the observer neither decrypts payloads nor creates authority.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Session proof remains process-local and intentionally non-durable; it is judge evidence, not an audit replacement.
- First-observation ledger semantics prevent retries, repeated approvals, repeated reconciliation or repeated recovery from inflating proof.
- The shared ledger contains only a closed enum + timestamp projection; no prompts, URLs, browser text, memory content, approval scope, filenames, provider ids, remote job ids, checkpoint payloads or raw errors are retained.
- Browser proof cannot be inferred from driver-reported success alone: terminal completion plus a trusted verified-step checkpoint is required.
- Nebius background proof cannot be inferred from dispatch, a remote-looking Running state, cancellation request, provider error, or ambiguous delivery. It requires the stronger authenticated result-applied trust boundary.
- The new remote decorator is observation-only and cannot dispatch, approve, retry, reconcile, cancel, decrypt, mutate provider state, or alter durable provenance beyond delegating to the existing trusted runtime.
- Existing browser authorization, durable job, audit, quarantine, emergency-stop and ambiguous-side-effect recovery authority is unchanged.
- Existing remote result/audit/cleanup/binding authority boundaries are unchanged.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- The new remote evidence decorator needs isolated contract regressions proving dispatch/running/failure/cancellation/exception paths remain non-evidence and authenticated `ResultApplied` records exactly once.
- Product-level and ambiguous-recovery browser evidence observation remain separate; ledger idempotence prevents proof inflation, but redundant observation should be removed only after equivalent host-level coverage is proven.
- The process ledger intentionally spans one desktop-process lifetime; a future in-app "new demo session" UX must explicitly clear it.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.

## Single Best Next Task
Add isolated contract regressions for `EvidenceObservingRemoteResearchClientRuntime` using a passive fake remote runtime and isolated ledger. Prove dispatch, dispatched/running, remote failure, cancellation, thrown reconciliation, and malformed/untrusted states cannot record `NebiusBackgroundExecutionObserved`, while a trusted `ResultApplied` return records it idempotently. Then audit whether `ResultApplied` should additionally require a completed-stage checkpoint invariant before considering the milestone contract final.
