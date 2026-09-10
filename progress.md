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
- `ResearchJobRuntime` is intentionally local-only and validates current durable location/provenance under the mutation lease before any local Nemotron/Tavily handler invocation. Its concrete constructor is assembly-internal so external product/plugin code cannot bootstrap it around lifecycle-aware routing.
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
Added `BrowserProductRuntime`, replaced the public composition-root raw-host getter with `GetBrowserProductAsync()`, migrated WPF browser/download flows, added reflection regression coverage, made `BrowserHostRuntime.CreateAsync(...)` assembly-internal, and narrowed `BrowserGoalAgent` so the privileged concrete-host constructor is assembly-internal while `IBrowserGoalHost` remains the public integration contract.

### 2026-09-10 — Current run: close direct local-research construction bypass
Completed:
- Re-read this ledger completely and inspected the current repository head, recent commits/tree, `NvideaCompositionRoot`, `ResearchProductRuntime`, `ResearchJobRuntime`, `ResearchCloudExecutionCoordinator`, the worker, contract probe, browser recovery/local-state surfaces, and existing API-surface test conventions before changing code.
- Re-confirmed that the composition root no longer exposes `ResearchJobRuntime`, but the concrete local-only runtime itself still had a public constructor. External product/plugin code could therefore instantiate it directly against the same state directory and intentionally bypass the lifecycle-aware `ResearchProductRuntime` facade.
- Changed only `ResearchJobRuntime` construction from `public` to `internal`. The class and `CapabilityId` remain public because separate executable tooling references the capability identity; all local execution, recovery, cancellation, durable leases, replay protection, report/status reads, and provider behavior are unchanged.
- Added XML documentation explaining that trusted Core composition owns the concrete runtime and that external callers should integrate through `ResearchProductRuntime` / `ILocalResearchRuntime`.
- Added `tests/Nvidea.Core.Tests/ResearchJobRuntimeApiSurfaceTests.cs`. Reflection coverage asserts that the concrete local runtime has no public instance constructor, preserves the expected assembly-internal `(string, ResearchEngine, IAuditTrail?)` construction path for trusted Core/tests, still implements `ILocalResearchRuntime`, and keeps that least-authority interface public.
- Inspected `ResearchCloudExecutionCoordinator` separately and intentionally left its public construction untouched: unlike `ResearchJobRuntime`, it is itself the constrained lifecycle boundary and revalidates exact authorization, durable state, private-data policy, and remote provenance before provider work.

Commits this run:
- `623258301bdce26c3218e883a26b16645a5d45b5` — narrow local research runtime construction.
- `ad7f7c250f28b85ea31ac76e13eb68608fe103af` — lock local research runtime construction boundary with reflection coverage.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Post-change source inspection confirms `ResearchJobRuntime` keeps its full existing implementation but its constructor is now `internal`.
- Post-change test source was re-read and verifies both non-public concrete construction and preservation of the public least-authority interface.
- `NvideaCompositionRoot` remains in the same `Nvidea.Core` assembly and can continue constructing `ResearchJobRuntime` normally. `Nvidea.Worker` does not construct it, and the separate Nebius contract probe only references the public `ResearchJobRuntime.CapabilityId` in the inspected live-probe path.
- The execution environment was checked again and has no usable `dotnet` binary, so Core compilation, WPF/XAML compilation, and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- Product/plugin consumers can no longer obtain the concrete local runtime through ordinary public construction and use local execution/recovery/cancellation as an alternate authority path around remote lifecycle state.
- The public `ILocalResearchRuntime` contract remains available for least-authority composition and test doubles; `ResearchProductRuntime` remains the intended durable product surface.
- Existing remote replay prevention, exact cloud disclosure approval, state-directory mutation leases, private-data rejection, provider cancellation/reconciliation, and emergency-stop behavior are unchanged.
- This is an assembly/API authority boundary, not a process sandbox; fully trusted in-process reflection can still bypass ordinary .NET accessibility.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research, browser authority, browser integration and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, continue the public Core authority audit with the remaining low-level job/provider types, but only narrow constructors that are genuinely bypass-capable; preserve public constrained coordinators/protocol abstractions needed by the worker, contract tooling, and future cloud composition.
