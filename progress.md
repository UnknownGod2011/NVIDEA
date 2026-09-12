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
- Object Storage disables automatic redirects; live configuration validates credential-free topology/alignment before local signing material or provider credentials.
- Worker envelope public key is public-only RSA >=2048 bits; client dispatch-signing key is usable private RSA >=2048 bits; client verification identity is public-only RSA >=2048 bits.
- `NebiusResearchWorkerRuntimeConfiguration` is the single worker environment boundary. Credential-free bootstrap/timing/destination/model checks run before `NVIDEA_WORKER_PRIVATE_KEY_PEM`, then worker-key validation runs before `NEBIUS_API_KEY` / `TAVILY_API_KEY` reads.
- Worker private-key trust is now centralized in `WorkerEnvelopePrivateKeyTrust`: bounded PEM, private RSA >=2048 bits, exact OAEP-SHA256 capability proof, canonical PKCS#8 output, and temporary probe zeroization.
- `ResearchWorkItemProtector.Unprotect(...)` now reuses that exact shared trust primitive, so lower-level protocol callers cannot bypass worker startup validation with malformed, public-only, weak, or wrong-capability RSA material.
- Windows voice invocation is local and review-first. Memory maintenance re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
- Deterministic judging/readiness tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, redacted deployment fingerprints, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product, evaluator, and judging hardening
Added research/browser product runtimes, WPF lifecycle-aware research, restart-safe browser-goal recovery, one-shot cloud approval, local review-first voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, prompt-injection mutation approval hardening, unified judging evidence, deterministic demo-package validation, and adversarial validator tests.

### 2026-09-11 to 2026-09-12 — Provider trust + worker startup hardening
Verified current Nemotron routing defaults; hardened Token Factory, Tavily, Serverless, model-catalog, contract-probe, Object Storage, and worker credential paths against suffix lookalikes, user-info, wrong ports, unsafe redirects, cross-region storage origins, Unicode-confusable regions, and early secret reads. Added credential-free topology/alignment, immutable-image validation, strict RSA role policies, multiline PEM handling, public-only verification identity, testable worker bootstrap/runtime loading, and first-secret worker private-key capability validation.

### 2026-09-12 — Protocol-level worker private-key trust hardening
Completed this run:
- Re-read this ledger and inspected the current worker runtime loader and lower-level protected research work-item protocol before editing.
- Added `WorkerEnvelopePrivateKeyTrust` as the single policy for the worker secret-backed envelope identity.
- Shared policy requires bounded PEM, private RSA material, >=2048 bits, and a real RSA OAEP-SHA256 encrypt/decrypt capability proof using a 32-byte probe. Temporary probe/wrapped/unwrapped buffers are zeroed.
- Valid keys can be canonicalized to PKCS#8; runtime startup now delegates to this shared trust primitive instead of owning duplicate crypto policy.
- `ResearchWorkItemProtector.Unprotect(...)` now obtains its RSA instance from `WorkerEnvelopePrivateKeyTrust.CreateValidatedRsa(...)` rather than directly importing arbitrary caller-supplied PEM. This closes the lower-level bypass path identified in the prior run.
- Added `ResearchWorkItemProtectorKeyTrustTests` covering a valid RSA-2048 round trip plus public-only, RSA-1024, and malformed worker-key rejection at the protocol boundary, and a shared-policy equivalence check between runtime startup and protocol trust.
- Static review caught and fixed malformed-PEM exception normalization so invalid PEM follows the fail-closed `InvalidOperationException` contract rather than leaking raw import exceptions.

Engineering commits before this ledger update:
- `298b61a95f6c4f9d17e1a44a5fba40720b8c572a` — centralize worker envelope private-key trust.
- `e1880ef2cc300310b4d2a0a947c23f22920402ea` — route worker runtime validation through the shared policy.
- `12b06a81fc77b860845872c659c5ba3a3538935c` — enforce shared worker key trust inside `ResearchWorkItemProtector.Unprotect(...)`.
- `bb8e61d9398d88daf12e1d164d14b76c58ce1b81` — add protocol adversarial key-trust coverage.
- `da750ed7fab4da63ad82ea20da85a3d51138153f` — normalize malformed worker-key failures after static review.

Validation / evidence:
- GitHub compare from prior ledger head `d9556ae2d1ff2b832c252f2e9764c73587106cc7` to engineering head `da750ed7fab4da63ad82ea20da85a3d51138153f` reports **5 commits ahead / 0 behind**.
- Static re-fetch confirms worker startup and protocol unprotection use the same `WorkerEnvelopePrivateKeyTrust` policy rather than duplicate RSA checks.
- `dotnet --info` still returns `dotnet: command not found`; no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- A future caller bypassing normal worker runtime construction still cannot send public-only, weak, malformed, or non-OAEP-capable RSA material into work-item decryption.
- The private-key policy is now one cohesive abstraction instead of diverging runtime/protocol implementations.
- Existing encrypted transport, signed dispatch binding, provider trust, cancellation/recovery, Tavily/Nemotron behavior, and Windows product behavior were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and newly added focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- Recent RSA/public-key, secret-ordering, worker-bootstrap/runtime, and protocol key-trust regressions are statically reviewed but remain unexecuted because `dotnet` is unavailable.
- `ResearchWorkItemProtector.Protect(...)` still directly imports the caller-supplied worker public key. Deployment preflight currently validates the normal path, but defense-in-depth could centralize a strict public-only worker encryption-key policy at the lower-level protocol boundary as well.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, centralize the worker **public** envelope encryption-key policy and enforce it directly in `ResearchWorkItemProtector.Protect(...)`, with direct protocol tests proving private-key PEM, weak RSA, malformed PEM, and oversized/control-character input fail closed even when deployment preflight is bypassed.
