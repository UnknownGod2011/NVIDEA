# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must be independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Architecture / Product State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`.
- Nebius Token Factory / NVIDIA Nemotron inference abstraction with structured output/tool handling, retry/timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + advanced Extract research with canonical deduplication, credit accounting, authority/freshness/diversity ranking, uncertainty warnings, provenance, untrusted-evidence boundaries, and machine-validated `[src:SOURCE_ID]` citations.
- Durable research stages: request -> Nemotron plan -> Tavily Search/Extract + ranking -> prepared evidence -> Nemotron synthesis -> completed report. Exact prepared evidence is checkpointed so resume does not repeat Search/Extract or rerank evidence.
- `JsonAgentJobStore` and `JsonLinesAuditTrail` use Windows CurrentUser DPAPI by default on Windows. Job-store CAS protects remote provenance/result application.
- `ResearchJobRuntime` remains the trusted local production host. WPF surfaces durable research create/resume/cancel/recovery/report flows and participates in emergency stop.
- Playwright browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit recovery, emergency stop, crash recovery, and pre-transport single-owner browser-state locking.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Nebius Serverless Jobs client follows current subnet/disk requirements, direct status GET, conservative parsing, MysteryBox secret refs, plaintext-secret rejection, retry/timeout/cancellation, and endpoint allow-listing.
- Research cloud privacy boundary exists in both directions: work checkpoints are AES-256-GCM protected with per-item keys wrapped to a pinned worker RSA key; worker results are independently protected back to the originating client key. Serverless args carry only opaque work-item IDs and non-secret protocol metadata.
- `NebiusResearchWorker` executes exactly one `ResearchJobHandler` stage, rejects private OS-local/approval-bearing work, and returns a protected checkpoint.
- `RemoteResearchResultIngestor` verifies exact local job, remote job, opaque work item, checkpoint identity, crypto envelope and state before exact-once CAS result application.
- **New two-phase dispatch boundary:** `DispatchReserved` provenance freezes the exact local checkpoint + opaque work-item ID before Nebius job creation. `TwoPhaseNebiusResearchDispatcher` performs prepare/upload -> durable reservation -> Nebius create -> receipt attach. This prevents the previous failure mode where a remote job could be accepted before any local provenance existed.
- Production execution remains truthfully local: no concrete shared cloud transport, built/published worker image, or live Nebius validation exists yet.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, initial Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and OS-backed single-owner browser state.

### 2026-09-09 — Tavily quality, resumable research, Windows UX, recovery
Added Tavily Extract enrichment, exact credit accounting, deterministic evidence quality/staleness/diversity handling, staged research boundaries, exact prepared-evidence checkpoints, restart-safe synthesis without repeat Search/Extract, privacy-safe status, protected local persistence, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control-plane foundation
Refreshed the Serverless client to current required subnet/disk shape, direct status GET, conservative parsing, MysteryBox refs, secret rejection, and endpoint allow-listing. Added encrypted opaque-ID research dispatch, protected worker-result return, one-stage `NebiusResearchWorker`, durable remote provenance, job-store CAS, and exact-once result ingestion.

### 2026-09-09 — Current run: two-phase crash-safe remote dispatch
Completed:
- Re-read `progress.md` completely and inspected the current remote research provenance, dispatcher, Serverless client contract, job-store CAS path, and remote-result tests before changing code.
- Added `RemoteResearchProvenanceState.DispatchReserved` and made `RemoteJobId` nullable only during that state.
- Added `RemoteResearchDispatchReservation` and `RemoteResearchResultIngestor.ReserveDispatchAsync`:
  - only a Pending + Local research stage with no approval authority can be reserved;
  - reservation CAS-freezes the exact checkpoint step/timestamp plus opaque work-item ID;
  - the job becomes Running but remains Local, attempt count is not incremented, and an audit event is emitted;
  - a second reservation or normal local replay fails closed.
- Hardened `AttachDispatchAsync` so it no longer attaches directly from Pending. It requires an exact `DispatchReserved` record whose opaque ID, checkpoint step and checkpoint timestamp match the Nebius receipt; only then does execution become `NebiusServerless` and attempt increment.
- Added `TwoPhaseNebiusResearchDispatcher`:
  - `PrepareAsync` validates exact cloud disclosure, encrypts the work item and uploads it without contacting Nebius;
  - `DispatchWithReservationAsync` persists the exact opaque-ID reservation before invoking `CreateAsync`;
  - `StartPreparedAsync` sends only the opaque work-item ID to the worker and uses a deterministic remote job name derived from it;
  - preparation ciphertext is best-effort cleaned if local reservation fails;
  - ambiguous create success with no resource ID deliberately leaves the reservation + encrypted payload intact rather than silently replaying or deleting uncertain work.
- Added/updated deterministic tests covering reservation-before-create ordering, direct-attach rejection, exact reservation attachment, successful result ingestion after two-phase dispatch, ambiguous create fail-closed behavior, and ciphertext cleanup when reservation itself fails.

Commits this run:
- `fa8f33c294385fed7d80b2063b4a363e3c40e47b` — add durable `DispatchReserved` provenance and exact attach invariant.
- `24f2bb2943fa4d79a1bae138ba02bc0a26cf2909` — update remote-ingestion tests for mandatory reservation.
- `534fefe9693ea96df502ca725874e80c4abdf6a2` — add two-phase crash-safe Nebius research dispatcher.
- `6f0fa340e0cd7fa8fef03e5782442d029692cb6f` — add two-phase dispatch ordering/failure regression coverage.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- No mutation was made to keyboard.wtf or any other repository.
- Tests use the real JSON job store, generated RSA keys, protected work-item crypto, in-memory transports, and an inspecting fake `INebiusServerlessJobClient`; no Nebius/Tavily credentials are required.
- The fake Serverless client asserts the durable job is already `DispatchReserved` before its `CreateAsync` callback can return.
- This execution environment still has no usable `dotnet`, `msbuild`, or `csc`; **compilation and test execution are not claimed**.
- No GitHub Actions workflow was triggered merely to obtain a green signal.

Security / privacy / permissions / failure / cost review:
- Reservation provenance contains only opaque identifiers and checkpoint identity; user question/evidence remain inside protected local state and encrypted transport payloads.
- A reservation carries no remote job ID and no approval authority; it is deliberately non-replayable until explicitly reconciled.
- Attempt count increments only once a concrete Nebius resource ID is attached, avoiding accounting a mere local reservation as executed provider work.
- The remaining unavoidable ambiguity is narrower: a crash **after Nebius accepts Create but before its returned resource ID is CAS-attached** leaves a durable `DispatchReserved` record instead of losing all provenance. Its opaque ID also gives a deterministic remote job name for future list/reconciliation logic.
- Encrypted work objects remain TTL-bounded; uncertain remote acceptance never triggers destructive cleanup that could strand a real worker.

## Known Blockers / Risks
- No verified .NET 8/Windows execution signal is available here; `dotnet build`, `dotnet test`, XAML load, DPAPI, crypto runtime, and live WPF behavior remain unverified.
- New two-phase dispatcher/reservation code is statically reviewed but not compiled/executed.
- No production transport is accessible by both Windows and Nebius Serverless. It needs authenticated, bounded, TTL-backed, preferably create-once/write-once semantics.
- No built/published NVIDEA worker executable/image exists yet. `NebiusResearchWorker` remains a core primitive, not a deployable entry point.
- A crash after Nebius accepts Create but before receipt attachment is still ambiguous; the reservation now preserves the exact opaque ID/checkpoint and deterministic job name, but typed list/reconciliation logic is not yet implemented.
- Remote status polling and explicit remote cancellation are not integrated with the durable provenance lifecycle yet.
- `ResearchJobRuntime` still deliberately rejects non-local records and therefore is not yet the production Serverless controller.
- WPF research still favors the newest nonterminal job instead of a polished multi-job selector/history.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that remains unavailable, implement **typed remote lifecycle reconciliation** for `DispatchReserved`/`Dispatched`: parse Nebius List/Get into conservative job states, match only the deterministic job name derived from the durable opaque ID, attach a unique matching remote resource ID by CAS, fail closed on zero/multiple/unknown matches, expire truly unstarted reservations only after their encrypted work-item TTL, and add explicit remote cancellation with durable `CancelRequested -> Cancelled` transitions. Then add the deployable worker entry point/image + authenticated shared transport and perform live Nebius validation before advertising Serverless in WPF.
