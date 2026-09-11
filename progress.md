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
- Layered personal memory with privacy-aware writes, durable provenance, hybrid lexical/semantic/recency/importance retrieval, local Ollama embeddings, vector-space provenance isolation, safe local-only migration/re-index maintenance, and deterministic retrieval-quality fixtures.
- Tavily Search + Extract research with canonical deduplication, source quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, and restart-safe staged checkpoints.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify execution, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, crash recovery, and explicit approval for state-changing actions on prompt-injection-flagged pages.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Product research flows through `ResearchProductRuntime`; provider-aware remote execution through `ResearchCloudExecutionCoordinator`; WPF durable research uses lifecycle-aware product/UI projections.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- `Nvidea.NebiusContractProbe` supports planner, zero-cost live preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.
- Windows voice invocation is local and review-first: `Ctrl+Shift+V` / Voice asks for microphone consent, transcribes through installed Windows speech recognition, and places text into the prompt without auto-running it or routing audio to cloud speech.
- Windows Memory maintenance previews and safely re-indexes stale/missing local embeddings with explicit Sensitive/Restricted opt-ins, stale-preview revalidation, progress/cancellation, and aggregate-only UI disclosure.
- `tools/Nvidea.PersonalAiDemoEval` provides deterministic, credential-free positive cross-cutting evidence over real Core contracts.
- `tools/Nvidea.PersonalAiAdversarialEval` provides deterministic negative-path evidence for prompt injection, approval denial, verification failure, citation hallucination, exact approval scope, and ambiguous crash recovery.
- `tools/Nvidea.JudgingEvidenceVerifier` combines positive/adversarial evaluator artifacts with matching Nebius live deployment evidence into one bounded, hashed, redacted judge-facing PASS/FAIL summary.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` make the final <=3-minute judging plan machine-checkable: timing, required beats, repository-contained assets, command targets, explicit evidence classes, provider-live claim boundaries, and secret scanning.
- `tests/Nvidea.DemoPackageValidator.Tests` now adversarially exercises the demo-package validator itself, including a regression for a real command-validation precedence bug found during review.

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
Added embedding provenance/model-space isolation, loopback-only Ollama `/api/embed`, bounded requests/batches, redirect refusal, explicit desktop opt-in, deterministic fallback, safe local embedding migration with high-sensitivity exclusion by default, WPF Memory maintenance, stale-consent protection, progress/cancellation, and deterministic semantic retrieval-quality fixtures.

### 2026-09-11 — Deterministic Personal AI evaluators + browser hardening
Added positive and adversarial cross-cutting evaluators. Adversarial review exposed that prompt-injection-like pages could still permit otherwise-medium state-changing actions; hardened `BrowserSafetyPolicy` so flagged-page Navigate/Click/Type/Select/Download operations become High-risk and approval-gated while stricter independent rules remain intact.

### 2026-09-11 — Unified judging evidence + deterministic demo package
Added `Nvidea.JudgingEvidenceVerifier` with strict bounded JSON, exact stable check sets, duplicate-property rejection, exact artifact hashes, and canonical Nebius deployment/PASS verification. Added `Nvidea.DemoPackageValidator` and canonical `docs/demo-package.json`, currently budgeting 168 seconds for the seven judging beats and deliberately refusing to claim provider-live Nebius success without matching live evidence.

### 2026-09-11 — Demo validator fail-closed hardening and regression suite
Completed:
- Re-read this ledger completely and reviewed the existing validator before mutation.
- Found a real fail-open logic defect in `demo-commands`: because `&&` binds more tightly than `||`, the second project-path substring check could cause a command to pass even when its label was blank or another preceding condition was false.
- Fixed the command condition by grouping the two acceptable path representations under the required label/command/safe-path predicates.
- Added `EnsureManifestShape` so malformed JSON that deserializes required arrays/objects to `null` fails with a bounded `InvalidDataException` instead of reaching a null dereference; also bounded title length and per-manifest/per-beat collection sizes.
- Hardened repository-relative paths beyond host-platform `Path.IsPathRooted`: reject control characters, slash-rooted/UNC-like paths, `.`/`..` segments, and colon-bearing segments such as Windows drive-relative `C:foo`, while retaining canonical repository-root containment in `PathExists`.
- Added `tests/Nvidea.DemoPackageValidator.Tests/Nvidea.DemoPackageValidator.Tests.csproj`, reusing the repository's .NET 8/xUnit package versions and referencing the actual validator project.
- Added full CLI-level deterministic tests that invoke the actual validator `Main` through reflection rather than duplicating validator logic.
- Regression cases cover: valid package PASS, null required arrays, `../` traversal, Windows drive-relative paths, >180-second duration, duplicate beat IDs, missing required assets, the exact blank-label/project-substring precedence bypass, secret markers, and synthetic evidence attempting to support a provider-live claim.
- Corrected the temporary test fixture after static review so `LICENSE` and every other required asset are represented as files, while feature paths that are directories remain directories.
- Updated `docs/demo-package.md` with the hardened validation contract and exact offline regression-suite command.

Engineering commits before this ledger update:
- `52c30d65ee3b9921865c7292f4adbe3e0092e2f3` — harden demo package validator fail-closed boundaries.
- `7d6755e35468aacfdb2a7afd5bf9119e94e63765` — add demo package validator regression test project.
- `20790c5b12bebc00cefd0ad55312a0a67689f542` — cover adversarial manifest cases.
- `78b2debdeb4161723b164337f5c4c63fa985e371` — correct validator test fixture asset modeling.
- `8339bea10f05d766d9e79f899fb0ba8b18a7ec69` — document validator regression suite and hardened path/shape behavior.

Validation / evidence:
- Repository identity was explicitly re-verified before every GitHub mutation. Every mutation targeted exactly `UnknownGod2011/NVIDEA`; no mutation was made to `keyboard.wtf` or any other repository.
- GitHub compare from prior ledger head `2890731230f74fcd41aac2d1e1aa8f6e4d0730e1` to engineering head `8339bea10f05d766d9e79f899fb0ba8b18a7ec69` reports **5 commits ahead / 0 behind**.
- Static review confirms the regression suite references the actual validator project and invokes its CLI entry point, so a future executable run tests the production parsing/validation path rather than copied logic.
- No usable .NET 8 execution signal is available in this environment. Therefore the validator changes and new regression tests are **not claimed as compiled/executed/passing**.
- No GitHub Actions workflow was triggered merely to manufacture a green result.
- No live Nebius credentials/resources, Object Storage operation, Serverless job, Nemotron/Tavily call, Playwright browser, Ollama runtime, or paid service was used.

Security / privacy / failure review:
- This run strengthened only offline judge-package validation; it adds no provider authority, credentials, telemetry, or network dependency.
- The command-validation precedence fix closes an actual fail-open route in evidence-package validation.
- Malformed/null structural input now fails through a sanitized expected error path instead of depending on null-reference behavior.
- Cross-platform path validation no longer relies solely on the current host OS to recognize Windows-dangerous path forms.
- Tests use generated temporary fixture content only; no personal memory, browser session, API key, provider response, or private repository material is embedded in the suite.
- The manifest secret scan remains defense-in-depth and is not represented as a general repository secret scanner.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo package validator, and the new validator regression suite are not compiled or executed here.
- A real Windows/.NET 8 build/run remains mandatory before relying on any generated PASS evidence.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- The prompt-injection detector is heuristic; false negatives remain possible, so planner/system-prompt defenses and downstream capability gates remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- A real Windows machine still needs microphone permission plus an installed speech recognizer/language for voice validation.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility hashes/fingerprints prove internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all Nebius contract/evidence tools, both Personal AI evaluators, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.DemoPackageValidator.Tests`; compile WPF/XAML; run focused memory/retrieval/migration, voice, readiness, lifecycle/research, browser authority/integration, API-surface, security, evaluator, and demo-validator suites; then fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, next reconcile stale README status/setup text against the implemented Windows voice, local embeddings/migration, Personal AI evaluators, judging evidence verifier, and deterministic demo package, while keeping all live-provider claims explicitly unverified.
