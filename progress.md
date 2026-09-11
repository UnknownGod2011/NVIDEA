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
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, endpoint validation, and current default routing: Nano for Fast, Super for Standard, Ultra for Deep; all tiers remain environment-overridable and explicitly disabled optional tiers fail safely to Standard.
- Layered personal memory with privacy-aware writes, durable provenance, hybrid lexical/semantic/recency/importance retrieval, loopback-only local Ollama embeddings, vector-space provenance isolation, safe local-only migration/re-index maintenance, WPF maintenance UI, and deterministic retrieval-quality fixtures.
- Tavily Search + Extract research with canonical deduplication, source quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, and restart-safe staged checkpoints.
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, crash recovery, and explicit approval for state-changing actions on prompt-injection-flagged pages.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- `Nvidea.NebiusContractProbe` supports planning, zero-cost preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.
- Windows voice invocation is local and review-first: `Ctrl+Shift+V` / Voice asks for microphone consent, transcribes through installed Windows speech recognition, and places text into the prompt without auto-running it or routing audio to cloud speech.
- Windows Memory maintenance safely re-indexes stale/missing local embeddings with privacy-safe previews, explicit Sensitive/Restricted opt-ins, stale-preview revalidation, progress/cancellation, and aggregate-only disclosure.
- `tools/Nvidea.PersonalAiDemoEval` and `tools/Nvidea.PersonalAiAdversarialEval` provide deterministic positive and negative-path cross-cutting evidence over real Core contracts.
- `tools/Nvidea.JudgingEvidenceVerifier` now combines positive/adversarial artifacts, matching Nebius live deployment evidence, and a **fresh live** Token Factory model-catalog PASS into one bounded judge-facing summary.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` make the final <=3-minute judging plan machine-checkable; its adversarial regression suite covers timing, path, command, secret, and provider-live claim failures.
- `tools/Nvidea.NebiusModelCatalogCheck` provides a zero-inference model-catalog readiness gate for the configured Nano / Super / Ultra tier IDs using either a captured or live authenticated `GET /v1/models` response.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, mounted transport, and worker hardening.

### 2026-09-10 — Native Object Storage + reproducible evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned worker requirements, live runtime modes, reproducible redacted deployment fingerprints, MysteryBox validation, machine-readable evidence, atomic artifact persistence, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product lifecycle / authority hardening
Added `ResearchCloudExecutionCoordinator`, `ResearchProductRuntime`, lifecycle-aware WPF research, `BrowserProductRuntime`, restart-safe browser-goal recovery, assembly-internal privileged construction, lifecycle/dispatch readiness, one-shot cloud approval, Tavily-independent remote recovery, and local review-first Windows voice invocation.

### 2026-09-11 — Production local semantic memory
Added embedding provenance/model-space isolation, loopback-only Ollama `/api/embed`, bounded requests/batches, redirect refusal, explicit desktop opt-in, deterministic fallback, safe local embedding migration, WPF Memory maintenance, stale-consent protection, progress/cancellation, and deterministic semantic retrieval-quality fixtures.

### 2026-09-11 — Deterministic Personal AI evaluators + browser hardening
Added positive and adversarial cross-cutting evaluators. Adversarial review exposed that prompt-injection-like pages could still permit otherwise-medium state-changing actions; hardened `BrowserSafetyPolicy` so flagged-page Navigate/Click/Type/Select/Download operations become High-risk and approval-gated while stricter independent rules remain intact.

### 2026-09-11 — Unified judging evidence + deterministic demo package
Added `Nvidea.JudgingEvidenceVerifier`, strict bounded JSON and artifact hashing, canonical Nebius deployment/PASS verification, `Nvidea.DemoPackageValidator`, canonical 168-second `docs/demo-package.json`, provider-live claim boundaries, and adversarial CLI-level validator tests. Found and fixed a real command-validation boolean-precedence fail-open and hardened cross-platform path handling.

### 2026-09-11 — Current verified Nemotron tier routing + README reconciliation
Verified current official Nebius Token Factory model identifiers and changed fresh-install routing to Nano / Super / Ultra for Fast / Standard / Deep while preserving explicit environment overrides and safe Standard fallback for programmatically disabled optional tiers. Added routing/request-payload tests and reconciled README status/validation boundaries.

### 2026-09-11 — Nebius model-catalog drift readiness gate
Added `Nvidea.NebiusModelCatalogCheck` with captured and live zero-inference modes, exact case-sensitive configured-tier matching, bounded/strict catalog parsing, SHA-256 evidence binding, redirect refusal, HTTPS/provider-host validation, bounded streaming, sanitized failures, atomic evidence output, tests, and documentation. A catalog PASS proves model IDs were listed, not quota or inference success.

### 2026-09-11 — Fresh live catalog evidence integrated into judge chain
Completed:
- Re-read this ledger completely and inspected the current NVIDEA repository, recent commits, judging verifier, model-catalog checker, demo package, and existing test conventions before changing anything.
- Re-verified repository identity before every mutation; all writes targeted exactly `UnknownGod2011/NVIDEA`.
- Upgraded `tools/Nvidea.JudgingEvidenceVerifier` from schema 1 to schema 2 output and added a fifth required input: the model-catalog evidence artifact.
- The unified verifier now accepts **only `mode: live`** catalog evidence for current provider-readiness claims. Captured catalog snapshots are explicitly rejected even when they previously passed the standalone catalog checker.
- Added a hard catalog freshness window of **15 minutes** and a maximum tolerated future clock skew of **2 minutes**. A once-valid but stale artifact therefore cannot be presented to judges as current Token Factory readiness.
- Bound catalog evidence to the exact currently configured Fast / Standard / Deep model IDs using the same `NVIDEA_MODEL_FAST`, `NVIDEA_MODEL_STANDARD`, and `NVIDEA_MODEL_DEEP` override semantics as production routing.
- Added strict validation for catalog schema, PASS state, zero failure codes, bounded model count, lowercase SHA-256, exactly three unique required tiers, all tiers present, and trusted Nebius endpoint host.
- Host trust in the unified evidence gate uses an exact DNS suffix boundary (`nebius.com` or `*.nebius.com`), so lookalikes such as `evilnebius.com` are rejected.
- The judge summary now exposes the catalog evidence class (`provider-live-readiness`), observation timestamp, 900-second freshness limit, endpoint hostname, catalog hash/model count, exact required model bindings, and artifact hash while excluding full catalog contents, credentials, provider error bodies, and paths.
- Kept explicit non-claims: a fresh live catalog proves recent provider listing only; it does not prove quota, feature support, tool calling, context length, chat completion success, or Serverless execution.
- Made the catalog evidence validator callable directly for deterministic regression tests without requiring live credentials or a complete Nebius deployment artifact.
- Added `tests/Nvidea.JudgingEvidenceVerifier.Tests` with focused cases for:
  - fresh live catalog PASS;
  - captured snapshot rejection;
  - stale (>15 minute) evidence rejection;
  - excessive future clock skew rejection;
  - `evilnebius.com` suffix-lookalike rejection;
  - mismatched configured-model binding rejection; and
  - contradictory PASS evidence containing failure codes.
- Updated `docs/judging-evidence-verifier.md` with the fresh-live evidence contract, recommended execution order, strict captured-vs-live semantics, freshness policy, redaction boundary, non-claims, and regression coverage.
- Updated canonical `docs/demo-package.json` so the pre-demo command sequence creates `artifacts/nebius-model-catalog-live.json` via `--live` immediately before the unified verifier, and passes that artifact into the judge summary command.
- Static review caught and corrected a nullable-flow risk in the first verifier integration before finalizing the run.

Engineering commits before this ledger update:
- `8514f60ebb0453baf3167b8ba29de704773cc5be` — require fresh live Nebius catalog evidence.
- `9746fb1ac9c08bdcbf4154ed7adf623b9cf596ce` — make catalog evidence gate regression-testable and correct nullable-flow handling.
- `79d7a4199e5318fb8be6b251116bd6135167494f` — add judging catalog evidence regression test project.
- `061fbb6352d47f30037d66a1da3987ab1b97dee6` — cover live catalog freshness and binding rules.
- `4640ffc12096c2da89f4b0dc2a7b4698ba1280ac` — document fresh live catalog evidence boundary.
- `eade678ea350dbee0dc2408157e6dad91eb0d787` — add live catalog gate to judge demo package.

Validation / evidence:
- GitHub compare from prior ledger head `38f96a4e7d70baa78587cd2b50485692be1b150d` to engineering head `eade678ea350dbee0dc2408157e6dad91eb0d787` reports **6 commits ahead / 0 behind** across five focused files.
- Re-fetched the updated verifier and regression suite after mutation for static review.
- `command -v dotnet` / `dotnet --info` still reports `dotnet: command not found` in this execution environment. Therefore the updated verifier and new xUnit project are **not claimed as compiled or passing**.
- No GitHub Actions workflow was triggered merely to manufacture a green result.
- No live Nebius API key, paid inference, chat completion, Tavily call, Object Storage operation, Serverless job, Playwright browser, Ollama runtime, or other paid provider resource was used in this run.

Security / privacy / failure review:
- Captured catalog evidence remains useful for offline development but cannot be elevated into a current-readiness judge PASS.
- Freshness is checked at verification time; successful output does not add a new wall-clock timestamp, so reviewed evidence remains bounded and mostly deterministic while naturally expiring.
- Exact current model bindings prevent a PASS generated for different configured tiers from being silently reused after routing changes.
- The catalog artifact remains separately SHA-bound; the judge summary does not expose its file path or complete provider model list.
- The unified host boundary rejects DNS suffix lookalikes before accepting catalog evidence as provider-live readiness.
- Synthetic evaluator evidence, live deployment evidence, and live provider-readiness evidence remain separate classes with explicit non-claims.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and new judging-verifier tests are not compiled or executed here.
- A real Windows/.NET 8 restore/build/run remains mandatory before relying on generated PASS evidence.
- The standalone catalog checker’s live endpoint validation should receive the same exact DNS-boundary regression scrutiny as the unified verifier; the judge gate itself now rejects suffix lookalikes.
- Provider catalogs can change after a catalog check; the 15-minute gate reduces but cannot eliminate this race. Run it immediately before demo/judging.
- A model appearing in `/v1/models` does not prove every required capability or quota; a real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility hashes/fingerprints prove internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all Nebius contract/evidence tools, both Personal AI evaluators, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, `Nvidea.NebiusModelCatalogCheck`, and all focused tests; compile WPF/XAML; then fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, next harden the standalone catalog checker’s live endpoint host validation to the same exact DNS-suffix boundary as the judge verifier and add a credential-safe regression that proves a malicious lookalike host is rejected before any HTTP request can carry the bearer token.
