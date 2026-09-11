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
- Desktop Nebius research lifecycle is explicitly opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE=true`. It reuses full live deployment preflight and privately composes Object Storage, Serverless, signed remote runtime, lifecycle coordinator, and protected cloud audit. This lifecycle-only recovery surface can now be composed even when `TAVILY_API_KEY` is unavailable, so already-remote jobs can still be reconciled/cancelled without recreating local research authority.
- New paid dispatch is a separate opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH=true`; it requires lifecycle support **and** an available local Tavily-backed research runtime. Missing Tavily credentials therefore fail closed all local execution and all new cloud dispatch while preserving safe recovery of existing Nebius work.
- WPF can deliberately dispatch an eligible local research checkpoint through a **separate one-shot approval dialog**. The UI discloses exact job/checkpoint scope, encrypted cloud-data boundary, potential Serverless/Nemotron/Tavily cost, and private-data restrictions; it re-reads durable state after confirmation and mints an in-memory authorization only if the exact reviewed checkpoint is still eligible.
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
- Added strict desktop cloud gates `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE` and `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH`; dispatch requires lifecycle and both default false.
- Lifecycle opt-in reuses the complete live Nebius deployment validation path before composing Object Storage, Serverless, signed remote runtime, protected audit, and `ResearchCloudExecutionCoordinator` behind `ResearchProductRuntime`.
- Preserved fail-closed remote semantics: ambiguous dispatch must reconcile; unfinished remote provenance cannot replay locally; cancellation remains provider-aware; provider resources are disposed on normal and partial-startup paths.
- Added `DesktopResearchCloudModeTests.cs` and `docs/desktop-remote-research.md`.
- Commits: `40b0e854c2ca04111c0808e0864b857f8a5ce62d`, `747c94ed662ce5f3052f7078490ea7a5b14b7d91`, `18ec3c1f62d678d0009eea02de19146f714b2842`, `9e49057f33ecac1a1ec43847066927de85a628d4`, `531bc5231ccacfc3b5c509968eda1c3ef9e2b81a`, ledger `0a722481f76caade4ae57bcc4780e0d48f65217b`.

### 2026-09-11 — One-shot Nebius research dispatch approval UX
Completed:
- Extended privacy-safe `ResearchJobStatus` with only the non-secret checkpoint step identifier plus a coarse `ContainsPrivateOsData` classification; checkpoint payload/question/source/provider/approval material remains hidden from status UI.
- Added `ResearchProductUiState.DispatchEnabled`; new dispatch is offered only for an eligible pending local checkpoint with no unfinished remote provenance and no private OS-local data.
- Added `ResearchCloudDispatchDialog.xaml/.cs` with exact job/stage/checkpoint scope, on-device encryption disclosure, opaque Serverless metadata boundary, cloud/provider cost warning, private-data restriction, and a positive acknowledgement checkbox.
- Added `Run stage on Nebius` to WPF. After modal approval, the desktop re-reads durable state and refuses dispatch if state/location/checkpoint/privacy/reconciliation eligibility changed; only then is one in-memory `ResearchCloudAuthorization` minted and immediately consumed.
- Existing coordinator/protector still independently re-read/validate exact authorization and durable state at the execution/cryptographic boundaries.
- Added `ResearchDispatchUiStateTests.cs` covering eligible dispatch, private-data rejection, missing checkpoint, busy suppression, and unfinished remote lifecycle suppression.
- Commits: `2e5b70ef1821702d6c060149c3e21a50e083a8f1`, `74df1f8461661f7c5f2f46e82fbdea024ea0b818`, `ef1229fa9d91dabc5e3cc58288e7310d3a097f4b`, `0fa352dc4dd820ba58d22882e07db9cdb62781d0`, `163a9751173dc19bd926aea5b107a005adbe5049`, `688d02f3ffc6c222d36de7d9ab221880cda5e422`, `c998e4a55c182e7bc370feea162262d362ea19fd`, ledger `6ecc23ad9d305b9f6915e958bf8eb9e208e59939`.

### 2026-09-11 — Tavily-independent Nebius lifecycle recovery
Completed:
- Re-read this ledger completely, inspected the current composition/product/WPF/tests/docs, and selected the persisted highest-value fallback task because executable .NET validation remains unavailable.
- `ResearchProductRuntime` now treats local execution and remote lifecycle as independent authorities. Its local runtime is nullable; `LocalExecutionAvailable` is explicit; shared durable List/Get remain available without Tavily; remote reconcile/provider cancellation remain available with only the cloud coordinator; local Create/Run/Recover/local Cancel/report-read fail closed through `RequireLocal()` when Tavily-backed execution is unavailable.
- New remote dispatch cannot be enabled without **both** a cloud lifecycle coordinator and local research runtime. This prevents recovery-only composition from becoming an implicit paid-dispatch path.
- `NvideaCompositionRoot` now parses cloud mode and builds the validated Nebius lifecycle graph independently of `TAVILY_API_KEY`. If lifecycle is enabled and live Nebius preflight passes, WPF can obtain a lifecycle-only `ResearchProductRuntime` even when Tavily is missing. If Tavily later exists, the same facade gains local research; dispatch remains enabled only when both policy and local runtime allow it.
- WPF now keeps the research panel available in lifecycle-only mode, leaves Start/Resume/new dispatch/local cancellation/report reads locked, and keeps Reconcile plus provider-aware remote Cancel usable. Status/disclosure copy states that existing remote jobs can be recovered without local Tavily credentials.
- `ResearchProductUiState` now explicitly supports this split: remote reconciliation/cancellation can be enabled while local controls stay disabled, and impossible `remoteDispatchEnabled` combinations still throw.
- Added `ResearchLifecycleOnlyRuntimeTests.cs` covering reserved-work reconciliation, dispatched-job provider cancellation, local mutation fail-closed behavior, dispatch-without-local rejection, and UI projection for recovery-only mode.
- Updated `docs/desktop-remote-research.md` so setup/safety guidance accurately documents recovery-only composition and the continued Tavily requirement for local/new-dispatch work.

Commits this run:
- `3382dc0699ac087ee28849fe0597e8003a0117d6` — decouple remote research recovery from local Tavily runtime.
- `4ddd47e4af606ae76de6840924b89c498ff16ac6` — compose Nebius lifecycle without requiring Tavily.
- `47f14ad73770810ba834458bd95afddca0a9f834` / `76dee6246767911fd161dfa0fda58f655f142f48` — add lifecycle-only UI projection then preserve the existing projection call contract.
- `912c85383acb667f721eb7899042288a3ba447fe` — keep Nebius recovery usable when local research is unavailable.
- `341aa23e0cca0f7b6af5d62e567227c2d0bc40b9` / `f2b2a198d64a0ac5600ddba4932612d5ec295890` — add and tighten lifecycle-only recovery regression tests.
- `cc78a35a17334b7c5bbba52a6325921463101bbb` — document Tavily-independent Nebius lifecycle recovery.

Validation / evidence:
- Repository identity was explicitly verified before every mutation; every write target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static compare from prior ledger head `6ecc23ad9d305b9f6915e958bf8eb9e208e59939` to engineering head `cc78a35a17334b7c5bbba52a6325921463101bbb` is **8 commits ahead / 0 behind** and changes exactly six files: `NvideaCompositionRoot.cs`, `ResearchProductRuntime.cs`, `ResearchProductUiState.cs`, `MainWindow.Research.cs`, new `ResearchLifecycleOnlyRuntimeTests.cs`, and `docs/desktop-remote-research.md`.
- The compare reports 361 additions / 87 deletions across those files; most additions are focused regression coverage and explicit safety/documentation paths rather than duplicated provider implementations.
- `command -v dotnet` again returned no path and `dotnet --info` returned no output. Core/WPF/Worker compilation and test execution are therefore **not claimed**.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron/Tavily paid calls, or GitHub Actions runs were used.

Security / privacy / failure review:
- Recovery-only composition never constructs a Tavily client or local `ResearchJobRuntime`; it only exposes durable status plus the already-constrained Nebius lifecycle coordinator.
- Missing Tavily cannot trigger local replay, local cancellation mutation, report reads, or a new cloud dispatch. All such routes fail closed before provider work.
- Existing remote/ambiguous provenance remains authoritative: reserved dispatch must reconcile, dispatched work uses provider-aware cancellation, and no local fallback was introduced.
- Existing live Nebius configuration validation, Object Storage protection, signed binding, exact-once result ingestion, shared mutation lease, cloud audit, private-data restrictions, one-shot dispatch authorization, and provider disposal paths remain intact.
- The WPF recovery-only view receives no additional checkpoint payload, question, source evidence, provider credential, private key, or transport authority.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; the current Core/WPF/Worker code, XAML and tests are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Local voice/transcription and a verified production embedding adapter remain absent.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused lifecycle-only recovery, research dispatch/cloud-mode, browser authority/integration, API-surface, and WPF/XAML suites and fix every compile/runtime defect. If executable validation remains unavailable, add a **credential-safe desktop startup diagnostics surface** that reports local research / Nebius lifecycle / new-dispatch readiness and exact redacted preflight blockers without exposing secrets or weakening fail-closed startup, so judges/users can understand why a capability is locked and how to make the demo deployment ready.
