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
- Worker envelope public key is validated as public-only RSA >=2048 bits before client signing material or provider credentials.
- Client dispatch-signing key is validated as usable private RSA >=2048 bits with an actual SHA-256/PKCS#1 signature operation before Serverless/Object Storage credentials. Its canonical public identity is derived locally.
- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` is a strict public-only RSA >=2048-bit verification identity. Deployment rejects private-key PEM and rejects MysteryBox-backed use of this non-secret verification identity; live dry-run reuses the same shared validator.
- Remote worker startup is composed through `NebiusResearchWorkerRuntimeConfiguration`: credential-free bootstrap, timing, Nebius endpoint, and model configuration are validated through one injectable environment reader before any worker/provider secret is accessed.
- `NVIDEA_WORKER_PRIVATE_KEY_PEM` is now the first worker secret read after credential-free trust checks. It must be usable private RSA >=2048 bits, successfully round-trip the exact OAEP-SHA256 operation used by remote work-item encryption, and is canonicalized to PKCS#8 before the Token Factory or Tavily API keys are read.
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
- Added `NebiusResearchWorkerRuntimeConfiguration` as the single worker runtime environment boundary with an injectable environment reader.
- Credential-free bootstrap, binding timing, trusted Token Factory destination, and model overrides are validated before worker/provider secret reads.
- `Nvidea.Worker/Program.cs` now uses the composed runtime object instead of directly reading provider/timing/private-key environment variables.
- Added counting-reader regressions for valid ordering, malformed bootstrap, suffix-lookalike Nebius destinations, and invalid timing.
- Prior commits: `53faf991dc0ae88b3343f60a95f9e857c0d5d806`, `386f99e18f119dfb702d0ea7178cc8cc95a7e345`, `acd6ca292aca1d2fd1f17d43851129d0c32301a9`, `7b1cac317c4041ec2922e1299d2b28777e8aa04c`.

### 2026-09-12 — Worker envelope private-key trust hardening
Completed this run:
- Re-read this ledger and inspected the current worker runtime loader, worker executable startup, deployment RSA trust helpers, remote work-item/result protocol, recent repo tree, and focused worker-runtime tests before editing.
- Added strict `NebiusResearchWorkerRuntimeConfiguration.ValidateWorkerPrivateKey(...)` validation for `NVIDEA_WORKER_PRIVATE_KEY_PEM`.
- The worker envelope key must now be bounded PEM with usable **private** RSA material of at least 2048 bits. Public-only PEM, malformed PEM, and weak RSA are rejected.
- The validator performs an actual 32-byte RSA OAEP-SHA256 encrypt/decrypt round-trip, matching the padding used by `ResearchWorkItemProtector`, and compares the probe in fixed time. Probe, wrapped, and unwrapped temporary buffers are zeroed in `finally`.
- Valid worker private keys are canonicalized to PKCS#8 PEM before downstream worker construction.
- Secret-read ordering was tightened: after all credential-free bootstrap/timing/destination/model checks, the worker private key is read and cryptographically validated **before** `NEBIUS_API_KEY` or `TAVILY_API_KEY` is accessed. A malformed worker identity therefore cannot cause independent provider credentials to be read.
- Expanded `NebiusResearchWorkerRuntimeConfigurationTests` with valid ordering assertions plus malformed, public-only, and RSA-1024 worker-key regressions. These tests assert provider credentials are not read when the worker key fails.
- Added a direct canonicalization test for a valid RSA-2048 worker private identity.

Engineering commits before this ledger update:
- `6969de8816950ab45a0bb2393b3272a439a270a7` — harden worker envelope private-key trust.
- `6812f6fdb9c5dbea5c7a668304ea3482e7b0c2ed` — cover worker private-key trust ordering.

Validation / evidence:
- GitHub compare from prior ledger head `ac26d52058634fc65bd1789db19d8d07ae60e06a` to engineering head `6812f6fdb9c5dbea5c7a668304ea3482e7b0c2ed` reports **2 commits ahead / 0 behind** and isolates the engineering delta to the worker runtime configuration and its focused test file.
- Static re-fetch of the runtime loader confirms all public bootstrap/timing/Nebius/model checks precede `NVIDEA_WORKER_PRIVATE_KEY_PEM`, and that successful worker private-key validation precedes both provider API-key reads.
- Static inspection of `ResearchWorkItemProtector.Unprotect` confirms OAEP-SHA256 is the production work-item unwrap operation, matching the new capability proof.
- `dotnet --info` still returns `dotnet: command not found`; no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- Public bootstrap and provider destination trust still fail before every secret read.
- Malformed/public-only/weak worker decryption identities now fail after only the worker secret itself is read; Token Factory and Tavily API keys remain unread.
- The capability proof is protocol-specific to RSA OAEP-SHA256 rather than merely checking that private parameters exist.
- Temporary cryptographic probe buffers are explicitly zeroed; the validated key is normalized to a stable PKCS#8 representation.
- Existing encrypted transport, signed dispatch binding, provider redirect policy, cancellation/recovery, Tavily/Nemotron behavior, and Windows product behavior were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and newly added focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change after a check; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Recent RSA/public-key, PEM-ordering, worker-bootstrap, runtime-ordering, and worker-private-key regressions are statically reviewed but remain unexecuted because `dotnet` is unavailable.
- Defense-in-depth remains incomplete at the lower-level protocol boundary: `ResearchWorkItemProtector.Unprotect(...)` still directly imports the caller-supplied worker PEM. Normal production startup now provides a validated canonical key, but a future/bypassing caller could reach the protocol API without passing through `NebiusResearchWorkerRuntimeConfiguration`.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, extract/reuse the worker private-key trust policy at the lower-level `ResearchWorkItemProtector.Unprotect(...)` boundary (without creating duplicated crypto policy), then add direct protocol adversarial tests proving malformed, public-only, and weak RSA worker identities fail closed even if the normal runtime configuration loader is bypassed.
