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
- `ResearchJobRuntime` is intentionally local-only and validates the current durable location/provenance under the mutation lease before any local Nemotron/Tavily handler invocation.
- `ResearchProductRuntime` is the lifecycle-aware product facade: it projects truthful local/remote status, routes local work only to `ILocalResearchRuntime`, remote lifecycle operations only to `IResearchCloudExecutionCoordinator`, and independently gates new paid Serverless dispatch.
- `NvideaCompositionRoot` now composes `ResearchProductRuntime` whenever Tavily is configured, while deliberately leaving `cloud: null` and `remoteDispatchEnabled: false` until the live deployment contract is proven.
- WPF durable-research reads and local actions now use `ResearchProductRuntime`, not the direct local runtime. Remote/ambiguous records are never offered as local resume/recovery, and a separate Nebius reconciliation control exists but remains disabled when no validated cloud lifecycle coordinator is composed.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, authoritative signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, `NebiusResearchLiveRuntimeFactory`, explicit live modes, reproducible/redacted deployment fingerprints, MysteryBox reference validation, machine-readable PASS evidence, atomic artifact persistence, strict evidence verification, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 — Remote research production safety
Added `ResearchCloudExecutionCoordinator`, narrow remote-runtime contracts, explicit per-stage cloud approval, remote lifecycle reconciliation/cancellation, privacy-safe Nebius status projection, and fail-closed recovery/cancellation for ambiguous `DispatchReserved` state. Closed duplicate-execution paths where uncertain/remote Serverless work could otherwise be mistaken for crashed local work.

### 2026-09-10 — Lifecycle-aware product facade + local replay-race closure
Added `ResearchProductRuntime`, `ILocalResearchRuntime`, and `IResearchCloudExecutionCoordinator`. The facade reads shared durable research state directly for privacy-safe status, routes remote lifecycle operations to the cloud coordinator, and feature-gates new Serverless dispatch independently. `ResearchJobRuntime.RunNextStepAsync` now re-reads and validates local location + absence of unfinished remote provenance under the state-directory lease before the local orchestrator can invoke Nemotron/Tavily. Regression source asserts zero inference/research-provider calls for ambiguous `DispatchReserved` local replay attempts.

### 2026-09-10 — Current run: WPF lifecycle-aware research integration
Completed:
- Re-read this ledger completely and inspected the latest repository tree, recent commits, composition root, `ResearchProductRuntime`, research status projection, WPF research handlers/XAML, and relevant remote-recovery tests before changing code.
- Updated `NvideaCompositionRoot` to construct a `ResearchProductRuntime` over the same durable research directory whenever Tavily is configured. The trusted local `ResearchJobs` property remains for internal compatibility, but product/UI code now has a dedicated `Research` facade.
- Desktop composition explicitly passes `cloud: null` and `remoteDispatchEnabled: false`. Merely adding the product facade therefore cannot silently activate Serverless, paid execution, provider credentials, or new cloud data disclosure.
- Added `ResearchProductRuntime.RemoteLifecycleAvailable`, deliberately separate from `RemoteDispatchEnabled`. This allows future builds to reconcile/cancel already-remote work while still keeping new paid dispatch locked.
- Added privacy-safe `ResearchJobStatus.RequiresRemoteReconciliation`. It is derived from unfinished `DispatchReserved` / `Dispatched` / `CancelRequested` provenance and exposes no provider ID, checkpoint payload, question, URL, source content, approval grant, or provider error. This prevents WPF from inferring lifecycle authority from human-readable status strings.
- Migrated `MainWindow.Research.cs` from `_root.ResearchJobs` to `_root.Research` for create/list/status/report/local step/recovery/cancellation. Local step execution now calls `RunNextLocalStepAsync`, so an ambiguous/remote checkpoint cannot be sent through a local handler even if a stale UI event fires.
- Added a distinct **Reconcile Nebius state** WPF control. It is enabled only when the durable status requires remote reconciliation **and** a provider-aware lifecycle coordinator is actually composed. The current desktop composition has no coordinator, so the button remains disabled and no provider-backed request can originate from WPF.
- Remote/ambiguous status explicitly disables local Resume/Re-arm. Cancellation also fails closed when remote lifecycle authority is unavailable instead of falling back to local cancellation.
- Added `ResearchCloudStatusText` so the Windows UI truthfully distinguishes local Nemotron+Tavily availability, provider-lifecycle availability, and new Serverless dispatch enablement. Status copy explicitly states that checkpoint/source/provider/approval details are not surfaced.
- Found and fixed a cancellation-token bug during static review: reconciliation initially risked reusing a previously cancelled `_researchCts`. It now disposes the old token source, creates a fresh operation token, and treats local cancellation of reconciliation as an **unknown remote outcome** that still requires provider reconciliation before retry.
- Extended `ResearchRemoteRecoverySafetyTests` to assert the machine-readable `RequiresRemoteReconciliation` flag for stale ambiguous dispatch reservations while preserving the existing zero-Nemotron / zero-Tavily replay assertions.

Commits this run:
- `a1a3f54cf58929b8d2e35d13cc528a995cf311a4` — compose lifecycle-aware research facade in the desktop root.
- `a7f1c2457d6f1914875c9d290c430417079fa203` — expose cloud lifecycle availability independently from dispatch enablement.
- `8bc842ad3457141228e8e429b0db6bcd33a8bbf5` — surface privacy-safe remote reconciliation requirement.
- `d7ab656add02dfc29264a45eef6f4d22ded6f7ac` — route WPF research through the product facade.
- `828b2b827d2965d1c17b190897739f0031a0fa22` — add explicit Nebius reconciliation/disclosure UX.
- `fbc1808c1788d434e8d36e7cbbe3d1b71555d9cb` — isolate reconciliation cancellation with a fresh operation token.
- `c36b7a073bfaa41ab1cb7daf95a6822ed9c8bc80` — assert remote reconciliation projection safety.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static review confirms the current desktop composition creates no cloud coordinator and leaves `RemoteDispatchEnabled == false`.
- Static review confirms WPF no longer calls `ResearchJobRuntime.RunNextStepAsync` directly; local product execution goes through `ResearchProductRuntime.RunNextLocalStepAsync`.
- Static review confirms remote/ambiguous status cannot enable local Resume/Re-arm, and the new Reconcile control requires actual provider-lifecycle availability.
- Static review confirms cancellation of a reconciliation attempt is not interpreted as provider cancellation or local recovery; the UI states that the durable remote outcome remains authoritative/unknown.
- Existing replay-safety regression source still records Nemotron/Tavily provider call counters; the ambiguous `DispatchReserved` test now also asserts `RequiresRemoteReconciliation == true`.
- `dotnet` was checked again in this execution environment and remains unavailable, so compilation, XAML compilation, and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- WPF receives only the product facade and privacy-safe status projection; no Serverless client, Object Storage handle, MysteryBox reference, API key, signing key, remote provider ID, encrypted work item, or checkpoint payload was introduced into the UI layer.
- New paid dispatch remains impossible from the current composition. There is no WPF dispatch control and the facade is instantiated with both no cloud coordinator and `remoteDispatchEnabled: false`.
- Reconciliation is a separate provider-aware action rather than reusing Resume. This preserves the core invariant that uncertain remote work is never replayed locally.
- A cancelled reconciliation attempt remains ambiguous by design; no local retry/recovery is granted from cancellation alone.
- The direct local runtime remains exposed on the composition root only for trusted compatibility. New UI code should use `Research`; future hardening may reduce that surface once downstream usage is proven migrated.
- Source changes remain statically reviewed only; compile/XAML/runtime defects are possible until a .NET 8-capable environment executes the focused suite.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
Obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused `ResearchProductRuntime`, `ResearchJobRuntime`, cloud-coordinator, remote-recovery, and status suites and fix every compile/XAML/runtime defect before attempting the first credential-backed Nebius Serverless live PASS. If execution remains unavailable, extract WPF research-control enablement/disclosure decisions into a UI-independent Core presenter/state projector with adversarial tests for local, `DispatchReserved`, `Dispatched`, `CancelRequested`, terminal, and lifecycle-unavailable states.