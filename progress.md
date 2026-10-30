# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, authoritative research provenance, serialized browser-verification state, production-observed session milestones, and a fail-closed recording gate.

## Persistent history
### 2026-09-06 to 2026-09-21 — product foundation and hardening
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling. Hardened browser transport, prompt-injection/consequential-action gates, emergency stop, protected browser receipts, durable research lineage and crash-ambiguous reconciliation.

### 2026-09-22 to 2026-09-23 — composition lifetime, browser qualification, citation authority
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, integrated `CompositionLifetimeGate`, and moved browser facades behind root-lifetime authority. Added exactly-one-lease browser goal semantics and qualified shutdown behavior. Added deterministic research citation verification, Verified/Partial/Unverified/NoSources authority, payload-safe `ResearchJudgeEvidence`, evaluator/Windows presentation, and provider-free durable evidence reads.

### 2026-09-24 — durable provenance and remote-result authentication migration
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, production desktop authenticated-result composition, and MysteryBox-only live signing-key deployment preflight. Actual-ingestor adversarial coverage includes unsigned results, wrong pinned worker identity, signed remote-job replay, and post-signature ciphertext/report-payload substitution. Added an operator-grade worker result-signing lifecycle runbook, recording-day signing readiness, and provider-free source-contract guards.

### 2026-10-14 — public worker authentication evidence model
Added `WorkerAuthenticationJudgeEvidence` with validated uppercase SHA-256 SPKI fingerprint and fixed RSA-PSS/SHA-256 scheme, plus provider-free privacy and malformed-input tests. This work was initially persisted on `automation/worker-auth-evidence`; `main` was not altered.

## Latest run — Windows worker-authentication judge evidence integration (2026-10-14)
Repository/branch:
- Mutation target checked before every GitHub write: exactly `UnknownGod2011/NVIDEA`.
- Code branch: `automation/worker-auth-evidence`, proposed to `main` in draft PR #1 (https://github.com/UnknownGod2011/NVIDEA/pull/1). No other repository changed.

Files changed:
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml` — added a first-class Remote worker authentication panel, adjusted grid row layout.
- `src/Nvidea.Windows/JudgeEvidenceDialog.xaml.cs` — receives a Core public-only projection, renders it, and fails closed when missing.
- `src/Nvidea.Windows/MainWindow.Readiness.cs` — reads validated public-key fingerprint via `WorkerAuthenticationJudgeEvidence.FromPinnedEnvironment()`, catches invalid/missing configuration without exposing exception contents, passes projection to judge view.
- `tests/Nvidea.DemoPackageValidator.Tests/WorkerAuthenticationJudgeWiringTests.cs` — provider-free source/XAML contract guarding exactly one panel, production factory wiring, null fail-closed status, non-attestation wording, and absence of raw environment access in dialog.
- `progress.md` — this persistent run record.
- Prior branch files (retained): `src/Nvidea.Core/Desktop/WorkerAuthenticationJudgeEvidence.cs`, `tests/Nvidea.Core.Tests/WorkerAuthenticationJudgeEvidenceTests.cs`.

Engineering result:
- The Windows judge view shows a locally PINNED worker public-key SHA-256 fingerprint and RSA-PSS/SHA-256 scheme, or NOT VERIFIED if the projection cannot be constructed.
- A public pin is configuration readiness only: the panel explicitly says the deployed worker identity is NOT VERIFIED by this view. No cloud/worker attestation is fabricated.
- No PEM, private-key material, deployment secret name/ID/version, research prompt/source/answer, provider exception, or URL is rendered in this panel.
- Existing receipt/provenance/permissions/emergency-stop and remote authentication paths remain unchanged.

Validation/evidence:
- GitHub connector accepted four production-file commits plus the regression-test commit (latest code/test commit `9c9f43524599e6150976e9c3fc063e6a06159251`).
- Ten repository-content static checks passed: correct row count/index range, unique UI field, dialog binding, null fail-closed, no claimed deployed-worker verification, valid C# newline escape, factory + constructor wiring, no raw exception display, xUnit project present.
- `main...automation/worker-auth-evidence`: branch ahead by 6 commits before this progress update, behind by 0.
- `.NET`, PowerShell and Windows/WPF runtime are not installed here. Executable compile/test PASS is NOT claimed. New xUnit source contract is added but unexecuted.
- No Nebius/Tavily/Serverless calls, paid providers, live signed-result canary, or GitHub Actions reruns occurred.

Risks and blockers:
- Execute `dotnet test` (including Core + DemoPackageValidator projects), WPF Windows build, and interactive Judge Evidence smoke test on an appropriate Windows/.NET 8 host.
- Draft PR #1 requires review and CI/Windows qualification before merge; changes are currently branch-only.
- Public-pin presence is not proof the deployed MysteryBox signing private key corresponds to the Windows public pin.
- Single-key rotation still requires coordinated dispatch pause/drain; auth must never downgrade to unsigned results.

## Single Best Next Task
Implement a real end-to-end **signed-result canary** across deployed Nebius Worker and Windows pinned-key verifier that proves signer/pin matching without exposing research data, with negative cases for wrong key, replay and signing-version drift. Integrate its verified outcome into fail-closed recording readiness; run .NET 8 + WPF qualification and update draft PR #1 based on results.

## Current run — voice-first keyboard-wtf implementation stage (2026-10-30)

Repository/branch inspected before work: exact mutation target `UnknownGod2011/NVIDEA`, `main`, starting HEAD `923384905bb445432c8625082169f681439ac794`; origin is `https://github.com/UnknownGod2011/NVIDEA.git`. The read-only `keyboard.wtf` reference was inspected for its actual `AudioRecorderService`, Vosk streaming-session, `VoiceCaptureService`, local TTS, overlay and Jarvis routing patterns. It was not changed.

Implemented a cohesive first voice runtime in NVIDEA:
- `JarvisConversation` uses the existing `IAgentInferenceClient` (NVIDIA Nemotron/Nebius) and `PersonalMemoryService`, preserves a bounded conversation window, retrieves eligible memory per turn, accepts only three JSON-schema typed tools, rejects malformed/unsupported tool calls, and limits a turn to one verified tool-follow-up round.
- `SystemSpeechStreamingRecognizer` replaces one-shot-only voice behavior with local continuous Windows recognition, partial transcript projection and a 900 ms speech-pause turn boundary. It does not send microphone audio to a network provider.
- `LocalSpeechOutput` provides cancellable local Windows SAPI playback. The voice hotkey interrupts active Jarvis speech; Emergency Stop cancels speech and the active turn/session.
- Windows now exposes Local Dictation, Smart Dictation and Jarvis in one voice selector. Local Dictation restores the exact pre-invocation foreground HWND before insertion and fails closed if the target changed. Smart Dictation sends transcript text only to Nemotron, asks for local insertion approval, and never sends microphone audio to Nebius. Jarvis has a continuous pause-turn-listen/reason/speak loop and its initial typed local tools are approved application launch, approved HTTPS website launch and approved captured-target text insertion. It never accepts a model-generated shell command.
- `docs/voice-first-windows-assistant.md` documents the reference-to-NVIDEA map, runtime privacy behavior, optional Riva/Speech NIM strategy, and physical-device validation boundary. README now describes the three modes accurately.

Validation actually executed on this Windows .NET 8 host:
- `dotnet test tests/Nvidea.WorkerAuth.Tests/Nvidea.WorkerAuth.Tests.csproj -c Release --no-restore` — PASS (7/7), including typed tool receipt/follow-up, unsupported shell-style tool rejection, bounded conversation follow-up, plus the existing worker-authentication suite.
- `dotnet build src/Nvidea.Windows/Nvidea.Windows.csproj -c Release --no-restore` — PASS, including WPF/XAML compilation.
- `git diff --check` — PASS.

Follow-up correction: composition now supplies an `UnconfiguredInferenceClient` only when `NEBIUS_API_KEY` is absent, so the desktop app can start for genuine local-only Local Dictation. Smart Dictation/Jarvis fail at their actual cloud-reasoning boundary with safe setup guidance. Focused provider-free suite was rerun after this change: PASS (8/8).

Interactive Windows smoke: launched the freshly compiled `Nvidea.Windows.exe` with no `NEBIUS_API_KEY`; the WPF application opened successfully, showed the Local Dictation / Smart Dictation / Jarvis selector plus Voice button, and reported local research blocked only by missing Tavily rather than crashing at startup. The smoke deliberately did not open the microphone consent prompt or invoke microphone/speaker hardware, so it is UI/startup evidence only—not physical ASR/TTS qualification.

Branding follow-up: changed visible WPF titles, headings, confirmation and quarantine-dialog product copy to `keyboard-wtf`, retaining `Nvidea.*` assemblies, repository identity, local data root and configuration variable names for compatibility. Rebuilt the WPF host and launched it again; its actual title, heading, prompt tooltip and Local Dictation / Smart Dictation / Jarvis controls rendered as expected.

Voice reliability follow-up: fixed the continuous Windows recognizer so it retains the final bounded live hypothesis if `SpeechRecognitionEngine` has not committed it before the pause boundary. WPF Release build and focused provider-free regression suite both passed after the correction.

Not yet claimed: no physical microphone/speaker smoke was run; no live Nebius inference was made because no credential was loaded; Riva/Speech NIM ASR/TTS is not implemented or configured; Vosk/Whisper local-model fallback and the floating orb visual layer remain future work. The current default local speech path requires an installed Windows desktop speech recognizer and SAPI voice. The next highest-value task is a physical Windows voice smoke with a Nebius key, then a Vosk local-model fallback and an operator-configured Riva/Speech NIM adapter if lower-latency cloud speech is desired.

## Judge onboarding and NVIDIA-only provider audit (2026-10-30)

Preserved the existing keyboard.wtf-inspired Windows interaction model; no `keyboard.wtf` files were accessed or changed. NVIDEA has no legacy-provider adapter or credential path: cloud reasoning remains NVIDIA Nemotron through Nebius Token Factory, local voice remains Windows-native, and optional Ollama remains local-memory-only.

Updated judge-facing onboarding:
- `README.md` now has a Windows source-build quick start, current-session and persisted Token Factory/Tavily key setup, four RSA role documentation, remote signing-pin instructions, focused worker-auth test command, and portable GitHub Release artifact guidance.
- `docs/judge-setup.md` gives a clean judge setup for normal desktop use, optional local voice/memory, and explicit paid Serverless boundaries.
- `scripts/publish-windows.ps1` creates a self-contained `win-x64` publish directory inside the repository for later GitHub Release upload; it does not create a release or claim release qualification.
- Removed legacy-provider wording from tracked judge-facing documentation. A tracked-source scan for legacy provider names passed.

Validation actually executed:
- `dotnet build src/Nvidea.Windows/Nvidea.Windows.csproj -c Release` — PASS.
- `dotnet test tests/Nvidea.WorkerAuth.Tests/Nvidea.WorkerAuth.Tests.csproj -c Release` — PASS (4/4).
- `scripts/publish-windows.ps1 -OutputDirectory .\\artifacts\\NVIDEA-win-x64` — PASS; output contained `Nvidea.Windows.exe`.
- `git diff --check` — PASS.

## Current run — remote signed-worker canary completion attempt (2026-10-30)

Repository/branch inspected before work: `UnknownGod2011/NVIDEA`, `automation/worker-auth-evidence`, starting HEAD `d1e22c77bbe2b1921a0adb6765b7f2a4b6977a9e`; origin remains `https://github.com/UnknownGod2011/NVIDEA.git`. `keyboard.wtf` was not accessed or changed.

Implemented the production missing-secret fix: `NebiusResearchLiveConfigurationLoader` now maps `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM` into `SecretEnvironmentVariables` using the dedicated live secret ID and immutable version variables. It remains absent from plaintext environment configuration and is preflight-validated.

Implemented the first real signed-worker canary path: a fresh ten-minute domain-separated RSA-PSS/SHA-256 challenge is submitted in a dedicated Serverless job using the configured digest-pinned image, volume and signing-secret reference. The returned job ID is hashed into a public invocation artifact; the worker reads it and create-once publishes a bounded proof artifact to the existing Object Storage transport. The client requires completed job state, verifies the public proof against the outstanding challenge/current public pin/job binding/deployment fingerprint, and persists only a DPAPI-protected receipt. The Windows Judge Evidence panel now projects pin-only, running, failed, verified and stale states; it never accepts a caller-provided verification boolean and never displays secret/provider identifiers.

The deterministic stale-clock regression was corrected in `WorkerSigningCanaryTests`: its successful round trip now captures a fresh UTC timestamp while expiration tests retain a deterministic clock. Added loader assertions for exact dedicated secret ID/version, plaintext exclusion, missing ID/version, and malformed version failure.

Documentation updated: `docs/nebius-research-worker.md` now documents all four RSA roles and the exact result-signing secret mapping; `docs/worker-result-signing-key-lifecycle.md` documents Object Storage proof return, job/image/secret/pin binding, receipt staleness and rollback. The proof artifact approach was selected over log parsing because it is bounded, create-once, works through the existing S3-compatible protected transport, and is always cryptographically verified before acceptance.

Validation actually executed on this Windows .NET 8 host:
- `dotnet build src/Nvidea.Core/Nvidea.Core.csproj -c Release --no-restore` — PASS.
- `dotnet build src/Nvidea.Worker/Nvidea.Worker.csproj -c Release --no-restore` — PASS.
- `dotnet build src/Nvidea.Windows/Nvidea.Windows.csproj -c Release --no-restore` — PASS, including WPF/XAML compilation.
- `dotnet test tests/Nvidea.Core.Tests/Nvidea.Core.Tests.csproj -c Release --no-restore` — NOT PASS: the existing suite has unrelated API-drift/old-source compile failures across browser, memory, research and analyzer tests; no test result is claimed.
- `dotnet test tests/Nvidea.DemoPackageValidator.Tests/Nvidea.DemoPackageValidator.Tests.csproj -c Release --no-restore` — NOT PASS: existing test source has malformed interpolated raw string syntax; no test result is claimed.
- `Nvidea.sln` is absent, so the requested solution command cannot run.

Follow-up validation added after the baseline-suite audit:
- `tests/Nvidea.WorkerAuth.Tests/Nvidea.WorkerAuth.Tests.csproj` is a focused provider-free regression project so worker-authentication coverage remains executable while the unrelated legacy Core suite is repaired.
- `dotnet test tests/Nvidea.WorkerAuth.Tests/Nvidea.WorkerAuth.Tests.csproj -c Release` — PASS (3/3): fresh one-use proof verification, pin/deployment/job binding, immutable secret requirement, plaintext-secret exclusion, and a provider-free Serverless orchestration contract that verifies use of the configured digest-pinned image and immutable signing-secret version before accepting a returned-job-bound proof.

Recording-gate completion follow-up:
- Added `tools/Nvidea.WorkerSigningCanaryVerifier`, a local no-network verifier that loads the active live deployment configuration, DPAPI-unprotects a receipt from `NVIDEA_WORKER_SIGNING_CANARY_EVIDENCE_PATH`, and independently projects `VERIFIED` only if the signature, expiry, active image, immutable signing-secret version, and public pin still match.
- `scripts/live-demo-readiness.ps1 -RequireCloudResearch` now invokes that verifier and blocks recording unless the result is `VERIFIED`; configuration-variable presence or an editable JSON boolean is insufficient.
- `dotnet test tests/Nvidea.WorkerAuth.Tests/Nvidea.WorkerAuth.Tests.csproj -c Release` — PASS (4/4), including protected-receipt revalidation, stale evidence, and rejection of plain `verified` JSON.
- `dotnet build tools/Nvidea.WorkerSigningCanaryVerifier/Nvidea.WorkerSigningCanaryVerifier.csproj -c Release --no-restore` — PASS.
- `dotnet build src/Nvidea.Windows/Nvidea.Windows.csproj -c Release` — PASS.
- Draft PR #1 security qualification: GitGuardian Security Checks PASS on the canary branch; PR remains OPEN/DRAFT and mergeable. No merge was performed.

No provider credentials were loaded, no Nebius job was launched, and no paid/live deployment or live canary is claimed. Remaining live blockers are an operator-provisioned Nebius Serverless/Object Storage/SecretStash deployment, the result-signing secret ID/version, matching public pin, and explicit authorization to create the paid canary job. The single best next task after repairing the unrelated test-suite source drift is to run that operator-confirmed canary and record the Windows dialog state/evidence for the submission video.

## Current goal continuation — voice controls, accurate branding, and full-duplex audit (2026-10-30)

Repository checked before this continuation: `UnknownGod2011/NVIDEA`, branch `main`, HEAD `e644493d5503375e370482324d5fd1e6cbd1c2be`, origin `https://github.com/UnknownGod2011/NVIDEA.git`. The remote `main` matched that commit. `keyboard.wtf` remains read-only.

Completed the shortcut-settings follow-up already pushed in `e644493` and continued product polish in this run:
- Local Dictation started from the app now leaves its exact transcript in the prompt for review when no external target was captured.
- Smart Dictation started from the app now leaves the polished result for review when no external target was captured. It no longer reports a failed insertion after successfully producing a result.
- Updated the voice docs and remaining Windows dialog/status copy to the visible `keyboard-wtf` product name. The tracked source/docs scan found no Gemini or Google AI adapter, credential, or user-facing references.
- The settings UI continues to expose independently configurable global shortcuts for app invocation, Local Dictation, Smart Dictation, Jarvis, Emergency Stop, and Settings, with duplicate detection, restore-defaults, and per-user persistence.

Voice capability was re-audited against the running code and current official NVIDIA documentation. The implemented default Jarvis path is turn-taking, not full-duplex: Windows `System.Speech` performs local recognition, a pause ends each utterance, the transcript is sent through the existing Nebius Token Factory Nemotron text adapter, and Windows SAPI speaks the completed response. It does not stream microphone audio to NVIDIA or begin speech while the user is still talking. NVIDIA Nemotron VoiceChat is currently described by NVIDIA as early-access evaluation; NVIDIA's official `NeMo-Speech.cpp` documents a full-duplex WebSocket API, but its model/runtime is separate from this app and the documented local setup requires an NVIDIA GPU with at least 16 GB accessible memory. The current Windows host exposes Intel UHD integrated graphics and has no `nvidia-smi`; a live VoiceChat endpoint or NVIDIA API credential is not configured. No API route was guessed, no GPU service was launched, and full-duplex operation is not claimed. A question about an operator-provided VoiceChat endpoint is pending.

Validation actually executed during this continuation:
- `dotnet build src/Nvidea.Windows/Nvidea.Windows.csproj -c Release -p:BaseOutputPath=C:\Users\Admin\AppData\Local\Temp\keyboard-wtf-build\` — PASS (WPF/XAML compile, 0 warnings, 0 errors). A prior app process held the normal output DLL open, so this qualification build used an isolated temporary output path.
- `dotnet test tests/Nvidea.WorkerAuth.Tests/Nvidea.WorkerAuth.Tests.csproj -c Release` — PASS (8/8, provider-free).
- `git diff --check` — PASS.
- No microphone, speaker, NVIDIA GPU, NVIDIA speech endpoint, or live Nebius inference was exercised. The broader legacy Core and DemoPackageValidator test projects were not rerun in this continuation; their last recorded attempts fail at test-source compilation for unrelated API drift/analyzer errors and malformed raw-string syntax respectively.

Next best task: if an operator endpoint is provided, implement and test an optional standards-documented Nemotron VoiceChat client with the existing approval-bounded Windows tools; otherwise keep the honest Nebius text-turn path as the judge-ready default and qualify it with an actual microphone/speaker session. Do not call the current Jarvis path full-duplex.

## Settings continuity follow-up (2026-10-30)

Repository inspected before this follow-up: `UnknownGod2011/NVIDEA`, root `C:\Users\Admin\OneDrive\Desktop\NVIDEA`, branch `main`, local HEAD `dff14e274b3851629d7781718f0c4f856eaeeb52`, origin `https://github.com/UnknownGod2011/NVIDEA.git`. At inspection time the remote `main` still pointed to `e644493d5503375e370482324d5fd1e6cbd1c2be`. The separate `keyboard.wtf` repository was read-only and not modified.

Settings continuity work:
- Kept the existing NVIDEA/keyboard-wtf six-shortcut UI and confirmed its default mappings align with the reference actions: Smart Writing → Smart Dictation (`Ctrl+Alt+K`), Dictation (`Ctrl+Alt+D`), Jarvis (`Ctrl+Alt+Q`), Cancel (`Ctrl+Alt+X`), and Open Settings (`Ctrl+Alt+,`). App invocation remains `Ctrl+Shift+Space`.
- Added first-run migration from `%LOCALAPPDATA%\keyboard.wtf\settings.json` into the separate `%LOCALAPPDATA%\keyboard-wtf\keyboard-settings.json`, but only when the latter does not exist. The importer copies compatible shortcut fields only, validates conflicts and duplicate JSON property names, and leaves the source file unchanged. Existing keyboard-wtf settings take precedence. Legacy provider credentials and unrelated preferences are not copied.
- Added `tests/Nvidea.Windows.Tests` with migration, precedence, source-preservation, credential-exclusion, conflicting-shortcut, and ambiguous-duplicate regressions.
- Updated README and voice setup documentation to describe the import and retained hotkey behavior. This follow-up does not claim that every setting from the separate legacy app has been ported; unsupported legacy Gemini/provider and speech-engine controls remain outside this settings migration.

Validation actually executed:
- `dotnet build src/Nvidea.Windows/Nvidea.Windows.csproj -c Release -p:BaseOutputPath=C:\Users\Admin\AppData\Local\Temp\keyboard-wtf-build\` — PASS (WPF/XAML, 0 warnings, 0 errors).
- `dotnet test tests/Nvidea.Windows.Tests/Nvidea.Windows.Tests.csproj -c Release -p:BaseOutputPath=C:\Users\Admin\AppData\Local\Temp\keyboard-wtf-build\` — PASS (4/4 provider-free Windows tests).
- `git diff --check` — PASS (only normal Windows CRLF conversion notices).
- `README.md`, `docs`, `src`, and `tests` were searched for Gemini/Google AI references after the settings change; none were present.

The broader active goal remains incomplete: live microphone/speaker acceptance, full-duplex NVIDIA voice endpoint qualification, and confirmation of a real Nebius inference are still outstanding. The single best next task is to finish judge-facing Settings integration for the supported Nemotron/Nebius configuration while preserving this hotkey migration, then run an actual desktop voice session when the operator endpoint/access is available.

## Current continuation — shortcut-first settings and streaming Jarvis (2026-10-30)

Repository and branch verified before this continuation: root `C:\Users\Admin\OneDrive\Desktop\NVIDEA`, exact origin `https://github.com/UnknownGod2011/NVIDEA.git`, branch `main`, local and remote `main` both at `1b88a38e0653e22d342213bb19da1c771e1edd15`. `main` is the only local branch. The separate `keyboard.wtf` repository was read for reference only; it was not modified.

Settings correction: the first draft expanded the existing shortcut dialog into a large settings window. That changed the familiar hotkey screen even though it did not remove the shortcuts. Restored the compact 540×490, non-resizable `Keyboard shortcuts` presentation and its original heading; the Shortcuts tab opens by default and retains all six editable hotkeys, capture behavior, duplicate validation, immediate application, and restore defaults. Nebius/Tavily/model setup and voice preferences remain in separate scrollable tabs. Keys remain CurrentUser DPAPI protected, are not re-displayed, and environment variables take precedence. Existing shortcut settings are saved separately from provider settings, so provider-save errors do not discard shortcut changes. No keyboard.wtf settings or files were changed.

Implemented Nebius Token Factory SSE response streaming for Jarvis, including bounded line/event/response/tool-call parsing, fragmented typed-tool-call reassembly, cancellation, truncated-stream rejection, and retry only before output starts. Jarvis now displays Nemotron text deltas and can queue completed sentence-sized chunks to local Windows SAPI while generation continues. The configured local voice, speech rate, recognizer language, listening time, end-of-turn pause, assistant name/tone, spoken-reply toggle, and personal-memory toggle are saved as user preferences. Using the Jarvis shortcut during a response cancels that response and returns to the local listening loop. This is streaming turn-taking, not full-duplex voice conversation; local microphone audio is not sent to Nebius.

Updated README and judge setup instructions with the in-app Nebius key/model setup and truthful voice behavior. Added provider-settings persistence tests, streaming adapter/tool-call tests, and Jarvis streaming/persona tests. Repaired the demo-validator fixture syntax and aligned its worker-auth wiring assertions with the current canary dialog. No Gemini or Google AI references were found in README, docs, source, or the focused test trees scanned.

Validation actually executed on this Windows .NET 8 host:
- `dotnet build src/Nvidea.Windows/Nvidea.Windows.csproj -c Release -p:BaseOutputPath=...` — PASS, including WPF/XAML compilation, 0 warnings and 0 errors.
- `dotnet test tests/Nvidea.Windows.Tests/Nvidea.Windows.Tests.csproj -c Release -p:BaseOutputPath=...` — PASS (10/10).
- `dotnet test tests/Nvidea.WorkerAuth.Tests/Nvidea.WorkerAuth.Tests.csproj -c Release -p:BaseOutputPath=...` — PASS (13/13, provider-free).
- `dotnet test tests/Nvidea.DemoPackageValidator.Tests/Nvidea.DemoPackageValidator.Tests.csproj -c Release -p:BaseOutputPath=...` — PASS (26/26).
- `git diff --check` — PASS; only normal CRLF conversion notices were printed.
- `dotnet build Nvidea.sln -c Release` cannot run because this repository contains no `.sln` or `.slnx`; the Windows application project built successfully instead.
- The broad legacy `Nvidea.Core.Tests` suite was not rerun in this continuation; its previously recorded attempt fails compilation due unrelated pre-existing test/source API drift and analyzer errors.

No Nebius credential was read or used, and no live API call, paid Serverless job, microphone session, or speaker-output session was run. Provider-free tests establish code contracts only; live Nemotron and real-device voice behavior remain unverified. The strongest next validation is to configure a Nebius Token Factory key through Settings and run a real local microphone/SAPI Jarvis turn, then capture the working modes for the public demo.

## Current continuation — Jarvis session target stability and speech-runtime audit (2026-10-30)

Repository checked before this continuation: `UnknownGod2011/NVIDEA`, root `C:\Users\Admin\OneDrive\Desktop\NVIDEA`, branch `main`, local and remote HEAD `9cd5c0b7faf02055bcb69b56af491a56726b3090`, origin `https://github.com/UnknownGod2011/NVIDEA.git`; working tree was clean. `keyboard.wtf` remains read-only.

Fixed a Jarvis session target regression in `src/Nvidea.Windows/MainWindow.Voice.cs`: the continuous session used to re-capture the foreground application after each answer, when keyboard-wtf had taken focus. Later `type_text` tool calls could therefore lose the original invocation target. Jarvis now retains the invocation-time desktop context and insertion target until that voice session ends, matching the executor's existing approval and target-binding contract.

Audited current speech hardware without opening the microphone: this host reports installed SAPI recognizers `en-GB` and `en-US`, and Realtek microphone-array and speaker endpoints with status `OK`. Only Intel UHD graphics is present and `nvidia-smi` is unavailable. This establishes speech recognizer/device availability only; it does not prove live capture, accurate transcription, or audible playback.

Rechecked current NVIDIA speech options against primary sources. The `nvidia/nemotron-voicechat` hosted page exposes a free endpoint but requires applying for early access; it is a separate NVIDIA API credential/access path from Nebius Token Factory. NVIDIA's official NeMo-Speech.cpp provides a Windows runtime and documented local speech APIs; its recommended Q4 full-duplex VoiceChat model setup requires at least 16 GB GPU-accessible memory. The current host cannot qualify that local GPU route. No undocumented endpoint was guessed, no API key was accessed, and no microphone audio was sent to a hosted trial. The shipped Jarvis path remains accurately described as Nemotron text streaming with local SAPI output between user turns, not full-duplex.

Validation actually executed after the target fix:
- `dotnet test tests/Nvidea.Windows.Tests/Nvidea.Windows.Tests.csproj -c Release -p:BaseOutputPath=...` — PASS (10/10); this compiles the Windows WPF/XAML project.
- Hardware enumeration — PASS for two installed recognizers and the default Realtek input/output endpoints; this is inventory only, not a capture/playback test.
- `git diff --check` — PASS (normal CRLF notices only).

No cloud inference or physical microphone/speaker session was exercised. The broad legacy Core test project remains previously known to fail compilation from unrelated test/source API drift; the repository has no solution file. Next best task: run the actual app with an operator-configured Nebius key and explicit microphone consent, verify a local dictation and one Smart Dictation/Jarvis turn, then capture honest demo evidence. Full-duplex VoiceChat requires NVIDIA early-access approval or a separately provisioned compatible local runtime.
