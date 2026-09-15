# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Dispatch-binding V2 signs authoritative remote id + canonical SHA-256 of the exact encrypted work-item envelope; worker verifies and pins the envelope before execution.
- Protected local CAS state supports a durable V2 binding-publication obligation used by fresh production dispatch and restart recovery.

## Persistent history
### 2026-09-06 to 2026-09-12
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment preflight and judging/evaluator tooling.

### 2026-09-13 to 2026-09-15
Hardened exact-once browser behavior and remote dispatch: durable external-action/cleanup/audit intents, exact remote provenance, crash-resumable cancellation, envelope commitment, V2 sender authenticity, pinned-envelope worker execution, bounded worker transport retry/SIGTERM, atomic reservation + audit + digest CAS, provider-delivery ambiguity reconciliation, shared reservation trust validation and final pre-Create durable authority revalidation.

### 2026-09-15 — Durable V2 binding publication obligation
Added `PendingResearchDispatchBinding` and `DurableResearchDispatchBindingObligation`: the exact V2 publication intent is CAS-staged before shared binding-transport I/O, publication is idempotent, protected state is re-read/revalidated after publish, and the obligation is cleared only after successful publication. Restart/reconciliation consumes this obligation and fails closed on changed remote id, opaque id, digest or expiry. Fresh and resumed production dispatch both route envelope-bound publication through the same coordinator. Fault coverage proves failed first publication preserves the exact obligation and blocks cancellation until restart republishes it.

### 2026-09-15 — Binding completion race hardening
Hardened the post-publication completion window with a bounded four-attempt protected-state re-read/CAS loop. Pre-publication still requires an audit-settled active Nebius stage; post-publication clearing tolerates only benign concurrent local transitions while every authority-bearing obligation/provenance field remains exact. Another actor completing the exact obligation converges idempotently; substitution or sustained contention leaves durable recovery debt.

### 2026-09-15 — Deterministic post-publication race seam
Added internal/test-only `IResearchDispatchBindingCompletionObserver` after successful shared V2 publication and before protected-state completion. Focused regressions prove a benign `Dispatched -> CancelRequested` transition converges with one publication, while remote-job-id substitution fails closed and leaves the exact obligation durable.

### 2026-09-15 — Binding authority substitution matrix
Added data-driven post-publication regressions for opaque work-item identity, canonical encrypted-envelope SHA-256 and expiry substitution. Together with remote-id coverage, all principal V2 obligation authority dimensions fail closed and preserve the original durable obligation.

### 2026-09-15 — Real cancellation pipeline race (latest run)
Completed:
- Re-read this ledger completely and inspected the production binding coordinator, runtime wiring, lifecycle reconciler, cancellation audit/outbox path, and existing deterministic race tests before mutation.
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `DurableResearchDispatchBindingLifecycleRaceTests` using the internal post-publication observer to invoke the real `NebiusResearchLifecycleReconciler.RequestCancellationAsync` path rather than directly editing protected provenance.
- The regression drives the actual CAS `Dispatched -> CancelRequested` transition, stages and flushes the durable `research.remote_cancel_requested` audit event, and performs the provider cancellation while V2 binding completion bookkeeping is paused.
- The test requires the binding coordinator to converge afterward with the exact obligation cleared, no pending audit event, one signed binding transport write, one provider cancellation against the authoritative remote id, and one cancellation-request audit event.
- Static review caught and corrected an initial test helper typo (`AuditEvent.Id` -> `AuditEvent.EventId`) before finalizing.

Files changed:
- `tests/Nvidea.Core.Tests/DurableResearchDispatchBindingLifecycleRaceTests.cs`
- `progress.md`

Commits this run before ledger:
- `d6fe04daa900456bf46f85e860341e0329976ad6` — add real cancellation-pipeline binding race regression.
- `1bbdc6900ca5de674db783ff42355d301a181a7f` — correct audit identity helper in the new regression.

Validation/evidence:
- The test uses the production `DurableResearchDispatchBindingObligation`, `NebiusResearchLifecycleReconciler`, `DurableJobAuditOutbox` path reached by `RequestCancellationAsync`, `RemoteResearchResultIngestor`, and protected CAS job store; only provider/binding/result transports and audit sink are deterministic in-memory test doubles.
- Consequential-side-effect assertions are explicit: binding `PutAsync` exactly once and Nebius `CancelAsync` exactly once, with no Create/Get/List provider calls permitted by the fake client.
- The durable audit must be settled (`PendingAuditEvent == null`) and exactly one `research.remote_cancel_requested` event must exist before completion is accepted.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The deterministic observer remains internal/test-only and cannot be supplied through the public production constructor or runtime configuration.
- The new regression exercises real cancellation authority and audit sequencing after publication; it does not introduce any new production side-effect authority.
- Binding completion is allowed only after the exact V2 authority fields remain unchanged. Existing remote-id/opaque-id/digest/expiry substitution regressions continue to preserve the original durable obligation on mismatch.
- The cancellation provider call is asserted against the authoritative attached remote id, and the test fake rejects unrelated provider operations.
- No plaintext research content, credentials, tokens or private keys were added to durable repository content.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- The real cancellation regression currently observes the fully flushed audit path. A crash/interruption while the cancellation audit itself is still pending should be fault-injected separately so restart convergence is proven across that intermediate durable state.
- Terminal-state behavior still needs an explicit policy/regression: an already-published obligation must never be silently erased if result ingestion or cancellation reaches `Cancelled`, `Completed`, `Failed`, `ResultApplied`, `RemoteFailed`, or `Expired` before binding completion bookkeeping.

## Single Best Next Task
Define and regression-lock terminal-state semantics for an already-published but not-yet-cleared V2 binding obligation. Fault-inject cancellation/result ingestion so the job reaches each legitimate terminal provenance state before completion bookkeeping, require exact authority-field continuity, and prove the obligation is either safely cleared as completed publication bookkeeping or intentionally retained for deterministic recovery—never silently dropped and never causing duplicate provider/binding side effects.
