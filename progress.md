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
- NVIDIA Nemotron through Nebius Token Factory with retries, timeout/cancellation, structured tool calling, response-schema support, and default routing: Nano for Fast, Super for Standard, Ultra for Deep. Environment overrides remain supported.
- Layered personal memory with privacy-aware writes, provenance, hybrid lexical/semantic/recency/importance retrieval, local Ollama embeddings, vector-space isolation, migration/re-indexing, WPF maintenance UI, and deterministic retrieval-quality fixtures.
- Tavily Search + Extract research with canonical deduplication, source quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, and restart-safe staged checkpoints.
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Native Nebius Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- Windows voice invocation is local and review-first. Windows Memory maintenance safely re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
- `tools/Nvidea.PersonalAiDemoEval` and `tools/Nvidea.PersonalAiAdversarialEval` provide deterministic positive/negative cross-cutting evidence.
- `tools/Nvidea.JudgingEvidenceVerifier` combines positive/adversarial artifacts, matching Nebius live deployment evidence, and a fresh live Token Factory catalog PASS.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` make the <=3-minute judging plan machine-checkable.
- `tools/Nvidea.NebiusModelCatalogCheck` provides captured/live zero-inference readiness checks with strict provider endpoint trust.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, digest-pinned worker requirements, redacted deployment fingerprints, MysteryBox validation, machine-readable evidence, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product, memory, evaluator, and judging hardening
Added research/browser product runtimes, WPF lifecycle-aware research, restart-safe browser-goal recovery, one-shot cloud approval, local review-first voice, production local semantic memory, migration UI, positive/adversarial Personal AI evaluators, browser prompt-injection mutation approval hardening, unified judging evidence, deterministic demo package validation, and adversarial validator tests.

### 2026-09-11 — Current Nemotron routing + catalog readiness
Verified current Token Factory model IDs and set Nano / Super / Ultra defaults for Fast / Standard / Deep. Added routing tests, README reconciliation, zero-inference model-catalog readiness, strict bounded catalog parsing, SHA-256 evidence binding, fresh-live catalog requirements in the judging chain, and standalone catalog DNS/user-info/redirect credential-boundary hardening.

### 2026-09-12 — Production Token Factory credential-boundary hardening
Completed:
- Re-read this ledger and inspected current repo state before changes.
- Audited the production Token Factory, Tavily, Ollama, Serverless, and composition-root HTTP trust surfaces.
- Found a real production credential-boundary defect in `NebiusOptions.Validate()`: `Host.EndsWith("nebius.com")` accepted suffix lookalikes such as `evilnebius.com`, even though the standalone catalog checker had already been hardened.
- Replaced the production Token Factory host test with the exact DNS boundary: only `nebius.com` or a real `*.nebius.com` subdomain is accepted.
- Added absolute-HTTPS, no-URI-user-info, and standard TLS port 443 requirements for the Token Factory base URI.
- Reordered `NebiusOptions.FromEnvironment()` so the endpoint is parsed and trusted **before** `NEBIUS_API_KEY` is read. A malicious `NVIDEA_NEBIUS_BASE_URL` therefore fails before credential loading or Authorization-header construction.
- Added focused regression coverage for exact-domain/real-subdomain acceptance and rejection of HTTP, suffix lookalikes, domain-confusion (`nebius.com.evil.example`), embedded user-info, and non-standard TLS ports.
- Added a direct trust-boundary regression proving `evilnebius.com` is rejected by the production validator before request construction.

Engineering commits before this ledger update:
- `18943f9f5b325cbe3549537bc35510dcb0184497` — harden Token Factory credential endpoint trust.
- `64285c0a2451826eca9dc3bc09cae821ba8826a0` — cover Token Factory endpoint trust boundary.

Validation / evidence:
- GitHub compare from prior ledger head `3ab4cde5c32c88844f01e2bb64f4047320e82caf` to engineering head `64285c0a2451826eca9dc3bc09cae821ba8826a0` reports **2 commits ahead / 0 behind** and only the production Token Factory client plus its focused tests changed.
- Static inspection confirms endpoint validation now occurs before environment API-key lookup and before client construction can attach a bearer token.
- No GitHub Actions workflow was triggered merely to manufacture a green result.
- The automation environment still has no usable .NET 8 execution signal, so the changed Core code and xUnit tests are **not claimed as compiled or passing**.
- No live Nebius/Tavily/Ollama/Playwright/Object Storage/Serverless operation or paid inference was used.

Security / privacy / failure review:
- `evilnebius.com`, `not-nebius.com`, `nebius.com.evil.example`, HTTP endpoints, user-info URLs, and non-443 Token Factory endpoints now fail closed before credential use.
- The production inference path now matches the standalone model-catalog checker’s Nebius DNS-boundary semantics.
- Tavily already pins the exact `api.tavily.com` host, and Ollama’s production constructor already uses strict loopback validation plus a no-redirect owned `HttpClient`. The remaining notable HTTP trust gap is composition-root/provider `HttpClient` redirect policy: Token Factory, Tavily, and Serverless are currently constructed with default `HttpClient()` instances, so redirect behavior should be made explicitly no-redirect at the trusted composition boundary rather than relying on runtime defaults.
- Nebius Serverless already pins `api.nebius.cloud` (with localhost only for contract tests), but its URI-user-info/port rules should be made explicit for consistency.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this automation environment; Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before relying on generated PASS evidence.
- Provider catalogs can change after a check; the 15-minute judging gate reduces but cannot eliminate this race.
- A model appearing in `/v1/models` does not prove quota, inference success, tool calling, context length, or every required capability; a real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evidence/evaluator tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, harden the **trusted composition-root HTTP clients to disable redirects explicitly** for Token Factory, Tavily, and Nebius Serverless, add redirect-regression tests, and make Serverless user-info/port validation explicit so credentials and private prompts/research payloads cannot leave their intended provider origin through redirect/configuration mistakes.
