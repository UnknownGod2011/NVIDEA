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
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, and validated citations.
- Durable staged research checkpoints make synthesis restart-safe without repeating Tavily retrieval.
- Protected local state uses Windows CurrentUser DPAPI by default, job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and explicit crash recovery.
- Remote research has bidirectional encrypted payload transport, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation across bounded complete job listings, durable cancellation, terminal/result-expiry handling, mounted encrypted transport, non-root worker image, and a signed authoritative remote-ID binding protocol.
- Signed binding publication is wired into normal two-phase dispatch, crash-window reservation recovery, and dispatched reconciliation; terminal binding cleanup is race-safe and best effort after durable terminal state.
- `NebiusResearchClientRuntime` provides one client-only composition boundary from a single signing identity.
- Nebius Serverless job creation models `spec.volumes[]`; research dispatch carries configured shared transport mounts to the worker.
- `NebiusResearchDeploymentPreflight` verifies the live worker topology without resolving secret values: exact `NVIDEA_TRANSPORT_ROOT`/volume-path match, `READ_WRITE` transport, MysteryBox-backed Nebius/Tavily/worker-private-key credentials, and the client public key required for signed binding verification.
- `NebiusResearchLiveRuntimeFactory` is the explicit production composition gate and runs deployment preflight before constructing the live remote runtime.
- Current Nebius Serverless lifecycle parsing recognizes preparation states `PROVISIONING`, `IMAGE_PULLING`, `STARTING`; active `RUNNING`; teardown states `CANCELLING`, `DELETING`; terminal `COMPLETED`, `FAILED`, `ERROR`, and `CANCELLED`. Unknown future states remain fail-closed.
- `tools/Nvidea.NebiusContractProbe` has an explicit opt-in `--live-research` mode that constructs through the production live factory and can exercise all three durable research stages remotely. The cheap Token Factory planner probe remains the default.
- Native Windows-side S3-compatible protected transport now exists: `NebiusObjectStorageClient` + `S3ProtectedResearchTransport` allow the client to publish/read encrypted work items, signed bindings, and encrypted results directly through Nebius Object Storage instead of requiring an external bucket mount.
- Production remains truthfully local until the live Nebius/Object Storage/MysteryBox/container end-to-end probe actually succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Added current Jobs REST contracts, MysteryBox secret refs, encrypted opaque-ID dispatch, protected result return, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable cancellation, bounded pagination, terminal/result-expiry reconciliation, mounted encrypted transport, non-root worker image, signed authoritative resource-ID bindings, automatic binding publication after durable remote-ID attachment, and terminal binding cleanup.

### 2026-09-09 — Live deployment preflight + probe
Added `NebiusServerlessVolumeMount`, strict mount validation, dispatcher passthrough, `NebiusResearchDeploymentPreflight`, `NebiusResearchLiveRuntimeFactory`, current provider-state handling, and `Nvidea.NebiusContractProbe --live-research` for the complete three-stage durable research workflow. The probe remains opt-in and no live PASS is claimed without real infrastructure.

### 2026-09-10 — Current run: native Nebius Object Storage protected transport
Completed:
- Re-read `progress.md` completely and inspected the existing mounted-directory transport, protected work-item/result/binding interfaces, live probe, test style, and current project dependencies.
- Refreshed current Nebius Object Storage/Serverless documentation. Nebius Object Storage is S3-compatible, uses static service-account keys for S3 clients, and Serverless Jobs support read/write Object Storage bucket mounts. AWS SDK for .NET v4 supports conditional `PutObject` with `If-None-Match: *`.
- Added `src/Nvidea.Core/Jobs/NebiusObjectStorageProtectedResearchTransport.cs`.
- Added `IProtectedResearchObjectStoreClient`, a narrow object-store boundary with create-once put, bounded get, and idempotent delete.
- Added `NebiusObjectStorageClient`, a production S3-compatible client using AWS SDK for .NET v4 and static Object Storage credentials.
- Added `S3ProtectedResearchTransport`, implementing all three existing protected research transport interfaces without changing the cryptographic protocols.
- Preserved namespace isolation: `work-items/`, `dispatch-bindings/`, `results/`.
- Added create-once S3 writes using `IfNoneMatch = "*"`; 412 duplicate writes fail closed, 409 conflicts use a small bounded retry budget, and exhausted conflicts never overwrite.
- Added strict 4 MiB transport bounds on writes and reads, including streaming byte-count enforcement after the provider content-length check.
- Added per-call cancellation-backed deadlines, explicit retry bounds, object-key/prefix validation, and sanitized provider failures that do not include bucket names, object keys, endpoints, access-key IDs, response bodies, or secrets.
- Disabled opaque SDK retry behavior (`MaxErrorRetry = 0`) so retry behavior remains explicit in NVIDEA.
- Added `AWSSDK.S3` v4 dependency (`4.0.102.5`), chosen from the current supported v4 line rather than EOL v3.
- Added `tests/Nvidea.Core.Tests/S3ProtectedResearchTransportTests.cs` covering round-trip behavior, namespace isolation, duplicate-write fail-closed behavior, idempotent delete, invalid opaque IDs, non-HTTPS endpoint rejection, escaping-prefix rejection, and retry-bound validation.
- Added `docs/nebius-object-storage-transport.md` describing the security model, credentials boundary, deployment topology, and validation needed before UI exposure.

Commits this run:
- `6e0a0675916892caee87220d8214fccd9b827733` — add native Nebius Object Storage research transport.
- `cd9805429d1f210afd671fe4ced5f6f2410becec` — add AWS S3 SDK v4 dependency.
- `213ca5b4eb11d3fd4b0033c8a5ea39078d7cca70` — add native S3 transport regression tests.
- `d5637c270b6253f0b454461ee2a97d221c2d95e2` — document native Object Storage transport.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation and every write targeted exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Current official Nebius documentation confirms Object Storage S3-compatible access and read/write Object Storage mounts for Serverless Jobs.
- Current AWS SDK for .NET v4 documentation confirms `PutObjectRequest.IfNoneMatch` supports atomic create-if-absent behavior.
- Static review confirmed `S3ProtectedResearchTransport` reuses the existing protected envelope interfaces, so two-phase dispatch, signed binding publication, lifecycle reconciliation, exact-once ingestion, and cleanup can consume it without protocol changes.
- Static review confirmed the object-store abstraction keeps AWS-specific behavior out of the protected-envelope codec and makes the transport regression-testable without network credentials.
- This runtime has no .NET SDK (`dotnet` is unavailable) and cannot resolve GitHub from the container, so compilation/test execution is **not claimed**.
- No live Nebius Object Storage credentials/bucket or Serverless resources are available here, so no real S3 or Serverless PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Object Storage receives the same encrypted work-item/result envelopes and signed control-plane binding already used by the mounted transport; research plaintext is not newly exposed.
- Static Object Storage credentials are constructor inputs only and are never formatted into NVIDEA error messages or logs.
- The production client requires HTTPS and rejects path-like namespace escapes/control characters.
- Writes never replace an existing protected object. Ambiguous provider conflicts fail closed after bounded retries.
- Reads trust neither declared object length nor stream length alone; both are bounded.
- Client cancellation and local timeouts remain distinguishable so user cancellation is not misreported as provider timeout.
- A dedicated least-privilege Object Storage service account/bucket is still required for production deployment.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new Object Storage code is statically reviewed but not compiled/executed.
- No live Object Storage bucket, static Object Storage service-account key, immutable registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- `--live-research` still constructs `DirectoryProtectedResearchTransport` for the client. The new native S3 transport is not yet wired into the probe because the exact bucket-root/prefix-to-Serverless-mounted-path mapping must be made explicit and tested rather than guessed.
- The worker still appropriately uses `DirectoryProtectedResearchTransport` against its mounted bucket; the Windows/client path should switch to `S3ProtectedResearchTransport` only after mount-prefix alignment is encoded in live configuration/preflight.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live end-to-end contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Wire `S3ProtectedResearchTransport` into `Nvidea.NebiusContractProbe --live-research` with an explicit dedicated-bucket/prefix mapping that is proven to resolve to the worker's mounted directory, add preflight tests preventing bucket/prefix/mount mismatches, then run the first real credential/resource-supplied end-to-end contract when infrastructure is available: encrypted S3 dispatch -> authoritative Nebius ID -> signed binding -> mounted worker -> Nemotron/Tavily stage -> encrypted S3 result -> exact-once local ingestion -> cleanup.
