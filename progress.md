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
Added `PendingResearchDispatchBinding` and `DurableResearchDispatchBindingObligation`: the exact V2 publication intent is CAS-staged before shared binding-transport I/O, publication is idempotent, protected state is re-read/revalidated after publish, and the obligation is cleared only after successful publication. Restart/reconciliation consumes this obligation and fails closed on changed remote id, opaque id, digest or expiry. Fresh and resumed production dispatch both route envelope-bound publication through the same coordinator.

### 2026-09-15 — V2 publication failure/restart coverage (latest run)
Completed:
- Re-read this ledger completely and inspected recent commits, the durable obligation, binding publisher, recovery path and existing recovery tests before mutation.
- Verified before each GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Upgraded `ResearchDispatchBindingRecoveryTests` from legacy/no-envelope fixtures to envelope-bound V2 fixtures with a canonical 64-hex commitment.
- Added production-runtime fault injection for the critical first-publication failure: shared binding transport throws after the durable obligation is staged; the test asserts provider cancellation remains blocked and the exact pending opaque id, authoritative remote id, envelope SHA-256 and expiry survive in protected job state.
- Simulates process restart by constructing a fresh `NebiusResearchClientRuntime` over the same protected store, removes the injected transport fault, and proves the restart publishes the exact staged V2 obligation, clears it only after successful publication, then proceeds to the provider cancellation exactly once.
- Strengthened normal cancellation coverage to assert the binding visible before the consequential provider call is V2 and carries the expected envelope commitment.
- Strengthened conflicting-binding coverage to assert a failed/conflicting shared publication leaves the durable V2 obligation pending rather than erasing publication debt.
- Direct recovery idempotency coverage now uses the durable coordinator and asserts repeated recovery produces one shared transport write and no remaining obligation.

Files changed:
- `tests/Nvidea.Core.Tests/ResearchDispatchBindingRecoveryTests.cs`
- `progress.md`

Commits this run before ledger:
- `48aa8de9901a32b05b5669ad52aca11460f0af48` — test durable V2 binding failure and restart recovery.

Validation/evidence:
- Static call-path review confirms the injected first publication failure occurs inside `ResearchDispatchBindingPublisher` only after `DurableResearchDispatchBindingObligation` has CAS-staged `PendingResearchDispatchBinding`.
- The restart test exercises the actual production `NebiusResearchClientRuntime.Create(...)` composition rather than a test-only recovery coordinator.
- Assertions pin all authority-bearing obligation fields (opaque id, remote id, digest, expiry), provider side-effect count, final obligation clearing, and the published V2 binding payload.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- A failed first V2 publication is now regression-locked as durable debt: it cannot silently disappear and cannot permit provider cancellation before worker-verifiable binding publication succeeds.
- Restart authority comes from protected local state; the test does not re-upload or re-hash mutable shared work-item storage.
- Conflicting shared binding content fails closed while preserving the exact pending protected obligation.
- The cancellation race is partially covered at the production boundary: cancellation is blocked while publication fails, and after restart the exact binding is published before the cancellation side effect. A narrower concurrent mutation race during the publish/re-read/CAS-clear window still deserves deterministic fault injection.
- No research plaintext, credentials or secrets were added to durable obligation state or tests.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Need deterministic concurrent-state mutation coverage while a binding publication is in flight, especially `CancelRequested` plus its audit/external-action transitions.
- Need to inspect whether durable binding completion should tolerate a successfully published binding when protected-state CAS clearing loses a benign race, while still never clearing a substituted obligation.

## Single Best Next Task
Add a deterministic in-flight cancellation/state-mutation fault test around `DurableResearchDispatchBindingObligation`: pause after successful shared V2 publication but before protected-state re-read/CAS clear, transition the job through the real cancellation intent/audit path, then prove the exact obligation is neither substituted nor erased and restart reconciliation converges safely without duplicate consequential provider actions. If that exposes benign CAS-clear starvation, harden the coordinator with a bounded, equality-checked completion retry rather than weakening its fail-closed checks.
