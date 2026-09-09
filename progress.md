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
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + Extract research with canonical deduplication, exact credit accounting, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, and validated citations.
- Durable staged research checkpoints prepared evidence so restart-safe synthesis does not repeat Tavily retrieval work.
- Protected local state uses Windows CurrentUser DPAPI by default, job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and explicit crash recovery.
- Remote research has bidirectional protected transport primitives, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation across bounded complete job listings, durable remote cancellation, persisted encrypted-work-item expiry, and explicit provider terminal/delayed-result handling.
- Production remains truthfully local until a concrete shared transport, deployable worker image, and live Nebius validation exist.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, MysteryBox secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, and bounded pagination.

### 2026-09-09 — Current run: durable terminal/expiry reconciliation
Completed:
- Re-read this file completely and inspected current recent commits, `RemoteResearchResultIngestor`, `TwoPhaseNebiusResearchDispatcher`, `NebiusResearchLifecycleReconciler`, job contracts, and lifecycle tests before changing code.
- Extended durable remote provenance with the encrypted work item's exact expiry and terminal timestamp while preserving backward-compatible optional record parameters.
- Extended `RemoteResearchDispatchReservation` with optional exact expiry; legacy callers conservatively default to the protocol's 24-hour maximum lifetime.
- `TwoPhaseNebiusResearchDispatcher` now persists the authenticated `ProtectedResearchWorkItemEnvelope.ExpiresAt` into the reservation before Nebius creation.
- Added typed `RemoteResearchResultNotAvailableException` so lifecycle code no longer relies on matching exception strings to distinguish a delayed result from malformed/substituted/cryptographically invalid results.
- Added `RemoteResearchProvenanceState.RemoteFailed` and `Expired`.
- Added `NebiusResearchLifecycleReconciler.ReconcileDispatchedAsync`:
  - direct-GET verifies exact remote id and deterministic opaque-id-derived name before any durable mutation;
  - `PROVISIONING` / `STARTING` / `RUNNING` / `CANCELLING` remain nonterminal;
  - `FAILED` / `ERROR` become CAS-protected local `Failed` + `RemoteFailed` with audit evidence;
  - unexpected provider `CANCELLED` becomes a truthful local cancelled terminal state;
  - `COMPLETED` attempts protected exact-once result ingestion;
  - a missing result before persisted expiry remains retryable rather than being treated as failure;
  - a missing result after persisted expiry becomes local `Failed` + `Expired`, with an explicit non-secret error/audit event rather than hanging indefinitely.
- Terminal reconciliation performs best-effort cleanup of protected result/work-item transports only after durable CAS succeeds.
- Hardened cancellation reconciliation to verify both exact remote id and deterministic job name through the same direct-GET identity boundary.
- Added regression coverage for remote terminal failure, delayed completed result before expiry, result-missing terminal expiry, exact persisted expiry, terminal audit evidence, and stricter cancellation identity verification.

Commits this run:
- `3f72aa62290c797c9bd542136f14698bcbd9faa5` — persist remote expiry and typed missing-result state.
- `ee247ff485317b9ac8949229f745f3077982a527` — persist exact protected work-item expiry during two-phase reservation.
- `17d39a23936656dbb1d6376f3bd91a9716ee1be4` — add durable provider terminal and delayed-result reconciliation.
- `3befe98afabb6639b807d12525874bd764e00632` — cover remote failure and delayed-result expiry semantics.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- No mutation was made to keyboard.wtf or any other repository.
- Static review confirms terminal mutations remain behind the existing JSON job-store compare-and-swap boundary.
- Direct GET now verifies both resource id and deterministic name for dispatched reconciliation and cancellation reconciliation.
- Delayed result handling distinguishes only the typed absence condition; malformed, substituted, expired, or cryptographically invalid result envelopes are not downgraded to "not ready".
- Existing constructors remain source-compatible because added record fields are optional/defaulted.
- No GitHub Actions workflow was triggered merely to obtain a green signal.
- This environment still lacks a verified usable .NET/Windows toolchain, so **compilation and test execution are not claimed**.

Security / privacy / cost review:
- Persisted expiry/terminal provenance contains only timestamps and opaque identifiers; no research question, Tavily evidence, checkpoint payload, approval scope, or secret is added.
- Provider terminal errors intentionally use generic local text instead of storing remote raw response bodies that could later contain unexpected provider data.
- A temporarily delayed completed result does not cause repeated Nemotron/Tavily work; reconciliation only polls control-plane/result transport state.
- Cleanup occurs only after a terminal CAS succeeds, preventing cleanup from racing a state that failed to become authoritative.
- Unknown provider states and identity substitution remain fail-closed.

## Known Blockers / Risks
- No verified .NET 8/Windows execution signal is available here; `dotnet build`, `dotnet test`, XAML load, DPAPI, crypto runtime, and live WPF behavior remain unverified.
- The new terminal/expiry paths and tests are statically reviewed but not compiled/executed.
- A `DispatchReserved` record with no attached remote id still remains deliberately ambiguous when bounded listing returns zero; expiry alone is not used to claim that a remote job never existed.
- A provider job that remains nonterminal beyond encrypted input expiry still requires an explicit policy (likely durable cancellation) rather than silently declaring the remote process stopped.
- No production transport is accessible by both Windows and Nebius Serverless, and no built/published worker executable/image exists yet.
- `ResearchJobRuntime` still deliberately rejects non-local records; WPF Serverless controls remain intentionally absent until cloud execution is real and validated.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that remains unavailable, implement the deployable NVIDEA research worker entry point/image plus an authenticated TTL-backed shared transport adapter, then wire a narrow end-to-end Serverless contract probe that exercises opaque dispatch -> one Nemotron/Tavily stage -> protected result -> exact-once ingestion without exposing WPF controls until live Nebius validation succeeds.
