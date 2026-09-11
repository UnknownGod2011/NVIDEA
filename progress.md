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
- Tavily Search + Extract research with canonical deduplication, source quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, restart-safe staged checkpoints, and an exact production endpoint boundary (`api.tavily.com`, HTTPS/443, no URI user-info).
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Native Nebius Object Storage transport and Serverless-mounted worker transport share one protected protocol; preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- Credential-bearing desktop provider clients use an explicit no-auto-redirect HTTP policy; the remote worker, model-catalog checker, and live Nebius contract-probe follow the same fail-closed redirect posture.
- Token Factory endpoints require HTTPS/443, no URI user-info, and exact `nebius.com` or genuine `*.nebius.com` DNS boundaries. Endpoint trust is validated before API-key lookup in the environment-based production path.
- Nebius Serverless production endpoints require exact `api.nebius.cloud`, HTTPS/443, and no URI user-info. HTTPS loopback remains available only for isolated contract-test injection.
- Nebius Object Storage static-key clients now require the exact regional origin `https://storage.<region>.nebius.cloud:443/`, bind the hostname to the configured region, reject paths/query/fragment/user-info, and reject malformed region identifiers.
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

### 2026-09-12 — Provider credential and redirect hardening
Completed a systematic production/provider trust review:
- Fixed Token Factory suffix-lookalike acceptance (`evilnebius.com`) and required HTTPS/443 with no URI user-info.
- Reordered Token Factory environment loading so endpoint trust is established before `NEBIUS_API_KEY` is read.
- Added a Core no-auto-redirect HTTP factory and wired desktop Token Factory, Tavily, and Nebius Serverless clients through it.
- Applied no-auto-redirect behavior to the remote worker and live Nebius contract probe.
- Hardened Serverless to exact `api.nebius.cloud` over HTTPS/443 with no user-info while preserving explicit HTTPS loopback test injection.
- Hardened Tavily to exact `api.tavily.com` over HTTPS/443 with no user-info and added request-observation regressions proving invalid configuration fails before provider traffic.

Representative commits:
- `18943f9f5b325cbe3549537bc35510dcb0184497` / `64285c0a2451826eca9dc3bc09cae821ba8826a0` — production Token Factory trust boundary and regressions.
- `8460e6898631a7eebf90e10e1f1cb33b5a4082b8` / `053feb527ef638c58b2a11ba4aa4073e9e3adf3c` / `4c791fca61ea4af558810f9b56b8b34462327458` — no-redirect provider HTTP construction.
- `9b6e03a9cff7f04ab65226f348fff8123fc1bdb6` — Serverless endpoint boundary.
- `117b89aa126909f6ed89d6cb43c2dc6dcaaab891` — remote Worker redirect hardening.
- `e0362868160b87ae9012794192a67486042f77fb` / `15e24a26a9c1bb5f5fd27ff14789d2ca8c604d1a` — Tavily endpoint trust and regressions.
- `7eaf01318fd1814dc22d8b86a67a8387b75e18a8` — live contract-probe redirect hardening.

### 2026-09-12 — Nebius Object Storage static-credential boundary hardening
Completed:
- Re-read this ledger and audited the remaining native provider/tool HTTP construction surfaces instead of repeating already-hardened desktop/Worker paths.
- Identified a real remaining static-credential egress risk in `NebiusObjectStorageClient`: the S3-compatible client accepted any absolute HTTPS origin while holding Nebius Object Storage access-key credentials.
- Verified Nebius' documented regional S3 endpoint form (`https://storage.<region>.nebius.cloud:443`) from current official Nebius material before changing the trust rule.
- Hardened `NebiusObjectStorageClient` so production Object Storage now requires the exact regional origin `storage.<configured-region>.nebius.cloud`, HTTPS, port 443, no URI user-info, no path beyond `/`, no query, and no fragment.
- Bound endpoint hostname to the separately configured region so a valid Nebius endpoint for a different region cannot silently receive the configured static credentials.
- Restricted region identifiers to bounded lowercase ASCII letters, digits, and interior hyphens to prevent region text from widening or confusing the trusted hostname construction.
- Added `NebiusObjectStorageEndpointTrustTests` covering the valid regional origin plus HTTP, alternate port, URI user-info, arbitrary host, Nebius-domain confusion, prefix lookalike, path/query injection, region mismatch, and malformed region identifiers.

Engineering commits before this ledger update:
- `66181222e88fd5a373b0b0e5a55b4e603d90a481` — harden Nebius Object Storage endpoint trust boundary.
- `92ec271c50249e5f80fe732b2746932fe9626b9a` — cover Object Storage endpoint trust boundary.

Validation / evidence:
- GitHub compare from prior ledger head `8c7f9e4e58ba4df0b239c916d3e9deaaef2cff00` to engineering head `92ec271c50249e5f80fe732b2746932fe9626b9a` reports **2 commits ahead / 0 behind** and only the Object Storage client plus its focused trust-boundary test were changed.
- Static re-fetch confirms the constructor calls `ValidateOptions` before constructing `BasicAWSCredentials` / `AmazonS3Client`.
- Static re-fetch confirms endpoint host equality is derived from the separately validated region rather than using a suffix match.
- `command -v dotnet` still produced no executable path in this runtime. Therefore no compile/unit-test/WPF/Worker/tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- Static S3 credentials can no longer be configured against an arbitrary HTTPS origin through `NebiusObjectStorageClientOptions`.
- Region/endpoint disagreement fails closed before the AWS SDK client is constructed.
- Provider response bodies remain excluded from the client's sanitized storage exceptions.
- One remaining defense-in-depth opportunity exists in the live environment loader: it currently reads several live credentials before the final aggregate preflight validates every endpoint. The Object Storage constructor itself is safe, but environment-read ordering could be improved so untrusted endpoint configuration is rejected before the corresponding static credential variables are read.

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
- The AWS SDK's own redirect/transport behavior is not directly controlled by `ProviderHttpClientFactory`; exact-origin validation significantly narrows initial credential egress, but a real integration test should verify redirect behavior for the specific AWSSDK.S3 version used by NVIDEA.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evidence/evaluator tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, refactor the live Nebius configuration loader so Object Storage endpoint/region trust is validated before `NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID` and `NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY` are read, then add a counting environment-reader regression proving invalid endpoint configuration cannot cause those credential variables to be accessed.
