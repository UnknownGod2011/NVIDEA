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
- Hardened the boundary so `ResultApplied + Local` is insufficient while a durable `PendingAuditEvent` remains; judge proof is downstream of both authenticated result CAS and its accountability append.
- Added an internal composition seam for isolated testing without provider/network activity.

### 2026-09-16 — remote evidence adversarial contract suite
- Added isolated adversarial coverage proving dispatch/cancel, non-result lifecycle states, pending-audit crash windows and thrown reconciliation remain non-evidence.
- Successful audited `ResultApplied` recovery establishes `NebiusBackgroundExecutionObserved` exactly once with first-observation semantics.

### 2026-09-16 — safe new demo session UX
- Added `DesktopInvocationService.ResetSessionEvidence()` as a deliberately narrow boundary that can reach only the injected ephemeral session ledger.
- Added a `New demo session` control to `JudgeEvidenceDialog`, with explicit Yes/No confirmation defaulting to No and precise disclosure that durable memory/jobs/browser/audit state is preserved.
- Changed dialog composition from a frozen snapshot to narrow snapshot/reset delegates so the UI refreshes immediately without receiving durable-store authority.

### 2026-09-16 — desktop evidence reset contract (latest run)
Completed:
- Re-read this ledger completely and inspected `DesktopInvocationService`, `SessionEvidenceLedgerTests`, and the existing `DesktopInvocationTests` before mutation.
- Explicitly verified repository metadata as exactly `UnknownGod2011/NVIDEA` immediately before every GitHub mutation; no other repository was mutated.
- Added `Reset_session_evidence_clears_only_injected_projection_and_genuine_success_can_reestablish_proof` to `DesktopInvocationTests`.
- The regression injects a private ledger with deterministic timestamps, establishes genuine Nemotron completion evidence through the normal successful invocation path, resets through `DesktopInvocationService.ResetSessionEvidence`, and proves both the service snapshot and injected ledger are empty.
- It then performs a second genuine invocation and requires a fresh `NemotronInferenceCompleted` entry with the later timestamp, proving reset does not poison future production evidence and that proof cannot reappear without another successful invocation.
- The test also checks inference request count before and after reset: reset itself cannot invoke the model or manufacture execution evidence.

Files changed this run:
- `tests/Nvidea.Core.Tests/DesktopInvocationTests.cs`
- `progress.md`

Validation/evidence:
- Static inspection confirms the test uses the real `DesktopInvocationService.InvokeAsync` success path and the existing deterministic `SessionEvidenceLedger(Func<DateTimeOffset>)` clock seam rather than mutating the ledger to simulate post-reset success.
- The reset boundary remains a single `_sessionEvidence.Clear()` call and owns no durable memory/job/browser/audit authority.
- The regression checks the same injected ledger directly after reset, preventing an accidental implementation that merely swaps/hides the service snapshot while leaving evidence resident.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Session proof remains process-local, payload-free and intentionally non-durable; reset is a demo-evidence operation, not an audit or privacy deletion feature.
- Reset cannot reach durable memory, research jobs, browser state, downloads or audit stores through its composition surface.
- Confirmation defaults to No, reducing accidental evidence loss during a demo.
- Resetting evidence does not revoke permissions, cancel jobs, clear authentication, modify browser sessions, delete downloads or erase accountability history.
- The new contract proves reset itself performs no inference and that fresh evidence requires a subsequent genuine successful invocation.
- Browser and Nebius evidence trust boundaries remain unchanged.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Product-level and ambiguous-recovery browser evidence observation remain separate; ledger idempotence prevents proof inflation, but redundant observation should be removed only after equivalent host-level coverage is proven.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- The WPF reset flow and new regression should be executed on Windows/.NET 8 before submission; any compile/XAML/test issue must be fixed rather than bypassing confirmation.

## Single Best Next Task
Perform the deterministic <=3 minute demo/rubric audit against the actual current implementation and turn the result into an executable demo checklist that maps each required judging beat to a real UI action, production evidence milestone, expected visible state, fallback/recovery behavior, and preflight dependency. Prioritize any discovered functional blocker over cosmetic documentation.