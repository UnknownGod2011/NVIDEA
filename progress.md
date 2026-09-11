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
- Desktop diagnostics use `DesktopResearchReadiness`: startup failure and successful-start UI report capability state plus missing configuration **names only**. Secret values, PEM material, provider IDs, research payloads, and raw provider exception text are not surfaced.
- Successful-start WPF keeps a compact research-readiness strip visible and offers a read-only Details view distinguishing `ready`, `blocked`, and `locked` capabilities without constructing new provider authority.
- Windows voice invocation now exists as a local, review-first path: `Ctrl+Shift+V` or the Voice button asks for explicit microphone consent, transcribes with the installed Windows desktop speech recognizer, and places text into the prompt for review without auto-running it or routing audio to Nebius/Tavily/cloud speech.

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

### 2026-09-11 — Credential-safe desktop readiness diagnostics
- Added `DesktopResearchReadiness`, a side-effect-free projection for local Tavily research, Nebius lifecycle recovery, and new Nebius dispatch.
- Missing configuration is disclosed by documented variable **name only**; configured secret values and raw provider exceptions are never echoed.
- WPF startup stopped showing raw `ex.Message` for composition failures and instead uses sanitized readiness guidance.
- Added successful-start readiness strip + Details surface based only on already-composed runtime capabilities; no provider client/network side effect is introduced by viewing readiness.
- Added focused regression tests for ready/blocked/locked projection and non-disclosure.

### 2026-09-11 — Local review-first Windows voice invocation
Completed:
- Re-read this ledger fully, verified the current NVIDEA head/recent history, inspected the WPF hotkey/context/emergency-stop path and current project dependencies, and selected the persisted voice/transcription fallback because no usable .NET SDK is exposed in this execution environment.
- Added `src/Nvidea.Core/Desktop/LocalVoiceTranscription.cs` with a least-authority `ILocalVoiceTranscriber` contract and `LocalVoiceTranscript` review boundary. Transcript preparation trims only outer whitespace, rejects blank text, rejects invalid confidence values, and fails closed on oversized transcripts rather than silently truncating them.
- Added `src/Nvidea.Windows/SystemSpeechLocalTranscriber.cs`, a one-shot local Windows speech implementation using the installed `System.Speech` / SAPI recognizer and default microphone. It prefers the current UI culture, falls back to the matching language, then to the first installed recognizer.
- Added bounded recognition (20 seconds at the desktop call site), cancellation through a linked token, explicit timeout behavior, and generic failure handling. Captured audio is not intentionally sent to Nebius, Tavily, Token Factory, or a cloud speech service.
- Added stable `System.Speech` package reference (`9.0.8`) to the Windows project after checking current package compatibility; the package explicitly supports .NET 8 and Windows speech recognition.
- Added `src/Nvidea.Windows/MainWindow.Voice.cs`. The successful-start desktop dynamically exposes a Voice button without disturbing existing XAML layout ownership, plus a real global `Ctrl+Shift+V` hotkey registered against the existing WPF HWND.
- The global voice hotkey mirrors the existing text-hotkey privacy boundary: it captures foreground app/window/selection context before NVIDEA activates. Clipboard capture still obeys the existing opt-in checkbox.
- Every capture requires an explicit microphone disclosure/confirmation. Declining does not start recognition. The transcript is placed in `PromptBox` for user review and **never auto-runs**.
- Emergency stop cancels the active recognition operation. Window close also cancels and disposes recognition state, unregisters the voice hotkey, and removes its Win32 hook.
- Low-confidence recognition remains reviewable but is visibly labelled low-confidence. Empty/invalid/oversized results fail closed.
- Added `tests/Nvidea.Core.Tests/LocalVoiceTranscriptionTests.cs` covering trim semantics, blank rejection, oversize rejection, and invalid confidence rejection.
- Added `docs/local-voice.md` documenting usage, privacy boundary, Windows prerequisites, cancellation/failure semantics, and why the path is local-only.

Engineering commits before this ledger update:
- `aa8151ab8878b8141bea989f5b37afeadca9a862` — add least-authority local voice transcription contract.
- `365118666a152c37f411e06fd2cfa27485adadc2` — add offline Windows speech transcriber.
- `30f9eeee2ba92195cf9768e8f90fbc7140238cf6` — reference Windows offline speech recognition.
- `2d5654c9dca1436dff29cbc655461260dac2f9ce` — integrate explicit local voice capture flow.
- `a27a9175218f8d19e7702bf633741d7fed300242` — wire local voice capture into desktop controls.
- `fee7a50106838f39fa1673c6469c47bba56c0436` — initialize local voice entry point on desktop load.
- `8f5bb53ff2d5c17ef362fb403084fbecbdd63b01` — test local voice transcript review boundary.
- `43e313cf3bdc39c11613a635831c99cbef575675` — add global review-first voice invocation hotkey.
- `f4715e332f9b5c8a0610010857929923d46e283c` — harden offline speech recognizer result handling.
- `114cfa3d2a2751d07eb9b1c580c525f01bd541af` — document local review-first voice invocation.

Validation / evidence:
- Repository identity was explicitly verified before every GitHub mutation; every mutation targeted exactly `UnknownGod2011/NVIDEA`. No mutation was performed against `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `6bf584f763e1c09b5c9681c1ccec0144db5275bc` to engineering head `114cfa3d2a2751d07eb9b1c580c525f01bd541af` is **10 commits ahead / 0 behind** and changes exactly seven focused files: the Core voice contract, Windows local transcriber, Windows voice integration, one successful-start initialization line, the Windows project package reference, Core tests, and local-voice documentation.
- Current NuGet/Microsoft documentation was checked before choosing the speech dependency: `System.Speech` is Windows speech recognition and package `9.0.8` explicitly supports .NET 8.
- `command -v dotnet` and `dotnet --info` again produced no usable .NET signal in this automation environment. Core/Windows compilation, WPF runtime behavior, package restore, SAPI microphone capture, and test execution are therefore **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green check.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron/Tavily paid calls, or cloud speech calls were used.

Security / privacy / failure review:
- Voice capture is one-shot and explicitly consent-gated per attempt; microphone use does not begin on hotkey press until the user confirms the disclosure.
- The speech implementation has no Nebius/Tavily/provider dependency and receives only microphone audio; it does not receive durable research state, browser credentials, provider credentials, memory stores, or approval capabilities.
- The transcript is review-only until the existing Run action is separately invoked; no voice transcript can directly trigger a consequential tool action.
- Emergency stop and window close cancel microphone recognition; the global hotkey is unregistered on close.
- Existing clipboard disclosure remains opt-in. Foreground context capture occurs before app activation to preserve the current text-hotkey semantics.
- Generic voice failure messaging avoids echoing raw device/SAPI exceptions into the UI.
- Existing research dispatch approvals, private-data restrictions, browser permission gates, audit trail, cloud lifecycle semantics, and emergency-stop behavior remain unchanged.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML and tests are not compiled or executed here.
- The new `System.Speech` dependency has been documentation-checked but package restore and Windows runtime behavior still need a real .NET 8 Windows build/test pass.
- A real Windows machine still needs microphone permission plus an installed desktop speech recognizer/language; absence of either must be verified against the actual packaged app.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of the Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- The dry run cannot prove that the worker-private-key MysteryBox version corresponds to the configured worker public key without resolving the secret; the real worker protocol remains authoritative proof.
- Successful-start readiness intentionally reflects startup composition plus current opt-in/configuration presence; it does not continuously poll provider health or create background cloud authority.
- A verified production embedding adapter remains absent.
- Reproducibility evidence proves internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, and the Nebius contract tools; compile WPF/XAML; run the focused voice, readiness, lifecycle-only recovery, research dispatch/cloud-mode, browser authority/integration, API-surface, and security suites; then exercise a real local microphone recognition/cancel/timeout cycle and fix every compile/runtime defect. If executable validation remains unavailable, implement the missing **production embedding provider abstraction + real local/on-device embedding adapter** for semantic memory retrieval, with deterministic fallback, privacy-aware routing, model/version provenance, bounded batching, and retrieval-quality tests so memory no longer depends on an unverified placeholder-quality embedding path.
