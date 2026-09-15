# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Dispatch-binding V2 signs authoritative remote id + canonical SHA-256 of the exact encrypted work-item envelope; worker verifies and pins the envelope before execution.
- Protected local CAS state now supports a durable V2 binding-publication obligation for restart recovery.

## Persistent history
### 2026-09-06 to 2026-09-12
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment preflight and judging/evaluator tooling.

### 2026-09-13 to 2026-09-15
Hardened exact-once browser behavior and remote dispatch: durable external-action/cleanup/audit intents, exact remote provenance, crash-resumable cancellation, envelope commitment, V2 sender authenticity, pinned-envelope worker execution, bounded worker transport retry/SIGTERM, atomic reservation + audit + digest CAS, provider-delivery ambiguity reconciliation, shared reservation trust validation and final pre-Create durable authority revalidation.

### 2026-09-15 — Durable V2 binding publication obligation (latest run)
Completed:
- Re-read this ledger completely and inspected current head, job contracts, binding recovery, client runtime, dispatcher and V2 binding protocol before mutation.
- Verified every GitHub mutation target was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `PendingResearchDispatchBinding` to protected durable `AgentJobRecord` state. It commits obligation id, opaque work-item id, authoritative remote job id, canonical envelope SHA-256, expiry and creation time.
- Added `DurableResearchDispatchBindingObligation`. It stages the exact V2 publication obligation with CAS before shared binding transport I/O, validates it against current protected provenance, publishes idempotently, re-reads state, revalidates the obligation, and clears it with CAS only after successful publication.
- Recovery now consumes this durable obligation for envelope-bound production jobs. Publication failure leaves the obligation durable for restart; a changed remote id, opaque id, digest or expiry fails closed instead of clearing stale authority.
- Wired the obligation coordinator into `NebiusResearchClientRuntime` recovery composition. Legacy V1/V2 recovery remains available only when the coordinator is not supplied, preserving lower-level compatibility.
- During static review, restored the client runtime `IngestAsync` contract and original result-applied recovery semantics after an intermediate compact rewrite; no intentional runtime API was removed.

Files changed:
- `src/Nvidea.Core/Jobs/DurableResearchDispatchBindingObligation.cs` (new)
- `src/Nvidea.Core/Jobs/JobContracts.cs`
- `src/Nvidea.Core/Jobs/ResearchDispatchBindingRecovery.cs`
- `src/Nvidea.Core/Jobs/NebiusResearchClientRuntime.cs`
- `progress.md`

Commits this run before ledger:
- `dfbea3edbf7d86f827995dc59e54dc7eff62a073` — add durable V2 binding publication obligation.
- `c4b5bbc2a22e1d3e5b238d1506f8c354db4fb5b2` — persist pending binding obligation in job state.
- `6e58cf7bcf7b33c248fa397ed8877eea0dabc916` — recover V2 binding through durable obligation.
- `bafe92b8f9fb3130593222adc3725f75420c9c34` / `65bffb4da5904a3268b03dd84146cbef0fededa6` — production recovery wiring and API-contract correction.

Validation/evidence:
- Starting head was `1e499f02b477056fd9676884de8fe1860beadd2d`.
- Static review confirms transport publication occurs only after the obligation CAS and obligation clearing occurs only after successful `PublishEnvelopeBoundAsync` plus a protected-state re-read/equality check.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- The obligation stores no research plaintext or credential; it contains protected control-plane provenance already required for V2 verification.
- Digest equality is fixed-time; protocol identity remains enforced by the existing V2 signer/worker verifier.
- Failed publication does not erase obligation state. A state mutation between publish and clear fails closed and leaves reconciliation work visible.
- IMPORTANT remaining gap: the normal fresh dispatcher still calls its existing publisher immediately after remote-id attachment. The durable obligation is currently guaranteed on restart/reconciliation recovery, but is not yet staged on the first fresh publication attempt. Therefore a crash in the narrow fresh attach→publish window still requires reconstruction from protected provenance rather than replay of a pre-existing obligation.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- No focused executable tests for the new obligation have run yet.
- Fresh-dispatch publication must be routed through the same obligation coordinator to fully close the attach→publish crash window.

## Single Best Next Task
Route `TwoPhaseNebiusResearchDispatcher` fresh and resumed post-attachment V2 publication through `DurableResearchDispatchBindingObligation`, so the exact obligation is committed before the first shared-transport publish attempt, then add fault-injection tests proving transport failure/crash leaves the obligation pending and restart publishes exactly that remote-id/digest/expiry before clearing it.
