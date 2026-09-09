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
- `NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment` additionally proves that the native client bucket/prefix and the Serverless mounted bucket/SourcePath resolve to the same protected research objects before any job is submitted.
- `NebiusResearchLiveRuntimeFactory` is the explicit production composition gate and runs deployment preflight before constructing the live remote runtime.
- Current Nebius Serverless lifecycle parsing recognizes preparation states `PROVISIONING`, `IMAGE_PULLING`, `STARTING`; active `RUNNING`; teardown states `CANCELLING`, `DELETING`; terminal `COMPLETED`, `FAILED`, `ERROR`, and `CANCELLED`. Unknown future states remain fail-closed.
- Native Windows-side S3-compatible protected transport exists: `NebiusObjectStorageClient` + `S3ProtectedResearchTransport` publish/read encrypted work items, signed bindings, and encrypted results directly through Nebius Object Storage.
- `tools/Nvidea.NebiusContractProbe --live-research` now uses that native Object Storage transport on the client and the Serverless-mounted directory transport in the worker, with explicit bucket/prefix/mount alignment preflight. The cheap Token Factory planner probe remains the default.
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

### 2026-09-10 — Native Nebius Object Storage transport
Added `IProtectedResearchObjectStoreClient`, `NebiusObjectStorageClient`, and `S3ProtectedResearchTransport`; preserved independent `work-items/`, `dispatch-bindings/`, and `results/` namespaces; added create-once conditional S3 writes, 4 MiB bounds, explicit retries/timeouts, sanitized errors, and regression coverage. Added AWS SDK for .NET v4 and documented the least-privilege Object Storage security boundary.

### 2026-09-10 — Current run: native S3 live-probe integration
Completed:
- Re-read `progress.md` completely and inspected the existing live research probe, deployment preflight, Serverless volume model, S3 protected transport, and preflight tests before changing anything.
- Refreshed current official Nebius documentation: Serverless Jobs remain finite container workloads, Object Storage remains S3-compatible, and Object Storage client access uses static service-account keys.
- Added `NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment`.
- The alignment preflight first runs the existing live deployment validation, then requires the native Object Storage bucket to exactly match the mounted Serverless volume source.
- It also requires the native client prefix to exactly match the mounted volume `SourcePath`, after conservative slash normalization. Path traversal, backslashes, control characters, path-like volume sources, and ambiguous mismatches fail before Serverless submission.
- Added regression tests covering exact bucket/prefix mapping, bucket-root mapping, bucket mismatch, prefix mismatch, and path-like volume-source rejection.
- Replaced `DirectoryProtectedResearchTransport` in `Nvidea.NebiusContractProbe --live-research` with `NebiusObjectStorageClient` + `S3ProtectedResearchTransport`.
- The live client now accepts explicit Object Storage endpoint, region, bucket, static access key id/secret, and prefix configuration. The worker remains directory-backed against the same Serverless-mounted bucket prefix.
- `NVIDEA_LIVE_TRANSPORT_SOURCE_PATH` defaults to the native S3 prefix, and any explicit mismatch is rejected by preflight.
- Removed the architectural need for `NVIDEA_LIVE_CLIENT_TRANSPORT_ROOT`; the probe host no longer needs to mount the worker's bucket locally.
- Updated `docs/nebius-contract-probe.md` with the exact object-key-to-worker-path mapping, required native S3 variables, credential handling, and fail-closed topology rules.

Commits this run:
- `c9dc06cdf899eaaaedb31d870573912371256294` — validate native Object Storage / Serverless mount alignment.
- `a6440ce34deb2d941b4eacc3eb3d6f54d9a618f2` — add alignment regression tests.
- `380f5fa0bbf260a3d69cea954dde8c144ede6590` — switch live research probe to native Object Storage transport.
- `5a229bd2c85aaa6dd9587fdd4977e003dd5765fa` — document native Object Storage live-probe topology.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation and every write targeted exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Official Nebius documentation checked during this run confirms Serverless Jobs are bounded run-to-completion container workloads and Object Storage is the S3-compatible storage service.
- Static review confirms the client and worker now share one deterministic namespace mapping: client `<prefix>/work-items/...` corresponds to worker `<transport-root>/work-items/...` when `volume.SourcePath == prefix`; the same holds for bindings/results.
- Static review confirms alignment validation happens before `NebiusResearchLiveRuntimeFactory` can submit a job in the live probe.
- Static review confirms S3 credentials are local constructor/environment inputs only; values are not printed by the probe or storage error paths.
- This runtime still has no .NET SDK (`dotnet: command not found`), so compilation and test execution are **not claimed**.
- No live Nebius Object Storage credentials/bucket or Serverless resources are available here, so no real S3 or Serverless PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Object Storage continues to receive encrypted work/result envelopes and signed control-plane bindings rather than research plaintext.
- Native Object Storage bucket and mounted worker source must be exactly the same dedicated bucket; path-like/ambiguous source forms fail closed in the live alignment gate.
- Prefix mismatch cannot degrade into a silent remote timeout because submission is rejected before job creation.
- Static S3 credentials are not injected into the worker. Worker Nebius/Tavily/RSA credentials remain MysteryBox-backed.
- The client RSA private key remains local; only its public verification key enters the worker environment.
- Live mode remains opt-in, bounded by polling and overall timeout, uses synthetic non-private research, and avoids logging provider/protected bodies.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; the new alignment/live-probe code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, immutable registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- The exact provider acceptance of the configured Serverless Object Storage `Source`/`SourcePath` must still be proven by a real job; NVIDEA now fails locally on client/worker mismatch but does not claim provider-side mount success without the live contract.
- Live Object Storage static credentials currently enter the probe via environment variables; this is suitable only for an explicit local contract run with a least-privilege short-lived shell/secret-injection mechanism, not a future polished end-user credential UX.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live end-to-end contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Run or make maximally easy to run the first credential/resource-supplied end-to-end contract: add a zero-cost `--live-research-preflight`/dry-run mode that validates every live variable, RSA material, S3 endpoint/bucket/prefix mapping, MysteryBox reference shape, worker image immutability, and Serverless spec without creating a job or performing model calls; then, when infrastructure is supplied, execute `encrypted S3 dispatch -> authoritative Nebius ID -> signed binding -> mounted worker -> Nemotron/Tavily stage -> encrypted S3 result -> exact-once local ingestion -> cleanup` and persist the real PASS/failure evidence without exposing secrets.
