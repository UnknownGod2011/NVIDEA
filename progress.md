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
- Tavily Search + Extract research with canonical deduplication, source quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, restart-safe staged checkpoints, and exact production endpoint trust (`api.tavily.com`, HTTPS/443, no URI user-info).
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Native Nebius Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- Credential-bearing Token Factory, Tavily, Serverless, worker, model-catalog, and contract-probe HTTP paths use explicit no-auto-redirect behavior.
- Token Factory endpoints require HTTPS/443, no URI user-info, and exact `nebius.com` or genuine `*.nebius.com` DNS boundaries; endpoint trust is validated before API-key lookup in the environment-based production path.
- Nebius Serverless production endpoints require exact `api.nebius.cloud`, HTTPS/443, and no URI user-info. HTTPS loopback remains available only for isolated contract-test injection.
- Nebius Object Storage static-key paths share one credential-free endpoint/region trust policy: exact `https://storage.<region>.nebius.cloud:443/`, strict ASCII `[a-z0-9-]` region syntax, no path/query/fragment/user-info, and endpoint/region binding. The live loader applies this before reading static credentials. The production AWSSDK.S3 configuration now also explicitly disables automatic redirects.
- Windows voice invocation is local and review-first. Memory maintenance safely re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
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

### 2026-09-12 — Provider credential and redirect hardening
Completed a systematic provider trust review:
- Fixed Token Factory suffix-lookalike acceptance and required HTTPS/443 with no URI user-info.
- Reordered Token Factory environment loading so endpoint trust is established before `NEBIUS_API_KEY` is read.
- Added a Core no-auto-redirect HTTP factory and wired desktop Token Factory, Tavily, and Nebius Serverless clients through it.
- Applied no-auto-redirect behavior to the remote worker and live Nebius contract probe.
- Hardened Serverless to exact `api.nebius.cloud` over HTTPS/443 with no user-info while preserving explicit HTTPS loopback test injection.
- Hardened Tavily to exact `api.tavily.com` over HTTPS/443 with no user-info and request-observation regressions.

Representative commits: `18943f9f`, `64285c0a`, `8460e689`, `053feb52`, `4c791fca`, `9b6e03a9`, `117b89aa`, `e0362868`, `15e24a26`, `7eaf0131`.

### 2026-09-12 — Object Storage credential boundary
Completed:
- Restricted `NebiusObjectStorageClient` to the exact regional Nebius S3-compatible origin and bound endpoint hostname to configured region.
- Added strict HTTPS/443, no user-info/path/query/fragment, bounded bucket/prefix/key behavior, sanitized errors, and strict ASCII region syntax.
- Added a shared credential-free `NebiusObjectStorageEndpointTrust` used by both the live configuration loader and runtime client so those boundaries cannot drift independently.
- Reordered live configuration so Object Storage endpoint + region trust is established before static access-key environment variables are read.
- Added counting-reader regressions proving arbitrary-host, cross-region, and Unicode-confusable region configurations fail before static credential lookup.

Representative commits: `66181222`, `92ec271c`, `126376af`, `8aceba9d`, `3efcdf95`, `307fe25a`, `f087b509`, `541d7247`, `10a84323`, `f9f71518`.

### 2026-09-12 — AWSSDK.S3 redirect egress hardening
Completed this run:
- Re-read the full ledger and current Object Storage production path before changing code.
- Inspected the pinned `AWSSDK.S3` version (`4.0.102.5`) and current AWS SDK V4 documentation/source rather than assuming redirect behavior.
- Confirmed `AmazonS3Config` inherits `AllowAutoRedirect`, and current upstream `ClientConfig` defaults that flag to `true`; the AWS HTTP pipeline uses the flag to control redirect following.
- Added `NebiusObjectStorageClient.CreateSdkConfiguration` as an internal, test-visible construction seam for the exact production `AmazonS3Config`.
- Explicitly set `AllowAutoRedirect = false`, while preserving the trusted service URL, authentication region, virtual-host behavior, and disabled SDK retry budget.
- Routed the production constructor through that exact configuration factory.
- Added `SdkConfiguration_DisablesAutomaticRedirects` regression coverage asserting redirects remain disabled alongside the expected service URL, auth region, path-style setting, and retry setting.
- Updated `docs/nebius-object-storage-transport.md` so automatic redirects are an explicit security invariant and dependency upgrades must preserve the regression.

Engineering commits before this ledger update:
- `abeca9f90cc93ac5c8d4b22f2c1ca78cd7c2fd9a` — harden Object Storage SDK redirect policy.
- `1a1c11a295273b8cb4fdd92e0c5ce243f779e97d` — cover Object Storage redirect policy.
- `034a62736e5aed3ae1fec5938e739df879b206c4` — document Object Storage redirect boundary.

Validation / evidence:
- GitHub compare from prior ledger head `f9f715184e477db06bdc5aa881f29d1fd8c16d2a` to engineering head `1a1c11a295273b8cb4fdd92e0c5ce243f779e97d` reported **2 commits ahead / 0 behind** across only the intended production client and focused test before the documentation commit.
- Static source inspection confirms the pinned package is `AWSSDK.S3 4.0.102.5`.
- Current AWS SDK V4 API documentation confirms `AmazonS3Config` exposes inherited `AllowAutoRedirect`; current upstream `ClientConfig` source shows the default backing value is `true`, making the explicit override necessary.
- Current upstream HTTP pipeline source documents/applies the configuration flag to redirect handling.
- Static re-fetch confirms the production client is constructed from the configuration object containing `AllowAutoRedirect = false`.
- `command -v dotnet` / `dotnet --info` still produce no executable signal in this runtime. Therefore no compile/unit-test/WPF/Worker/tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- Initial Object Storage credential egress is constrained by exact Nebius regional-origin validation before SDK construction.
- Subsequent HTTP `3xx` handling is now explicitly fail-closed at the AWSSDK configuration layer instead of depending on the SDK default, preventing silent redirect-following of signed requests to another origin.
- Static credential-read ordering, strict region syntax, local timeout/retry budgets, bounded payloads, and sanitized errors remain intact.
- The new test seam is `internal` and exposed only through the existing `InternalsVisibleTo("Nvidea.Core.Tests")`; it does not enlarge the public production API.

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
- The explicit SDK redirect flag is statically verified but still requires executable contract coverage against the pinned package before being treated as runtime evidence.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evidence/evaluator tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, audit the live research configuration for **credential-read ordering beyond Object Storage** (Serverless token, Token Factory key, Tavily key, RSA material): establish every provider endpoint/trust boundary before reading its corresponding secret, and add counting-reader regressions where ordering is not already proven.
