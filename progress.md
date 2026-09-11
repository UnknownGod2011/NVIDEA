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
- `tools/Nvidea.JudgingEvidenceVerifier` combines positive/adversarial artifacts with matching Nebius live deployment evidence into a bounded, hashed, redacted judge-facing PASS/FAIL summary.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` make the final <=3-minute judging plan machine-checkable; its adversarial regression suite covers timing, path, command, secret, and provider-live claim failures.
- `tools/Nvidea.NebiusModelCatalogCheck` now provides a zero-inference model-catalog readiness gate for the configured Nano / Super / Ultra tier IDs using either a captured or live authenticated `GET /v1/models` response.

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
Verified current official Nebius Token Factory cookbook identifiers and changed fresh-install routing to:
- Fast: `nvidia/nvidia-nemotron-3-nano-30b-a3b`
- Standard: `nvidia/nemotron-3-super-120b-a12b`
- Deep: `nvidia/Nemotron-3-Ultra-550b-a55b`
Preserved explicit environment overrides and safe Standard fallback for programmatically disabled optional tiers. Added routing/request-payload tests and reconciled README status/validation boundaries.

### 2026-09-11 — Nebius model-catalog drift readiness gate
Completed:
- Re-read this ledger completely and inspected current Nebius routing, repository state, recent commits, tool/test conventions, and README before changing anything.
- Re-verified the repository identity before every GitHub mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Checked current official Nebius Token Factory documentation. The current API reference documents `GET /v1/models` as an authenticated endpoint that lists currently available models, and the quickstart/API docs confirm the OpenAI-compatible `https://api.tokenfactory.nebius.com/v1/` surface.
- Added `tools/Nvidea.NebiusModelCatalogCheck` with two modes:
  - `--input <models.json>` for credential-free validation of a captured catalog response.
  - `--live` for an authenticated GET of the Token Factory `models` endpoint without issuing a chat completion.
- Reused `NebiusOptions` verified default constants so the drift checker cannot silently diverge from production model routing; the same `NVIDEA_MODEL_FAST`, `NVIDEA_MODEL_STANDARD`, and `NVIDEA_MODEL_DEEP` environment overrides are honored.
- Exact, case-sensitive presence is required for Fast / Standard / Deep. Missing tiers fail closed with stable codes such as `missing_deep_model`.
- Hardened catalog parsing: 2 MiB body ceiling, maximum 2,048 model entries, maximum 512-character IDs, no comments/trailing commas, maximum JSON depth 32, duplicate JSON-property rejection, duplicate model-ID rejection, non-empty IDs, and control-character rejection.
- Hardened live transport: HTTPS only, `nebius.com` host suffix only, URI user-info rejected, redirects disabled/refused, 20-second request timeout, bounded streamed response, bearer key supplied only in the Authorization header, and no raw HTTP error body emitted.
- Evidence output is deliberately redacted and machine-readable: schema version, observation time, mode, PASS/FAIL, SHA-256 of exact catalog bytes, unique model count, live endpoint host only, required tier IDs/presence, and stable failure codes. It never emits API keys, Authorization headers, raw provider error bodies, source file paths, or the complete provider catalog.
- Added atomic `--output` persistence with explicit exit codes: 0 PASS, 1 validation/provider failure, 2 CLI misuse, 3 evidence-write failure.
- Added `tests/Nvidea.NebiusModelCatalogCheck.Tests` covering all-tier PASS, missing Deep drift, exact case sensitivity, duplicate model IDs, duplicate JSON properties, malformed shape, captured-mode endpoint redaction, and live host-only disclosure.
- Static review caught a likely API-overload portability/compile risk in the initial parser boundary; standardized the checker on `byte[]` before finalizing the run.
- Added `docs/nebius-model-catalog-check.md` describing offline/live usage, evidence semantics, safety boundaries, exit codes, regression tests, and recommended pre-demo ordering.

Engineering commits before this ledger update:
- `e321582bfcc1e8c313e8a186f6b14220c63a7265` — add Nebius model catalog drift checker project.
- `c5ab91159734b586786706be913c3faa1bccabc5` — implement fail-closed Nebius model catalog drift check.
- `d33e0eb8467b8eab5dd5ee4cd258a9a7e8a75599` — add catalog drift checker test project.
- `aac610fb5f92b0543ef4a8edf81ccd9264711aee` — cover catalog drift failure cases.
- `5245f51a91c7af4e26f23b14e45ce49c93995d2a` — harden catalog checker byte parsing.
- `b4569f87d9d0c3b4fb172cf21b2d190247fa38e8` — align catalog tests with byte-array parser.
- `66da4c012e75479190fef2b9da4f0bf8986c214c` — document Nebius model catalog drift evidence.

Validation / evidence:
- GitHub compare from prior ledger head `6c188850069adf63e50c426c5fd98b2c2ef1695f` to engineering head `66da4c012e75479190fef2b9da4f0bf8986c214c` reports **7 commits ahead / 0 behind**.
- Current official Nebius Token Factory docs explicitly document `GET /v1/models`, Bearer authentication, and a response containing `data[].id` for currently available models; this validates the checker’s external contract assumption.
- `command -v dotnet` / `dotnet --info` still reports `dotnet: command not found` in this execution environment. Therefore the new tool and xUnit project are **not claimed as compiled or passing**.
- No GitHub Actions workflow was triggered merely to manufacture a green result.
- No live Nebius API key, paid inference, chat completion, Tavily call, Object Storage operation, Serverless job, Playwright browser, Ollama runtime, or other paid provider resource was used in this run.

Security / privacy / failure review:
- The live checker never sends prompts, personal memory, browser context, Tavily evidence, or user content; it only requests the provider model list.
- API keys remain environment-only and are never serialized into evidence.
- Redirect refusal plus HTTPS/Nebius-host validation prevents bearer credentials from being intentionally forwarded to arbitrary hosts by this tool.
- Provider error bodies are intentionally discarded rather than echoed into logs/evidence.
- Captured-mode evidence does not emit the source path and binds itself to exact bytes with SHA-256.
- A catalog PASS proves configured IDs were present in that catalog response; it does **not** prove quota, model feature support, chat-completion success, or live Serverless execution. Those remain separate validation layers.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; current Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, and new catalog checker/tests are not compiled or executed here.
- A real Windows/.NET 8 build/run remains mandatory before relying on generated PASS evidence.
- Provider catalogs can change after a catalog check; use fresh live evidence immediately before judging and keep environment overrides available.
- A model appearing in `/v1/models` does not prove every required capability (structured output/tool calling/context length) or quota; a real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility hashes/fingerprints prove internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all Nebius contract/evidence tools, both Personal AI evaluators, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, `Nvidea.DemoPackageValidator.Tests`, `Nvidea.NebiusModelCatalogCheck`, and its tests; compile WPF/XAML; run focused routing, catalog, memory/retrieval/migration, voice, readiness, lifecycle/research, browser authority/integration, API-surface, security, evaluator, and demo-validator suites; then fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, next integrate the catalog-check artifact into the unified judge evidence/demo package with an explicit freshness window and a strict distinction between captured vs live catalog evidence so stale snapshots cannot be presented as current provider readiness.
