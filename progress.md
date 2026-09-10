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
- `ResearchProductRuntime` is the lifecycle-aware product facade: truthful local/remote status, local work only through `ILocalResearchRuntime`, remote lifecycle only through `IResearchCloudExecutionCoordinator`, and independently gated paid Serverless dispatch.
- WPF durable-research reads/actions use `ResearchProductRuntime`; remote/ambiguous records are never offered as local recovery. `ResearchProductUiState` centralizes control enablement, labels, and cloud disclosure.
- Browser product/UI access goes through constrained `BrowserProductRuntime`; the composition root does not expose raw `BrowserHostRuntime`, and WPF navigation/download flows use only the facade.
- Raw `BrowserHostRuntime` construction is assembly-internal: there is no public instance constructor or public static factory that external product/plugin code can use to create the privileged host.
- `BrowserGoalAgent` now publicly accepts only `IBrowserGoalHost`; its convenience constructor taking privileged `BrowserHostRuntime` is assembly-internal for trusted Core composition and tests.

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
Added `ResearchProductRuntime`, lifecycle-aware status projection, replay-race closure under the mutation lease, WPF migration to the product facade, separate reconciliation UX, explicit cloud-disclosure copy, cancellation-token hardening, `ResearchProductUiState`, and removal of the public local `ResearchJobRuntime` composition-root escape hatch.

### 2026-09-10 — Browser product authority narrowing
Added `BrowserProductRuntime`, replaced the public composition-root raw-host getter with `GetBrowserProductAsync()`, migrated WPF browser/download flows, added reflection regression coverage, and made `BrowserHostRuntime.CreateAsync(...)` assembly-internal so external product/plugin code cannot normally bootstrap the privileged raw host.

### 2026-09-10 — Current run: narrow BrowserGoalAgent privileged construction
Completed:
- Re-read this ledger completely and inspected the current repository head, recent commits, tree, `BrowserGoalAgent`, `BrowserHostRuntime`, `NvideaCompositionRoot`, browser recovery surface, and relevant tests before changing code.
- Re-confirmed the next authority-surface debt: `BrowserGoalAgent` still had a public convenience constructor accepting privileged `BrowserHostRuntime`, even though external callers already had a safer public `IBrowserGoalHost` constructor and raw-host creation had been made assembly-internal.
- Changed only the concrete-host convenience constructor from `public` to `internal`. No goal-loop behavior, planner behavior, approval semantics, durable child sequencing, crash handling, or browser execution logic was removed or simplified.
- Preserved the public `BrowserGoalAgent(IBrowserGoalHost, ...)` constructor so external consumers can still integrate a browser goal host through the least-authority contract.
- Trusted `NvideaCompositionRoot.CreateBrowserGoalAgentAsync()` remains in the same Core assembly and can still use the concrete internal constructor with the existing raw host, preserving production composition behavior.
- Added `tests/Nvidea.Core.Tests/BrowserGoalAgentApiSurfaceTests.cs` with reflection coverage that asserts: (1) no public constructor accepts `BrowserHostRuntime`, (2) a public constructor accepting `IBrowserGoalHost` remains, and (3) a non-public concrete-host path still exists for trusted Core/test composition.

Commits this run:
- `26f33ed49c506a351a0533ae6c092ca88e651d07` — narrow browser goal agent host constructor.
- `34433561387be037e1c65b4897ecaf25b809a309` — lock browser goal agent authority boundary with reflection regression coverage.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Post-change source inspection confirms the `BrowserHostRuntime` constructor overload is `internal` and the `IBrowserGoalHost` overload remains `public`.
- Post-change test source was re-read and covers both non-leakage and preservation of the safe public integration path.
- `NvideaCompositionRoot.CreateBrowserGoalAgentAsync()` was inspected before the change and remains assembly-local to `Nvidea.Core`, so its existing construction path is compatible with the new accessibility boundary.
- The execution environment was checked again and has no usable `dotnet` binary, so Core compilation, WPF/XAML compilation, and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- External Core consumers are no longer encouraged or able through normal public API construction to wire the goal loop directly to privileged `BrowserHostRuntime`; the least-authority `IBrowserGoalHost` contract is the public integration boundary.
- Existing exact approval, download quarantine, durable-job sequencing, prompt-injection defenses, state leases, audit trail, emergency stop, and ambiguous-side-effect behavior are unchanged.
- Trusted Core/test access remains intentionally assembly-scoped through the existing `InternalsVisibleTo` test configuration rather than reopening production authority solely for testability.
- This is an API/least-authority boundary, not a process sandbox; fully trusted in-process reflection can still bypass ordinary .NET accessibility.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research, browser authority, browser integration and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, continue the public Core authority audit and identify any remaining signatures that expose privileged concrete browser/research infrastructure or bypass capability approvals, lifecycle routing, or emergency-stop semantics; narrow only those surfaces while preserving the existing safe abstractions and trusted composition paths.
