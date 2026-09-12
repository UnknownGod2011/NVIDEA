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
- Nebius Serverless lifecycle interpretation is explicitly allowlisted. Bounded `state_details` diagnostics are retained only as untrusted operator/audit evidence and never determine lifecycle transitions.
- Recognized Nebius failure codes now map through a fixed local remediation allowlist. Windows-safe `ResearchJobStatus` can expose guidance without copying provider `message` text or granting retry/resource-change authority.
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
Final deployment preflight now requires both client public identities. The result identity is public-only RSA validated through `ClientResultEnvelopePublicKeyTrust`; secret-backed result public identities are rejected, both public identities are canonicalized, and deployment preflight rejects reuse of the same RSA identity for signing/verification and result encryption.

### 2026-09-12 — Separated-key operator documentation
- Updated `docs/nebius-research-worker.md` to document the actual three-keypair protocol: worker work-item identity, client dispatch-signing identity, and distinct client result-envelope identity.
- Added safe OpenSSL RSA-3072 generation examples while retaining the code-enforced minimum of RSA-2048.
- Corrected worker configuration so `NVIDEA_CLIENT_PUBLIC_KEY_PEM` is dispatch verification only and `NVIDEA_CLIENT_RESULT_PUBLIC_KEY_PEM` is result encryption only.
- Documented the corresponding local-only private-key files: `NVIDEA_LIVE_CLIENT_PRIVATE_KEY_PEM_FILE` and `NVIDEA_LIVE_CLIENT_RESULT_PRIVATE_KEY_PEM_FILE`.
- Updated `docs/nebius-contract-probe.md` and `README.md` so live-probe requirements and setup instructions match the enforced two-client-key runtime and explicitly prohibit signing/result key reuse.

### 2026-09-13 — Serverless lifecycle contract audit
- Re-checked the current Nebius Serverless AI lifecycle contract. Documented job states are `STATE_UNSPECIFIED`, `PROVISIONING`, `STARTING`, `IMAGE_PULLING`, `RUNNING`, `COMPLETED`, `CANCELLING`, `CANCELLED`, `DELETING`, `FAILED`, and `ERROR`.
- Confirmed `NebiusServerlessJobSnapshotParser.ParseState(...)` already handled every actionable documented state conservatively.
- Replaced partial state tests with exhaustive contract coverage and fail-closed regressions for `STATE_UNSPECIFIED`, future/unknown values, blank/null values, and plausible-but-undocumented aliases such as `PENDING` and `SUCCESS`.
- Kept case/outer-whitespace tolerance without broadening accepted lifecycle vocabulary.

Engineering commit:
- `f9c9553cce535ca713953545d9d4855983ff0f95` — exhaustively cover documented Nebius job lifecycle states.

### 2026-09-13 — Bounded untrusted Serverless failure diagnostics
Completed in this run:
- Re-checked current Nebius SDK/provider metadata showing `JobStatus.state_details` carries a provider `code` and human-readable `message`.
- Added `NebiusRemoteJobDiagnostic` to snapshots without changing the existing three-field positional/deconstruction surface of `NebiusRemoteJobSnapshot`.
- `NebiusServerlessJobSnapshotParser` accepts either provider/proto JSON spelling (`stateDetails` or `state_details`) only when exactly one is present and object-shaped. Ambiguous duplicate shapes are ignored.
- Diagnostic `code` is bounded to 128 characters and `message` to 1024; blank values are ignored, non-string values are rejected, and control characters cause the diagnostic to be discarded.
- Diagnostic parsing is intentionally ancillary: it never feeds `ParseState(...)`. Unknown/future lifecycle values remain `Unknown` and are rejected before durable mutation even if a diagnostic claims success.
- For a verified `FAILED`/`ERROR` lifecycle only, safe bounded diagnostics are appended to `LastError` and `research.remote_failed` audit summaries with an explicit `Provider diagnostic (untrusted)` label.
- Unsafe, malformed, oversized, or ambiguous diagnostics degrade to the previous generic terminal-failure message rather than weakening failure handling or throwing away the authoritative lifecycle state.
- Preserved the public snapshot API after static review by making `Diagnostic` an additive init property rather than a fourth positional record component.

Engineering commits before this ledger update:
- `7b95aea7a3d331572e1c296c60374df6b8fb21b5` — surface bounded Nebius failure diagnostics.
- `432c2b345f04f62470cd3d6f27709831007d7cf2` — add adversarial/untrusted diagnostic coverage.
- `60903449a815178e09550a5f2882525fe52cf0a2` — preserve Serverless snapshot API compatibility.
- `cae356d2df69c8de787e49d2030208cfbce07b5e` — reject ambiguous duplicate diagnostic shapes in focused tests.

### 2026-09-13 — Safe fixed Serverless failure remediation
Completed in this run:
- Added `NebiusFailureRemediationPolicy`, a pure allowlist that accepts only a bounded normalized provider **code** and returns fixed local guidance. It accepts no provider message and has no retry/cancel/resubmit/resize authority.
- Seeded the allowlist with `NotEnoughResources` and `Quota`, which current Nebius first-party Serverless tooling documents as capacity and quota failure classifications respectively. Unknown/future/lookalike codes return no guidance.
- Added a compatibility parser for NVIDEA's own durable `LastError` failure-evidence prefix. It reads only the code field before the provider message; provider message text is neither copied nor consulted for classification.
- `ResearchJobStatus` now exposes optional `FailureRecoveryGuidance` only for durable `AgentJobState.Failed` records whose remote provenance is specifically `RemoteFailed`.
- The existing Windows research UI already renders `ResearchJobStatus.DisplayText`; recognized remote failures therefore receive fixed actionable guidance without exposing raw provider text. Local failures and unknown remote codes remain the generic `Research failed` UI.
- Added adversarial tests for normalization, unknown/lookalike/control-character/oversized codes, message-based spoof attempts, fixed-guidance projection, raw-message non-disclosure, and prevention of local-failure spoofing.
- Static review rejected an intermediate design that persisted a free-form guidance string in `AgentJobRecord`; those two JobContracts commits were reversed in-run and have **no net diff**, avoiding a new untrusted durable UI-text surface.

Net meaningful files changed from the previous ledger head:
- `src/Nvidea.Core/Jobs/NebiusFailureRemediationPolicy.cs` — fixed allowlist + durable-evidence compatibility classification.
- `src/Nvidea.Core/Jobs/ResearchJobStatus.cs` — privacy-safe remediation projection into product status/display text.
- `tests/Nvidea.Core.Tests/NebiusFailureRemediationPolicyTests.cs` — focused/adversarial policy and UI-projection coverage.

Key persisted commits before this ledger update:
- `382ab8da0ae6c198a1b263a5c9bfef213d117afb` — introduce the fixed remediation policy.
- `6ae4b1cae44a920e3ebbc171147628239777faec` — add durable failure-evidence classification.
- `848326d9a4ac0310992b4f88e158fb383c661223` / `590e8432dc716db2637d7adaf8d1904e01600dad` — project and surface safe remediation through research status.
- `7f7e8ddcc9df67ce56e7663892a84d1d5b563c1b` — add policy/adversarial tests.
- `ad29f0ddd4b31aa475afb41f95ed702a52dcefdb` — align policy comments and conservative parsing with the final architecture.

Validation / evidence:
- Current Nebius SDK metadata was checked on 2026-09-13 and confirms `JobStatus.state_details` / `JobStateDetails` expose `code` and `message`.
- Current Nebius first-party `nebius-physical-ai` Serverless tooling was inspected read-only and documents capacity failures as `NotEnoughResources` and quota failures as `Quota`; the local policy does not infer any unverified additional codes.
- Added focused tests for both JSON spellings, bounded valid diagnostics, oversized fields, control-character injection, duplicate-shape ambiguity, generic fallback, audit/LastError surfacing, unknown-state fail-closed behavior, remediation allowlisting, message spoofing, and privacy-safe product projection.
- Static review confirms provider diagnostic text is not consulted by lifecycle parsing, remote provenance verification, cancellation semantics, result ingestion, durable transition selection, or remediation selection.
- GitHub comparison from `95d8f59c2ff1b961431e995e2478a95b693f713c` before this ledger commit is 8 commits ahead / 0 behind, with a net diff limited to the three files listed above.
- `dotnet --info` and `csc` remain unavailable in this execution environment. **No compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Dispatch-signing and result-decryption private material remain client-local. Worker/deployment plaintext receives only validated canonical public identities.
- Final deployment preflight, live loader, and worker bootstrap independently require separated client key purposes.
- Worker work-item private material remains a separate MysteryBox-backed key and is never included in source or example values.
- Serverless lifecycle interpretation remains allowlisted rather than heuristic: undocumented/future provider states are Unknown and cannot mutate durable local job state.
- Provider `state_details` is treated as untrusted evidence only: bounded, control-character-free, non-authoritative, and ignored when malformed or ambiguous.
- Fixed remediation is selected only from an allowlisted provider code and is product guidance only. Provider `message` text cannot select guidance, authorize a retry, choose another resource shape/project, or mutate durable state.
- Windows product projection never copies raw provider message text into `ResearchJobStatus`; recognized guidance is locally authored and unknown codes fail to generic UI.
- `STATE_UNSPECIFIED` remains Unknown, while `ERROR` remains terminal Failed in accordance with the current Nebius contract.
- Existing authenticated associated data, encrypted transport, signed binding, endpoint trust, cancellation/recovery, exact-once ingestion, Tavily/Nemotron behavior, browser safety, and Windows UX were not removed or weakened.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove this fallback only after executable migration coverage exists.

## Known Blockers / Risks
- No usable .NET 8 executable exists in this execution environment, so all new and existing .NET/WPF/Worker code still needs a real restore/build/test/run.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real `embeddinggemma` ranking quality still needs a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- Remediation currently rehydrates the bounded code from NVIDEA's own formatted durable `LastError` for backward compatibility. A future migration should persist a bounded structured diagnostic code alongside remote provenance so product projection no longer depends on an internal display/evidence serialization format.
- Only `NotEnoughResources` and `Quota` are currently allowlisted because those were the current failure classifications verified in first-party Nebius Serverless tooling during this run. Do not add timeout/start/container classifications until current provider evidence or a real contract capture confirms their exact codes.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, migrate the recognized remote failure **code** into a bounded structured field in durable remote provenance (with backward-compatible migration from existing `LastError` evidence), then make `ResearchJobStatus` classify that structured field directly instead of depending on formatted error text.