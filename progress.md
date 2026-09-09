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
- Production remains truthfully local until a live Nebius/Object Storage/MysteryBox/container end-to-end probe succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, explicit terminal/result-expiry reconciliation, mounted encrypted transport, deployable non-root worker image, signed authoritative resource-ID bindings, automatic binding publication after durable remote-ID attachment, and terminal binding cleanup.

### 2026-09-09 — Verified Serverless transport + live preflight
Added `NebiusServerlessVolumeMount`, current `spec.volumes[]` serialization, strict mount validation, dispatcher passthrough, REST contract tests, worker deployment documentation, `NebiusResearchDeploymentPreflight`, and `NebiusResearchLiveRuntimeFactory`. Live construction now fails fast on transport/secret topology mismatches without reading secret contents.

### 2026-09-09 — Current run: current Nebius lifecycle drift
Completed:
- Re-read `progress.md` completely and inspected current `main`, recent commits, `NebiusResearchLifecycleReconciler`, existing lifecycle tests, and the live contract probe.
- Rechecked the current Nebius Serverless direction before changing provider-state assumptions.
- Updated `NebiusServerlessJobSnapshotParser.ParseState` so `IMAGE_PULLING` maps to the existing nonterminal `Pending` category and `DELETING` maps to the existing nonterminal `Cancelling` category.
- Preserved conservative semantics: all unrecognized/future states still map to `Unknown`, and direct verified reconciliation continues to refuse durable mutation for `Unknown`.
- Added `NebiusServerlessLifecycleStateTests.cs` covering documented preparation states, teardown states, whitespace/case tolerance, and unknown-future-state fail-closed behavior.

Commits this run:
- `55e09f7539f9d65b49bb670773aab7ad05d20e69` — handle current Nebius preparation and teardown states.
- `65788e917c606d7a9e56231dfd9e55c1ad76951a` — add lifecycle-state regression tests.

Validation / evidence:
- Repository metadata was checked before each mutation and every write targeted exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Static re-read of the updated parser confirms only `IMAGE_PULLING` and `DELETING` were added to known mappings; no wildcard/fallback behavior was weakened.
- `ReconcileDispatchedAsync` already treats `Pending`, `Running`, and `Cancelling` as nonterminal and therefore now naturally waits through image pulling and provider teardown instead of misclassifying them as unknown.
- `ReconcileCancellationAsync` likewise leaves `DELETING` cancellation-pending until Nebius actually reports `CANCELLED`, preserving truthful terminal confirmation.
- No executable .NET 8/Windows/container toolchain is available in this automation environment, so compilation and test execution are **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- This change does not alter credential handling, encrypted payloads, binding signatures, permission policy, or local/cloud disclosure.
- Mapping `IMAGE_PULLING` to `Pending` prevents a normal provider preparation phase from causing false recovery failure.
- Mapping `DELETING` to `Cancelling` prevents teardown from being treated as an unknown provider state while still refusing to claim terminal cancellation until `CANCELLED` is observed.
- Unknown future states still fail closed to avoid silently assigning unsafe semantics after future Nebius API drift.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket/filesystem, immutable registry image, MysteryBox refs, subnet, or Serverless job has been provisioned/validated in this environment.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live end-to-end contract succeeds.
- `tools/Nvidea.NebiusContractProbe` currently validates Token Factory structured planner behavior only; it does not yet construct the mounted remote research runtime or prove worker/result transport.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Extend `tools/Nvidea.NebiusContractProbe` with an opt-in live-research mode that constructs through `NebiusResearchLiveRuntimeFactory`, requires explicit resource/secret-reference configuration, and exercises the narrow real path without touching WPF: `encrypted opaque dispatch -> mounted shared transport -> durable remote-id attachment -> signed binding -> worker one-stage Nemotron/Tavily execution -> encrypted result -> exact-once local ingestion -> terminal binding cleanup`. Keep the existing cheap Token Factory planner probe as the default mode, keep GitHub Actions uninvolved, fail closed on incomplete live configuration, and never print secret values or protected research payloads.