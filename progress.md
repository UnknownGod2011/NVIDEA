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
- Remote research has bidirectional protected transport primitives, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation, and durable remote cancellation.
- Production remains truthfully local until a concrete shared transport, deployable worker image, and live Nebius validation exist.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, MysteryBox secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, and durable `CancelRequested -> Cancelled` handling.

### 2026-09-09 — Current run: bounded Nebius pagination foundation
Completed:
- Re-read this file completely and inspected the current Jobs client, lifecycle reconciler, job contracts, remote provenance model, and existing Nebius tests before changing code.
- Extended `INebiusServerlessJobClient` with an explicit continuation-page overload while preserving source compatibility for existing fake clients through a fail-closed default implementation.
- Updated the real `NebiusServerlessJobClient` so continuation pages are requested with a URL-escaped `pageToken`; tokens are bounded to 4096 characters and control characters are rejected before network I/O.
- Added `NebiusBoundedJobListReader` as the safe pagination primitive for lifecycle reconciliation:
  - default maximum 8 pages;
  - default maximum 2,000 aggregate parsed resources;
  - hard configuration ceilings of 32 pages / 10,000 items;
  - repeated continuation-token detection;
  - terminal failure when the page bound is exhausted before a final page;
  - no partial listing is returned when any safety bound is violated.
- Added deterministic tests for successful two-page traversal, repeated-token rejection, page-bound rejection, and aggregate-item-bound rejection.

Commits this run:
- `1927123427ac1cd1eda5ffa1b8ddc4c40ee0791c` — continuation-page Nebius client contract.
- `130eb21c011a97c75f21571a506733a6a4224723` — bounded pagination reader.
- `5533561706d5bd57d335a37293c9b4cfcebb5099` — pagination safety regression coverage.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- No mutation was made to keyboard.wtf or any other repository.
- The pagination path is bounded against provider-controlled infinite loops and oversized result sets.
- Existing fake clients remain source-compatible for first-page listing; any attempt to follow a continuation token through a client that has not implemented pagination fails closed.
- No GitHub Actions workflow was triggered merely to obtain a green signal.
- This environment still lacks a verified usable .NET/Windows toolchain, so **compilation and test execution are not claimed**.

Security / privacy / cost review:
- Pagination carries only Nebius control-plane continuation tokens and project-scoped job metadata; it does not send user questions, Tavily evidence, checkpoints, or approval scopes.
- Tokens are never logged or persisted by the new reader.
- Repeated provider tokens and excessive page/item counts fail closed, bounding API cost and preventing malicious or malformed pagination loops.
- The existing lifecycle reconciler has not yet been switched from its single-page refusal to the new bounded reader in this run; that integration must preserve exact deterministic-name uniqueness across all bounded pages and direct-GET verification.

## Known Blockers / Risks
- No verified .NET 8/Windows execution signal is available here; `dotnet build`, `dotnet test`, XAML load, DPAPI, crypto runtime, and live WPF behavior remain unverified.
- The new pagination client/reader/tests are statically reviewed but not compiled/executed.
- Lifecycle reconciliation still refuses a paginated first response until it is explicitly wired to `NebiusBoundedJobListReader`; the safe primitive now exists, but production reconciliation does not consume it yet.
- Reserved encrypted work-item expiry is not durably represented in local provenance.
- Remote terminal failure (`FAILED`/`ERROR`) and delayed/missing result handling still need explicit durable state transitions.
- No production transport is accessible by both Windows and Nebius Serverless, and no built/published worker executable/image exists yet.
- `ResearchJobRuntime` still deliberately rejects non-local records; WPF Serverless controls remain intentionally absent until cloud execution is real and validated.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that remains unavailable, wire `NebiusResearchLifecycleReconciler.ReconcileReservedAsync` to `NebiusBoundedJobListReader` so deterministic uniqueness is checked across all bounded pages, then add durable work-item expiry and explicit `FAILED`/`ERROR` / delayed-result terminal handling. After that, build the deployable worker entry point/image + authenticated shared transport and perform live Nebius validation before advertising Serverless in WPF.
