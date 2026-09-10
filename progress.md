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
- `ResearchJobRuntime` is intentionally local-only and validates current durable location/provenance under the mutation lease before any local Nemotron/Tavily handler invocation.
- `ResearchProductRuntime` is the lifecycle-aware product facade: it projects truthful local/remote status, routes local work only to `ILocalResearchRuntime`, remote lifecycle operations only to `IResearchCloudExecutionCoordinator`, and independently gates new paid Serverless dispatch.
- `NvideaCompositionRoot` composes `ResearchProductRuntime` whenever Tavily is configured, while deliberately leaving `cloud: null` and `remoteDispatchEnabled: false` until the live deployment contract is proven.
- WPF durable-research reads/actions use `ResearchProductRuntime`; remote/ambiguous records are never offered as local recovery, and a separate Nebius reconciliation control remains disabled when no validated cloud lifecycle coordinator is composed.
- `ResearchProductUiState` now centralizes research control enablement, labels, and cloud disclosure independently of WPF, so lifecycle authority can be adversarially unit-tested.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, explicit live modes, reproducible/redacted deployment fingerprints, MysteryBox reference validation, machine-readable PASS evidence, atomic artifact persistence, strict evidence verification, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 — Remote research production safety
Added `ResearchCloudExecutionCoordinator`, narrow remote-runtime contracts, explicit per-stage cloud approval, remote lifecycle reconciliation/cancellation, privacy-safe Nebius status projection, and fail-closed recovery/cancellation for ambiguous `DispatchReserved` state. Closed duplicate-execution paths where uncertain/remote Serverless work could otherwise be mistaken for crashed local work.

### 2026-09-10 — Lifecycle-aware product + Windows integration
Added `ResearchProductRuntime`, lifecycle-aware status projection, replay-race closure under the mutation lease, WPF migration to the product facade, a separate reconciliation control, explicit cloud-disclosure copy, and cancellation-token hardening. Current desktop composition still has no cloud coordinator and keeps new Serverless dispatch disabled.

### 2026-09-10 — Current run: testable research-control authority
Completed:
- Re-read this ledger completely and inspected the current repository tree, recent commits, `ResearchJobStatus`, `ResearchProductRuntime`, WPF research handlers, and existing test conventions before changing code.
- Added `src/Nvidea.Core/Jobs/ResearchProductUiState.cs`, a UI-independent projection for Start, Resume/Re-arm, Reconcile, Cancel, resume labeling, and cloud-disclosure state.
- The projector fails closed if `remoteDispatchEnabled` is ever presented without provider lifecycle availability, preventing an impossible/unsafe composition from being silently rendered as valid UI authority.
- Local resume/re-arm remains disabled whenever `RequiresRemoteReconciliation` is true. Reconcile is enabled only when unfinished remote provenance exists and a provider-aware lifecycle coordinator is available.
- Remote cancellation is only exposed when the underlying status permits cancellation and provider lifecycle authority is available. `DispatchReserved` and `CancelRequested` remain non-cancellable until reconciliation.
- Busy-state projection disables all new mutation starts. Durable Cancel is now enabled during a busy operation only if an active durable job actually exists; this removes the previous UI state where Cancel could be enabled during startup before any job id existed.
- Added `tests/Nvidea.Core.Tests/ResearchProductUiStateTests.cs` covering local pending work, interrupted local re-arm labeling, ambiguous `DispatchReserved`, dispatched Serverless work, `CancelRequested`, terminal state, busy/no-job behavior, lifecycle-unavailable fail-closed behavior, and invalid dispatch-without-lifecycle composition.
- Migrated `src/Nvidea.Windows/MainWindow.Research.cs` control enablement and cloud-disclosure rendering onto `ResearchProductUiState`. WPF no longer duplicates the lifecycle-control boolean logic.
- Static review after the migration confirms the WPF helper supplies only privacy-safe status/feature booleans; no provider ids, credentials, work-item payloads, checkpoint contents, URLs, or source text were introduced into the presenter boundary.

Commits this run:
- `f429cd9202900d7cddcc9112454bd1bd7a3e7e4e` — extract research UI lifecycle state projector.
- `dcd9025499a75beff56c2efa521b053ee2adf636` — add adversarial research UI lifecycle projection tests.
- `f904804dda471b3996c8212b51a2129d6370fa03` — simplify string assertions for current xUnit compatibility.
- `98eaf76e259dd30dd1de59fc206a2dd8cceaccba` — route WPF controls/disclosure through the tested projector.
- `ea0c99e30bdab19f2111bba670c25343b2630867` — add the explicit xUnit import required by repository test conventions.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Current `main` was re-read after mutation and the WPF call site was statically inspected to confirm control decisions now flow through `ResearchProductUiState.Project(...)`.
- Existing test project conventions were inspected; the new test file was corrected to use the repository's explicit `using Xunit;` convention.
- `dotnet` was checked directly in this execution environment and is still unavailable, so Core compilation, WPF/XAML compilation, and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- The projector consumes only `ResearchJobStatus` plus product feature booleans. It does not receive durable checkpoint payloads, approval grants, Serverless ids, Object Storage handles, MysteryBox references, API keys, signing keys, user queries, URLs, source content, or provider errors.
- Ambiguous and remote lifecycle states cannot regain a local Resume/Re-arm affordance through WPF-specific branching because the authority logic now lives in one Core projection.
- `RemoteDispatchEnabled` still remains false in the desktop composition. This run did not add any WPF control that can initiate paid Serverless dispatch.
- Source remains statically reviewed only; compile/XAML/runtime defects remain possible until a .NET 8-capable environment executes the focused suite.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
Obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused `ResearchProductUiState`, `ResearchProductRuntime`, `ResearchJobRuntime`, cloud-coordinator, remote-recovery, and status suites and fix every compile/XAML/runtime defect before attempting the first credential-backed Nebius Serverless live PASS. If execution remains unavailable, audit all remaining direct `ResearchJobs`/`ResearchJobRuntime` consumers and reduce that trusted compatibility surface so product code cannot bypass `ResearchProductRuntime` lifecycle routing.