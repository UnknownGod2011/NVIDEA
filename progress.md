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
- Layered personal memory with privacy-aware writes, durable provenance, hybrid lexical/semantic/recency/importance retrieval, local Ollama embeddings, vector-space provenance isolation, and local-only migration/re-index maintenance.
- Tavily Search + Extract research with canonical deduplication, quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify execution, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Browser pages flagged by the Playwright prompt-injection detector require explicit approval for state-changing Navigate/Click/Type/Select/Download actions; Read/Back/Refresh remain usable, Upload remains independently approval-gated, and credential typing remains blocked.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Product research flows through `ResearchProductRuntime`; provider-aware remote execution through `ResearchCloudExecutionCoordinator`; WPF durable research uses lifecycle-aware product/UI projections.
- Desktop remote-research lifecycle and paid dispatch are separately opt-in. Lifecycle-only recovery remains available without Tavily; new paid dispatch requires local research availability plus exact one-shot approval.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- `Nvidea.NebiusContractProbe` supports planner, zero-cost live preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.
- Desktop readiness diagnostics distinguish ready/blocked/locked research capabilities without exposing secret values or constructing new provider authority.
- Windows voice invocation is local and review-first: `Ctrl+Shift+V` / Voice asks for microphone consent, transcribes through installed Windows speech recognition, and places text into the prompt without auto-running it or routing audio to cloud speech.
- Windows Memory maintenance previews and safely re-indexes stale/missing local embeddings with explicit Sensitive/Restricted opt-ins, stale-preview revalidation, progress/cancellation, and aggregate-only UI disclosure.
- `tools/Nvidea.PersonalAiDemoEval` provides deterministic, credential-free positive cross-cutting evidence over real Core contracts.
- `tools/Nvidea.PersonalAiAdversarialEval` provides deterministic, credential-free negative-path evidence for prompt-injection authority, approval denial, verification failure, citation hallucination, exact approval scope, and ambiguous crash recovery.
- `tools/Nvidea.JudgingEvidenceVerifier` combines positive/adversarial synthetic evaluator artifacts with matching Nebius live deployment evidence into one bounded, hashed, redacted judge-facing PASS/FAIL summary.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` now make the final <=3-minute judging plan itself machine-checkable: timing, required demo beats, referenced assets, command targets, safe repository paths, explicit evidence classes, provider-live claim boundaries, and a basic secret scan are validated offline.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, mounted transport, and worker hardening.

### 2026-09-10 — Native Object Storage + reproducible evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned worker requirements, live runtime factory/modes, reproducible redacted deployment fingerprints, MysteryBox validation, machine-readable evidence, atomic artifact persistence, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product lifecycle / authority hardening
Added `ResearchCloudExecutionCoordinator`, `ResearchProductRuntime`, lifecycle-aware WPF research, `BrowserProductRuntime`, restart-safe browser-goal recovery, assembly-internal privileged construction, lifecycle/dispatch readiness, one-shot cloud approval, Tavily-independent remote recovery, and local review-first Windows voice invocation.

### 2026-09-11 — Production local semantic memory
Added embedding provenance/model-space isolation, loopback-only Ollama `/api/embed`, bounded requests/batches, strict redirect refusal, explicit desktop opt-in, deterministic fallback, safe local embedding migration with high-sensitivity exclusion by default, WPF Memory maintenance, stale-consent protection, progress/cancellation, and deterministic semantic retrieval-quality fixtures.

### 2026-09-11 — Deterministic Personal AI evaluators + browser hardening
Added positive and adversarial cross-cutting evaluators. Adversarial review exposed that prompt-injection-like pages could still permit otherwise-medium state-changing actions; hardened `BrowserSafetyPolicy` so flagged-page Navigate/Click/Type/Select/Download operations become High-risk and approval-gated while independent stricter rules remain intact.

### 2026-09-11 — Unified judging evidence verifier
Added `tools/Nvidea.JudgingEvidenceVerifier` to require exact evaluator check sets, strict bounded JSON, duplicate-property rejection, all-PASS state, positive metrics, exact artifact SHA-256 hashes, and matching redacted Nebius deployment/live PASS evidence through the existing canonical Nebius verifier. Success output separates synthetic and live evidence and excludes raw evaluator details, paths, credentials, provider errors, prompts/results, browser/session data, memory content, tokens, and key material.

### 2026-09-11 — Deterministic judge demo package validation
Completed:
- Re-read this ledger completely, inspected the current README, docs/evaluator evidence structure, repository tree, and latest commits before implementation.
- Added `tools/Nvidea.DemoPackageValidator/Nvidea.DemoPackageValidator.csproj`, targeting .NET 8 with nullable checking and warnings-as-errors and no provider/network dependency.
- Added `tools/Nvidea.DemoPackageValidator/Program.cs` implementing strict offline validation for the final judging package.
- Validator bounds the manifest to 128 KiB, rejects comments/trailing commas/duplicate JSON properties, requires schema version 1, enforces unique beat IDs and positive beat durations, and requires the seven intended demo beats: Windows invocation/context, durable memory, Tavily research, browser verification, permission gate, Nebius background work, and architecture proof.
- Enforces both the declared manifest duration and a hard <=180-second limit.
- Ensures all referenced feature/evidence/project paths are relative, cannot traverse outside the repository, and exist before the package can PASS.
- Requires core judge assets (README, license, positive/adversarial evaluator docs/projects, and unified evidence verifier project).
- Requires explicit evidence classification (`synthetic`, `local-live`, `provider-live`, `documentation`) and fails a `requiresProviderLive=true` beat unless that beat cites provider-live evidence, preventing synthetic-only artifacts from supporting a live-provider claim.
- Validates that every declared demo command references its declared project path and scans manifest strings for common private-key/bearer/API-key/secret-prefix forms.
- Emits machine-readable PASS/FAIL JSON plus SHA-256 of the exact demo manifest bytes; optional `--output` persistence uses temp-file then replace semantics.
- Added canonical `docs/demo-package.json`: a 168-second seven-beat demo plan leaving 12 seconds of contingency under the 180-second cap. It explicitly avoids claiming live Nebius Serverless success while this repository still lacks a credential-backed live PASS.
- Added `docs/demo-package.md` with validation command, evidence-class semantics, build/evidence generation order, recording/privacy checklist, and explicit instructions not to present synthetic evidence as live provider proof.
- Static repository inspection confirmed the manifest's major referenced Core directories (`Desktop`, `Memory`, `Research`, `Browser`, `Capabilities`) exist and the intended evaluator/evidence docs are present.

Engineering commits before this ledger update:
- `3720f7d8e8e2f56d1c88e0b29ea883d4057e6c22` — add deterministic demo package validator project.
- `56613d76fbe1bfd3684af38d256fd2c259da1262` — implement deterministic demo package validation.
- `0b765cbd350e9c8cf6e90ea322f9dcc0a079687b` — add deterministic hackathon demo manifest.
- `e45b239893ba38472d9bf8bb3c7124d88efb6549` — document judge-ready demo package validation.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- Static compare from prior ledger head `8f137e4f86be4d107325cddca430f88efe70060b` to engineering head `e45b239893ba38472d9bf8bb3c7124d88efb6549` is **4 commits ahead / 0 behind**.
- Planned demo duration is deterministically 168 seconds (`20+22+28+28+20+28+22`), leaving 12 seconds under the hard three-minute cap.
- No usable .NET 8 execution signal is available in this environment. Therefore the new validator, Core/WPF/Worker compilation, XAML compilation, tests, evaluator binaries, and judging tools are **not claimed as compiled/executed/passing**.
- No GitHub Actions workflow was triggered merely to manufacture a green result.
- No live Nebius credentials/resources, Object Storage operations, Serverless jobs, Nemotron/Tavily calls, Playwright browser, Ollama runtime, or paid service was used by this run.

Security / privacy / failure review:
- The new validator is offline and does not resolve provider credentials or perform network calls.
- Repository path validation rejects rooted paths and `..` traversal and then verifies canonical resolved paths remain under the supplied repository root.
- The manifest deliberately labels synthetic/documentation evidence separately and does not assert provider-live Nebius success.
- The secret scan is intentionally conservative defense-in-depth, not a replacement for repository secret scanning.
- SHA-256 binds the result to the exact manifest bytes but is not a signature or third-party attestation.
- The current validator implementation is statically reviewed only; malformed-shape/null-field behavior and command-validation edge cases still need executable tests once .NET is available.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML, tests, evaluator tools, judging evidence verifier, and demo package validator are not compiled or executed here.
- The new demo validator has not yet been run against `docs/demo-package.json`; a real .NET 8 build/run must be treated as mandatory before relying on its PASS output.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows .NET 8 build/run pass.
- The prompt-injection detector is heuristic; false negatives remain possible, so planner/system-prompt defenses and downstream capability gates remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- A real Windows machine still needs microphone permission plus an installed speech recognizer/language for voice validation.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility hashes/fingerprints prove internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, Nebius contract tools, `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, and `Nvidea.DemoPackageValidator`; compile WPF/XAML; run focused memory/retrieval/migration, voice, readiness, lifecycle/research, browser authority/integration, API-surface, and security suites; run both evaluators and the demo-package validator; then fix every compile/runtime defect before treating generated evidence as judge-ready. If executable validation remains unavailable, the next implementation target is to harden `Nvidea.DemoPackageValidator` with deterministic unit tests for malformed/null manifests, path traversal, duration overflow, duplicate beats, missing assets, command mismatches, secret markers, and provider-live evidence overclaiming, and then reconcile stale README status text against the now-implemented voice/local-embedding/evidence features without overstating live provider validation.
