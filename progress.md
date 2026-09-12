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
- .NET 8 core in `src/Nvidea.Core`, WPF Windows host in `src/Nvidea.Windows`, deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with retries, cancellation/timeouts, structured tool calling, response-schema support, and Nano/Super/Ultra routing.
- Layered personal memory with privacy-aware writes, provenance, recency/importance/semantic retrieval, local Ollama embeddings, migration/re-indexing, and maintenance UX.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, evidence quality/freshness/diversity ranking, citations/provenance, resumable checkpoints, and explicit untrusted-evidence handling.
- Safe Playwright browser agent with persistent Chromium state, popup tracking, plan-act-observe-verify, prompt-injection detection, consequential-action approvals, quarantined downloads, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default; job state uses durable CAS, leases, and hash-chained/segmented audit.
- Remote research uses encrypted opaque work items, signed resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Nebius Object Storage + Serverless-mounted worker transport share one protected protocol. Preflight validates mount alignment, READ_WRITE transport, MysteryBox references, immutable worker image, bounded resources, and RSA identities.
- Token Factory, Tavily, Serverless, Object Storage, worker, model-catalog, and contract-probe credential paths have explicit endpoint/redirect trust boundaries.
- Worker envelope public/private keys and client result-envelope public/private keys use dedicated trust primitives: bounded PEM, RSA >=2048, strict public/private role separation, OAEP-SHA256 capability proofs, canonicalization, and temporary-buffer zeroization.
- Client dispatch-signing material is private RSA >=2048; worker verification material is public-only RSA >=2048.
- **Client RSA purposes are separated end-to-end.** Dispatch signing/verification uses one RSA identity; result-envelope encryption/decryption uses a second. Live client configuration requires distinct private keys; worker configuration receives only their distinct public halves.
- `NebiusResearchWorkerRuntimeConfiguration` is the single worker environment boundary. Credential-free topology/model/timing validation happens before worker-private-key and provider-secret reads.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product, evaluator, provider, and protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role validation, protocol-level worker/client envelope trust, and deployment/protocol policy reuse.

### 2026-09-12 — Client RSA key-purpose separation
Separated dispatch signing/verification from result encryption/decryption across live configuration, runtime composition, worker bootstrap, worker execution, contract probe, and focused fixtures. Worker bootstrap rejects identical public identities before any worker/provider secret read; live composition rejects identical private identities before Serverless/Object Storage credential reads.

### 2026-09-12 — Raw deployment preflight key-purpose enforcement
Completed in this run:
- Added `NebiusResearchDeploymentPreflight.ClientResultPublicKeyEnvironmentVariable` for `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`.
- Final deployment preflight now requires both client public identities, not just dispatch verification.
- The result identity must be plaintext public-only RSA and is validated through the existing `ClientResultEnvelopePublicKeyTrust` OAEP-SHA256 policy.
- Secret-backed result public identities are rejected, matching the signing public identity policy.
- Both public identities are canonicalized and deployment preflight rejects reuse of the same RSA identity for signing/verification and result encryption.
- `ValidateCredentialFreeTopology(...)` intentionally remains credential/key-identity independent so early topology checks can still run before client key loading.
- Added direct raw-`NebiusResearchDispatchOptions` regressions for distinct-key success, missing result identity, signing/result identity reuse, private material in the result-public slot, and secret-backed result identity.
- Repaired the older deployment-preflight fixture to construct two distinct RSA-2048 client public identities so its existing assertions keep testing their intended invariants under the new contract.

Engineering commits in this run before the ledger update:
- `3b1b222dcd0f1546a35f967bb0cb11f9a9f56960` — enforce separated client result identity in deployment preflight.
- `58193802d9fb404912fb7459fc9fe37bc567b326` — cover deployment client key-purpose separation.
- `5bd88d30e692c360612e5b44c75238538b02c271` — update deployment-preflight fixtures for separated client identities.

Validation / evidence:
- GitHub compare from prior ledger head `958810191361556f53b3988163561dcdfe51c946` to engineering head `5bd88d30e692c360612e5b44c75238538b02c271` reports **3 commits ahead / 0 behind**.
- Diff scope is exactly three intended files: deployment preflight, one new focused regression file, and the existing preflight fixture.
- Static review confirms raw final deployment validation can no longer accept a worker configuration missing `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`.
- Static review confirms canonical signing and result public identities are compared after role-specific validation, so alternate PEM formatting cannot bypass distinct-key enforcement.
- Static regression review found and repaired the stale one-key preflight fixture introduced by tightening the contract.
- `dotnet --info` still returns `dotnet: command not found` in this execution environment. **No compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Dispatch-signing and result-decryption private material remain client-local. Worker/deployment plaintext receives only validated canonical public identities.
- Final deployment preflight, live loader, and worker bootstrap now all independently require separated client key purposes, reducing blast radius and preventing a future raw-dispatch caller from bypassing the live composition policy.
- Result public validation reuses the protocol trust primitive, avoiding drift in PEM bounds, key size, private-material rejection, and OAEP-SHA256 capability requirements.
- Existing authenticated associated data, encrypted transport, signed binding, endpoint trust, cancellation/recovery, exact-once ingestion, Tavily/Nemotron behavior, browser safety, and Windows UX were not removed or weakened.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove this fallback only after executable migration coverage exists.

## Known Blockers / Risks
- No usable .NET 8 executable exists in this execution environment, so all new and existing .NET/WPF/Worker code still needs a real restore/build/test/run.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- Documentation still contains parts of the former single-client-key setup. `README.md`, `docs/nebius-contract-probe.md`, and `docs/nebius-research-worker.md` need explicit generation/configuration instructions for both client RSA keypairs and the new result-public environment contract.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real `embeddinggemma` ranking quality still needs a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by the two-key migration. If executable validation remains unavailable, update `README.md`, `docs/nebius-contract-probe.md`, and `docs/nebius-research-worker.md` to document secure generation/storage of two distinct client RSA-2048+ keypairs, `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE`, `NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE`, `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, and `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM`, including the explicit prohibition on key reuse.
