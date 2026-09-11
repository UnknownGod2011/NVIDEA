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
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, endpoint validation, and current default routing: Nano for Fast, Super for Standard, Ultra for Deep; all tiers remain environment-overridable and optional Fast/Deep tiers fail safely to Standard when deliberately disabled.
- Layered personal memory with privacy-aware writes, provenance, hybrid lexical/semantic/recency/importance retrieval, loopback-only local Ollama embeddings, vector-space isolation, local-only migration/re-indexing, WPF maintenance UI, and deterministic retrieval-quality fixtures.
- Tavily Search + Extract research with canonical deduplication, source quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, and restart-safe staged checkpoints.
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, crash recovery, and explicit approval for state-changing actions on prompt-injection-flagged pages.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, and a non-root worker image.
- Native Windows S3-compatible Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- `Nvidea.NebiusContractProbe` supports planning, zero-cost preflight, explicit paid live research, redacted PASS evidence, and offline fail-closed verification.
- Windows voice invocation is local and review-first: `Ctrl+Shift+V` / Voice asks for microphone consent, transcribes through installed Windows speech recognition, and places text into the prompt without auto-running it or routing audio to cloud speech.
- Windows Memory maintenance safely re-indexes stale/missing local embeddings with privacy-safe previews, explicit Sensitive/Restricted opt-ins, stale-preview revalidation, progress/cancellation, and aggregate-only disclosure.
- `tools/Nvidea.PersonalAiDemoEval` and `tools/Nvidea.PersonalAiAdversarialEval` provide deterministic positive and negative-path cross-cutting evidence over real Core contracts.
- `tools/Nvidea.JudgingEvidenceVerifier` combines positive/adversarial artifacts, matching Nebius live deployment evidence, and a **fresh live** Token Factory model-catalog PASS into one bounded judge-facing summary.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` make the final <=3-minute judging plan machine-checkable; adversarial regression coverage includes timing, path, command, secret, and provider-live claim failures.
- `tools/Nvidea.NebiusModelCatalogCheck` provides captured and live zero-inference model-catalog readiness checks for the configured Nano / Super / Ultra tiers with strict provider endpoint trust.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 — Research durability + Nebius Serverless control plane
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, mounted transport, and worker hardening.

### 2026-09-10 — Native Object Storage + reproducible evidence
Added native S3-compatible protected transport, exact S3 ↔ Serverless mount mapping, digest-pinned worker requirements, live runtime modes, reproducible redacted deployment fingerprints, MysteryBox validation, machine-readable evidence, atomic artifact persistence, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product lifecycle / authority hardening
Added research and browser product runtimes, lifecycle-aware WPF research, restart-safe browser-goal recovery, assembly-internal privileged construction, lifecycle/dispatch readiness, one-shot cloud approval, Tavily-independent remote recovery, and local review-first Windows voice invocation.

### 2026-09-11 — Production local semantic memory
Added embedding provenance/model-space isolation, loopback-only Ollama `/api/embed`, bounded requests/batches, redirect refusal, explicit desktop opt-in, deterministic fallback, safe local embedding migration, WPF Memory maintenance, stale-consent protection, progress/cancellation, and deterministic semantic retrieval-quality fixtures.

### 2026-09-11 — Deterministic Personal AI evaluators + browser hardening
Added positive and adversarial cross-cutting evaluators. Adversarial review exposed that prompt-injection-like pages could still permit otherwise-medium state-changing actions; hardened `BrowserSafetyPolicy` so flagged-page Navigate/Click/Type/Select/Download operations become High-risk and approval-gated while stricter independent rules remain intact.

### 2026-09-11 — Unified judging evidence + deterministic demo package
Added `Nvidea.JudgingEvidenceVerifier`, strict bounded JSON and artifact hashing, canonical Nebius deployment/PASS verification, `Nvidea.DemoPackageValidator`, canonical 168-second `docs/demo-package.json`, provider-live claim boundaries, and adversarial CLI-level validator tests. Found and fixed a real command-validation boolean-precedence fail-open and hardened cross-platform path handling.

### 2026-09-11 — Current verified Nemotron tier routing + README reconciliation
Verified current official Nebius Token Factory model identifiers and changed fresh-install routing to Nano / Super / Ultra for Fast / Standard / Deep while preserving explicit environment overrides and safe Standard fallback for deliberately disabled optional tiers. Added routing/request-payload tests and reconciled README status/validation boundaries.

### 2026-09-11 — Nebius model-catalog drift readiness + judge-chain freshness
Added `Nvidea.NebiusModelCatalogCheck` with captured/live zero-inference modes, exact configured-tier matching, bounded strict parsing, SHA-256 evidence binding, redirect refusal, HTTPS/provider-host validation, bounded streaming, sanitized failures, atomic evidence output, tests, and docs. Integrated catalog evidence into `Nvidea.JudgingEvidenceVerifier` as a fifth required artifact. Unified judging accepts only `mode: live`, requires <=15-minute freshness, <=2-minute future skew, exact current tier bindings, all tiers present, zero failure codes, trusted Nebius hostname, and keeps captured snapshots from being misrepresented as current provider readiness.

### 2026-09-11 — Standalone live catalog credential-boundary hardening
Completed:
- Re-read `progress.md` completely and inspected current `NVIDEA` head, recent commits, standalone model-catalog checker, tests, and operator docs before changing anything.
- Re-verified repository identity before each mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Found a real mismatch between the judge verifier and standalone checker: standalone `ValidateTrustedEndpoint` used `Host.EndsWith("nebius.com")`, which would accept DNS suffix lookalikes such as `evilnebius.com`.
- Replaced that fail-open suffix check with an exact DNS boundary: only `nebius.com` or a host ending in `.nebius.com` is trusted, case-insensitively.
- Kept HTTPS mandatory and URI user-info forbidden.
- Moved endpoint parsing/trust validation ahead of reading `NEBIUS_API_KEY`; an untrusted `NVIDEA_NEBIUS_BASE_URL` now fails before the API key is read or any Authorization header/request can be constructed.
- Exposed only the trust-validation internals to the focused test assembly via `InternalsVisibleTo`; production API surface remains unchanged.
- Expanded `Nvidea.NebiusModelCatalogCheck.Tests` with exact-domain/real-subdomain acceptance, `evilnebius.com`, `not-nebius.com`, `nebius.com.evil.example`, HTTP, and embedded-user-info rejection.
- Added a direct regression asserting the suffix-lookalike is rejected by `ValidateTrustedEndpoint` before the credentialed request path.
- Updated `docs/nebius-model-catalog-check.md` to document the exact DNS boundary, validation-before-credential order, explicit lookalike examples, and regression scope.

Engineering commits before this ledger update:
- `1c7e9a4428097621c2c8f448cec7fb384715627a` — harden Nebius catalog endpoint trust boundary.
- `71627c750a57cdc427025e1f9265083af49322fb` — expose catalog trust internals to focused tests.
- `8fc05868b257a709ae50397b0df9150288e5df8d` — cover Nebius catalog endpoint trust boundary.
- `44e7f4e132554d4cc4070b681215ce23ec97091c` — document strict Nebius endpoint trust boundary.

Validation / evidence:
- GitHub compare from prior ledger head `330c47e625bd814419f57480bc0acfd00dc01c83` to engineering head `44e7f4e132554d4cc4070b681215ce23ec97091c` reports **4 commits ahead / 0 behind**.
- Re-fetched the changed checker after mutation and statically verified endpoint validation executes before API-key lookup/request construction and exact host matching is `nebius.com` or `.nebius.com` only.
- No GitHub Actions workflow was triggered merely to manufacture a green result.
- This execution environment still has no trusted usable .NET 8 runtime signal from prior runs; therefore the changed checker and xUnit suite are **not claimed as compiled or passing**.
- No live Nebius request, bearer credential, paid inference, Tavily call, Object Storage operation, Serverless job, Playwright browser, or Ollama runtime was used in this run.

Security / privacy / failure review:
- A malicious environment override such as `https://evilnebius.com/v1/` can no longer pass the standalone catalog checker’s host gate.
- Endpoint trust is established before secret lookup and before Authorization header construction, reducing credential exfiltration risk from configuration tampering.
- Redirects remain disabled; an accepted Nebius endpoint cannot redirect the bearer token to another host through this client.
- Live evidence still emits only the trusted endpoint host and never the API key, Authorization header, path/query/user-info, raw provider error body, or full catalog.
- The standalone checker and unified judge verifier now use the same DNS trust semantics for Nebius-host evidence.

## Known Blockers / Risks
- No usable .NET 8 execution signal has been available in this automation environment; Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and focused tests still require a real restore/build/run.
- A real Windows/.NET 8 restore/build/run remains mandatory before relying on generated PASS evidence.
- Provider catalogs can change after a check; the 15-minute judging gate reduces but cannot eliminate this race. Run it immediately before demo/judging.
- A model appearing in `/v1/models` does not prove quota, inference success, tool calling, context length, or every required capability; a real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Reproducibility hashes/fingerprints prove internal consistency, not third-party attestation.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all Nebius contract/evidence tools, both Personal AI evaluators, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, `Nvidea.NebiusModelCatalogCheck`, and all focused tests; compile WPF/XAML; then fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, next perform a systematic **endpoint/redirect/secret-egress trust audit across every Nebius/Tavily/Ollama HTTP client** and centralize duplicated provider-host validation where doing so improves consistency without weakening provider-specific policies.
