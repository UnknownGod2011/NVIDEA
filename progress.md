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
- `tools/Nvidea.NebiusContractProbe` now has an explicit opt-in `--live-research` mode that constructs through the production live factory and can exercise all three durable research stages remotely. The cheap Token Factory planner probe remains the default.
- Production remains truthfully local until the live Nebius/Object Storage/MysteryBox/container end-to-end probe actually succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, explicit terminal/result-expiry reconciliation, mounted encrypted transport, deployable non-root worker image, signed authoritative resource-ID bindings, automatic binding publication after durable remote-ID attachment, and terminal binding cleanup.

### 2026-09-09 — Verified Serverless transport + live preflight
Added `NebiusServerlessVolumeMount`, current `spec.volumes[]` serialization, strict mount validation, dispatcher passthrough, REST contract tests, worker deployment documentation, `NebiusResearchDeploymentPreflight`, and `NebiusResearchLiveRuntimeFactory`. Live construction now fails fast on transport/secret topology mismatches without reading secret contents.

### 2026-09-09 — Current Nebius lifecycle drift
Mapped `IMAGE_PULLING` to nonterminal `Pending` and `DELETING` to nonterminal `Cancelling`, with regression coverage. Unknown future provider states remain fail-closed.

### 2026-09-09 — Current run: opt-in end-to-end live research probe
Completed:
- Re-read `progress.md` completely and inspected the current probe, production live factory, dispatch options, two-phase dispatcher, result ingestor, durable research state model, research handler stages, worker image command, mounted transport, and deployment documentation.
- Found an important live-path assumption: the worker can see a Nebius-mounted Object Storage/filesystem volume, but the client also needs a host-mounted view of the same backing storage when using `DirectoryProtectedResearchTransport`. The new probe documents and requires that boundary rather than pretending an unrelated local directory is shared cloud storage.
- Extended `tools/Nvidea.NebiusContractProbe/Program.cs` with explicit `--live-research`; default invocation remains the cheap Token Factory structured-planner probe and therefore does not create Serverless jobs.
- Live mode fails closed on missing/invalid resource configuration, reads local client/worker public PEM material from files, derives the client public verification key in memory, and supplies worker credentials only as MysteryBox secret references through `NebiusResearchLiveRuntimeFactory`.
- Live mode creates a synthetic non-private durable research job and remotely executes the full three-stage workflow: requested->planned (Nemotron), planned->evidence (Tavily Search/Extract), evidence->completed (Nemotron synthesis).
- Every stage uses the existing production two-phase encrypted dispatch, durable remote-ID attachment, signed authoritative binding, lifecycle reconciliation, encrypted result return, exact-once ingestion, and terminal binding cleanup paths.
- Added bounded polling (1-30 seconds), bounded total runtime (2-60 minutes), a maximum of exactly three expected durable research stages, and final evidence/citation assertions.
- Sanitized probe output never prints credentials, PEM contents, protected payloads, evidence bodies, binding contents, or provider response bodies.
- Rewrote `docs/nebius-contract-probe.md` to document both modes, all required live resource/secret-reference inputs, the host/shared-storage requirement, cost/side-effect warning, and what a live PASS actually proves.

Commits this run:
- `14d39cfe571dd71edcd5af0deed19608024ed4bc` — add opt-in live Nebius research contract probe.
- `da008e9ff117f25c2880215d0b745c97adeef627` — correct final evidence validation against `ResearchBatch.Sources`.
- `ed04213d300ac7e5a7efdacc8c18d7928859936b` — document live research contract mode and required topology.

Validation / evidence:
- Repository metadata was checked immediately before every mutation and every write targeted exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Static cross-check confirmed the live mode calls `NebiusResearchLiveRuntimeFactory.Create`, not the lower-level fixture factory.
- Static cross-check confirmed the seeded research definition/checkpoint match `ResearchJobRuntime` / `ResearchJobHandler` conventions and that the three handler checkpoints naturally exercise Nemotron planning, Tavily evidence gathering, then Nemotron synthesis.
- Static cross-check confirmed `RemoteResearchResultIngestor` returns nonterminal remote results to local `Pending` and final synthesis to local `Completed`, which is the loop contract used by the probe.
- Static cross-check caught and fixed an initial probe typo (`ResearchBatch.Items` -> `ResearchBatch.Sources`) before this run was finalized.
- No executable .NET 8/Windows/container toolchain is available in this automation environment, so compilation and test execution are **not claimed**.
- No live Serverless credentials/resources are available here, so a live PASS is **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- Live mode is opt-in and may incur real cloud/Tavily/Token Factory cost; default behavior stays cheap and side-effect-free apart from Token Factory inference.
- The synthetic research job declares `ContainsPrivateOsData: false`; each remote stage gets a fresh exact cloud authorization scoped to its current durable checkpoint.
- Client private key material is used locally for signing/decryption only and is never inserted into the Serverless spec. Worker private key, worker Token Factory key, and Tavily key are represented only by MysteryBox secret ids.
- The client public key is derived from the client private key in memory and passed as public worker verification configuration.
- Provider/body diagnostics remain suppressed on HTTP failures to avoid echoed request material reaching logs.
- The host transport root must be a mounted view of the same backing store as the worker volume. The code cannot cryptographically prove that two filesystem paths share backing storage, so this remains an operator/deployment invariant and is explicitly documented.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new probe code is statically reviewed but not compiled/executed.
- No live Object Storage bucket/filesystem, immutable registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated in this environment.
- The live probe currently needs the client machine to mount the same backing storage used by the worker's Nebius volume. NVIDEA does not yet have a native client-side Nebius Object Storage/S3 transport adapter, so production Windows background research would otherwise require external mounting software.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live end-to-end contract succeeds.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Implement a native bounded client-side Nebius Object Storage transport for the protected work-item/result/binding interfaces (prefer the current supported S3-compatible path, with create-once semantics, object-size limits, retries/timeouts, key-prefix isolation, no secret logging, and contract tests). Keep the worker's mounted-directory transport, but let Windows publish/read the same bucket directly without requiring an external host mount. Then wire that transport into `--live-research` and run the first real credential/resource-supplied end-to-end probe when infrastructure is available.