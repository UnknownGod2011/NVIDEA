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
Added `PendingResearchDispatchBinding` and `DurableResearchDispatchBindingObligation`: the exact V2 publication intent is CAS-staged before shared binding-transport I/O, publication is idempotent, protected state is re-read/revalidated after publish, and the obligation is cleared only after successful publication. Restart/reconciliation consumes this obligation and fails closed on changed remote id, opaque id, digest or expiry.

### 2026-09-15 — Fresh dispatch now uses the durable publication obligation (latest run)
Completed:
- Re-read this ledger completely and inspected the dispatcher, obligation coordinator and production client runtime before mutation.
- Verified before each mutation that the GitHub target was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Extended `TwoPhaseNebiusResearchDispatcher` with an optional `DurableResearchDispatchBindingObligation` dependency while preserving the legacy/lower-level publisher path for compatibility.
- Changed both fresh `DispatchWithReservationAsync` and safe `ResumeReservedAsync` post-attachment paths to return the durable publication result. For envelope-bound production jobs, publication now calls `EnsurePublishedAsync(jobId)` rather than writing the shared binding transport directly.
- Therefore the first production V2 binding attempt now follows: durable remote-id attachment -> protected CAS publication obligation -> shared V2 binding publish -> protected-state re-read/equality validation -> CAS obligation clear.
- Wired the same obligation instance into both production dispatcher and `ResearchDispatchBindingRecovery`, so fresh dispatch and restart recovery share one publication protocol rather than reconstructing different paths.
- Preserved direct `ResearchDispatchBindingPublisher` behavior only when the durable coordinator is not supplied, avoiding a breaking change for lower-level tests/compositions.

Files changed:
- `src/Nvidea.Core/Jobs/TwoPhaseNebiusResearchDispatcher.cs`
- `src/Nvidea.Core/Jobs/NebiusResearchClientRuntime.cs`
- `progress.md`

Commits this run before ledger:
- `a6ead1b403a20a909cfa15a02d1d4b9d14e935c0` — route production binding publication through durable obligation.
- `f560feca81ce7bd999c1440aa5f21dd7add484f3` — wire durable binding obligation into fresh dispatch.

Validation/evidence:
- Static call-path review confirms production runtime constructs one `DurableResearchDispatchBindingObligation` and supplies it to both dispatcher and recovery.
- Static review confirms envelope-bound production publication cannot touch binding transport through the dispatcher before `EnsurePublishedAsync` stages its protected CAS obligation.
- Existing coordinator semantics retain the obligation on publisher failure and clear only after successful publish plus durable state re-read/equality validation.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Fresh and restart publication now share the same protected obligation; a transport failure after remote-id attachment no longer relies on reconstructing publication authority from scratch.
- The obligation contains no research plaintext or credential; it contains protected control-plane provenance required for V2 worker verification.
- Digest equality remains fixed-time and V2 worker verification still binds remote id to the exact encrypted work-item envelope.
- Legacy direct publisher fallback remains intentionally available only for compositions that do not provide the production obligation coordinator; the production runtime supplies it.
- Remaining verification gap: focused executable fault-injection tests have not yet proven first-attempt publisher failure leaves the exact obligation pending and restart clears only that obligation. This is now the highest-value test gap.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Need focused dispatcher-level fault-injection coverage for the newly unified fresh publication path.
- Cancellation racing binding publication deserves explicit regression coverage: cancellation may change provenance to `CancelRequested`, which the coordinator accepts, but the exact pending obligation must remain authoritative and must never be silently replaced.

## Single Best Next Task
Add focused dispatcher/runtime fault-injection tests for the unified durable V2 publication path: force the first shared binding publish to fail after staging, assert the exact `PendingResearchDispatchBinding` survives with remote-id/digest/expiry unchanged, then simulate restart/reconciliation and prove it publishes that exact obligation once and clears it only after success. Include a cancellation-race case so `CancelRequested` cannot substitute or erase the staged obligation.
