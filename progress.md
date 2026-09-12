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
- Live-research configuration validates provider-independent topology, MysteryBox references, volume/root alignment, and Object Storage bucket/prefix alignment before Serverless bearer-token or Object Storage static-key reads.
- Object Storage bucket/prefix alignment now consumes a narrow credential-free `NebiusObjectStorageTransportAlignment`, not the credential-bearing runtime client options record.
- Client dispatch-signing private-key PEM is now read only after all credential-free topology/alignment checks pass; its derived public key is then injected and the mandatory full preflight still verifies the final identity contract.
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

Representative commits through this point include `18943f9f`, `64285c0a`, `e0362868`, `15e24a26`, `7eaf0131`, `66181222`, `92ec271c`, `126376af`, `8aceba9d`, `3efcdf95`, `541d7247`, `10a84323`, `abeca9f9`, `1a1c11a2`, `034a6273`, `f8b9871e`, `061bc757`, and `eb31cb98`.

### 2026-09-12 — Credential-free topology contract + delayed signing-key access
Completed this run:
- Re-read the full progress ledger and the current live configuration/preflight paths before changing code.
- Introduced `NebiusObjectStorageTransportAlignment`, containing only bucket and prefix namespace identity. `ValidateObjectStorageAlignment` can now prove native-client/Serverless-mounted namespace equality without accepting endpoint credentials or any other runtime client secrets.
- Kept the existing `NebiusObjectStorageClientOptions` overload only as a compatibility adapter; it immediately discards secret fields and delegates to the narrow alignment contract.
- Split deployment validation into mandatory final `Validate(...)` and `ValidateCredentialFreeTopology(...)`. The latter verifies transport root, exact mounted READ_WRITE volume, required MysteryBox references, and plaintext-secret exclusions while intentionally deferring only the client-public-key requirement.
- Reordered `NebiusResearchLiveConfigurationLoader.Load` so topology dispatch is built without `NVIDEA_CLIENT_PUBLIC_KEY_PEM` and bucket/prefix alignment is validated before `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` is requested.
- After topology passes, the loader reads/imports the client RSA private key, derives `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, creates the final dispatch options, then performs the existing full live preflight. The derived-public-key identity contract is therefore preserved rather than weakened.
- Removed the prior `credential-not-read` Object Storage sentinel workaround entirely.
- Strengthened `InvalidTransportBucketAlignment...` so it now proves a bucket mismatch prevents reads of the client private-key path, Serverless token, Object Storage access key, and Object Storage secret key.
- Added `NebiusResearchCredentialFreeTopologyTests` proving: valid topology can pass without client public key; full validation still rejects its absence; exact bucket/prefix alignment succeeds using the narrow contract; mismatch fails without constructing runtime Object Storage client options.

Engineering commits before this ledger update:
- `8629150bb040ad33af1dc79580b5aa5c47ccbfcb` — refactor credential-free research topology validation.
- `9cea0b9b9cbf2f27148f1572cf9ba1ab8dbf4a46` — delay client signing key until topology passes.
- `48068a9863c8464a390836a40659c4e5c1c57d7e` — cover client signing key behind topology gate.
- `4b048f2c0b8a29131bd54716040f248552649a56` — test credential-free topology contract.

Validation / evidence:
- Static re-fetch confirms the live loader validates `NebiusObjectStorageTransportAlignment` before `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE`, `NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN`, `NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID`, or `NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY` are requested.
- Static re-fetch confirms final dispatch options receive the public key derived from the client private key before `NebiusResearchLivePreflightReporter.ValidateAndBuild` executes.
- Search finds no remaining `credential-not-read` sentinel.
- GitHub compare from prior ledger head `eb31cb98b56c0b34db368f281c05699adb2956ad` to engineering head `4b048f2c0b8a29131bd54716040f248552649a56` reports **4 commits ahead / 0 behind**, limited to two production files and two focused test files; 197 additions / 57 deletions.
- `dotnet --info` still returns `dotnet: command not found`; no compile/unit-test/WPF/Worker/tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference request was performed.

Security / privacy / failure review:
- Provider endpoint/redirect protections remain unchanged.
- Malformed deployment topology can no longer cause the client signing private-key file to be read merely to derive an identity that will never be used.
- Alignment code is structurally unable to inspect Object Storage static credentials when called through the new narrow contract.
- Required worker credentials remain MysteryBox references and plaintext worker credentials are still rejected in both topology and final validation.
- Final validation still requires the derived client public key, so delaying private-key file access does not remove authoritative dispatch-binding verification.
- Worker public-key PEM is still read before topology alignment because it is public verification/envelope material rather than a secret; malformed PEM semantics remain covered by the later full preflight.

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
- New topology/signing-key ordering is statically verified but still needs executable regression coverage against the actual .NET runtime.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, audit the **worker public-key and deployment-image trust ordering**: move any cryptographic file parsing or image-specific validation that can safely occur after purely textual deployment-shape checks, add counting/file-access regressions for malformed topology, and preserve the mandatory digest-pinned image + RSA identity checks in final preflight.
