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
- Worker public envelope identity is centralized in `WorkerEnvelopePublicKeyTrust`: bounded PEM, public-only RSA >=2048 bits, OAEP-SHA256 encryption capability proof, canonical SubjectPublicKeyInfo output, and temporary probe zeroization. `ResearchWorkItemProtector.Protect(...)`, deployment preflight, and live worker-key loading now reuse it.
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
Hardened Token Factory, Tavily, Serverless, model-catalog, contract-probe, Object Storage, and worker credential paths against suffix lookalikes, user-info, wrong ports, unsafe redirects, cross-region storage origins, Unicode-confusable regions, mutable worker images, RSA role confusion, malformed PEM, and early secret reads. Added credential-free topology/alignment, strict client signing/verification trust, testable worker bootstrap/runtime loading, protocol-level worker private-key trust, and shared worker-public-key trust.

### 2026-09-12 — Protocol + deployment worker public encryption trust
Completed across the latest runs:
- `ResearchWorkItemProtector.Protect(...)` now uses `WorkerEnvelopePublicKeyTrust.CreateValidatedRsa(...)` directly, so lower-level callers cannot bypass the worker public-key policy.
- Added protocol regressions for valid RSA-2048 round trip and private, weak RSA-1024, malformed, oversized, and embedded-control-character worker public-key rejection.
- Replaced `NebiusResearchDeploymentPreflight.ValidateWorkerPublicKey(...)`'s duplicate RSA parser with direct delegation to `WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(...)`.
- Deployment preflight therefore shares the exact same bounded-input, public-only, RSA >=2048, and OAEP-SHA256 capability policy as the cryptographic protocol boundary.
- Live configuration now stores the canonical SubjectPublicKeyInfo identity returned by the shared policy instead of retaining the caller's original PEM representation.
- Added focused deployment regressions proving canonical identity equality between deployment/protocol trust and rejection of private, weak, malformed, oversized, and control-character-bearing worker public keys.

Engineering commits in this run before the ledger update:
- `2182f9f040d0422f4ffbd72cde511c7ab083f0aa` — unify deployment worker public key trust.
- `97cd0bf77942981362417b6c9456261e93907c58` — canonicalize worker public key before live deployment.
- `fd75c02ec17e29008a0398219cea020937732b92` — cover shared deployment worker key trust.

Validation / evidence:
- GitHub compare from prior ledger head `03ca5c391e5b7f396983edc4c181ad215a4a0b98` to engineering head `fd75c02ec17e29008a0398219cea020937732b92` reports **3 commits ahead / 0 behind**.
- Changed production scope is limited to `NebiusResearchDeploymentPreflight.cs` and `NebiusResearchLiveConfiguration.cs`, plus one focused test file.
- Static review confirms deployment, live loading, and work-item encryption now converge on the same worker public-key trust primitive.
- `dotnet --info` still returns `dotnet: command not found` in this execution environment, so no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- Worker encryption identity policy can no longer drift between deployment and protocol layers.
- Live deployment persists only the canonical public identity after role/size/capability validation.
- Invalid worker public key material still fails before client signing material or provider credentials are read.
- Existing encrypted transport, signed dispatch binding, provider trust, cancellation/recovery, Tavily/Nemotron behavior, and Windows product behavior were not removed or weakened.
- Static inspection identified the next equivalent lower-level trust gap in `ResearchResultProtector`: client result-encryption public keys and result-decryption private keys still use direct `RSA.ImportFromPem(...)` without a dedicated shared role/size/OAEP-SHA256 capability boundary.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and recent focused RSA/secret-ordering regressions still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- `ResearchResultProtector.Protect(...)` and `Unprotect(...)` still directly import client RSA PEM and need the same lower-level role/capability policy already applied to work-item protection.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, centralize result-envelope client RSA trust: add a public-only RSA >=2048 OAEP-SHA256 client result-encryption trust primitive and a private RSA >=2048 OAEP-SHA256 result-decryption trust primitive, wire them directly into `ResearchResultProtector.Protect(...)` / `Unprotect(...)`, and add adversarial protocol tests for private/public role confusion, weak RSA, malformed/oversized/control-character PEM, and valid round-trip behavior.
