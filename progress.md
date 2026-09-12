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
- .NET 8 core in `src/Nvidea.Core`; WPF Windows host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with timeout/cancellation, retries, structured tool calling, response-schema support, and Nano / Super / Ultra routing for Fast / Standard / Deep work.
- Layered personal memory with privacy-aware writes, provenance, lexical/semantic/recency/importance retrieval, local Ollama embeddings, vector-space isolation, migration/re-indexing, maintenance UX, and deterministic retrieval fixtures.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, freshness/quality/diversity ranking, untrusted-evidence boundaries, citations/provenance, restart-safe checkpoints, and exact production endpoint trust.
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan-act-observe-verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Native Nebius Object Storage and Serverless-mounted worker transport share one protected protocol. Preflight validates mount alignment, READ_WRITE transport, MysteryBox references, immutable worker image, RSA identities, bounded resources, and redacted fingerprints.
- Token Factory, Tavily, Serverless, remote worker, model-catalog, contract-probe, and Object Storage credential-bearing paths have explicit endpoint and redirect trust boundaries.
- Token Factory requires HTTPS/443, no URI user-info, and exact `nebius.com` or genuine `*.nebius.com`; endpoint trust is established before API-key lookup.
- Nebius Serverless requires exact `api.nebius.cloud`, HTTPS/443, and no URI user-info in production; HTTPS loopback is test-only.
- Nebius Object Storage requires exact `https://storage.<region>.nebius.cloud:443/`, strict ASCII region syntax, endpoint/region binding, no path/query/fragment/user-info, and AWSSDK automatic redirects disabled.
- Live configuration validates provider-independent topology, immutable image, MysteryBox references, transport volume/root, and Object Storage bucket/prefix alignment before reading local signing material or provider credentials.
- Object Storage namespace alignment uses credential-free `NebiusObjectStorageTransportAlignment`.
- Worker envelope key is validated as public-only RSA >=2048 bits before client signing material or provider credentials.
- Client dispatch-signing key is validated as usable private RSA >=2048 bits with an actual SHA-256/PKCS#1 signature operation before Serverless/Object Storage credentials. Its canonical public identity is derived locally.
- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` is a strict public-only RSA >=2048-bit verification identity. Deployment rejects private-key PEM and rejects MysteryBox-backed use of this non-secret verification identity; live dry-run reuses the same shared validator.
- Remote worker startup is now composed through `NebiusResearchWorkerRuntimeConfiguration`: credential-free bootstrap, timing, Nebius endpoint, and model configuration are validated through one injectable environment reader before Token Factory, Tavily, or worker-private-key secrets are accessed.
- Worker `NEBIUS_API_KEY` and `TAVILY_API_KEY` values are never read by the desktop live loader; only validated Nebius MysteryBox references are handled there.
- Windows voice invocation is local and review-first. Memory maintenance re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
- Deterministic judging/readiness tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, redacted deployment fingerprints, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product, evaluator, and judging hardening
Added research/browser product runtimes, WPF lifecycle-aware research, restart-safe browser-goal recovery, one-shot cloud approval, local review-first voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, prompt-injection mutation approval hardening, unified judging evidence, deterministic demo-package validation, and adversarial validator tests.

### 2026-09-11 — Current Nemotron routing + catalog readiness
Verified current Token Factory model IDs and set Nano / Super / Ultra defaults for Fast / Standard / Deep. Added routing tests, zero-inference model-catalog readiness, bounded catalog parsing, SHA-256 evidence binding, fresh-live catalog requirements, and catalog endpoint/redirect credential hardening.

### 2026-09-12 — Provider/Object Storage trust and secret-read ordering
Hardened Token Factory, Tavily, Serverless, model-catalog, worker, contract-probe, and Object Storage egress against suffix lookalikes, user-info, wrong ports, unsafe redirects, cross-region storage origins, Unicode-confusable regions, and credential reads before trust validation. Object Storage disables AWSSDK redirects. Added credential-free topology/alignment, immutable-image validation, worker-public-key validation, client-signing-key validation, multiline PEM handling, strict public-only client verification identity, and worker-side bootstrap trust.

### 2026-09-12 — Testable full worker runtime configuration
Completed this run:
- Re-read this ledger completely and inspected current `src/Nvidea.Worker/Program.cs`, `NebiusResearchWorkerBootstrapTrust`, `NebiusOptions`, `TavilyOptions`, recent repository commits, and the previous focused bootstrap tests before editing.
- Added `src/Nvidea.Core/Jobs/NebiusResearchWorkerRuntimeConfiguration.cs` as the single worker runtime environment boundary. It accepts an injectable environment reader and composes validated bootstrap trust, binding timing, the Token Factory destination/model configuration, Token Factory credentials, Tavily credentials, and the worker private-key value.
- The loader validates all credential-free values first: absolute bounded transport root, canonical public-only RSA client verification key, binding poll/wait durations, trusted Nebius endpoint, and model overrides. Only after those checks succeed does it read `NEBIUS_API_KEY`, `TAVILY_API_KEY`, and `NVIDEA_WORKER_PRIVATE_KEY_PEM`.
- Updated `src/Nvidea.Worker/Program.cs` to use this configuration object exclusively. The executable no longer directly calls `NebiusOptions.FromEnvironment()`, `TavilyOptions.FromEnvironment()`, `Environment.GetEnvironmentVariable(...)`, or local timing helpers for runtime startup.
- Added `tests/Nvidea.Core.Tests/NebiusResearchWorkerRuntimeConfigurationTests.cs` with counting-reader coverage proving: (1) valid configuration reads all public trust/timing/provider-destination values before the first secret; (2) malformed transport bootstrap prevents every provider/worker secret read; (3) a Nebius suffix-lookalike endpoint prevents every secret read; and (4) malformed timing prevents every secret read.
- Static review caught and corrected an initial test-only use of `IReadOnlyList<T>.IndexOf`; the final helper performs an explicit ordinal scan instead.

Engineering commits before this ledger update:
- `53faf991dc0ae88b3343f60a95f9e857c0d5d806` — centralize trusted worker runtime configuration.
- `386f99e18f119dfb702d0ea7178cc8cc95a7e345` — route worker startup through the testable runtime configuration.
- `acd6ca292aca1d2fd1f17d43851129d0c32301a9` — add full worker runtime secret-read ordering tests.
- `7b1cac317c4041ec2922e1299d2b28777e8aa04c` — correct the focused ordering-test helper after static review.

Validation / evidence:
- GitHub compare from prior ledger head `f94998adabb5086440e137a6eed6e1da6f9a806f` to engineering head `7b1cac317c4041ec2922e1299d2b28777e8aa04c` reports **4 commits ahead / 0 behind** and exactly three intended files changed: one new Core runtime configuration component, worker startup integration, and one focused test file.
- Static re-fetch of `Program.cs` confirms startup now performs a single `NebiusResearchWorkerRuntimeConfiguration.Load()` and consumes only the resulting validated values for provider construction, transport, binding timing, and worker private-key handoff.
- Static re-fetch of the new runtime loader confirms `NebiusOptions.ValidateTrustedBaseUri(...)` runs before `NEBIUS_API_KEY` access and the worker bootstrap trust boundary runs before all three secrets.
- `dotnet --info` still returns `dotnet: command not found`; no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- Invalid public worker bootstrap, invalid binding timing, or an untrusted Token Factory destination now fails before Token Factory/Tavily/worker-private-key secret access through the production startup path.
- Provider HTTP auto-redirect remains disabled; Token Factory endpoint trust and canonical client verification identity remain unchanged.
- Client signing authority remains local; the remote worker still receives only the public client verification identity.
- Existing encrypted transport, signed binding verification, cancellation/recovery, Tavily/Nemotron behavior, and worker execution behavior were not removed or simplified.
- The runtime loader deliberately does not claim to validate worker-private-key cryptographic role yet; it centralizes and orders that read so the next hardening step can validate it in one place.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and newly added focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change after a check; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Recent RSA/public-key, PEM-ordering, worker-bootstrap, and full worker runtime ordering regressions are statically reviewed but remain unexecuted because `dotnet` is unavailable.
- `NVIDEA_WORKER_PRIVATE_KEY_PEM` is now read through one testable boundary, but that boundary currently treats it as a required non-empty secret and leaves RSA/private-role validation to downstream worker construction. A malformed worker key can therefore survive configuration loading even though no provider request has yet been made.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, inspect the worker envelope-decryption implementation and centralize a strict worker private-key policy in `NebiusResearchWorkerRuntimeConfiguration`: require usable private RSA material of the intended minimum strength/role immediately after the secret is read, canonicalize it if safe, and add counting-reader/adversarial tests so malformed/public-only/weak worker key material fails before provider clients or research execution are constructed.
