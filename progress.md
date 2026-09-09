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
- `NebiusResearchClientRuntime` provides a single client-only composition boundary that constructs dispatcher, reconciler, ingestor, and binding publisher from one signing identity and performs race-safe post-terminal binding cleanup.
- Nebius Serverless job creation now models the documented `spec.volumes[]` contract and research dispatch passes configured shared transport mounts through to the worker, closing the previous control-plane gap that made `DirectoryProtectedResearchTransport` undeployable on a real job.
- Production remains truthfully local until a live Nebius/Object Storage/SecretStash-or-MysteryBox/container end-to-end probe succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, explicit terminal/result-expiry reconciliation, mounted encrypted transport, deployable non-root worker image, signed authoritative resource-ID bindings, automatic binding publication after durable remote-ID attachment, and terminal binding cleanup.

### 2026-09-09 — Current run: verified Serverless transport mount contract
Completed:
- Re-read `progress.md` completely and inspected current NVIDEA repo state, recent commits, the existing Nebius contract probe, Serverless REST client, two-phase dispatcher, worker, and relevant tests.
- Re-checked current official Nebius Serverless documentation and the current official Nebius Go SDK protobuf before changing API assumptions.
- Found a verified deployment blocker: Nebius Jobs support mounted buckets/shared filesystems through `spec.volumes[]`, but `NebiusServerlessJobSpec` did not model or serialize volumes, while `Nvidea.Worker` requires a shared `NVIDEA_TRANSPORT_ROOT` for work items, signed bindings, and encrypted results.
- Added `NebiusServerlessVolumeMount` with `Source`, optional `SourcePath`, absolute `ContainerPath`, and provider mode `READ_WRITE`/`READ_ONLY`.
- `NebiusServerlessJobClient.CreateAsync` now serializes configured mounts under the documented `spec.volumes[]` shape and omits the property when no mounts are configured, preserving existing non-volume job payloads.
- Added pre-network validation for blank/oversized/control-character sources, non-absolute container paths, duplicate mount points, unsupported modes, and malformed source paths.
- Extended `NebiusResearchDispatchOptions` with optional volume mounts without breaking existing positional callers, and passed those mounts through both `NebiusResearchDispatcher` and the crash-safe `TwoPhaseNebiusResearchDispatcher`.
- Added `NebiusServerlessVolumeMountTests.cs` covering exact REST JSON shape, omission when unused, relative-path/unsupported-mode rejection, and duplicate-container-path rejection.
- Added `TwoPhaseNebiusVolumeMountTests.cs` proving configured shared transport mount plus `NVIDEA_TRANSPORT_ROOT` survive the research dispatcher boundary into the exact Nebius job specification.
- Updated `docs/nebius-research-worker.md` with the now-explicit shared mount contract and removed the stale statement that dispatch-binding cleanup was still missing.

Commits this run:
- `93870a913c04bd4f0cc8cbda0126eff9f97ab824` — add Nebius Serverless volume mount support.
- `14bc6e1fd83e58d10df8f5bad99b2221ef3a4bd7` — harden optional volume serialization and absolute-path validation.
- `fecd8635d4523c78b59fdb023e38c57f342f0dd6` — wire shared transport volumes into research dispatch options/legacy path.
- `dfac05c7e1fc71e8a8896e21bbdc989d646c5c4f` — pass mounted volumes through the two-phase dispatcher.
- `b098acdcdea52399d959b10165606d17e58aaece` — add Serverless volume REST contract tests.
- `d17bcbcfcd39feab5d08525d9ee8eac8adb3db66` — add two-phase transport mount passthrough regression test.
- `791fcbc27f34d23e58ce7b03085d972d25b1c19c` — document verified Nebius transport mount contract.

Validation / evidence:
- Repository metadata reported `repository_full_name = UnknownGod2011/NVIDEA` immediately before every GitHub mutation; no other repository was mutated.
- Current Nebius Serverless REST documentation explicitly documents `spec.volumes[].source`, absolute `containerPath`, and `mode = READ_ONLY|READ_WRITE`, and recommends mounted volumes for job results/checkpoints.
- Current official Nebius SDK protobuf independently confirms `JobSpec.volumes` and the same volume field/mode names.
- Static review confirms research dispatch now carries the mount without placing research payloads or API keys into arguments/environment.
- Static review confirms the worker's plaintext `NVIDEA_TRANSPORT_ROOT` can point to the same non-secret absolute container mount path while API/RSA secrets remain secret-injected.
- `dotnet`/Windows/container execution is not available in this automation environment, so compilation and unit-test execution are **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Volume configuration contains infrastructure identity/path metadata only; encrypted research payloads remain in the protected transport objects.
- The research worker needs `READ_WRITE` because it must publish encrypted results and consume/create-once control-plane artifacts; no secret is introduced into the new volume model.
- Optional volumes are omitted entirely for jobs that do not use them.
- Invalid volume paths/modes fail before any network request, reducing accidental host-path assumptions and ambiguous provider behavior.
- Shared transport remains protected by encryption/signatures, bounded opaque IDs, create-once publication, TTLs, terminal cleanup, and provider lifecycle retention as defense in depth.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket/filesystem, registry image, SecretStash/MysteryBox keys, subnet, or Serverless job has been provisioned/validated in this environment.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live end-to-end contract succeeds.
- Current official Nebius protobuf exposes additional legitimate job states `IMAGE_PULLING` and `DELETING`; NVIDEA's lifecycle parser still maps those to `Unknown`. This now needs a small verified parser/test refresh before live reconciliation to avoid false fail-closed handling during normal provider transitions.
- The code does not yet enforce that a configured `NVIDEA_TRANSPORT_ROOT` exactly equals one configured volume's `ContainerPath`; deployment docs specify the invariant, but production composition should validate it automatically.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Finish the live-deployment preflight instead of adding unrelated abstraction: refresh lifecycle parsing/tests for current Nebius `IMAGE_PULLING`/`DELETING` states, add a fail-fast research deployment validator that proves `NVIDEA_TRANSPORT_ROOT` matches a `READ_WRITE` volume and all required worker secrets are secret references, then extend the existing contract probe to create/reconcile one real mounted worker job when credentials/resources are supplied. The proof target remains `encrypted opaque dispatch -> mounted shared transport -> durable remote-id attachment -> signed binding -> worker one-stage Nemotron/Tavily execution -> encrypted result -> exact-once local ingestion -> terminal binding cleanup`. WPF Serverless controls stay hidden until that succeeds.
