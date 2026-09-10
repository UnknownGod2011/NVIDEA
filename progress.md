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
- `NvideaCompositionRoot` exposes only lifecycle-aware `ResearchProductRuntime` for durable research; concrete `ResearchJobRuntime` remains an internal construction dependency.
- WPF durable-research reads/actions use `ResearchProductRuntime`; remote/ambiguous records are never offered as local recovery, and a separate Nebius reconciliation control remains disabled when no validated cloud lifecycle coordinator is composed.
- `ResearchProductUiState` centralizes research control enablement, labels, and cloud disclosure independently of WPF.
- Browser product/UI access now goes through `BrowserProductRuntime`, a constrained facade over trusted `BrowserHostRuntime`. The composition root no longer returns the raw host, and WPF navigation/download flows use only the facade. The facade intentionally omits low-level create/advance/re-arm/reconcile/observation/session-snapshot authority.

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

### 2026-09-10 — Current run: narrow browser product authority
Completed:
- Re-read this ledger completely and inspected the current repository tree, recent commits, `NvideaCompositionRoot`, `BrowserHostRuntime`, WPF browser/download call sites, recovery code, and existing reflection-based API-surface test conventions before changing code.
- Identified a concrete product-facing authority escape hatch: `NvideaCompositionRoot.GetBrowserAsync()` returned the full `BrowserHostRuntime`, exposing low-level durable-job creation/advancement, re-arming, direct ambiguous-child reconciliation, observation/session snapshots, and other trusted-host APIs to UI/plugin callers.
- Added `src/Nvidea.Core/Desktop/BrowserProductRuntime.cs`, a deliberately constrained facade exposing only the current product operations: start one policy-enforced browser action, exact approval+resume, cancellation, trusted download listing, exact download handoff/discard preparation, and exact approved export/discard.
- Replaced the public composition-root raw-host getter with `GetBrowserProductAsync()`. Raw host creation is now a private `GetBrowserHostAsync()` implementation detail used by trusted goal-agent/recovery composition.
- Migrated `src/Nvidea.Windows/MainWindow.xaml.cs` browser navigation from `BrowserHostRuntime` to `BrowserProductRuntime`.
- Migrated `src/Nvidea.Windows/MainWindow.Downloads.cs` trusted download recovery/export/discard paths to the same product facade without weakening existing quarantine snapshot revalidation or exact approval checks.
- Added `tests/Nvidea.Core.Tests/NvideaCompositionRootBrowserApiSurfaceTests.cs`. It asserts that the composition root exposes the product browser boundary but no public member typed with `BrowserHostRuntime`, and locks the facade to an explicit allowlist so low-level create/advance/re-arm/reconcile/observation APIs cannot silently leak into product code.
- Preserved all existing working browser functionality used by WPF; this is an authority narrowing/refactor, not deletion of the underlying host implementation.

Commits this run:
- `81bb32b6dd3b1f2cc52aa4d8ba3c8a2ce46e5485` — add constrained browser product runtime.
- `a9ca929d025d7181d88dda08be108a7dfd2676a3` — hide raw browser host behind composition-root product boundary.
- `546c6d07d5924a40dea78830ea246d19e247029d` — route WPF browser navigation through the product boundary.
- `740786c499e7d86deb3111dd1d737bea6ef4eceb` — route WPF download UX through the product boundary.
- `1d46297ade257990439f3830dfcc26ea18dc8ca4` — lock browser authority API surface with reflection regression coverage.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- The current repo tree and recent commit history were inspected before implementation.
- Post-change static inspection confirms `NvideaCompositionRoot` publicly returns `BrowserProductRuntime` and keeps `BrowserHostRuntime` behind a private getter.
- Post-change static inspection confirms the main WPF browser action path uses `GetBrowserProductAsync()` and a `BrowserProductRuntime` field.
- The new reflection regression source was re-read after persistence and contains both the composition-root raw-host exclusion and product-method allowlist assertions.
- `dotnet` was checked directly in the execution environment and remains unavailable, so Core compilation, WPF/XAML compilation, and test execution are **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- Product/UI code no longer receives a raw host through the trusted composition root, reducing accidental bypass of durable-job sequencing and authority separation.
- Exact approval remains required for consequential browser actions and download export/discard; the facade delegates to the existing hardened host rather than duplicating authorization logic.
- Passive download inspection remains browser-free through `LocalStateRuntime`; initializing the product browser facade is still an explicit trusted-runtime action.
- No provider credentials, browser typed values, approval grants, audit payload contents, quarantine bytes, or secret material were added to the new facade.
- Emergency-stop cancellation semantics are preserved because WPF still owns and cancels the operation token while durable browser cancellation remains available through the constrained facade.
- Remaining authority risk: `BrowserHostRuntime` itself is still a public Core type with a public creation API, and `BrowserGoalAgent` has a public convenience constructor taking it. The composition root no longer leaks it, but a separate Core consumer could instantiate the raw host directly. This should be narrowed once compile compatibility is checked.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.
- `BrowserHostRuntime` remains publicly constructible at the Core assembly boundary even though the desktop composition root no longer exposes it.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research and new browser API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, finish the browser authority boundary by making raw `BrowserHostRuntime` construction assembly-internal (and narrowing the `BrowserGoalAgent` raw-host convenience constructor as needed), while preserving test access through the existing `InternalsVisibleTo("Nvidea.Core.Tests")` and keeping the safe `BrowserProductRuntime`/goal/recovery product paths intact.
