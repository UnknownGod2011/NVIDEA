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
- The remote worker now establishes a credential-free bootstrap trust boundary before constructing Token Factory or Tavily clients: bounded absolute transport root plus canonical public-only RSA client verification identity must pass first.
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
Hardened Token Factory, Tavily, Serverless, model-catalog, worker, contract-probe, and Object Storage egress against suffix lookalikes, user-info, wrong ports, unsafe redirects, cross-region storage origins, Unicode-confusable regions, and credential reads before trust validation. Object Storage disables AWSSDK redirects. Added credential-free topology/alignment, immutable-image validation, worker-public-key validation, client-signing-key validation, multiline PEM handling, and strict public-only client verification identity. Representative commits include `66181222`, `126376af`, `3efcdf95`, `abeca9f9`, `f8b9871e`, `8629150b`, `72b393c3`, `2b0a6bd1`, `36babdd9`, `0bf09acf`, `ee80126b`, `b1e6af42`.

### 2026-09-12 — Worker-side bootstrap trust boundary
Completed this run:
- Re-read the complete ledger and inspected the current worker bootstrap, deployment preflight, Token Factory environment loader, Tavily environment loader, repository tree, and recent persisted head before editing.
- Added `src/Nvidea.Core/Jobs/NebiusResearchWorkerBootstrapTrust.cs` as an explicit credential-free worker startup gate. It reads only `NVIDEA_TRANSPORT_ROOT` and `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, requires a bounded absolute Linux transport path, reuses `NebiusResearchDeploymentPreflight.ValidateClientVerificationPublicKey(...)`, and returns canonical public-only RSA verification material.
- Updated `src/Nvidea.Worker/Program.cs` so this trust gate runs immediately after command-line parsing and **before** construction of `NebiusOptions`, `TavilyOptions`, provider HTTP clients, research engine, binding waiter, or worker-private-key access. A malformed transport root/private/weak/malformed client verification key therefore fails before Token Factory, Tavily, or worker-private-key secrets are accessed.
- The worker now passes the canonicalized verification identity from the trust gate into both `ResearchDispatchBindingWaiter` and `NebiusResearchWorker`, preventing downstream code from consuming an unvalidated representation.
- Added `tests/Nvidea.Core.Tests/NebiusResearchWorkerBootstrapTrustTests.cs` with a counting environment reader. Coverage includes valid canonical bootstrap, private client key rejection, weak RSA-1024 rejection, and invalid transport-root rejection. The regressions explicitly assert that `NEBIUS_API_KEY`, `TAVILY_API_KEY`, and `NVIDEA_WORKER_PRIVATE_KEY_PEM` are not read by the credential-free trust phase.

Engineering commits before this ledger update:
- `08e9878c04ce16f7342d1864179c177414cf9c83` — add credential-free worker bootstrap trust boundary.
- `0afab00983816976d452517cc2b0eca76bf0a988` — enforce bootstrap trust before provider construction/secrets.
- `62f4bccca61f20ea1742dc0a37089b4a80ee4a2f` — focused worker bootstrap credential-read ordering regressions.

Validation / evidence:
- GitHub compare from prior ledger head `b1e6af42aae99ca1617ca9d7e52061b5d483cf88` to engineering head `62f4bccca61f20ea1742dc0a37089b4a80ee4a2f` reports **3 commits ahead / 0 behind** and exactly three intended files changed: one new Core trust component, worker bootstrap integration, and one focused test file.
- Static re-fetch of `src/Nvidea.Worker/Program.cs` before editing showed provider options were previously constructed before `NVIDEA_CLIENT_PUBLIC_KEY_PEM` was read. The new ordering intentionally reverses that trust boundary.
- Static inspection confirms the shared deployment validator rejects private client PEM, malformed RSA, and keys below 2048 bits and returns canonical SubjectPublicKeyInfo PEM.
- `dotnet --info` still returns `dotnet: command not found`; no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

Security / privacy / failure review:
- Worker startup can no longer read Token Factory/Tavily credentials before validating the public client identity used to authenticate dispatch bindings.
- Client signing authority remains local; the worker accepts only the derived public verification identity.
- Invalid transport/public trust fails before worker-private-key access as well as before provider credential access.
- Provider endpoint/redirect trust, MysteryBox-backed worker secrets, signed binding verification, encrypted transport, and existing cancellation/recovery behavior remain unchanged.
- No working functionality was removed; startup was reordered behind a stricter shared trust gate.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment. Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and newly added focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before treating generated evidence as judge-ready.
- Provider catalogs can change after a check; `/v1/models` presence does not prove quota, inference success, tool calling, context length, or every required capability. A real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior need a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- Recent RSA/public-key, PEM-ordering, and worker-bootstrap regressions are statically reviewed but remain unexecuted because `dotnet` is unavailable.
- Worker startup still relies on `NebiusOptions.FromEnvironment()` and `TavilyOptions.FromEnvironment()` directly after bootstrap trust; future work should make their environment readers injectable or compose them through a testable worker configuration loader so the full post-trust read order can be asserted end-to-end rather than only the credential-free phase.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, add a testable worker runtime configuration loader with injected environment access that composes Token Factory, Tavily, timing values, and worker-private-key loading **after** `NebiusResearchWorkerBootstrapTrust`, then add an end-to-end counting-reader regression proving invalid public bootstrap configuration prevents every provider/worker secret read while valid configuration reads secrets only after trust is established.
