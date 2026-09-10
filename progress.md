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
- `NvideaCompositionRoot` exposes only the lifecycle-aware `ResearchProductRuntime` to product/UI callers. The concrete local-only `ResearchJobRuntime` is now retained solely as an internal construction dependency and is no longer a public composition-root property.
- WPF durable-research reads/actions use `ResearchProductRuntime`; remote/ambiguous records are never offered as local recovery, and a separate Nebius reconciliation control remains disabled when no validated cloud lifecycle coordinator is composed.
- `ResearchProductUiState` centralizes research control enablement, labels, and cloud disclosure independently of WPF, so lifecycle authority can be adversarially unit-tested.

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
Added `ResearchProductRuntime`, lifecycle-aware status projection, replay-race closure under the mutation lease, WPF migration to the product facade, a separate reconciliation control, explicit cloud-disclosure copy, cancellation-token hardening, and the testable `ResearchProductUiState` authority projector.

### 2026-09-10 — Current run: close local-runtime product bypass
Completed:
- Re-read this ledger completely and inspected the current repository tree, recent commits, `NvideaCompositionRoot`, WPF call sites, and existing API-surface test conventions before changing code.
- Audited the remaining obvious product-facing compatibility surface and identified `NvideaCompositionRoot.ResearchJobs` as a public escape hatch exposing concrete `ResearchJobRuntime` despite WPF already migrating to `ResearchProductRuntime`.
- Removed the public `ResearchJobs` property and its constructor plumbing from `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`.
- Kept the same working local research implementation: `ResearchJobRuntime` is still constructed when Tavily is configured and passed directly into `ResearchProductRuntime`; functionality was not deleted or simplified away.
- Renamed the local construction variable to `localResearch` to make its role explicit and reduce accidental product-level reuse.
- Added `tests/Nvidea.Core.Tests/NvideaCompositionRootResearchApiSurfaceTests.cs` using reflection to lock the boundary: `NvideaCompositionRoot` must expose lifecycle-aware `ResearchProductRuntime` while exposing no public property/method signature typed as `ResearchJobRuntime`.
- This makes the safe routing boundary structural rather than merely advisory documentation: product/UI callers obtaining the composition root can no longer bypass remote/ambiguous lifecycle checks through a public local-only runtime property.

Commits this run:
- `fbd2ee1256141da197e3638234a318aa971891d3` — close public local research runtime bypass in desktop composition.
- `50d5375c806cc982c854ba857bb5dd07babbc6a2` — lock lifecycle-aware research API surface with reflection regression coverage.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Current `main` repository tree and recent commits were inspected before implementation.
- `src/Nvidea.Windows/MainWindow.xaml.cs` was inspected and contains no dependency on the removed `ResearchJobs` property; current WPF research work is already routed through the product facade in the dedicated research partial.
- Existing reflection-based API-surface test conventions were inspected before adding the new test.
- `dotnet` was checked directly in this execution environment and remains unavailable, so Core compilation, WPF/XAML compilation, and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- Removing the public local runtime reduces the authority exposed to UI/plugin/product code without changing persisted research state or provider behavior.
- Remote/ambiguous records still cannot be intentionally routed through local product actions because the publicly exposed research surface is now `ResearchProductRuntime` only.
- No provider credentials, checkpoint payloads, source content, approval material, Object Storage identifiers, Serverless ids, or signing keys were added to a public surface.
- New Serverless dispatch remains disabled in desktop composition and no paid-provider action was introduced.
- Source remains statically reviewed only; compile/runtime defects remain possible until a .NET 8-capable environment executes the focused suite.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.
- GitHub code search did not return indexed symbol hits during this run, so the compatibility-surface audit used the repository tree plus direct inspection of known composition/WPF files; a compile-capable full-reference scan is still desirable.

## Single Best Next Task
Obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused `NvideaCompositionRootResearchApiSurfaceTests`, `ResearchProductUiState`, `ResearchProductRuntime`, `ResearchJobRuntime`, cloud-coordinator, remote-recovery, and status suites and fix every compile/XAML/runtime defect before attempting the first credential-backed Nebius Serverless live PASS. If execution remains unavailable, continue the authority-surface audit across public Core APIs and remove or narrow any remaining product-accessible mutation path that can bypass capability approvals, lifecycle routing, or emergency-stop/cancellation semantics without removing working functionality.
