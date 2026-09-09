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
- Nebius Serverless job creation models `spec.volumes[]`; research dispatch passes configured shared transport mounts through to the worker.
- `NebiusResearchDeploymentPreflight` now verifies the live worker topology without resolving secret values: exact `NVIDEA_TRANSPORT_ROOT`/volume-path match, `READ_WRITE` transport, MysteryBox-backed Nebius/Tavily/worker-private-key credentials, and the client public key required for signed binding verification.
- `NebiusResearchLiveRuntimeFactory` is the explicit production composition gate; it runs deployment preflight before constructing a live remote runtime while keeping lower-level runtime construction available for unit/contract fixtures.
- Production remains truthfully local until a live Nebius/Object Storage/MysteryBox/container end-to-end probe succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, explicit terminal/result-expiry reconciliation, mounted encrypted transport, deployable non-root worker image, signed authoritative resource-ID bindings, automatic binding publication after durable remote-ID attachment, and terminal binding cleanup.

### 2026-09-09 — Verified Serverless transport mount contract
Added `NebiusServerlessVolumeMount`, current `spec.volumes[]` serialization, strict mount validation, volume passthrough through both research dispatchers, REST contract tests, dispatcher passthrough coverage, and deployment documentation. This closed the control-plane gap that previously made `DirectoryProtectedResearchTransport` undeployable on a real mounted job.

### 2026-09-09 — Current run: live research deployment preflight
Completed:
- Re-read `progress.md` completely and inspected current `main`, recent commits, `NebiusServerlessJobClient`, research dispatch options, two-phase dispatch, `Nvidea.Worker`, Token Factory/Tavily environment requirements, the shared transport mount test, and worker deployment documentation.
- Confirmed the live worker requires a consistent mounted transport plus `NEBIUS_API_KEY`, `TAVILY_API_KEY`, `NVIDEA_WORKER_PRIVATE_KEY_PEM`, and `NVIDEA_CLIENT_PUBLIC_KEY_PEM`; before this run there was no single production gate proving those references/topology agreed before remote runtime construction.
- Added `NebiusResearchDeploymentPreflight.cs` with fail-fast checks that never inspect secret contents:
  - `NVIDEA_TRANSPORT_ROOT` must be a bounded absolute Linux path;
  - it must exactly match one configured volume `ContainerPath`;
  - that mount must be `READ_WRITE`;
  - Nebius API, Tavily API, and worker private key must be MysteryBox secret references and cannot be plaintext configuration;
  - the client public key required for signed authoritative dispatch binding verification must be present as public config or a secret reference.
- Added `NebiusResearchLiveRuntimeFactory.cs`, an explicit live-only composition boundary that runs the deployment preflight before delegating to `NebiusResearchClientRuntime.Create`. This avoids weakening lower-level test/contract construction while giving production one mandatory fail-fast entry point.
- Added `NebiusResearchDeploymentPreflightTests.cs` covering the valid topology, transport-path mismatch, read-only transport, each missing required secret reference, plaintext credential rejection, and missing client public key.
- Updated `docs/nebius-research-worker.md` to distinguish actual secrets from the client public key, document the live runtime gate, and make the five production deployment invariants explicit.

Commits this run:
- `b824a45d01d0824f881dec34e753285aff67bda8` — add fail-fast Nebius research deployment preflight.
- `c501f3ccadc120ee736bb43f8902c6bda9069fb8` — add deployment preflight regression tests.
- `2c2693b1f2f9b94269466bc70958abecf383fc01` — gate live Nebius runtime construction behind preflight.
- `1652a4994abb6f5dd3a78d19f325f29dddc4c53b` — document live deployment preflight and secret/public-key distinction.

Validation / evidence:
- Repository metadata and every write call targeted exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Static inspection confirms worker environment consumption aligns with the new validator: `NEBIUS_API_KEY` feeds `NebiusOptions.FromEnvironment`, `TAVILY_API_KEY` feeds `TavilyOptions.FromEnvironment`, and the worker explicitly requires the transport root, client public key, and worker private key.
- The validator operates only on environment-variable names, secret references, and mount metadata. It never materializes, logs, compares, or persists credential values.
- Existing low-level dispatcher/serialization tests are not forced through live deployment requirements; only `NebiusResearchLiveRuntimeFactory` claims the production preflight boundary.
- No executable .NET 8/Windows/container toolchain is available in this automation environment, so compilation and test execution are **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- API credentials and worker private key must remain provider-secret-backed; plaintext duplicates are rejected by preflight and generic Serverless validation remains defense in depth.
- The transport mount must be writable because results and signed control-plane artifacts are create-once outputs; accepting a read-only mount would create a delayed worker failure and misleading dispatch success.
- Exact path matching prevents a worker from writing protected artifacts to ephemeral container storage while the client waits on a different shared mount.
- The client public key is not secret material, but its presence is mandatory because accepting unsigned/unverifiable remote-ID handoff would weaken result provenance.
- The client private signing key remains local and is not part of any Serverless configuration.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket/filesystem, immutable registry image, MysteryBox refs, subnet, or Serverless job has been provisioned/validated in this environment.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live end-to-end contract succeeds.
- Current Nebius lifecycle metadata includes legitimate provider states `IMAGE_PULLING` and `DELETING`; NVIDEA's lifecycle parser still maps them to `Unknown`. This remains a small but important pre-live-reconciliation fix; unknown future states should continue to fail closed.
- The new live factory is available, but existing product composition has not yet been switched to it because remote Serverless controls intentionally remain hidden pending the end-to-end probe.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Finish the remaining pre-live lifecycle drift and then extend the existing contract probe: map current Nebius `IMAGE_PULLING` conservatively as a nonterminal preparation state and `DELETING` as a nonterminal teardown state with regression tests, then make the credential/resource-supplied probe construct via `NebiusResearchLiveRuntimeFactory` and exercise one real mounted worker job. Proof target: `encrypted opaque dispatch -> mounted shared transport -> durable remote-id attachment -> signed binding -> worker one-stage Nemotron/Tavily execution -> encrypted result -> exact-once local ingestion -> terminal binding cleanup`. WPF Serverless controls stay hidden until that succeeds.
