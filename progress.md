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
- Concrete privileged persistence/runtime boundaries are Core-only where appropriate: `JsonAgentJobStore`, `ResearchJobRuntime`, raw `BrowserHostRuntime` construction, privileged `BrowserGoalAgent(BrowserHostRuntime,...)` construction, raw persistent Playwright transport, path-backed `JsonBrowserGoalSessionStore`, `BrowserDownloadStagingGuard`, and `BrowserDownloadQuarantine` construction cannot be bootstrapped by ordinary external product/plugin code.
- Product research flows through `ResearchProductRuntime`; provider-aware remote execution flows through `ResearchCloudExecutionCoordinator`; WPF durable research uses the lifecycle-aware facade and `ResearchProductUiState`.
- Desktop Nebius research lifecycle is now explicitly opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE=true`. It reuses the full live deployment preflight and privately composes Object Storage, Serverless, signed remote runtime, lifecycle coordinator, and protected cloud audit. New paid dispatch is a separate opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH=true` and cannot be enabled without lifecycle recovery.
- Browser product/UI flows through `BrowserProductRuntime`; WPF does not receive the raw host.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol. Deployment preflight enforces mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned worker image, RSA identity consistency, bounded resources, and redacted deployment fingerprints.
- `Nvidea.NebiusContractProbe` supports planner, zero-cost live preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, remote encrypted transport, two-phase Nebius dispatch, signed resource-ID binding, exact-once result ingestion, lifecycle/cancellation reconciliation, bounded pagination, mounted transport, and worker deployment hardening.

### 2026-09-10 — Native Object Storage + reproducible live evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned live worker requirement, current Nebius lifecycle states, live runtime factory, explicit live modes, reproducible/redacted deployment fingerprints, MysteryBox validation, machine-readable PASS evidence, atomic artifact persistence, strict evidence verification, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 — Product lifecycle and authority hardening
Added `ResearchCloudExecutionCoordinator`, `ResearchProductRuntime`, replay-race protection under mutation leases, WPF lifecycle-aware research integration, explicit remote reconciliation/disclosure UX, `ResearchProductUiState`, `BrowserProductRuntime`, WPF browser/download migration, internal raw browser-host construction, and least-authority `IBrowserGoalHost` composition.

### 2026-09-10 to 2026-09-11 — Concrete persistence/runtime boundary hardening
Made concrete `ResearchJobRuntime` and `JsonAgentJobStore` construction assembly-internal, preserved public least-authority interfaces, narrowed privileged `BrowserGoalAgent` construction, hid raw persistent Playwright transport, narrowed path-backed browser goal-session/staging/quarantine construction, and added reflection regression tests preventing public reconstruction of privileged local lifecycle/persistence/browser authorities.

### 2026-09-11 — Desktop Nebius lifecycle composition
Completed:
- Re-read this ledger completely and inspected current head/history before mutation.
- Re-audited the next protected research transport candidates. `DirectoryProtectedResearchTransport` remains intentionally public because the separate `Nvidea.Worker` assembly must construct it over the Serverless-mounted shared volume. `NebiusObjectStorageClient` / `S3ProtectedResearchTransport` remain intentionally public because the live contract tooling must construct the native S3-compatible client. Narrowing those types at the Core assembly boundary would break legitimate deployment paths rather than improve least authority.
- Selected the higher-value known product blocker instead: WPF already had safe remote reconcile/cancel UX, but `NvideaCompositionRoot` always composed `ResearchProductRuntime` with `cloud: null`, so persisted remote jobs could only be displayed and local replay blocked.
- Added internal `DesktopResearchCloudMode` with two strict boolean environment gates: `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE` and `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH`. Both default false. Dispatch=true without lifecycle=true fails closed. Ambiguous values such as `1` are rejected instead of silently enabling cloud behavior.
- Updated `NvideaCompositionRoot` so lifecycle opt-in loads `NebiusResearchLiveConfigurationLoader`, thereby reusing the complete existing zero-cost deployment validation before provider construction. It then passes through `NebiusResearchLiveProviderStartup.CreateAfterDestinationPreflight`, creates the native Object Storage client/transport, Serverless client, protected `JsonLinesAuditTrail`, `NebiusResearchClientRuntime`, and `ResearchCloudExecutionCoordinator`, and exposes only the existing `ResearchProductRuntime` to WPF.
- Kept **new paid dispatch separately disabled by default**. Lifecycle-only mode can reconcile/cancel already-remote durable jobs without granting future dispatch authority. `ResearchProductUiState` already distinguishes this state in the UI.
- Preserved the existing WPF safety semantics: `DispatchReserved` must reconcile; unfinished remote provenance cannot fall back to local execution; active remote cancellation goes through the provider-aware coordinator; request cancellation does not falsely claim provider termination; emergency-stop cancellation of a local reconcile request leaves durable remote provenance authoritative.
- Added deterministic disposal ownership for desktop-created Object Storage and Serverless HTTP clients, including partial provider-startup failure paths.
- Added `DesktopResearchCloudModeTests.cs` covering local-only defaults, lifecycle-without-dispatch, dispatch dependency on lifecycle, and strict boolean parsing.
- Added `docs/desktop-remote-research.md` documenting opt-in behavior, fail-closed composition, recovery semantics, private provider ownership, and safety invariants.

Commits this run:
- `40b0e854c2ca04111c0808e0864b857f8a5ce62d` — add explicit desktop research cloud mode.
- `747c94ed662ce5f3052f7078490ea7a5b14b7d91` — compose opt-in desktop Nebius research lifecycle.
- `18ec3c1f62d678d0009eea02de19146f714b2842` — test desktop research cloud opt-in policy.
- `9e49057f33ecac1a1ec43847066927de85a628d4` — close desktop cloud provider startup disposal gap.
- `531bc5231ccacfc3b5c509968eda1c3ef9e2b81a` — document desktop Nebius lifecycle opt-in.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static compare from previous ledger head `37cd5dd20846c27d3f4b2b020bb1cf5d2137ccd5` to `531bc5231ccacfc3b5c509968eda1c3ef9e2b81a` is five commits ahead / zero behind and changes exactly four files: new desktop cloud-mode policy, composition-root integration, its focused tests, and desktop remote-research documentation.
- Existing `MainWindow.Research.cs` was inspected: reconcile/cancel controls already depend on `RemoteLifecycleAvailable`, use `ResearchProductRuntime.ReconcileRemoteAsync` / `CancelAsync`, and block local replay for unfinished remote state, so no raw provider authority needed to be added to WPF.
- Existing `ResearchProductUiState` was inspected: it already reports lifecycle-only vs lifecycle+dispatch distinctly and rejects dispatch without lifecycle support.
- Existing live configuration/provider startup/runtime paths were reused rather than duplicating provider assumptions: full deployment preflight, artifact destination gate, Object Storage transport, signed runtime, lifecycle coordinator, and audit trail remain the single implementation paths.
- The execution container was checked again; `command -v dotnet` produced no path and `dotnet --info` produced no output. Core/WPF/Worker compilation, XAML compilation, and test execution are therefore **not claimed**.
- No live Nebius credentials/resources, Object Storage operations, Serverless calls, model calls, or paid provider work were performed. No GitHub Actions workflow was triggered merely to obtain a green result.

Security / privacy / failure review:
- Provider credentials and signing material remain behind `NvideaCompositionRoot`; WPF sees only `ResearchProductRuntime`.
- Remote lifecycle provider construction is opt-in and fail-closed on invalid/incomplete live configuration.
- New cloud dispatch is independently opt-in and remains disabled in lifecycle-recovery mode.
- Existing per-stage `ResearchCloudAuthorization`, private-OS-data rejection, approval-bearing-stage rejection, durable state-directory mutation leases, signed bindings, exact-once result ingestion, and ambiguous-dispatch reconciliation remain unchanged.
- Provider resources are disposed on normal root disposal and partial provider startup failure.
- Local browser, local research, memory, Tavily evidence handling, download quarantine, emergency-stop behavior, and existing authority boundaries were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; the new Core/WPF composition and tests are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Desktop lifecycle mode currently depends on durable research being composed, which still requires `TAVILY_API_KEY`; a future cloud-only recovery bootstrap could be useful if recovery must run without local research credentials.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- The WPF currently exposes reconciliation/cancellation for existing remote jobs but does not yet expose a user-facing, scoped-approval control for intentionally dispatching a local research stage to Nebius Serverless. The runtime keeps new dispatch locked unless explicitly enabled.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research lifecycle, desktop cloud-mode, browser authority/integration, and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, implement the **WPF explicit scoped-approval remote-dispatch UX** on top of `ResearchProductRuntime.DispatchCurrentStageAsync`: show exact job/checkpoint disclosure and cloud-cost/data boundary, mint authorization only from a deliberate confirmation, keep private OS data ineligible, and never persist/reuse approval grants.
