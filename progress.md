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
- Credential-bearing Token Factory, Tavily, Serverless, worker, model-catalog, contract-probe, and Object Storage paths now use explicit redirect/endpoint trust boundaries.
- Token Factory requires HTTPS/443, no URI user-info, and exact `nebius.com` or genuine `*.nebius.com`; endpoint trust is validated before API-key lookup.
- Nebius Serverless requires exact `api.nebius.cloud`, HTTPS/443, and no URI user-info in production. HTTPS loopback remains test-only.
- Nebius Object Storage static-key paths share a credential-free endpoint/region policy: exact `https://storage.<region>.nebius.cloud:443/`, strict ASCII `[a-z0-9-]` region syntax, no path/query/fragment/user-info, endpoint/region binding, and AWSSDK automatic redirects disabled.
- Live-research configuration now validates provider-independent deployment topology, MysteryBox references, volume/root alignment, and Object Storage bucket/prefix alignment before reading the Serverless bearer token or Object Storage static-key variables.
- Worker `NEBIUS_API_KEY` and `TAVILY_API_KEY` values are not read by the desktop live loader; only validated Nebius MysteryBox references are handled there.
- Windows voice invocation is local and review-first. Memory maintenance safely re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
- `tools/Nvidea.PersonalAiDemoEval` and `tools/Nvidea.PersonalAiAdversarialEval` provide deterministic positive/negative cross-cutting evidence.
- `tools/Nvidea.JudgingEvidenceVerifier` combines positive/adversarial artifacts, matching Nebius live deployment evidence, and a fresh live Token Factory catalog PASS.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` make the <=3-minute judging plan machine-checkable.
- `tools/Nvidea.NebiusModelCatalogCheck` provides captured/live zero-inference readiness checks with strict provider endpoint trust.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, digest-pinned worker requirements, redacted deployment fingerprints, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product, memory, evaluator, and judging hardening
Added research/browser product runtimes, WPF lifecycle-aware research, restart-safe browser-goal recovery, one-shot cloud approval, local review-first voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, browser prompt-injection mutation approval hardening, unified judging evidence, deterministic demo package validation, and adversarial validator tests.

### 2026-09-11 — Current Nemotron routing + catalog readiness
Verified current Token Factory model IDs and set Nano / Super / Ultra defaults for Fast / Standard / Deep. Added routing tests, zero-inference model-catalog readiness, strict bounded catalog parsing, SHA-256 evidence binding, fresh-live catalog requirements in the judging chain, and standalone catalog DNS/user-info/redirect credential-boundary hardening.

### 2026-09-12 — Provider and Object Storage security hardening
Completed a systematic trust/egress pass:
- Fixed Token Factory DNS suffix-lookalikes; required HTTPS/443/no user-info; moved trust validation before `NEBIUS_API_KEY` lookup.
- Added no-auto-redirect HTTP construction for desktop Token Factory/Tavily/Serverless, remote worker, model catalog, and contract probe.
- Hardened Serverless to exact `api.nebius.cloud` and Tavily to exact `api.tavily.com` over HTTPS/443 without user-info.
- Restricted Object Storage to exact regional Nebius origins; centralized endpoint/region trust; tightened region IDs to ASCII; added Unicode-confusable regressions.
- Moved Object Storage endpoint/region validation before static credential reads.
- Explicitly set `AmazonS3Config.AllowAutoRedirect = false` and added SDK-configuration regression coverage.

Representative commits: `18943f9f`, `64285c0a`, `8460e689`, `053feb52`, `9b6e03a9`, `e0362868`, `15e24a26`, `7eaf0131`, `66181222`, `92ec271c`, `126376af`, `8aceba9d`, `3efcdf95`, `541d7247`, `10a84323`, `abeca9f9`, `1a1c11a2`, `034a6273`, `bbd9f1b8`.

### 2026-09-12 — Live provider secret-read ordering hardening
Completed this run:
- Re-read the full progress ledger and current live-research loader/preflight paths before changing code.
- Confirmed the desktop live loader does **not** read actual worker Tavily or Token Factory API-key values; those are Nebius MysteryBox references, so no false secret-ordering claim/test was added for them.
- Identified a real ordering defect: malformed deployment topology (for example Object Storage bucket vs Serverless mounted source mismatch) could previously cause `NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN` and Object Storage static-key variables to be read before the topology was rejected.
- Reordered `NebiusResearchLiveConfigurationLoader.Load` into a credential-free topology phase followed by provider-secret acquisition.
- The early phase now reads trusted Object Storage endpoint/region, project/deployment shape, transport source/root/prefix/source path, bucket, public/verification inputs, and MysteryBox references; builds dispatch options; and runs `NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment` before provider credentials are requested.
- Added a topology-only Object Storage options value containing non-secret sentinel strings solely because the existing alignment API accepts the full options record; the alignment routine currently consumes only bucket/prefix after dispatch validation. Real static keys are read only after this gate succeeds.
- Deferred `NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN`, `NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID`, and `NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY` until after that local topology gate.
- Preserved the final full `NebiusResearchLivePreflightReporter.ValidateAndBuild` so credential shape, RSA signing identity, digest-pinned image, object-store shape, and the complete deployment contract are still validated before a live provider run.
- Added `InvalidTransportBucketAlignment_IsRejectedBeforeProviderCredentialsAreRead`, using a counting environment reader and real temporary 2048-bit RSA test material, proving an invalid bucket/source topology never accesses the Serverless token or either static Object Storage credential variable.

Engineering commits before this ledger update:
- `f8b9871ec7754ef4806911bb3c46a163873d3fc4` — defer live provider secret reads until topology validation.
- `061bc7574072d6219cf3af44e496b7b9f018dd2e` — cover provider secret reads behind topology gate.

Validation / evidence:
- Static re-fetch confirms the production loader now calls `NebiusResearchDeploymentPreflight.ValidateObjectStorageAlignment` before the three local provider credential variables are read.
- Static re-fetch confirms the focused counting-reader regression asserts all three credential variable names are absent from the observed read sequence after an intentional bucket/source mismatch.
- GitHub compare from prior ledger head `bbd9f1b855386a48a242919f6b3295772e83f6ef` to engineering head `061bc7574072d6219cf3af44e496b7b9f018dd2e` reports **2 commits ahead / 0 behind**, changing only `NebiusResearchLiveConfiguration.cs` and `NebiusResearchLiveConfigurationCredentialOrderingTests.cs` (42 production-line changes and 71 test additions).
- `dotnet --info` still returns `dotnet: command not found`; no compile/unit-test/WPF/Worker/tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference request was performed.

Security / privacy / failure review:
- A malformed Object Storage origin still fails before any static-key read as before.
- A valid origin paired with malformed deployment/MysteryBox/volume/bucket topology now also fails before Serverless bearer-token or Object Storage static-key reads.
- The desktop loader does not handle the actual worker Tavily/Token Factory secret values; worker credentials remain MysteryBox-backed rather than plaintext desktop configuration.
- Client dispatch-signing private-key PEM is still read during topology construction because its derived public key is part of worker verification topology. It is local file material rather than a provider-bound HTTP credential, but its read timing remains a reviewable boundary.
- The topology-only Object Storage sentinel is intentionally local and never used to construct an Object Storage client. A narrower credential-free alignment input/overload would remove that coupling and is a worthwhile cleanup if executable validation remains unavailable.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before relying on generated PASS evidence.
- Provider catalogs can change after a check; the 15-minute judging gate reduces but cannot eliminate this race.
- A model appearing in `/v1/models` does not prove quota, inference success, tool calling, context length, or every required capability; a real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Explicit provider redirect policies and this new secret-read ordering are statically verified but still need executable regression coverage against the pinned dependencies.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, remove the topology validator's dependency on credential-bearing `NebiusObjectStorageClientOptions` by introducing a narrow credential-free bucket/prefix alignment input or overload, then review whether client RSA private-key file access can be delayed until all remaining non-secret deployment-shape checks have passed without weakening the derived-public-key identity contract.
