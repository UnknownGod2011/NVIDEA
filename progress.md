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

### 2026-09-15 — Binding authority substitution matrix (latest run)
Completed:
- Re-read this ledger completely and inspected the existing deterministic completion-race tests before mutation.
- Verified before each GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `DurableResearchDispatchBindingAuthorityRaceTests`, extending deterministic post-publication adversarial coverage beyond remote-id substitution.
- Added a data-driven mutation matrix for opaque work-item identity, canonical encrypted-envelope SHA-256, and expiry substitution after the signed V2 binding is already published but before protected-state completion.
- Each mutation must throw/fail closed, leave the original exact `PendingResearchDispatchBinding` durable, and perform exactly one already-completed shared binding publication. The test also asserts the original remote id, opaque id, digest and expiry remain encoded in the pending obligation for restart/operator diagnosis.

Files changed:
- `tests/Nvidea.Core.Tests/DurableResearchDispatchBindingAuthorityRaceTests.cs`
- `progress.md`

Commits this run before ledger:
- `d7a1734382f035d6b259f27b6625bbf3a4b0781f` — add binding completion authority-substitution regressions.

Validation/evidence:
- Static review reuses the same production `DurableResearchDispatchBindingObligation` and internal completion observer seam already used by the existing remote-id race regression; no production API or runtime behavior was changed.
- The mutation matrix explicitly covers the remaining authority-bearing obligation dimensions requested by the prior run: opaque id, envelope digest and expiry. Remote-id substitution remains covered by `DurableResearchDispatchBindingCompletionRaceTests`.
- Every adversarial case requires one transport publication only and a still-pending exact original obligation after failure; no second external side effect is authorized by the test path.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The deterministic seam remains internal/test-only and cannot be supplied through the public production constructor or configuration.
- The new tests mutate protected provenance only after publication, so they test bookkeeping authority rather than granting publication authority.
- Remote id, opaque id, envelope digest and expiry substitutions are now all explicitly regression-locked to preserve the original durable obligation instead of clearing it against changed provenance.
- No plaintext research content, credentials, tokens or private keys were added to durable state or repository content.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- The deterministic seam still has not driven the entire lifecycle cancellation audit/outbox pipeline concurrently; the existing benign race test changes protected `CancelRequested` state directly.
- Terminal-state behavior still needs an explicit policy/regression: an already-published obligation must never be silently erased if result ingestion or cancellation reaches a terminal job state before completion bookkeeping.

## Single Best Next Task
Drive the real lifecycle cancellation intent + durable audit/outbox transition from the post-publication observer, rather than directly mutating provenance. Prove the exact binding obligation survives every intermediate audit state, converges without duplicate binding/provider side effects, and define/test fail-closed behavior if cancellation or result ingestion reaches a terminal job state before binding completion bookkeeping.
