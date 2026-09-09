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
- Tavily Search + Extract research with canonical deduplication, credit accounting, evidence quality/freshness/diversity ranking, provenance, untrusted-evidence boundaries, and validated `[src:SOURCE_ID]` citations.
- Durable research stages checkpoint exact prepared evidence, so resumed synthesis does not repeat Tavily Search/Extract or rerank evidence.
- Protected durable state: Windows CurrentUser DPAPI by default, job-store compare-and-swap, hash-chained/segmented audit trail, and OS-backed single-owner local mutation boundaries.
- WPF surfaces durable local research create/resume/cancel/recovery/report flows and emergency stop.
- Browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit recovery, emergency stop, crash recovery, and single-owner state locking.
- Capability registry, least-privilege policy, exact single-use approvals, durable jobs, and Nebius Serverless contracts exist.
- Nebius Jobs client follows current subnet/disk requirements, List/Get/Create/Cancel, MysteryBox secret refs, plaintext-secret rejection, retry/timeout/cancellation, and endpoint allow-listing.
- Remote research privacy boundary is bidirectional: checkpoint payloads are AES-256-GCM encrypted with per-item keys wrapped to pinned RSA keys; Serverless args contain only opaque work-item IDs and non-secret protocol metadata.
- `NebiusResearchWorker` executes exactly one `ResearchJobHandler` stage and returns a separately protected checkpoint.
- Two-phase cloud dispatch is crash-aware: encrypted work item -> durable `DispatchReserved` CAS -> Nebius Create -> durable remote-id attachment. `RemoteResearchResultIngestor` verifies exact provenance/crypto and applies returned checkpoints exactly once by CAS.
- **New lifecycle reconciler:** unresolved `DispatchReserved` records can recover a uniquely matching deterministic Nebius job through conservative List + direct Get verification without replaying Create. Explicit remote cancellation persists `CancelRequested` before the provider call and finalizes `Cancelled` only after direct Nebius confirmation.
- Production execution remains truthfully local: no concrete shared production transport, built/published worker image, or live Nebius validation exists yet.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, initial Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and OS-backed single-owner browser state.

### 2026-09-09 — Tavily quality and resumable research
Added Tavily Extract enrichment, exact credit accounting, deterministic evidence quality/staleness/diversity handling, staged research boundaries, exact prepared-evidence checkpoints, restart-safe synthesis without repeat Search/Extract, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control-plane foundation
Refreshed the Jobs client to current subnet/disk shape, List/Get/Create/Cancel, conservative responses, MysteryBox refs, secret rejection, and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected worker-result return, one-stage `NebiusResearchWorker`, durable remote provenance, exact job-store CAS, exact-once result ingestion, and two-phase `DispatchReserved` creation ordering.

### 2026-09-09 — Current run: typed remote lifecycle reconciliation and cancellation
Completed:
- Re-read `progress.md` completely and inspected `NebiusServerlessJobClient`, `RemoteResearchResultIngestor`, two-phase dispatcher, job contracts, and existing remote tests before changing code.
- Re-checked current Nebius job lifecycle definitions instead of relying on stale assumptions. Current AI v1 states used by the parser are `PROVISIONING`, `STARTING`, `RUNNING`, `CANCELLING`, `COMPLETED`, `FAILED`, `CANCELLED`, and `ERROR`; unknown future states remain fail-closed.
- Added `NebiusServerlessJobSnapshotParser` and typed `NebiusRemoteJobSnapshot` / `NebiusRemoteJobState`.
- Added `NebiusResearchLifecycleReconciler.ReconcileReservedAsync`:
  - accepts only local Running + exact `DispatchReserved` provenance with no remote id;
  - derives the deterministic job name solely from the durable opaque work-item id;
  - calls Nebius List but refuses paginated responses until pagination is implemented, because a same-name resource might exist on an unseen page;
  - requires exactly one exact-name match;
  - rejects unknown lifecycle states;
  - performs a direct Get on the selected id and re-verifies both id and deterministic name before attachment;
  - attaches via the existing CAS-protected `RemoteResearchResultIngestor` and never replays Create.
- Added explicit durable remote cancellation:
  - `RequestCancellationAsync` CAS-persists `CancelRequested` and an audit event before invoking Nebius Cancel;
  - provider failure deliberately leaves `CancelRequested` durable rather than pretending cancellation succeeded;
  - `ReconcileCancellationAsync` uses direct Get, rejects substituted ids/unknown states, and transitions local job to `Cancelled` only after Nebius reports `CANCELLED`.
- Added deterministic tests for unique exact reconciliation, direct-Get verification, unknown-state rejection, ambiguous duplicate rejection, pagination fail-closed behavior, current Nebius state mapping, durable cancellation-before-provider ordering, and confirmed cancellation finalization.

Commits this run:
- `597800e76b5701bf03d332243cfdae03ddc2e8fe` — initial typed lifecycle reconciler.
- `bc52771e2a3d95de3b4f7dab1fb614fe43232dcf` — initial lifecycle/cancellation tests.
- `07dcd6239b93a3d04ba59678fbe6c316550f11a2` — correct current Nebius states; add pagination refusal + direct Get verification.
- `b71548432ff00f2418ab2d03fdeffe0d3a7e2884` — harden lifecycle regression tests and fix interpolated JSON fixtures.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- No mutation was made to keyboard.wtf or any other repository.
- Current Nebius SDK/docs were consulted for List/Get/Cancel behavior, `items[]` resource listing, metadata id/name, and AI job lifecycle enums.
- Tests use the real JSON job store and CAS path with a deterministic fake Serverless client; no live Nebius/Tavily credentials are required.
- The cancellation fake reads durable state inside its provider callback and asserts `CancelRequested` already exists before the remote call proceeds.
- No GitHub Actions workflow was triggered merely to obtain a green signal.
- This environment still lacks a verified usable .NET/Windows toolchain; **compilation and test execution are not claimed**.

Security / privacy / permissions / failure / cost review:
- Reconciliation sends no user question/evidence to the control plane; matching uses only deterministic names derived from opaque encrypted work-item IDs.
- Zero matches, multiple matches, paginated partial lists, unknown states, substituted direct-Get identities, and stale CAS all fail closed without replaying provider work.
- Remote cancellation is two-step and durable, so a process crash cannot turn a requested cancellation into a falsely reported local success.
- Generic browser/consequential jobs are untouched; this recovery authority remains research-specific.
- One unresolved reservation should not be automatically abandoned merely because List returns zero: a partial/out-of-date control-plane view cannot prove the remote job never existed.

## Known Blockers / Risks
- No verified .NET 8/Windows execution signal is available here; `dotnet build`, `dotnet test`, XAML load, DPAPI, crypto runtime, and live WPF behavior remain unverified.
- New lifecycle reconciliation/cancellation code is statically reviewed but not compiled/executed.
- Current `ListAsync` exposes only one response page; reconciliation intentionally refuses `nextPageToken` instead of risking false uniqueness. Proper bounded pagination is still needed.
- No production transport is accessible by both Windows and Nebius Serverless. It needs authenticated, bounded, TTL-backed, preferably create-once/write-once semantics.
- No built/published NVIDEA worker executable/image exists yet. `NebiusResearchWorker` remains a core primitive, not a deployable entry point.
- Reserved encrypted work-item expiry is not yet durably represented in local provenance, so safe TTL-based reservation expiry/release is not implemented.
- Remote terminal failure (`FAILED`/`ERROR`) handling and result-not-found recovery need an explicit durable terminal path rather than an indefinite Running record.
- `ResearchJobRuntime` still deliberately rejects non-local records and therefore is not yet the production Serverless controller.
- WPF Serverless controls/history are intentionally absent until live cloud execution is real and validated.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that remains unavailable, complete the **remote terminal lifecycle**: add bounded pagination to Nebius List, persist work-item expiry in remote provenance, safely resolve expired `DispatchReserved` records only when absence can be proven, handle `FAILED`/`ERROR` terminal jobs with durable audit/error state, and reconcile completed jobs whose encrypted result is delayed/missing. Then build the deployable worker entry point/image + authenticated shared transport and perform live Nebius validation before advertising Serverless in WPF.
