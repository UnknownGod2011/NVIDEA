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
- Desktop Nebius research lifecycle is explicitly opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_LIFECYCLE=true`. It reuses full live deployment preflight and privately composes Object Storage, Serverless, signed remote runtime, lifecycle coordinator, and protected cloud audit. New paid dispatch is a separate opt-in via `NVIDEA_DESKTOP_REMOTE_RESEARCH_DISPATCH=true` and cannot be enabled without lifecycle recovery.
- WPF can now deliberately dispatch an eligible local research checkpoint through a **separate one-shot approval dialog**. The UI discloses exact job/checkpoint scope, encrypted cloud-data boundary, potential Serverless/Nemotron/Tavily cost, and private-data restrictions; it re-reads durable state after confirmation and mints an in-memory authorization only if the exact reviewed checkpoint is still eligible.
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
- Re-read this ledger completely and inspected current repo/head before changing code.
- Chose the highest-value unfinished product task: make already-built Nebius stage dispatch usable from Windows without turning the desktop feature flag into implicit authorization.
- Extended privacy-safe `ResearchJobStatus` with only the non-secret checkpoint step identifier plus a coarse `ContainsPrivateOsData` classification. The checkpoint payload, question, source URLs/content, provider IDs/errors, and approval material remain hidden from status UI.
- Extended `ResearchProductUiState` with `DispatchEnabled`. New dispatch is offered only when the validated composition enables it and the durable job is pending, local, runnable, has a checkpoint identifier, has no unfinished remote provenance, and is not classified as containing private OS-local data.
- Added `ResearchCloudDispatchDialog.xaml/.cs`. The modal dialog displays the exact job id, research stage and checkpoint identifier; explains that the checkpoint is encrypted on-device to the pinned worker key before Object Storage upload; explains that Serverless receives an opaque work-item id/lifecycle metadata rather than plaintext; warns that one Serverless job plus Nemotron/Tavily usage may incur cost; states that private OS-local research is ineligible and later stages require new consent; and requires a positive acknowledgement checkbox before `Approve once` is enabled.
- Added `Run stage on Nebius` to the research control panel, disabled unless `ResearchProductUiState` says the exact current stage is eligible.
- Added `ResearchDispatchButton_Click`. It retrieves the scope before showing the dialog, then **re-reads durable state after the modal confirmation** and refuses dispatch if state/location/checkpoint/privacy/reconciliation eligibility changed. Only after that revalidation does it construct a `ResearchCloudAuthorization` in memory with the exact job id, checkpoint step, current disclosure protocol version and current timestamp, and immediately pass it to `ResearchProductRuntime.DispatchCurrentStageAsync`.
- Approval is never persisted, cached or reused. Existing cloud-side `ResearchCloudExecutionCoordinator` and `ResearchWorkItemProtector.ValidateAuthorization` still independently re-read/validate durable state and exact authorization at the execution/cryptographic boundaries.
- Cancellation/failure copy is conservative: local cancellation of an in-flight dispatch never claims the remote job did not start; the UI directs lifecycle review/reconciliation before replay when provider acceptance may be ambiguous.
- Added `ResearchDispatchUiStateTests.cs` covering safe pending-local eligibility, private OS-data rejection, missing-checkpoint rejection, busy-operation suppression, and unfinished-remote-lifecycle suppression.

Commits this run:
- `2e5b70ef1821702d6c060149c3e21a50e083a8f1` — expose safe research dispatch scope metadata.
- `74df1f8461661f7c5f2f46e82fbdea024ea0b818` — project safe Nebius dispatch eligibility.
- `ef1229fa9d91dabc5e3cc58288e7310d3a097f4b` — add explicit Nebius research dispatch consent dialog.
- `0fa352dc4dd820ba58d22882e07db9cdb62781d0` — wire one-shot consent dialog.
- `163a9751173dc19bd926aea5b107a005adbe5049` — wire one-shot research dispatch approval.
- `688d02f3ffc6c222d36de7d9ab221880cda5e422` — add Nebius research stage dispatch control.
- `c998e4a55c182e7bc370feea162262d362ea19fd` — test Nebius dispatch UI eligibility.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation; every write target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Static compare from prior ledger head `0a722481f76caade4ae57bcc4780e0d48f65217b` to engineering head `c998e4a55c182e7bc370feea162262d362ea19fd` is **7 commits ahead / 0 behind** and changes exactly seven files: `ResearchJobStatus.cs`, `ResearchProductUiState.cs`, `MainWindow.Research.cs`, `MainWindow.xaml`, the new dispatch dialog XAML/code-behind, and the focused UI-state tests.
- The existing remote execution path was reused rather than bypassed: `ResearchProductRuntime.DispatchCurrentStageAsync` still gates feature enablement; `ResearchCloudExecutionCoordinator` still requires a pending local stage and rejects private OS data, approval-bearing stages, unfinished remote provenance, and ineligible checkpoints; `ResearchWorkItemProtector.ValidateAuthorization` still validates exact job/checkpoint/disclosure/time and rejects private OS data again before cryptographic/provider work.
- The execution container was checked again: `command -v dotnet` produced no path and `dotnet --info` produced no output. Core/WPF/Worker compilation, XAML compilation, and test execution are therefore **not claimed**.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron/Tavily paid calls, or GitHub Actions workflow runs were used for this run.

Security / privacy / failure review:
- The UI receives no checkpoint payload, question, source evidence, provider id, credential, worker private key, or transport object. It sees only the minimum non-secret scope needed for informed exact-stage approval.
- Private OS-local data is filtered in the UI projection and rejected again by the authoritative coordinator/protector.
- Consent is one-shot, exact-scope, in-memory and time-bound by the existing authorization validator; it is created only after the post-dialog durable-state re-read.
- A checkpoint/state race between consent and dispatch fails closed. The coordinator then performs another lease-protected durable-state validation, preserving defense in depth.
- Existing ambiguous-dispatch reconciliation, exact-once ingestion, cancellation, emergency stop, browser safety, memory, and Tavily evidence protections were not weakened or removed.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; the new Core/WPF code, XAML and tests are not compiled or executed here.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Desktop lifecycle mode currently depends on durable local research being composed, which requires `TAVILY_API_KEY`; a cloud-only recovery bootstrap would improve failure recovery when local research credentials are absent.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Local voice/transcription and a verified production embedding adapter remain absent.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable execution signal and compile `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; run the focused research lifecycle/dispatch, desktop cloud-mode, browser authority/integration, and API-surface suites and fix every compile/XAML/runtime defect. If executable validation remains unavailable, decouple **remote lifecycle recovery from `TAVILY_API_KEY`** so the desktop can safely reconcile/cancel an already-dispatched Nebius job even when local research credentials are missing, while continuing to keep new local research/dispatch unavailable until their required provider configuration is present.
