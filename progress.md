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
- Live configuration validates credential-free topology/alignment before local signing material or provider credentials.
- Worker private envelope identity is centralized in `WorkerEnvelopePrivateKeyTrust`: bounded PEM, private RSA >=2048 bits, OAEP-SHA256 capability proof, canonical PKCS#8, and probe zeroization. `ResearchWorkItemProtector.Unprotect(...)` reuses it directly.
- Worker public envelope identity is centralized in `WorkerEnvelopePublicKeyTrust`: bounded PEM, public-only RSA >=2048 bits, OAEP-SHA256 encryption capability proof, canonical SubjectPublicKeyInfo output, and temporary probe zeroization. `ResearchWorkItemProtector.Protect(...)` now reuses it directly.
- Client dispatch-signing key is usable private RSA >=2048 bits; client verification identity is public-only RSA >=2048 bits.
- `NebiusResearchWorkerRuntimeConfiguration` is the single worker environment boundary. Credential-free bootstrap/timing/destination/model checks run before worker-private-key validation, then provider secret reads.
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
Hardened Token Factory, Tavily, Serverless, model-catalog, contract-probe, Object Storage, and worker credential paths against suffix lookalikes, user-info, wrong ports, unsafe redirects, cross-region storage origins, Unicode-confusable regions, mutable worker images, RSA role confusion, malformed PEM, and early secret reads. Added credential-free topology/alignment, strict client signing/verification trust, testable worker bootstrap/runtime loading, protocol-level worker private-key trust, and a shared worker-public-key trust primitive.

### 2026-09-12 — Protocol-level worker public encryption trust
Completed this run:
- Re-read this ledger completely and inspected the current main branch, recent commits, `ResearchWorkItemProtector`, `WorkerEnvelopePublicKeyTrust`, deployment preflight, and focused key-trust tests before editing.
- Verified the repository boundary before each mutation; every write targeted exactly `UnknownGod2011/NVIDEA`.
- Replaced `ResearchWorkItemProtector.Protect(...)`'s direct `RSA.ImportFromPem(...)` path with `WorkerEnvelopePublicKeyTrust.CreateValidatedRsa(...)`.
- Lower-level callers that bypass deployment preflight can no longer use private, RSA <2048-bit, malformed, oversized, or control-character-bearing worker encryption-key material for protected work-item wrapping.
- The protocol now validates the worker encryption identity before serializing/encrypting the work item or generating the random envelope data key/nonce, reducing work performed after an invalid trust input.
- Added focused protocol regressions for a valid RSA-2048 round trip and rejection of private, weak RSA-1024, malformed, oversized (>65536 chars), and embedded-control-character worker public-key inputs.

Engineering commits before this ledger update:
- `e8a5437d6c416705dfa8bc252ef6fe5c8dc908f1` — enforce worker public key trust at protocol boundary.
- `754cc9e94a3b69ee8b80ad29ef690519413c8445` — cover protocol public key trust boundary.

Validation / evidence:
- GitHub compare from prior ledger head `a61507c1e8d50e605c03ebfce45a297f3dac0510` to engineering head `754cc9e94a3b69ee8b80ad29ef690519413c8445` reports **2 commits ahead / 0 behind** and exactly two intended changed files: `src/Nvidea.Core/Jobs/NebiusResearchDispatch.cs` and `tests/Nvidea.Core.Tests/ResearchWorkItemProtectorKeyTrustTests.cs`.
- Static review confirms `Protect(...)` and `Unprotect(...)` now both use dedicated shared RSA role/capability trust primitives at the protocol boundary.
- `dotnet --info` still returns `dotnet: command not found` in this execution environment, so no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- The worker encryption-key role is now enforced at the actual cryptographic protocol boundary, not only at deployment/configuration layers.
- Private worker key material cannot accidentally be accepted by `Protect(...)` and copied into a public-encryption role.
- Weak/malformed/oversized/control-character input fails before protected payload generation.
- Existing encrypted transport, signed dispatch binding, provider trust, cancellation/recovery, Tavily/Nemotron behavior, and Windows product behavior were not removed or weakened.
- Deployment preflight still has its own `ValidateWorkerPublicKey(...)` implementation instead of delegating to `WorkerEnvelopePublicKeyTrust`; this remaining policy-drift risk is intentionally recorded as unfinished.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and recent focused RSA/secret-ordering regressions still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- `NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(...)` still duplicates the worker-public-key policy and can drift from `WorkerEnvelopePublicKeyTrust` until it delegates to the shared primitive.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, replace `NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(...)` with delegation to `WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(...)` (or `CreateValidatedRsa(...)`), add focused deployment-preflight regressions proving private/weak/malformed/oversized/control-character worker public-key material is rejected by the same shared policy, then audit `ResearchResultProtector` client public/private RSA handling for an equivalent lower-level role/capability trust boundary.
