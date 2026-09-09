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
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker project in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + Extract research with canonical deduplication, exact credit accounting, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, and validated citations.
- Durable staged research checkpoints prepared evidence so restart-safe synthesis does not repeat Tavily retrieval work.
- Protected local state uses Windows CurrentUser DPAPI by default, job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and explicit crash recovery.
- Remote research has bidirectional protected transport primitives, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation across bounded complete job listings, durable remote cancellation, persisted encrypted-work-item expiry, explicit provider terminal/delayed-result handling, and a directory-backed encrypted transport suitable for a Nebius Object Storage mounted volume.
- `src/Nvidea.Worker/Dockerfile` now defines a non-root .NET 8 worker image that composes the real Nemotron + Tavily stage execution path.
- Production remains truthfully local until the authoritative Nebius remote-job-ID handoff into the running worker and live end-to-end Serverless validation exist.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, MysteryBox secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, and explicit terminal/result-expiry reconciliation.

### 2026-09-09 — Current run: deployable worker + mounted encrypted transport
Completed:
- Re-read `progress.md` completely and inspected current recent commits, remote dispatch/result protocol, `ResearchJobHandler`, `ResearchEngine`, Nemotron/Tavily constructors, and the current project layout before changing code.
- Verified current official Nebius Serverless documentation still supports persistent Object Storage bucket mounts for jobs and MysteryBox-backed secret injection; used those native primitives as the deployment target rather than introducing an unrelated cloud database.
- Added `DirectoryProtectedResearchTransport`, implementing both protected work-item and protected result transports over an explicitly shared absolute directory. It stores only encrypted protocol envelopes under separate `work-items/` and `results/` namespaces.
- Transport enforces bounded envelope size, strict opaque-ID validation/path traversal rejection, create-once semantics, temp-file write + move, bounded reads, and explicit deletion.
- Added `src/Nvidea.Worker/Nvidea.Worker.csproj` and `Program.cs` as a real .NET 8 executable entry point. It composes `NebiusTokenFactoryClient`, `TavilyResearchClient`, `ResearchEngine`, `ResearchJobHandler`, `DirectoryProtectedResearchTransport`, and `NebiusResearchWorker` to execute exactly one research checkpoint stage.
- Worker requires the Nebius/Tavily keys, worker private key, client public key, transport root, and exact remote job ID from environment. It logs only a generic success marker or exception type, never user research content/evidence/provider bodies/secrets.
- Added `src/Nvidea.Worker/Dockerfile`: multi-stage .NET 8 publish, runtime-only final image, non-root UID/GID `65532`, no baked credentials.
- Added `DirectoryProtectedResearchTransportTests` covering protected work-item round-trip/delete, duplicate result write refusal, and traversal/invalid opaque-ID rejection.
- Added `docs/nebius-research-worker.md` with the storage mount, MysteryBox secret contract, container build command, non-secret environment contract, and the unresolved authoritative remote-job-ID handoff documented explicitly.
- Identified a real architecture issue rather than hiding it: Nebius returns the authoritative job resource ID after create, while current official Serverless Jobs documentation does not document an automatically injected current-job-ID variable inside the container. The existing result protocol correctly authenticates the resource ID, so NVIDEA must not replace it with the deterministic job name.

Commits this run:
- `db33cd4684bbdca9f8ab7abb03ae227e9ac8444b` — add mounted encrypted research transport.
- `b702c85bb50ba280d749a4882f70dfe1a3af914c` — add deployable worker project.
- `d0081196772fb8118136e3cec06190fda7e53f34` — wire one-stage Nemotron/Tavily worker entry point.
- `77470cb6ddbc9619885e3d25f64a56235a77d1bb` — containerize worker with non-root runtime image.
- `93369493b0bd313027603271b531a668239be467` — add mounted transport safety regression tests.
- `9e1594ca2505b9c276184577a5a7bf7c6cb3efa3` — document Nebius worker deployment/provenance boundary.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- No mutation was made to keyboard.wtf or any other repository.
- Current official Nebius docs state that Serverless Jobs can mount Object Storage buckets as `--volume` paths and can inject `--env-secret` values from MysteryBox; Object Storage is S3-compatible and intended for persisted files/checkpoints.
- Static review confirms the worker invokes the genuine existing Nemotron + Tavily research stage rather than a fake/demo stub.
- Static review confirms only already-encrypted envelopes are written by the new shared-directory transport.
- The Docker image does not contain keys and runs the published worker as a non-root user.
- No GitHub Actions workflow was triggered merely to obtain a green signal.
- This environment still lacks a verified usable .NET/Windows/container toolchain, so **compilation, test execution, Docker build, and live Nebius execution are not claimed**.

Security / privacy / cost review:
- The shared transport stores protected work/result envelopes only; question/evidence/checkpoint plaintext remains inside authenticated encryption.
- Opaque IDs are constrained to the existing URL-safe alphabet before being used as filenames, blocking path traversal through transport keys.
- Create-once result/work-item semantics avoid silently replacing encrypted evidence for an existing opaque dispatch.
- Worker errors intentionally omit exception messages to reduce the chance of API/provider data entering Serverless logs.
- Worker private RSA key, Token Factory key, and Tavily key are intended for MysteryBox injection and are not committed.
- A dedicated Standard Object Storage bucket is sufficient for these small bounded encrypted envelopes; expensive enhanced-throughput storage is unnecessary for the demo path.

## Known Blockers / Risks
- No verified .NET 8/Windows execution signal is available here; `dotnet build`, `dotnet test`, XAML load, DPAPI, crypto runtime, Docker build, and live WPF behavior remain unverified.
- The new worker/transport/tests are statically reviewed but not compiled/executed.
- The worker currently requires `NVIDEA_REMOTE_JOB_ID`, but `TwoPhaseNebiusResearchDispatcher` cannot know that ID until after Nebius `Create` returns. Current official docs do not establish an automatic in-container job-ID variable, so automatic production dispatch is not yet wired.
- A `DispatchReserved` record with no attached remote id remains deliberately ambiguous when bounded listing returns zero; expiry alone is not used to claim that a remote job never existed.
- No live Object Storage bucket, registry image, MysteryBox keys, subnet, or Serverless job has been provisioned/validated in this environment.
- `ResearchJobRuntime` still deliberately rejects non-local records; WPF Serverless controls remain intentionally absent until cloud execution is real and validated.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Implement the durable **remote dispatch-binding handoff** keyed by opaque work-item ID: after `AttachDispatchAsync` (or crash reconciliation) authoritatively obtains the Nebius resource ID, publish a bounded binding into the shared encrypted/mounted transport; make the worker wait with timeout/cancellation for that exact binding before executing; verify opaque ID + deterministic job name/resource ID consistency; add duplicate/substitution/expiry tests; then run a narrow container/Serverless contract probe when credentials/tooling are available. Only after that end-to-end path succeeds should WPF expose Nebius Serverless research.
