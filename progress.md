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
- NVIDIA Nemotron through Nebius Token Factory with retries, timeout/cancellation, structured tool calling, response-schema support, and Nano / Super / Ultra routing for Fast / Standard / Deep tasks with environment overrides.
- Layered personal memory with privacy-aware writes, provenance, lexical/semantic/recency/importance retrieval, local Ollama embeddings, vector-space isolation, migration/re-indexing, maintenance UI, and deterministic retrieval-quality fixtures.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, quality/freshness/diversity ranking, untrusted-evidence boundaries, citations/provenance, restart-safe checkpoints, and exact production endpoint trust.
- Safe browser agent with persistent Chromium state, popup/new-tab tracking, plan/act/observe/verify, prompt-injection detection, consequential-action gates, durable download quarantine, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default, durable job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Remote research uses encrypted opaque work items, signed Nebius resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Native Nebius Object Storage transport and Serverless-mounted worker transport share one protected protocol. Preflight validates mount alignment, READ_WRITE transport, MysteryBox references, immutable worker image, RSA identities, bounded resources, and redacted fingerprints.
- Token Factory, Tavily, Serverless, remote worker, model-catalog, contract-probe, and Object Storage credential-bearing paths use explicit endpoint/redirect trust boundaries.
- Token Factory requires HTTPS/443, no URI user-info, and exact `nebius.com` or genuine `*.nebius.com`; endpoint trust is established before API-key lookup.
- Nebius Serverless requires exact `api.nebius.cloud`, HTTPS/443, and no URI user-info in production. HTTPS loopback remains test-only.
- Nebius Object Storage requires exact `https://storage.<region>.nebius.cloud:443/`, strict ASCII region syntax, no path/query/fragment/user-info, endpoint/region binding, and AWSSDK automatic redirects disabled.
- Live-research configuration validates provider-independent topology, immutable worker image, MysteryBox references, volume/root alignment, and Object Storage bucket/prefix alignment before opening local PEM files or reading Serverless/Object Storage credentials.
- Object Storage bucket/prefix alignment consumes a credential-free `NebiusObjectStorageTransportAlignment`, not credential-bearing runtime options.
- Worker envelope public-key PEM is read only after topology/alignment succeeds and is immediately validated as public-only RSA >=2048 bits before the client signing key or provider credentials are accessed.
- Client dispatch-signing private-key PEM is read only after the worker-key trust boundary and is now immediately validated as usable RSA private material >=2048 bits with an actual signature operation before any Serverless/Object Storage credential is accessed. The canonical public identity derived by that shared validator is injected into worker configuration, and final dry-run reuses the same private-key validator before exact public/private identity comparison.
- Worker `NEBIUS_API_KEY` and `TAVILY_API_KEY` values are not read by the desktop live loader; only validated Nebius MysteryBox references are handled there.
- Windows voice invocation is local and review-first. Memory maintenance re-indexes stale/missing embeddings with privacy-safe previews and explicit Sensitive/Restricted opt-ins.
- `tools/Nvidea.PersonalAiDemoEval`, `tools/Nvidea.PersonalAiAdversarialEval`, `tools/Nvidea.JudgingEvidenceVerifier`, `tools/Nvidea.DemoPackageValidator`, and `tools/Nvidea.NebiusModelCatalogCheck` provide deterministic/local judging and readiness evidence paths.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence quality/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, redacted deployment fingerprints, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-11 — Product, evaluator, and judging hardening
Added research/browser product runtimes, WPF lifecycle-aware research, restart-safe browser-goal recovery, one-shot cloud approval, local review-first voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, prompt-injection mutation approval hardening, unified judging evidence, deterministic demo-package validation, and adversarial validator tests.

### 2026-09-11 — Current Nemotron routing + catalog readiness
Verified current Token Factory model IDs and set Nano / Super / Ultra defaults for Fast / Standard / Deep. Added routing tests, zero-inference model-catalog readiness, strict bounded catalog parsing, SHA-256 evidence binding, fresh-live catalog requirements, and catalog endpoint/redirect credential-boundary hardening.

### 2026-09-12 — Provider / Object Storage security hardening
- Fixed Token Factory DNS suffix-lookalikes; required HTTPS/443/no user-info; moved trust validation before API-key lookup.
- Added no-auto-redirect HTTP construction for desktop Token Factory/Tavily/Serverless, remote worker, model catalog, and contract probe.
- Hardened Serverless to exact `api.nebius.cloud` and Tavily to exact `api.tavily.com` over HTTPS/443 without user-info.
- Restricted Object Storage to exact regional Nebius origins; centralized endpoint/region trust; tightened region IDs to ASCII; added Unicode-confusable regressions.
- Moved Object Storage endpoint/region validation before static credential reads.
- Explicitly set `AmazonS3Config.AllowAutoRedirect = false` and added SDK-configuration regression coverage.
- Reordered live configuration so malformed deployment topology fails before the Serverless token/Object Storage static credentials are read.
Representative commits: `18943f9f`, `64285c0a`, `e0362868`, `15e24a26`, `7eaf0131`, `66181222`, `92ec271c`, `126376af`, `8aceba9d`, `3efcdf95`, `541d7247`, `10a84323`, `abeca9f9`, `1a1c11a2`, `034a6273`, `f8b9871e`, `061bc757`, `eb31cb98`.

### 2026-09-12 — Credential-free topology + PEM read ordering
- Introduced `NebiusObjectStorageTransportAlignment` and split mandatory final validation from credential-free topology validation.
- Bucket/prefix alignment now completes before the client private-key path is requested.
- Immutable worker-image validation moved into credential-free topology.
- Worker public-key file access moved after immutable-image/topology/alignment checks.
- Counting-reader regressions prove storage mismatch/mutable image cannot access either PEM path or provider credentials.
Commits: `8629150b`, `9cea0b9b`, `48068a98`, `4b048f2c`, `72b393c3`, `6c13cfb2`, `86982d21`, `cec475af`, `b284a633`.

### 2026-09-12 — Shared worker trust boundary + multiline PEM repair
Completed this run:
- Re-read the complete ledger, recent commits, deployment preflight, live loader, live dry-run, and focused tests before editing.
- Centralized digest-pinned worker-image validation: `NebiusResearchLiveDryRunPreflight` now reuses `NebiusResearchDeploymentPreflight.ValidateDigestPinnedWorkerImage(...)`; the duplicate image predicate was removed.
- Added shared `ValidateWorkerPublicKey(...)` enforcing bounded public-only RSA PEM, valid RSA parsing, and >=2048-bit strength.
- Live configuration validates the worker public key immediately after topology succeeds and the worker PEM is read, before requesting the client private-key path, Serverless bearer token, or Object Storage static keys.
- Live dry-run now reuses the same worker public-key validator instead of maintaining a second RSA-public-key policy.
- Added `NebiusResearchWorkerTrustOrderingTests`: mutable/invalid digest checks, private/weak worker-key rejection, and a counting-reader regression proving malformed worker key material cannot trigger later sensitive reads.
- During static review, found a pre-existing live-path blocker: generic `char.IsControl` PEM bounds checks rejected normal `\r`/`\n` line breaks emitted by .NET PEM exporters. Worker and client PEM validation now allow canonical CR/LF while still rejecting other control characters.
- Repaired the existing live dry-run positive fixture: it now explicitly exercises multi-line worker/client PEM and supplies a valid MysteryBox SecretId+VersionId pair for the worker private-key reference.

Engineering commits before this ledger update:
- `2b0a6bd1aedde6dea57f94def5ebfd33dc314d0f` — centralize live worker trust validation.
- `ca749767e79feb9c2b7d739fa6b1ed1aae29126e` — reuse shared worker trust preflight.
- `1e08a3c2aa36faa1e765700e9c57369bb35fc4eb` — validate worker public key before later secrets.
- `c744478e4802230bc42d051c44137aa84782334b` — focused worker trust/order regressions.
- `8fb908a1d98f3cbd65d038680618de50ece47317` — allow canonical PEM line breaks in worker trust validation.
- `7b64479f03fe466fbe28fd194a27ceb0034b9490` — accept canonical PEM line breaks in live dry-run signing validation.
- `b475319ee51c888d59ea68ac3d3e9762de6ea42a` — repair live preflight PEM regression fixture.

Validation / evidence:
- Static re-fetch confirms the live loader calls `ValidateWorkerPublicKey(...)` before `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE`, `NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN`, `NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID`, and `NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY` are requested.
- Static re-fetch confirms live dry-run delegates immutable-image and worker-key policy to deployment preflight rather than duplicating either predicate.
- GitHub compare from prior ledger head `b284a633718e5efee91f868569807ae4d3bcbf7f` to engineering head `b475319ee51c888d59ea68ac3d3e9762de6ea42a` reports **7 commits ahead / 0 behind**, affecting exactly five intended files; 175 additions / 47 deletions.
- `dotnet --info` still returns `dotnet: command not found`; no compile/unit-test/WPF/Worker/tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture a green status.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference request was performed.

Security / privacy / failure review:
- Mutable/malformed worker images and malformed/private/weak worker public keys now fail before later sensitive material is read.
- Canonical PEM line endings are accepted, restoring intended valid RSA configuration without allowing arbitrary control characters.
- Provider endpoint/redirect protections, MysteryBox reference-only worker credentials, storage namespace alignment, and final client public/private identity checks remain intact.
- No working runtime functionality was removed; duplicated policy was replaced with a single stricter shared path.

### 2026-09-12 — Shared client signing trust before provider credentials
Completed this run:
- Re-read the complete ledger and the live configuration / dry-run / credential-ordering implementation before changing code.
- Confirmed executable validation is still unavailable locally: `dotnet --info` returns `dotnet: command not found`.
- Centralized client dispatch-signing private-key trust in `NebiusResearchLiveDryRunPreflight.ValidateAndDeriveClientPublicKey(...)`.
- The shared validator now rejects missing/invalid PEM, RSA keys below 2048 bits, and public-only RSA material; it proves actual private signing capability with the same SHA-256/PKCS#1 operation used by the prior final preflight and only then returns the canonical SubjectPublicKeyInfo PEM.
- `NebiusResearchLiveConfigurationLoader` now calls that shared validator immediately after reading the client private-key PEM and **before** reading `NVIDEA_LIVE_SERVERLESS_ACCESS_TOKEN`, `NVIDEA_LIVE_OBJECT_STORAGE_ACCESS_KEY_ID`, or `NVIDEA_LIVE_OBJECT_STORAGE_SECRET_ACCESS_KEY`.
- The loader injects the canonical public identity returned by the validated private-key path, removing its weaker import-only RSA block.
- Final live dry-run reuses the same client-private-key validator and preserves fixed-time comparison between the derived public identity and the worker-configured client verification identity.
- Added `NebiusResearchClientSigningTrustOrderingTests` covering a valid 2048-bit signing identity plus public-only, weak 1024-bit, and malformed client PEMs. Counting-reader assertions prove each invalid client identity fails after its PEM path is read but before any Serverless/Object Storage credential variable is accessed.

Engineering commits before this ledger update:
- `36babdd9e23c0955d02a364cde99a8638626d2b8` — centralize client signing-key validation in live dry-run.
- `c8731d69079c9f57ad223f810e9d407f575f7e31` — validate client signing identity before provider credentials.
- `0600a65d4e28fae1d9a482fc6a11f94b01684e84` — focused client signing trust / credential-ordering coverage.

Validation / evidence:
- Static re-fetch confirms the loader invokes `ValidateAndDeriveClientPublicKey(...)` immediately after reading `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` and before requesting the Serverless token or Object Storage static credentials.
- Static re-fetch confirms final dry-run delegates client private-key parsing, >=2048-bit enforcement, and private signing-capability proof to the same shared validator before performing the existing identity comparison.
- GitHub compare from prior ledger head `a38366278e0eb8c18ad23f0d3143d77a047f4b27` to engineering head `0600a65d4e28fae1d9a482fc6a11f94b01684e84` reports **3 commits ahead / 0 behind**, limited to two production files and one focused test file; 184 additions / 51 deletions.
- `dotnet --info` returned `dotnet: command not found`; no compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.
- No GitHub Actions workflow was triggered merely to manufacture validation.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference request was performed.

Security / privacy / failure review:
- Malformed, weak, or public-only client signing material now fails before provider credentials are even read from the environment.
- The standard loader path always sends only a derived public identity into worker plaintext configuration; the local private signing PEM remains local.
- Provider endpoint/redirect trust, topology gating, worker-key trust, MysteryBox reference-only worker secrets, and final fixed-time identity consistency remain intact.
- No working runtime functionality was deleted; a weaker duplicated import path was replaced by the stronger existing signing-capability policy.

## Known Blockers / Risks
- No usable .NET 8 executable is available in this environment; Core/WPF/Worker code, XAML, tests, evaluator tools, evidence verifier, demo validator, catalog checker, and focused tests still require a real restore/build/run.
- Real Windows/.NET 8 restore/build/run remains mandatory before relying on generated PASS evidence.
- Provider catalogs can change after a check; the 15-minute judging gate reduces but cannot eliminate this race.
- A model appearing in `/v1/models` does not prove quota, inference success, tool calling, context length, or every required capability; a real inference smoke test remains necessary.
- WPF maintenance/voice/readiness bindings and Windows-specific behavior require a real Windows execution pass.
- Prompt-injection detection remains heuristic; capability gates and approval boundaries remain required defense-in-depth.
- Real `embeddinggemma` semantic quality/ranking calibration still requires a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless access token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source`/`SourcePath` still requires a real job.
- New worker/client trust and PEM-line-ending regressions are statically reviewed but still need executable .NET verification.
- The normal live loader derives a public-only client verification key, but the lower-level dry-run API currently imports any RSA PEM supplied in `NVIDEA_CLIENT_PUBLIC_KEY_PEM` and does not explicitly reject private-key PEM input. A manual caller could therefore accidentally place private signing material into plaintext worker configuration even though the standard loader path is safe.

## Single Best Next Task
First obtain a .NET 8-capable Windows execution signal and restore/build `Nvidea.Core`, `Nvidea.Windows`, `Nvidea.Worker`, all evaluator/evidence tools, and focused tests; fix every compile/runtime defect before treating evidence as judge-ready. If executable validation remains unavailable, **centralize client verification-public-key validation as bounded public-only RSA >=2048 bits, reuse it in deployment/live preflight, and add a regression proving a manually supplied private PEM under `NVIDEA_CLIENT_PUBLIC_KEY_PEM` fails closed before it can become plaintext worker configuration.**
