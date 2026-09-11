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
- Desktop Nebius research lifecycle is explicitly opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE=true`. Lifecycle-only recovery can be composed without `TAVILY_API_KEY`, so already-remote jobs can still be reconciled/cancelled without recreating local research authority.
- New paid dispatch is separately opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH=true`; it requires lifecycle support, an available local Tavily-backed runtime, and a one-shot exact-checkpoint approval in WPF.
- Remote research uses encrypted opaque work items, signed authoritative Nebius resource-ID bindings, two-phase dispatch, crash reconciliation, provider lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows-side S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol. Deployment preflight enforces mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned worker image, RSA identity consistency, bounded resources, and redacted deployment fingerprints.
- `Nvidea.NebiusContractProbe` supports planner, zero-cost live preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.
- Desktop startup diagnostics now use `DesktopResearchReadiness`: readiness is reported as capability state plus missing configuration **names only**. Secret values, PEM material, provider IDs, research payloads, and raw provider exception text are not surfaced to startup UI.

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

### 2026-09-11 — Desktop Nebius lifecycle + explicit dispatch
- Added strict desktop cloud gates `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE` and `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH`; dispatch requires lifecycle and both default false.
- Lifecycle opt-in reuses the complete live Nebius deployment validation path before composing Object Storage, Serverless, signed remote runtime, protected audit, and `ResearchCloudExecutionCoordinator` behind `ResearchProductRuntime`.
- Added one-shot WPF Nebius dispatch approval with exact job/stage/checkpoint scope, encryption/cost/private-data disclosure, durable-state re-read after consent, and in-memory authorization only.
- Added focused cloud-mode, dispatch-eligibility, and safety tests plus `docs/desktop-remote-research.md`.

### 2026-09-11 — Tavily-independent Nebius lifecycle recovery
- Split local research and remote lifecycle authorities in `ResearchProductRuntime`.
- Missing Tavily now fails closed local create/run/recovery/local cancel/report/new cloud dispatch while preserving status reads plus provider-aware remote reconciliation/cancellation.
- `NvideaCompositionRoot` can compose validated Nebius lifecycle independently of `TAVILY_API_KEY`.
- WPF recovery-only mode keeps Start/Resume/new dispatch locked while preserving Reconcile and remote Cancel.
- Added `ResearchLifecycleOnlyRuntimeTests.cs` and updated remote-research docs.
- Engineering commits: `3382dc0699ac087ee28849fe0597e8003a0117d6`, `4ddd47e4af606ae76de6840924b89c498ff16ac6`, `47f14ad73770810ba834458bd95afddca0a9f834`, `76dee6246767911fd161dfa0fda58f655f142f48`, `912c85383acb667f721eb7899042288a3ba447fe`, `341aa23e0cca0f7b6af5d62e567227c2d0bc40b9`, `f2b2a198d64a0ac5600ddba4932612d5ec295890`, `cc78a35a17334b7c5bbba52a6325921463101bbb`; ledger `0042f11059232234ff6e176aa4c5e6ef6b1b5a3c`.

### 2026-09-11 — Credential-safe desktop readiness diagnostics
Completed:
- Re-read this ledger completely, verified current repository/head, inspected desktop composition, live Nebius configuration/preflight, WPF research controls, and startup behavior, then selected the persisted fallback priority because no usable .NET SDK is exposed in this execution environment.
- Added `src/Nvidea.Core/Desktop/DesktopResearchReadiness.cs`, a side-effect-free readiness projection for local Tavily research, Nebius lifecycle recovery, and new Nebius dispatch.
- Readiness inspection returns capability state and missing environment-variable **names only**. It never returns environment values, access tokens, S3 credentials, PEM/key material, MysteryBox identifiers beyond configuration field names, provider resource IDs, research payloads, or provider SDK exception text.
- Runtime readiness remains distinct from configuration presence: lifecycle/dispatch cannot report ready until the actual validated runtime says they are ready; opt-in remains required; new dispatch still requires both lifecycle and local Tavily-backed research.
- Added a generic cloud-preflight-failure blocker directing operators to the existing redacted Nebius contract probe instead of echoing arbitrary provider exceptions.
- Hardened `src/Nvidea.Windows/App.xaml.cs`: desktop startup no longer displays `ex.Message`. Startup failure now emits a credential-safe capability summary and named configuration blockers, with a generic fallback when even cloud-mode parsing is invalid.
- Added `tests/Nvidea.Core.Tests/DesktopResearchReadinessTests.cs` covering secret-value non-disclosure, exact missing-name reporting, requested+validated readiness requirements, and generic cloud-failure text.

Commits this run before this ledger update:
- `70cb1aa566cb7fadc25a2de6b4dc525e70dc8328` — add credential-safe desktop research readiness model.
- `62817dfb07478631d9a37b780d8d8d4398ac222e` — test credential-safe desktop research readiness.
- `75fdc25ab385aa4d66d93a1af7aed4ace4b9cda7` — show credential-safe startup research diagnostics.

Validation / evidence:
- Repository identity was explicitly verified before every mutation; every write target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static compare from prior ledger head `0042f11059232234ff6e176aa4c5e6ef6b1b5a3c` to engineering head `75fdc25ab385aa4d66d93a1af7aed4ace4b9cda7` is **3 commits ahead / 0 behind** and changes exactly three files: new `DesktopResearchReadiness.cs`, new `DesktopResearchReadinessTests.cs`, and `App.xaml.cs`.
- `Nvidea.Core.csproj` targets `net8.0` with implicit usings enabled, nullable enabled, and warnings-as-errors; the new readiness implementation only uses BCL APIs already covered by that project configuration.
- `command -v dotnet` and `dotnet --info` again produced no usable .NET signal. Compilation, WPF/XAML compilation, and test execution are therefore **not claimed**.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron/Tavily paid calls, or GitHub Actions runs were used.

Security / privacy / failure review:
- Diagnostics are read-only and do not construct provider clients, make network calls, mint approvals, mutate jobs, or weaken fail-closed startup.
- Missing configuration is disclosed by documented variable name only; configured secret values are never interpolated into blocker text.
- Raw startup exception messages are no longer shown to the user, reducing accidental leakage from provider/configuration exceptions.
- Cloud readiness cannot become true merely because variables are present: runtime validation remains authoritative.
- Existing local/private-data restrictions, exact cloud authorization, lifecycle reconciliation, cancellation semantics, encrypted transport, audit trail, and emergency-stop behavior are unchanged.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML and tests are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Successful-start WPF currently does not yet expose a dedicated always-visible full readiness/details view; this run primarily secures and improves startup-failure diagnostics plus provides the reusable Core readiness model.
- Local voice/transcription and a verified production embedding adapter remain absent.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused readiness, lifecycle-only recovery, research dispatch/cloud-mode, browser authority/integration, API-surface, and WPF/XAML suites and fix every compile/runtime defect. If executable validation remains unavailable, integrate `DesktopResearchReadiness` into an **always-visible successful-start WPF readiness/details surface** that distinguishes configured/requested/validated state, keeps secret values redacted, and gives judges/users actionable setup guidance without constructing new provider authority or weakening fail-closed behavior.
