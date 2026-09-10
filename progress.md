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
- Tavily Search + Extract research with canonical deduplication, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, validated citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport publishes encrypted work items/bindings/results directly through Nebius Object Storage while the worker consumes the same bucket prefix through a Serverless-mounted directory.
- Deployment preflight enforces exact Object Storage ↔ Serverless mount alignment, READ_WRITE transport, MysteryBox-backed worker credentials, digest-pinned worker image, RSA strength/identity consistency, bounded compute/storage settings, and a redacted reproducible deployment fingerprint.
- `Nvidea.NebiusContractProbe` supports cheap planner, zero-cost live preflight, and explicit paid live research; redacted manifest/PASS evidence persistence and offline verification are fail-closed and reproducible.
- `ResearchCloudExecutionCoordinator` is the narrow provider-aware bridge from durable research state to `NebiusResearchClientRuntime`, with exact checkpoint/disclosure approval, shared mutation lease, provider reconciliation/cancellation, private-data rejection, and no provider credential exposure.
- `ResearchJobStatus` distinguishes local interruption, active Nebius execution, and ambiguous `DispatchReserved`; unfinished remote provenance cannot advertise local recovery/cancellation.
- `ResearchJobRuntime` is now explicitly the `ILocalResearchRuntime` and validates current durable location/provenance **under the mutation lease before invoking the local orchestrator**, closing a stale-read race that could otherwise replay remotely reserved work locally.
- `ResearchProductRuntime` is the new lifecycle-aware product facade. It reads truthful local/remote status directly from the shared durable store, routes local work only to `ILocalResearchRuntime`, routes remote reconciliation/cancellation only to `IResearchCloudExecutionCoordinator`, and independently feature-gates new Serverless dispatch until the live contract is deliberately enabled.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, explicit live modes, reproducible/redacted deployment fingerprints, MysteryBox reference validation, machine-readable PASS evidence, atomic artifact persistence, strict evidence verification, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 — Remote research production safety
Added `ResearchCloudExecutionCoordinator`, narrow remote-runtime contracts, explicit per-stage cloud approval, remote lifecycle reconciliation/cancellation, privacy-safe Nebius status projection, and fail-closed recovery/cancellation for ambiguous `DispatchReserved` state. This closed the first duplicate-execution path where an uncertain Serverless create could have been treated as a crashed local stage.

### 2026-09-10 — Current run: lifecycle-aware product facade + local replay-race closure
Completed:
- Re-read this ledger completely and inspected recent commits, the current job/runtime/provenance contracts, `ResearchCloudExecutionCoordinator`, `NebiusResearchClientRuntime`, lifecycle reconciliation, WPF research flow, and desktop composition before changing code.
- Added `src/Nvidea.Core/Jobs/ResearchProductRuntime.cs` with `ILocalResearchRuntime`, `IResearchCloudExecutionCoordinator`, and a product-facing `ResearchProductRuntime` facade.
- The facade reads `research-jobs.json` directly for status/list projection so product UI can truthfully inspect local, active Nebius, and ambiguous records without calling the intentionally local-only `ResearchJobRuntime.GetStatusAsync` on a non-local record.
- Local step/recovery paths reject unfinished remote provenance before delegation. `DispatchReserved` is reconciliation-only; `Dispatched` cancellation routes through provider-aware cancellation; `CancelRequested` requires reconciliation; unsupported non-local state fails closed instead of falling back to local execution.
- New remote dispatch has a separate `RemoteDispatchEnabled` gate. Supplying a cloud coordinator does **not** enable paid Serverless dispatch by itself, and enabling dispatch without a coordinator is rejected at construction. This allows safe product composition before the first credential-backed live PASS.
- Made `ResearchJobRuntime` implement `ILocalResearchRuntime` and `ResearchCloudExecutionCoordinator` implement `IResearchCloudExecutionCoordinator` without exposing provider internals.
- Found and closed a second replay race in `ResearchJobRuntime.RunNextStepAsync`: it previously invoked `ResumableJobOrchestrator.RunNextStepAsync` before its local execution-location guard. A stale product status read followed by another process reserving/dispatching the same stage could therefore reach the local handler before the post-call guard failed. It now re-reads the durable record and validates local location + no unfinished remote provenance while holding the existing state-directory mutation lease, **before any local Nemotron/Tavily handler execution**.
- Added `tests/Nvidea.Core.Tests/ResearchProductRuntimeTests.cs` covering disabled remote dispatch before cloud invocation, ambiguous-reservation local-run rejection, reconciliation-only routing, provider-aware remote cancellation, normal local cancellation, and direct remote status projection without touching the local-only status method.
- Extended `ResearchRemoteRecoverySafetyTests` so ambiguous `DispatchReserved` now explicitly attempts `RunNextStepAsync` as well as recover/cancel; the test asserts zero Nemotron inference calls, zero Tavily provider calls, and unchanged durable remote provenance.

Commits this run:
- `1f257a308479b681e8198fe085ee7503fae4c3c0` — add lifecycle-aware research product runtime.
- `445ae6e68de37e4f6d323931298817b9d69de5b4` — harden local research routing boundary before handler execution.
- `efc45bcc2360e827b9dda7cfd21cb7001f60f08c` — expose cloud research coordinator through the narrow product contract.
- `78ba56396a8f0727b7278cc8d9cc778218168520` — add lifecycle-routing regression coverage.
- `e009f8a46d32d6d9ec55c5951bf0940d814f052b` — cover remote-reservation local-run replay safety.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static review confirms `ResearchProductRuntime.DispatchCurrentStageAsync` rejects dispatch while `RemoteDispatchEnabled == false` before calling the cloud coordinator.
- Static review confirms ambiguous/remote provenance cannot be routed through the facade's local step path and that dispatched cancellation has no local fallback.
- Static review confirms `ResearchJobRuntime.RunNextStepAsync` now reads/validates the exact current record under the same process semaphore + OS-backed state-directory lease that surrounds execution, before the orchestrator can invoke `ResearchJobHandler`.
- Regression source records provider call counters, making a replay failure observable as a nonzero Nemotron/Tavily call count rather than relying only on final state.
- `dotnet` was checked again in this execution environment and is unavailable, so compilation and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- The product facade stores no cloud approval grant, provider credential, transport handle, checkpoint payload, source content, or secret. Cloud authorization remains ephemeral and is validated again by `ResearchCloudExecutionCoordinator` and the cryptographic dispatch boundary.
- Direct durable reads are status projection only; they do not confer mutation authority and expose only `ResearchJobStatus`, which intentionally excludes checkpoint payload/source/query/provider details.
- Facade routing is advisory defense-in-depth; mutation runtimes independently re-read and revalidate under the shared state-directory lease, so a stale facade read cannot authorize a local replay or cloud mutation.
- Actual WPF Serverless dispatch remains disabled/unwired. The new facade is safe to compose locally, but the desktop must not claim production cloud execution until provider composition, disclosure UX, and the first live PASS are proven.
- New code remains statically reviewed only; compile/runtime errors are possible until a .NET 8-capable environment runs the focused suite.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current changes are statically reviewed but not compiled/executed.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- WPF still uses the existing local `ResearchJobs` property; `ResearchProductRuntime` is implemented and tested at the Core contract level but has not yet replaced the WPF direct-local call surface.
- WPF has no provider-aware reconcile/disclosure control yet and must not enable remote dispatch before a live contract PASS.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First, if any .NET-capable path becomes available, compile `Nvidea.Core`, `Nvidea.Worker`, the contract probe, and focused `ResearchProductRuntime` / cloud coordinator / replay-safety suites and fix every compile/runtime issue before extending architecture. If execution remains unavailable, compose `ResearchProductRuntime` into `NvideaCompositionRoot` and move WPF research reads/local actions onto that facade while keeping `RemoteDispatchEnabled=false`; add a clearly separate reconcile/disclosure UX path that remains disabled until a credential-backed Nebius Serverless PASS is obtained.
