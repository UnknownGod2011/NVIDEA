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

### 2026-09-15 — Binding completion race hardening (latest run)
Completed:
- Re-read this ledger completely and inspected the durable obligation implementation plus binding recovery/cancellation tests before mutation.
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Hardened the post-publication completion window in `DurableResearchDispatchBindingObligation` with a bounded four-attempt protected-state re-read/CAS completion loop.
- Split pre-publication authority from post-publication completion validation. Publication still requires an audit-settled active Nebius stage. After the exact V2 binding is externally visible, completion may tolerate a concurrent audit transition only while job type/state/location, remote provenance state, remote id, opaque id, envelope commitment and expiry remain exact.
- If another recovery actor already cleared the exact obligation, completion now converges idempotently instead of failing.
- If the obligation identity or any authority-bearing provenance changes, completion still fails closed and refuses to clear anything.
- If protected state keeps changing through all bounded attempts, the method fails with the durable obligation still pending for restart recovery rather than spinning or weakening validation.

Files changed:
- `src/Nvidea.Core/Jobs/DurableResearchDispatchBindingObligation.cs`
- `progress.md`

Commits this run before ledger:
- `0abd45de569adfff2f432be376a855cde0620f88` — harden binding completion against benign state races.

Validation/evidence:
- Static call-path review confirms the shared binding publication still occurs only after the exact durable obligation is staged and validated with settled audit state.
- The relaxed audit check applies only after successful external publication and only to clearing the same equality-checked obligation; it cannot authorize a new provider/binding side effect.
- Completion remains bounded and cancellation-aware through the store calls; no unbounded retry loop was introduced.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Pre-publication remains fail-closed on any pending audit event; audit transitions cannot create publication authority.
- Post-publication completion is explicitly bookkeeping for an already-visible signed V2 binding. It may survive benign concurrent audit mutation but still pins every authority-bearing obligation field to protected provenance.
- A substituted obligation ID, remote ID, opaque ID, digest, expiry, execution location, terminal state or unsupported provenance state prevents clearing.
- Bounded CAS retry reduces restart-only recovery for benign local races without creating duplicate consequential provider actions.
- No plaintext research content, credentials, tokens or private keys were added to durable state.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- The exact concurrent cancellation/audit mutation window still needs deterministic executable fault injection; current production logic is hardened but this run could not execute a race test.
- Need to ensure lifecycle cancellation/result-ingestion transitions never legitimately move to a terminal state before an already-published obligation can be cleared; if they can, completion policy should be explicitly modeled rather than broadly relaxed.

## Single Best Next Task
Add a deterministic test seam/fault harness around the post-publication/pre-clear boundary and exercise the real cancellation intent/audit transition concurrently. Prove a benign audit/CAS race converges without duplicate binding or provider actions, while substituted remote id/opaque id/digest/expiry and terminal-state mutations preserve the obligation and fail closed. Keep the seam internal/test-only rather than exposing timing controls in production APIs.
