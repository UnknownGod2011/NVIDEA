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
- `NVIDEA_CLIENT_PUBLIC_KEY_PEM` is now a strict public-only RSA >=2048-bit verification identity. Final deployment rejects private-key PEM and rejects MysteryBox-backed use of this non-secret verification identity; live dry-run reuses the same shared validator.
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

### 2026-09-12 — Provider and Object Storage trust hardening
Hardened Token Factory, Tavily, Serverless, model-catalog, worker, contract-probe, and Object Storage egress against suffix lookalikes, user-info, wrong ports, unsafe redirects, cross-region storage origins, Unicode-confusable regions, and credential reads before trust validation. Object Storage now disables AWSSDK automatic redirects. Representative commits: `18943f9f`, `64285c0a`, `e0362868`, `15e24a26`, `7eaf0131`, `66181222`, `92ec271c`, `126376af`, `8aceba9d`, `3efcdf95`, `541d7247`, `10a84323`, `abeca9f9`, `1a1c11a2`, `034a6273`, `f8b9871e`, `061bc757`, `eb31cb98`.

### 2026-09-12 — Credential-free topology and key-read ordering
Introduced credential-free Object Storage transport alignment, moved immutable-image/topology/alignment validation before local PEM reads, validated worker public RSA immediately after topology, repaired canonical multiline PEM handling, then centralized client signing-private-key validation and moved it before provider credential reads. Counting-reader tests cover malformed topology/images/worker keys/client signing keys. Representative commits: `8629150b`, `9cea0b9b`, `48068a98`, `4b048f2c`, `72b393c3`, `6c13cfb2`, `86982d21`, `cec475af`, `2b0a6bd1`, `ca749767`, `1e08a3c2`, `c744478e`, `8fb908a1`, `7b64479f`, `b475319e`, `36babdd9`, `c8731d69`, `0600a65d`, `66512d74`.

### 2026-09-12 — Public-only client verification identity
Completed this run:
- Re-read the complete ledger and inspected recent commits plus deployment/live preflight and focused tests before editing.
- Added shared `NebiusResearchDeploymentPreflight.ValidateClientVerificationPublicKey(...)` enforcing bounded multiline PEM, explicit rejection of any `PRIVATE KEY` material, valid RSA parsing, and >=2048-bit key strength. It returns canonical SubjectPublicKeyInfo PEM.
- Final deployment preflight now requires `NVIDEA_CLIENT_PUBLIC_KEY_PEM` as validated plaintext public-only RSA material and explicitly rejects placing this public verification identity behind a MysteryBox secret reference.
- Live dry-run now delegates verification-key parsing/strength/public-only policy to the same shared validator before fixed-time comparison with the public identity derived from the local client signing key.
- Added deployment-preflight regressions for private, weak 1024-bit, malformed, and secret-backed client verification identities plus canonical public-key acceptance.
- Added a live-dry-run regression where the **same** client private key is supplied both as signing material and as `NVIDEA_CLIENT_PUBLIC_KEY_PEM`; the configuration now fails specifically on the public-only boundary even though the cryptographic identities otherwise match.
- Repaired stale deployment-preflight fixtures discovered during review: the valid fixture now uses a digest-pinned worker image and a real RSA-2048 public verification key instead of placeholders that would contradict current production policy.

Engineering commits before this ledger update:
- `0bf09acf8bb8ef465fe49b5a72c65185a907696b` — enforce public-only client verification identity.
- `180d184de20a9ae0afca96bb3ec898a2bf068eef` — reuse strict client verification-key trust in live dry-run.
- `1426eddd569d9ec54849c607e989bace1b402546` — focused deployment-preflight regressions and valid-fixture repair.
- `ee80126bd39ee52ad9eba6145c1967a69a6d0029` — reject private client signing key in worker verification configuration even when identity matches.

Validation / evidence:
- Static re-fetch confirms final deployment validation now requires plaintext `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, rejects a secret-backed version of that public identity, and calls the shared public-only RSA validator.
- Static re-fetch confirms live dry-run calls the same shared public-only validator before canonical SPKI comparison.
- GitHub compare from prior ledger head `66512d74746f8971e3b81ad600ece2fd65c970ac` to engineering head `ee80126bd39ee52ad9eba6145c1967a69a6d0029` reports **4 commits ahead / 0 behind**.
- `dotnet --info` still returns `dotnet: command not found`; no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- A lower-level/manual caller can no longer accidentally place the client signing private key into plaintext worker configuration under `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, including the dangerous case where it is the exact matching signing identity.
- Verification identity is explicitly treated as public configuration; private signing authority stays local and worker provider credentials stay MysteryBox-backed.
- Existing fixed-time public/private identity consistency, topology gating, endpoint/redirect trust, worker-key trust, and provider credential-read ordering remain intact.
- No working runtime functionality was removed; the weaker duplicate verification-key import path was replaced by the stricter shared role validator.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and all newly added focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change after a check; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Recent RSA/public-key and PEM-ordering regressions are statically reviewed but remain unexecuted because `dotnet` is unavailable.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, inspect the remote worker's ingestion of `NVIDEA_CLIENT_PUBLIC_KEY_PEM` and add defense-in-depth validation there so the worker independently rejects private, weak, or malformed verification-key material even if deployment preflight is bypassed by a future caller.
