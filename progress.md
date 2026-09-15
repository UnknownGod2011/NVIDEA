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

### 2026-09-15 — Deterministic post-publication race seam and regressions (latest run)
Completed:
- Re-read this ledger completely and inspected the durable binding obligation plus existing restart/cancellation regression coverage before mutation.
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added internal `IResearchDispatchBindingCompletionObserver`, injected only through an internal constructor. The public production constructor remains unchanged and always supplies no observer.
- Positioned the seam exactly after successful shared V2 publication and before the first protected-state completion read/CAS, making the previously timing-dependent crash/race window deterministic in tests without exposing production timing controls.
- Added `InternalsVisibleTo` only for `Nvidea.Core.Tests` so the test assembly can exercise the seam.
- Added focused race regressions proving a post-publication `Dispatched -> CancelRequested` protected-state transition preserves exact authority and lets the obligation clear without a second publication.
- Added a fail-closed regression proving post-publication remote-job-id substitution leaves the original exact obligation pending rather than clearing publication debt against changed provenance.

Files changed:
- `src/Nvidea.Core/Jobs/DurableResearchDispatchBindingObligation.cs`
- `src/Nvidea.Core/Nvidea.Core.csproj`
- `tests/Nvidea.Core.Tests/DurableResearchDispatchBindingCompletionRaceTests.cs`
- `progress.md`

Commits this run before ledger:
- `1c725c7d9e02dd6739b026ef08611a97267665d3` — add deterministic binding completion fault seam.
- `cc9137588695c21e812cbdb9bf424fce7c89894c` — expose internal race seam only to Core tests.
- `8bf6eb0ae6339dbbdce8bae98aa3af3a054e7ef6` — add post-publication completion race regressions.

Validation/evidence:
- Static review confirms the observer cannot run before publication and therefore cannot create or widen authority for the shared side effect; it can only deterministically perturb the bookkeeping completion window.
- Public production construction is unchanged; no runtime caller can supply the internal observer outside the friend test assembly.
- The benign transition regression expects exactly one binding transport write and a cleared obligation after `CancelRequested` mutation.
- The adversarial regression expects exactly one already-completed binding write, preserves the original pending obligation, and fails when protected remote provenance is substituted before completion.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The seam is internal/test-only and is not reachable through the production constructor, configuration, environment variables or public APIs.
- It executes only after the signed V2 binding has been published, so it cannot grant publication authority or alter what was signed.
- Completion still validates the exact obligation ID, remote ID, opaque ID, envelope digest, expiry, execution location and supported provenance state before clearing.
- A changed remote ID remains fail-closed with the original obligation durable for operator/restart diagnosis instead of silently blessing changed provenance.
- No plaintext research content, credentials, tokens or private keys were added to durable state.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- The new deterministic seam now makes the completion window testable, but this run exercises the protected `CancelRequested` state transition directly rather than driving the entire lifecycle cancellation audit/outbox pipeline concurrently.
- Terminal-state behavior still needs an explicit policy/regression: an already-published obligation must never be silently erased if result ingestion or cancellation reaches a terminal job state before completion bookkeeping.

## Single Best Next Task
Use the new deterministic post-publication seam to drive the real lifecycle cancellation intent + durable audit/outbox transition concurrently, not just the provenance-state mutation. Prove the exact obligation survives every intermediate audit state, clears after benign convergence without duplicate binding/provider side effects, and remains pending/fail-closed if remote id, opaque id, digest, expiry or terminal job state changes.
