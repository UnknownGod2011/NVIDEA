# NVIDEA Hackathon Progress

## Mission
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must remain independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Product / Architecture State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + Extract research with canonical deduplication, exact credit accounting, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, and validated citations.
- Durable staged research checkpoints make synthesis restart-safe without repeating Tavily retrieval.
- Protected local state uses Windows CurrentUser DPAPI by default, job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and explicit crash recovery.
- Remote research has bidirectional encrypted payload transport, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation across bounded complete job listings, durable cancellation, terminal/result-expiry handling, mounted encrypted transport, non-root worker image, and a signed authoritative remote-ID binding protocol.
- Signed binding publication is wired into normal two-phase dispatch, crash-window reservation recovery, and dispatched reconciliation.
- `NebiusResearchClientRuntime` now provides a single client-only composition boundary that constructs dispatcher, reconciler, ingestor, and binding publisher from one signing identity and performs race-safe post-terminal binding cleanup.
- Production remains truthfully local until a live Nebius/Object Storage/MysteryBox/container end-to-end probe succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, MysteryBox secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, explicit terminal/result-expiry reconciliation, mounted encrypted transport, deployable non-root worker image, signed authoritative resource-ID bindings, and automatic binding publication after durable remote-ID attachment.

### 2026-09-09 — Current run: client composition + terminal binding cleanup
Completed:
- Re-read `progress.md` completely, inspected current NVIDEA repo state/recent commits and the relevant transport, dispatcher, reconciler, result-ingestion, and binding implementations.
- Added `ResearchDispatchBindingCleanup`, which only deletes a signed dispatch binding after the durable local record proves the remote stage is no longer executable: `ResultApplied` with local Pending/Completed, `Cancelled` with local Cancelled, or `RemoteFailed`/`Expired` with local Failed.
- Cleanup occurs after the existing CAS-protected state transition and is best-effort. A shared-storage deletion failure therefore cannot roll back, corrupt, or falsify durable job state.
- Added `NebiusResearchClientRuntime`, a client-only composition root that creates exactly one `ResearchDispatchBindingPublisher` from the client private key and passes that same publisher instance to both `TwoPhaseNebiusResearchDispatcher` and `NebiusResearchLifecycleReconciler`.
- The runtime also owns the matching `RemoteResearchResultIngestor` and exposes dispatch, reserved reconciliation, dispatched reconciliation, cancellation, cancellation reconciliation, and direct exact-once ingestion operations.
- Runtime wrappers perform signed-binding cleanup only after successful durable result application or terminal reconciliation. No cleanup occurs for `DispatchReserved`, `Dispatched`, or `CancelRequested` states, preserving worker/control-plane recovery races.
- Added `ResearchDispatchBindingCleanupTests.cs` covering successful result-applied cleanup, provider failure/cancellation/expiry cleanup, refusal to clean active states, refusal when execution remains remote, and cleanup-failure tolerance.

Commits this run:
- `a62d95afa678295bb2db0aee9ed94d2c8bd8bd4a` — add safe client runtime and binding cleanup boundary.
- `bc98e7d40f5766426cb5ff34a122382dde76cdfb` — add terminal binding cleanup regression tests.

Validation / evidence:
- Repository metadata reported `repository_full_name = UnknownGod2011/NVIDEA` immediately before every GitHub mutation; no other repository was mutated.
- Static review confirms binding deletion is post-CAS and gated by both local execution location and a terminal/result-applied provenance combination.
- Static review confirms active `DispatchReserved`, `Dispatched`, and `CancelRequested` stages retain the binding and therefore preserve worker/reconciliation safety.
- Static review confirms dispatcher and reconciler receive the exact same `ResearchDispatchBindingPublisher` instance from the new client composition boundary; the client private signing key is not added to worker options or Serverless environment variables.
- `dotnet`/Windows/container execution is not available in this automation environment, so compilation and unit-test execution are **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Dispatch bindings still contain only opaque/control-plane identity and bounded timestamps; they contain no research question, evidence, selected text, clipboard content, API key, or OS-private payload.
- Cleanup is deliberately delayed until durable local state proves the remote stage is terminal or its verified result is already applied.
- If cleanup fails, the signed binding remains cryptographically time-bounded and can be removed by Object Storage lifecycle policy; job correctness does not depend on deletion succeeding.
- The new composition root keeps the private signing key client-side and prevents normal production wiring from accidentally creating separate publisher identities for dispatch versus reconciliation.
- Existing lower-level constructors remain available for tests/backward compatibility, so production callers must deliberately use `NebiusResearchClientRuntime` when Serverless is eventually enabled.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket, registry image, MysteryBox keys, subnet, or Serverless job has been provisioned/validated in this environment.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live contract succeeds.
- The new client runtime is not yet wired into WPF, intentionally, because the cloud path has not passed the live end-to-end probe.
- Cleanup is best-effort by design and still relies on bucket lifecycle policy as a retention backstop if transport deletion fails.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Prove the real cloud boundary instead of adding more speculative abstraction: extend the existing Nebius contract probe into a narrow end-to-end research probe that uses the deployable worker image plus mounted Object Storage transport and MysteryBox-injected Nemotron/Tavily/worker secrets, then validate `encrypted opaque dispatch -> durable remote-id attachment -> signed binding -> worker one-stage Nemotron/Tavily execution -> encrypted result -> exact-once local ingestion -> terminal binding cleanup`. Capture exact API/container mismatches and fix only those verified gaps. WPF Serverless controls should remain hidden until this succeeds.
