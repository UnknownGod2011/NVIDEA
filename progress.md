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
- Worker envelope decryption uses `WorkerEnvelopePrivateKeyTrust`: bounded PEM, private RSA >=2048, OAEP-SHA256 capability proof, canonical PKCS#8, and probe zeroization. `ResearchWorkItemProtector.Unprotect(...)` reuses it directly.
- Worker envelope encryption uses `WorkerEnvelopePublicKeyTrust`: bounded PEM, public-only RSA >=2048, OAEP-SHA256 capability proof, canonical SPKI, and probe zeroization. Protocol protection, deployment preflight, and live loading share this policy.
- Client result-envelope encryption/decryption uses `ClientResultEnvelopePublicKeyTrust` / `ClientResultEnvelopePrivateKeyTrust`, with bounded input, strict public/private role separation, RSA >=2048, OAEP-SHA256 capability checks, canonicalization, and probe zeroization. `ResearchResultProtector.Protect(...)` / `Unprotect(...)` enforce these policies at the protocol boundary.
- Client dispatch-signing material is usable private RSA >=2048 and worker verification material is public-only RSA >=2048.
- **Live client RSA key purposes are now separated:** dispatch signing/verification and result-envelope encryption/decryption use distinct RSA identities. The live loader requires `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` and `NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE`; only their distinct public halves are placed into worker configuration. The worker uses the signing public key only for dispatch-binding verification and the result public key only for result encryption. The live runtime uses the signing private key only for binding signatures and the result private key only for result ingestion/decryption.
- `NebiusResearchWorkerRuntimeConfiguration` remains the single worker environment boundary. Credential-free bootstrap/timing/destination/model checks run before worker-private-key validation and provider secret reads.
- Windows voice invocation is local and review-first. Memory maintenance re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
- Deterministic judging/readiness tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, redacted deployment fingerprints, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product, evaluator, and judging hardening
Added research/browser product runtimes, WPF lifecycle-aware research, restart-safe browser-goal recovery, one-shot cloud approval, local review-first voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, prompt-injection mutation approval hardening, unified judging evidence, deterministic demo-package validation, and adversarial validator tests.

### 2026-09-11 to 2026-09-12 — Provider trust + cryptographic protocol hardening
Hardened Token Factory, Tavily, Serverless, model-catalog, contract-probe, Object Storage, and worker credential paths against suffix lookalikes, user-info, wrong ports, unsafe redirects, cross-region storage origins, Unicode-confusable regions, mutable worker images, RSA role confusion, malformed PEM, and early secret reads. Added credential-free topology/alignment, strict client signing/verification trust, testable worker bootstrap/runtime loading, protocol-level worker private/public envelope trust, deployment/protocol worker-key policy reuse, and lower-level client result-envelope public/private trust.

### 2026-09-12 — Client RSA key-purpose separation
Completed in this run:
- Split the worker bootstrap client identity into `ClientVerificationPublicKeyPem` and `ClientResultEncryptionPublicKeyPem`; added public environment contract `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`.
- Worker bootstrap validates both public-only RSA identities before any worker/provider secret read and rejects identical signing/result public identities.
- `Nvidea.Worker` now passes only the signing public identity to `ResearchDispatchBindingWaiter` and only the result-encryption public identity to `NebiusResearchWorker`.
- `NebiusResearchClientRuntime.Create(...)` now accepts an optional distinct result private key. Existing lower-level fixtures retain a compatibility fallback to the signing private key, but live production composition does not.
- `NebiusResearchLiveRuntimeFactory.Create(...)` now requires a distinct result private key, validates/canonicalizes it through `ClientResultEnvelopePrivateKeyTrust`, derives its public identity, and rejects reuse of the dispatch-signing identity before constructing the runtime.
- `NebiusResearchLiveConfiguration` now carries `ClientResultPrivateKeyPem`. The live loader requires `NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE`, validates it, derives its public SPKI identity, rejects reuse of the signing identity, and injects both distinct public identities into worker plain configuration before reading Serverless/Object Storage credentials.
- The Nebius contract probe passes both private identities into live runtime composition and reports key-purpose separation in preflight output.
- Updated focused bootstrap/runtime/live-loader/provider-startup fixtures for the two-key contract.
- Added adversarial coverage proving private result-key material is rejected in the worker public slot and signing/result identity reuse fails before provider or worker secrets are read.

Engineering commits in this run before the ledger update:
- `8e6e7996b84b88be349f6d6df7f0569d323bf085` — separate worker client signing and result-encryption identities.
- `7a7beb25c637f1a071822102973382d60ccdab1d` — use distinct client key roles in the remote worker.
- `7ac66ab5c43c552ae5a409f2d74f4aaab40c31aa` — separate client result-decryption key in the runtime.
- `6ecdd0619123685b5a0bb7b7b35dce4768912bfd` — require distinct result key in live client runtime composition.
- `f65b1bd07f3761dee4016bd4c9150994d74fcb74` — load distinct client signing/result-envelope keys.
- `bda1e77562bb91b7d23327c58d3b26a373d8f9cc` — use separated result key in the live contract probe.
- `0f8170ef997acea39b678154930eecaf1beebd9c` — test separated client key roles in worker bootstrap.
- `fbf33aa12db2e8b5fdd9355513c6f029dccb1896` — update worker runtime fixtures for separated client keys.
- `1264276c6ed40ffadd43684bc197bc7049c7dc2d` — cover live client key-purpose separation.
- `ccd26aab7e0fc39f5918ca292105332fa9b5447b` — update live provider-startup fixture for the result key.

Validation / evidence:
- GitHub compare from prior ledger head `ee4e1659b0085724906b4b0949dbf680b1bc0dfa` to engineering head `ccd26aab7e0fc39f5918ca292105332fa9b5447b` reports **10 commits ahead / 0 behind**.
- Engineering scope is limited to five production/runtime files, the worker entry point, three focused test files plus one constructor fixture, and the live contract probe.
- Static re-read confirms the worker no longer uses one client public key for both binding verification and result encryption.
- Static re-read confirms live runtime composition no longer sends the signing private key to result ingestion when the production live factory is used.
- Static regression audit found and repaired stale worker-runtime, live-loader, and provider-startup fixtures introduced by the two-key contract.
- `dotnet --info` still returns `dotnet: command not found` in this execution environment, so **no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- Dispatch-signing private material remains client-local; result-decryption private material remains client-local. Worker configuration receives only canonical public identities.
- Worker signature verification and result encryption now consume separate public keys, reducing cross-protocol key reuse and blast radius.
- Live configuration rejects identical signing/result identities before reading Serverless/Object Storage credentials; worker bootstrap independently rejects identical public identities before reading worker/provider secrets.
- Result private-key validation retains the existing OAEP-SHA256 capability proof and canonical PKCS#8 policy; result public-key validation retains the public-only OAEP-SHA256 policy.
- Existing authenticated associated data, encrypted transport, signed dispatch bindings, provider endpoint trust, cancellation/recovery, exact-once ingestion, Tavily/Nemotron behavior, and Windows product behavior were not removed or weakened.
- Lower-level `NebiusResearchClientRuntime.Create(...)` intentionally retains same-key fallback for existing unit/contract fixtures. The live production factory is stricter and rejects same-key use. This compatibility path should remain clearly non-production and eventually be removable after executable migration coverage exists.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and the new two-key API surface still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- `NebiusResearchDeploymentPreflight.Validate(...)` still formally requires/validates only the dispatch-signing client public environment entry. The live loader and worker bootstrap enforce the new result public identity, but a future direct caller constructing raw `NebiusResearchDispatchOptions` could pass deployment preflight without `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM` and fail only at worker startup. This policy should be centralized in deployment preflight.
- Documentation/setup still describes the previous single-client-key contract in places; `README.md`, `docs/nebius-contract-probe.md`, and `docs/nebius-research-worker.md` need the distinct signing/result key generation and environment contract documented after preflight is centralized.
- Provider catalogs can change; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect caused by or exposed through the two-key migration before treating the path as judge-ready. If executable validation remains unavailable in the next run, the highest-value safe fallback is to move `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM` validation and distinct-identity enforcement into `NebiusResearchDeploymentPreflight`, add direct raw-dispatch-options regressions, then update the Nebius worker/contract-probe setup documentation for generating and supplying the two distinct client RSA identities.
