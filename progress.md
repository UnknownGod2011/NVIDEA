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
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with retries, timeout/cancellation, structured tool calling, response-schema support, and default Nano / Super / Ultra routing for Fast / Standard / Deep tasks with environment overrides.
- Layered personal memory with privacy-aware writes, provenance, hybrid lexical/semantic/recency/importance retrieval, local Ollama embeddings, vector-space isolation, migration/re-indexing, WPF maintenance UI, and deterministic retrieval-quality fixtures.
- Tavily Search + Extract research with multi-stage planning, canonical deduplication, source quality/freshness/diversity ranking, untrusted-evidence boundaries, machine-verifiable citations, restart-safe checkpoints, and exact production endpoint trust.
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, crash/lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Native Nebius Object Storage transport and Serverless-mounted worker transport share one protected protocol. Preflight validates mount alignment, READ_WRITE transport, MysteryBox credentials, digest-pinned image, RSA identity consistency, bounded resources, and redacted fingerprints.
- Credential-bearing Token Factory, Tavily, Serverless, worker, model-catalog, contract-probe, and Object Storage paths use explicit redirect/endpoint trust boundaries.
- Token Factory requires HTTPS/443, no URI user-info, and exact `nebius.com` or genuine `*.nebius.com`; endpoint trust is validated before API-key lookup.
- Nebius Serverless requires exact `api.nebius.cloud`, HTTPS/443, and no URI user-info in production. HTTPS loopback remains test-only.
- Nebius Object Storage static-key paths share a credential-free endpoint/region policy: exact `https://storage.<region>.nebius.cloud:443/`, strict ASCII `[a-z0-9-]` region syntax, no path/query/fragment/user-info, endpoint/region binding, and AWSSDK automatic redirects disabled.
- Live-research configuration validates provider-independent topology, digest-pinned worker image, MysteryBox references, volume/root alignment, and Object Storage bucket/prefix alignment before opening either local PEM file or reading Serverless/Object Storage credentials.
- Object Storage bucket/prefix alignment consumes a narrow credential-free `NebiusObjectStorageTransportAlignment`, not credential-bearing runtime client options.
- Worker envelope public-key PEM is now read only after immutable-image/topology/alignment checks pass. Client dispatch-signing private-key PEM is read only after that worker-key boundary; its derived public key is injected and mandatory final preflight still verifies the RSA identity contract.
- Worker `NEBIUS_API_KEY` and `TAVILY_API_KEY` values are not read by the desktop live loader; only validated Nebius MysteryBox references are handled there.
- Windows voice invocation is local and review-first. Memory maintenance safely re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
- `tools/Nvidea.PersonalAiDemoEval` and `tools/Nvidea.PersonalAiAdversarialEval` provide deterministic positive/negative cross-cutting evidence.
- `tools/Nvidea.JudgingEvidenceVerifier` combines evaluator artifacts, matching Nebius live deployment evidence, and a fresh live Token Factory catalog PASS.
- `tools/Nvidea.DemoPackageValidator` plus `docs/demo-package.json` make the <=3-minute judging plan machine-checkable.
- `tools/Nvidea.NebiusModelCatalogCheck` provides captured/live zero-inference readiness checks with strict provider endpoint trust.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research stages, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, digest-pinned worker requirements, redacted deployment fingerprints, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product, memory, evaluator, and judging hardening
Added research/browser product runtimes, WPF lifecycle-aware research, restart-safe browser-goal recovery, one-shot cloud approval, local review-first voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, browser prompt-injection mutation approval hardening, unified judging evidence, deterministic demo-package validation, and adversarial validator tests.

### 2026-09-11 — Current Nemotron routing + catalog readiness
Verified current Token Factory model IDs and set Nano / Super / Ultra defaults for Fast / Standard / Deep. Added routing tests, zero-inference model-catalog readiness, strict bounded catalog parsing, SHA-256 evidence binding, fresh-live catalog requirements in the judging chain, and standalone catalog DNS/user-info/redirect credential-boundary hardening.

### 2026-09-12 — Provider / Object Storage security hardening
Completed a systematic trust/egress pass:
- Fixed Token Factory DNS suffix-lookalikes; required HTTPS/443/no user-info; moved trust validation before `NEBIUS_API_KEY` lookup.
- Added no-auto-redirect HTTP construction for desktop Token Factory/Tavily/Serverless, remote worker, model catalog, and contract probe.
- Hardened Serverless to exact `api.nebius.cloud` and Tavily to exact `api.tavily.com` over HTTPS/443 without user-info.
- Restricted Object Storage to exact regional Nebius origins; centralized endpoint/region trust; tightened region IDs to ASCII; added Unicode-confusable regressions.
- Moved Object Storage endpoint/region validation before static credential reads.
- Explicitly set `AmazonS3Config.AllowAutoRedirect = false` and added SDK-configuration regression coverage.
- Reordered live configuration so malformed deployment topology is rejected before reading the Serverless token or Object Storage static credentials.

Representative commits include `18943f9f`, `64285c0a`, `e0362868`, `15e24a26`, `7eaf0131`, `66181222`, `92ec271c`, `126376af`, `8aceba9d`, `3efcdf95`, `541d7247`, `10a84323`, `abeca9f9`, `1a1c11a2`, `034a6273`, `f8b9871e`, `061bc757`, and `eb31cb98`.

### 2026-09-12 — Credential-free topology contract + delayed signing-key access
- Introduced `NebiusObjectStorageTransportAlignment`, containing only bucket and prefix namespace identity.
- Split deployment validation into mandatory final `Validate(...)` and `ValidateCredentialFreeTopology(...)`.
- Reordered the live loader so bucket/prefix alignment is validated before `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` is requested.
- After topology passes, the loader reads/imports the client RSA private key, derives `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, creates final dispatch options, then performs the existing full live preflight.
- Removed the prior `credential-not-read` Object Storage sentinel workaround.
- Tests prove valid topology can pass without client public key; full validation still requires it; exact bucket/prefix alignment works via the narrow contract; mismatch fails without runtime Object Storage client options.

Commits: `8629150b`, `9cea0b9b`, `48068a98`, `4b048f2c`, `4381651e`.

### 2026-09-12 — Immutable image + worker public-key ordering hardening
Completed this run:
- Re-read the complete progress ledger and current live configuration/deployment-preflight paths before changing code.
- Moved digest-pinned worker-image validation into credential-free deployment preflight. Mutable tags, malformed/whitespace image references, non-64-byte digests, and non-hex digests now fail before any local PEM file access.
- `NebiusResearchLiveConfigurationLoader` now constructs topology with no worker public-key material, runs immutable-image/topology/Object-Storage namespace validation first, and only then requests `NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE`.
- After topology succeeds, the worker public key is injected into dispatch options; client signing private-key loading and the existing final RSA/full preflight remain mandatory.
- Strengthened the bucket-alignment counting-reader regression to prove a topology mismatch does not even request the worker public-key file path, client private-key file path, Serverless token, or Object Storage static credentials.
- Added a mutable-image counting-reader regression proving `registry.example/nvidea-worker:latest` fails before either PEM path or provider credential is accessed.
- Updated credential-free topology fixtures to use a digest-pinned image and added a focused test showing mutable images fail even when `WorkerPublicKeyPem` is empty.

Engineering commits before this ledger update:
- `72b393c3b98ecd3d1ac4496913417a25bc30ee0b` — fail closed on mutable worker images before key access.
- `6c13cfb20ae86385c8d9d84cf7297244f9acd787` — delay worker public key file access until topology passes.
- `86982d212901b18c47327db311ec73d7ba6089aa` — test immutable worker image in credential-free topology.
- `cec475af9499aa841824984b2ea369b6ebd0c6b2` — prove topology failures avoid worker public key file reads.

Validation / evidence:
- Static re-fetch confirms `ValidateCredentialFreeTopology` invokes digest-pinned image validation before topology/MysteryBox/volume checks.
- Static re-fetch confirms `ValidateObjectStorageAlignment(...)` runs before `NVIDEA_LIVE_WORKER_PUBLIC_KEY_PEM_FILE` and `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` are requested.
- GitHub compare from prior ledger head `4381651eee5610680248ea87646c8ddb02119413` to engineering head `cec475af9499aa841824984b2ea369b6ebd0c6b2` reports **4 commits ahead / 0 behind**, limited to two production files and two focused test files; 110 additions / 11 deletions.
- `dotnet --info` still returns `dotnet: command not found`; no compile/unit-test/WPF/Worker/tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference request was performed.

Security / privacy / failure review:
- Mutable deployment images now fail in the cheapest credential-free stage rather than after keys/tokens have already been read.
- Malformed storage topology can no longer cause either local PEM path to be requested.
- Worker public-key cryptographic semantics remain enforced by final dry-run preflight; client private/public RSA identity consistency remains mandatory.
- Provider endpoint/redirect protections remain unchanged.
- MysteryBox-backed worker secrets remain references only; plaintext worker credentials are still rejected.
- No working runtime functionality was removed; the changes only reorder validation and strengthen fail-closed deployment shape checks.

## Known Blockers / Risks
- No usable .NET 8 execution signal is available in this environment; Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before relying on generated PASS evidence.
- Provider catalogs can change after a check; the 15-minute judging gate reduces but cannot eliminate this race.
- A model appearing in `/v1/models` does not prove quota, inference success, tool calling, context length, or every required capability; a real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- New image/PEM ordering is statically verified but still needs executable regression coverage against the actual .NET runtime.
- Digest-pinned image validation now exists in deployment preflight and again in the live dry-run shape validator; this duplicate predicate should be consolidated so future rules cannot drift.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, **centralize digest-pinned worker-image validation so deployment preflight and live dry-run cannot drift, then move worker public-key RSA parsing/strength validation immediately after topology success and before client-private-key/provider-secret reads, with counting-reader regressions proving malformed worker-key material cannot cause later sensitive reads.**
