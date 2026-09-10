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
- `JsonAgentJobStore`, `ResearchJobRuntime`, raw `BrowserHostRuntime` construction, privileged `BrowserGoalAgent(BrowserHostRuntime,...)` construction, and the raw persistent Playwright transport are trusted Core-only construction surfaces rather than public product/plugin escape hatches.
- Product research flows through `ResearchProductRuntime`; provider-aware remote execution flows through `ResearchCloudExecutionCoordinator`; WPF durable research uses the lifecycle-aware facade and `ResearchProductUiState`.
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
Made concrete `ResearchJobRuntime` and `JsonAgentJobStore` construction assembly-internal, preserved public least-authority interfaces, and added reflection regression tests preventing public reconstruction of privileged local lifecycle/persistence authorities.

### 2026-09-11 — Current run: hide raw persistent Playwright transport
Completed:
- Re-read this ledger completely and inspected current repo head/history, Core tree, desktop composition root, `PersistentBrowserContextFactory`, `BrowserHostRuntime`, `PlaywrightBrowserSessionDriver`, assembly friend visibility, and existing authority-surface test conventions.
- Confirmed again that this execution environment has no usable `dotnet` executable; no compile/test success is claimed.
- Identified a genuine remaining browser authority bypass: `PersistentBrowserContextFactory` was public and returned `PersistentBrowserContextSession`, which exposes the raw Playwright `IBrowserContext`, session driver, download quarantine, and staging guard. External in-process product/plugin code could therefore launch against the NVIDEA-owned persistent browser profile and obtain direct Playwright execution authority without going through `BrowserProductRuntime`, `BrowserHostRuntime`, capability policy, exact approval gates, durable job orchestration, audit semantics, or emergency-stop ownership.
- Changed `PersistentBrowserContextFactory` from public to assembly-internal and changed `PersistentBrowserContextSession` from public to assembly-internal. The launch implementation, persistent-profile behavior, state-directory lease transfer, authenticated browser state, popup/tab safety, download quarantine/staging, timeouts, and trusted `BrowserHostRuntime` call path are unchanged.
- Added explicit XML documentation recording that this is privileged transport infrastructure and that product/plugin code must not receive this authority boundary.
- Added `tests/Nvidea.Core.Tests/PersistentBrowserTransportApiSurfaceTests.cs`. It asserts both transport types are absent from the Core assembly's exported type set while preserving the internal launch seam for trusted Core/tests through existing `InternalsVisibleTo("Nvidea.Core.Tests")`.

Commits this run:
- `9890dde2b12c354d761119d2cdc5dd0ef75012f8` — hide raw persistent browser transport boundary.
- `22fa6578109ab4975f4998a6a52c447943f2cd53` — lock raw browser transport behind Core API boundary.

Validation / evidence:
- Repository identity was explicitly re-verified immediately before every GitHub mutation; every mutation target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static inspection confirms `BrowserHostRuntime` calls the internal `PersistentBrowserContextFactory.LaunchOwnedAsync(...)` from the same Core assembly, so trusted product composition remains source-accessible.
- `src/Nvidea.Core/Properties/AssemblyInfo.cs` grants `InternalsVisibleTo("Nvidea.Core.Tests")`, so integration/API-surface tests retain access to the internal transport.
- No usable `dotnet` executable is installed in the execution environment; Core compilation, WPF/XAML compilation, Worker compilation, and test execution are therefore **not claimed**.
- No live Nebius credentials/resources were used and no GitHub Actions workflow was triggered merely to manufacture a green result.

Security / privacy / failure review:
- External product/plugin code can no longer use NVIDEA's public Core API to launch the NVIDEA-owned persistent Chromium profile and directly obtain raw Playwright context/driver authority around capability/approval/audit/job guards.
- This does not attempt to prevent fully trusted arbitrary code from separately using Playwright or directly accessing the user's filesystem; it prevents NVIDEA itself from exporting a privileged shortcut into its owned authenticated browser state.
- BrowserHostRuntime behavior, host allowlists, state-directory single-owner lease, profile ownership checks, download quarantine, approval semantics, prompt-injection defenses, crash recovery, and emergency stop are unchanged.
- The public lower-level browser abstractions remain available where they are generic protocols; this change narrows only the NVIDEA-owned persistent-profile bootstrap/session boundary.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/API-surface changes are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Current WPF composition intentionally has no cloud lifecycle coordinator, so pre-existing remote records can be displayed safely but cannot yet be reconciled/cancelled from the desktop. Local fallback remains blocked.
- Local voice/transcription and a verified production embedding adapter remain absent.
- The evidence pair proves reproducibility consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research, browser authority, browser integration and API-surface suites and fix every compile/XAML/runtime defect. If execution remains unavailable, continue the authority audit with `JsonBrowserGoalSessionStore`, download quarantine/staging and protected research transport constructors: narrow only concrete NVIDEA-owned path/credential-bearing mutation surfaces that can bypass product capability/lifecycle authorities, while preserving public protocol abstractions required by Worker, contract tooling, tests and future Nebius composition.
