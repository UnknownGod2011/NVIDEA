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
- Worker public envelope identity is centralized in `WorkerEnvelopePublicKeyTrust`: bounded PEM, public-only RSA >=2048 bits, OAEP-SHA256 encryption capability proof, canonical SubjectPublicKeyInfo output, and temporary probe zeroization. `ResearchWorkItemProtector.Protect(...)`, deployment preflight, and live worker-key loading reuse it.
- Client result-envelope encryption/decryption now has dedicated lower-level trust boundaries: `ClientResultEnvelopePublicKeyTrust` requires bounded public-only RSA >=2048 with OAEP-SHA256 encryption capability and canonical SPKI; `ClientResultEnvelopePrivateKeyTrust` requires bounded private RSA >=2048 with an OAEP-SHA256 round-trip capability proof and canonical PKCS#8. `ResearchResultProtector.Protect(...)` / `Unprotect(...)` use them directly.
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
Completed across the preceding runs:
- `ResearchWorkItemProtector.Protect(...)` now uses `WorkerEnvelopePublicKeyTrust.CreateValidatedRsa(...)` directly, so lower-level callers cannot bypass the worker public-key policy.
- Deployment preflight delegates to `WorkerEnvelopePublicKeyTrust.ValidateAndCanonicalize(...)`, so deployment/live loading/protocol encryption share the same bounded-input, public-only, RSA >=2048, OAEP-SHA256-capable identity policy.
- Live configuration persists the canonical SubjectPublicKeyInfo worker identity after validation.
- Focused regressions cover valid RSA-2048 round trip and private, weak, malformed, oversized, and embedded-control-character worker public keys.

### 2026-09-12 — Client result-envelope RSA trust
Completed in this run:
- Added `ClientResultEnvelopePublicKeyTrust` as a dedicated trust boundary for remote-result encryption identities. It rejects missing/oversized/control-character PEM, malformed/non-RSA material, RSA below 2048 bits, and any RSA PEM containing private material; it proves OAEP-SHA256 encryption capability, canonicalizes to SubjectPublicKeyInfo PEM, and zeroes temporary cryptographic probes.
- Added `ClientResultEnvelopePrivateKeyTrust` as the matching result-decryption boundary. It rejects missing/oversized/control-character PEM, malformed/non-RSA material, RSA below 2048 bits, and public-only identities; it proves usable private material with an OAEP-SHA256 encrypt/decrypt round trip, canonicalizes to PKCS#8 PEM, and zeroes probe/wrapped/unwrapped buffers.
- Replaced direct `RSA.ImportFromPem(...)` calls inside `ResearchResultProtector.Protect(...)` / `Unprotect(...)` with the two shared trust primitives. Result protection therefore fails closed on role/strength/capability errors even when callers bypass deployment/live preflight.
- Public-key validation now occurs before serializing/encrypting the result payload. Private-key validation occurs after envelope version/id/lifetime validation but before decoding and unwrapping the protected data key.
- Added `ResearchResultProtectorKeyTrustTests` with valid RSA-2048 round trip plus private/public role-confusion, weak RSA-1024, malformed PEM, >65,536-character PEM, embedded-control-character, and canonicalization coverage for both result-envelope key roles.

Engineering commits in this run before the ledger update:
- `0a09fa1b2dfed7c3da1741cdbee29ce1af4ed138` — harden client result-envelope public-key trust.
- `d8e0af3ac04a21a9ccfe804e3a6924b355be0642` — harden client result-envelope private-key trust.
- `6b4c4100fc4ea0eda1968bfbbe6f4412720b0cbb` — enforce client result-envelope RSA trust inside the protocol.
- `8a820bc61f864f1b7cc4231e0f01a299cea919b0` — add adversarial client result-envelope key-trust tests.

Validation / evidence:
- GitHub compare from prior ledger head `0a69effed8ea5b1f4e4b551548036239d9223360` to engineering head `8a820bc61f864f1b7cc4231e0f01a299cea919b0` reports **4 commits ahead / 0 behind**.
- Changed engineering scope is limited to two new trust primitives, `NebiusResearchResultProtocol.cs`, and one focused test file.
- Static re-read confirms both result-envelope protocol entry points now use the shared trust primitives rather than direct PEM imports.
- `dotnet --info` still returns `dotnet: command not found` in this execution environment, so no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- A future direct caller of `ResearchResultProtector` can no longer bypass client result-key role, minimum-strength, bounded-input, or OAEP-SHA256 capability policy.
- Private client result keys cannot be accepted by the result-encryption path; public-only keys cannot be accepted by result decryption.
- Temporary capability-test plaintext/ciphertext buffers are zeroized; actual AES result plaintext and data keys retain their existing zeroization behavior.
- Existing authenticated associated data, expiry checks, encrypted transport, signed dispatch binding, provider trust, cancellation/recovery, Tavily/Nemotron behavior, and Windows product behavior were not removed or weakened.
- Static architecture review shows the live configuration currently derives the worker-side client public identity from the same `ClientPrivateKeyPem` used for dispatch signing. That means one RSA identity is serving both signature/verification and OAEP result encryption/decryption roles; key-purpose separation should be audited and, if feasible without breaking migration, split into distinct signing and result-envelope identities.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and recent focused RSA/secret-ordering regressions still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- Client RSA key-purpose separation remains a security-hardening opportunity: current live configuration derives the result-encryption/worker-verification public identity from the same private key used for dispatch signing.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, audit and separate client RSA key purposes: introduce a distinct result-envelope client RSA key pair/configuration from the dispatch-signing identity, preserve safe migration/compatibility where necessary, thread the result public identity to the worker and result private identity to ingestion, and add tests proving signing keys cannot silently substitute for result encryption/decryption keys.
