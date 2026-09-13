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
- Layered personal memory with privacy-aware writes, provenance, semantic/recency/importance retrieval, local Ollama embeddings, migration/re-indexing, and maintenance UX.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, evidence quality/freshness/diversity ranking, citations/provenance, resumable checkpoints, and explicit untrusted-evidence handling.
- Safe Playwright browser agent with persistent Chromium state, popup tracking, plan-act-observe-verify, prompt-injection detection, consequential-action approvals, quarantined downloads, emergency stop, and crash recovery.
- Protected local state uses Windows CurrentUser DPAPI by default; job state uses durable CAS, leases, and hash-chained/segmented audit.
- Remote research uses encrypted opaque work items, signed resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, and race-safe cleanup.
- Nebius Object Storage + Serverless-mounted worker transport share one protected protocol. Preflight validates mount alignment, READ_WRITE transport, MysteryBox references, immutable worker image, bounded resources, and RSA identities.
- Worker envelope keys and client result-envelope keys use bounded PEM, RSA >=2048, strict role separation, OAEP-SHA256 capability proofs, canonicalization, and temporary-buffer zeroization. Dispatch-signing and result-envelope RSA purposes are separated end-to-end.
- `NebiusResearchWorkerRuntimeConfiguration` is the single worker environment boundary; credential-free topology/model/timing validation occurs before private-key/provider-secret reads.
- Nebius Serverless lifecycle interpretation is explicitly allowlisted. Bounded `state_details` diagnostics are untrusted evidence only and never determine lifecycle transitions.
- Durable remote provenance contains optional structured `ProviderFailureCode`. Newly verified remote failures place the parsed code into terminal provenance before CAS, so immediate and persisted records agree.
- `ProviderFailureCodeTrust` is the dedicated evidence trust boundary for structured provider failure codes: maximum 128 characters, surrounding-whitespace canonicalization, control-character rejection, unknown-code preservation, and no remediation authority.
- `RemoteResearchFailureProvenanceMigration` and `JsonAgentJobStore` enforce that malformed structured codes fail closed at save/CAS/load boundaries.
- **New invariant:** `ProviderFailureCode` is valid only when `RemoteResearchProvenance.State == RemoteFailed`. Structured failure metadata on cancelled, expired, dispatched, reserved, completed, or other provenance states is rejected as inconsistent durable state.
- `NebiusFailureRemediationPolicy` remains separate from evidence validation. Only verified local allowlist entries currently map to guidance: `NotEnoughResources` and `Quota`. Provider messages and unknown codes cannot authorize retry/resubmit/cancel, resize resources, change projects, spend money, or cause any other side effect.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capabilities/approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, and crash/single-owner recovery.

### 2026-09-09 to 2026-09-10 — Durable research + Nebius cloud execution
Added Tavily Extract enrichment, evidence ranking/staleness/diversity, restart-safe research, encrypted remote transport, two-phase Nebius dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, native Object Storage, Serverless-mounted transport, immutable worker images, MysteryBox validation, RSA role checks, destination preflight, and fail-closed provider construction.

### 2026-09-10 to 2026-09-12 — Product, evaluator, provider, and protocol hardening
Added research/browser product runtimes, WPF lifecycle integration, restart-safe browser recovery, one-shot cloud approval, local voice, semantic-memory migration UI, positive/adversarial Personal AI evaluators, unified judging evidence, demo-package validation, provider endpoint/redirect trust, credential-read ordering, worker/client RSA role validation, protocol-level worker/client envelope trust, and deployment/protocol policy reuse.

### 2026-09-12 — Client RSA key-purpose separation + deployment enforcement
Separated dispatch signing/verification from result encryption/decryption across live configuration, runtime composition, worker bootstrap/execution, contract probe, deployment preflight, tests, README, and Nebius operator docs. Worker/live/preflight reject reused identities; worker receives only public halves.

### 2026-09-13 — Serverless lifecycle, diagnostics, and structured failure provenance
- Audited Serverless lifecycle vocabulary and added exhaustive fail-closed lifecycle coverage.
- Added bounded `state_details.code/message` parsing (128/1024 chars), control-character rejection, duplicate-shape ambiguity rejection, generic fallback, and explicit `Provider diagnostic (untrusted)` evidence.
- Added fixed local remediation for `NotEnoughResources` and `Quota`; provider messages never select guidance or actions.
- Added optional `RemoteResearchProvenance.ProviderFailureCode`, durable migration from recognized legacy failure evidence, CAS equivalence over the structured field, and product UI precedence for structured provenance.
- Lifecycle reconciliation now places the parsed failure code into terminal provenance before CAS, closing the previous in-memory/durable mismatch.
- Added `ProviderFailureCodeTrust` as the shared structured-evidence validator/canonicalizer and routed durable migration/remediation through it.

### 2026-09-13 — Provider failure provenance-state invariant
Completed in this run:
- Hardened `RemoteResearchFailureProvenanceMigration.Migrate(...)` so a non-null canonical `ProviderFailureCode` is accepted only for `RemoteResearchProvenanceState.RemoteFailed`.
- Records that carry failure classification on cancelled/expired/dispatched/reserved/other provenance now fail closed with `InvalidDataException` instead of silently preserving semantically inconsistent structured metadata.
- Preserved legacy behavior for valid remote failures: bounded unknown codes remain evidence-only, recognized legacy failure evidence can still migrate when the structured field is absent, and remediation authority remains narrower than evidence validation.
- Added `ProviderFailureCodeTrustTests.Migration_RejectsStructuredFailureCodeOutsideRemoteFailedProvenance`.
- Added `ProviderFailureCodePersistenceBoundaryTests.JobStore_Save_RejectsStructuredFailureCodeOnNonFailureProvenance`, proving rejection occurs before protected job-state persistence.

Engineering commits before this ledger update:
- `7c9328b21c186fae528739cc524aaf1459495644` — reject provider failure codes outside remote failure provenance.
- `9a24fed646767bb0d9307642cf4ee8472eceb253` — test provider failure-code provenance-state invariant.
- `e4ea57231589af50542f994ad1a478c99190014e` — reject inconsistent provider failure code at job-store boundary.

Validation / evidence this run:
- Re-read `progress.md` completely before implementation and inspected current repository state, `NebiusResearchLifecycleReconciler`, `ProviderFailureCodeTrust`, `RemoteResearchFailureProvenanceMigration`, and focused persistence tests.
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- GitHub compare from prior ledger head `7b9b220e21982dc5207a4c577c094e3a4b79ca47` to engineering head `e4ea57231589af50542f994ad1a478c99190014e`: **3 commits ahead / 0 behind**, touching only the migration and its two focused test files.
- Static review confirms the new check executes before legacy migration and before `JsonAgentJobStore` persists a replacement, so inconsistent structured state cannot be normalized into authority or written as valid state.
- `dotnet` and `csc` are still unavailable in this execution environment; focused tests were committed but could not be executed here.
- **No compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider failure `message` remains non-authoritative and is not promoted into structured provenance or product guidance.
- `ProviderFailureCode` remains classification evidence only. Unknown/future bounded codes cannot trigger retries, resubmission, cancellation, resizing, project changes, billing actions, or other side effects.
- Structured failure metadata now has both shape and state invariants: it must be bounded/control-character-free **and** belong to `RemoteFailed` provenance.
- Malformed or semantically inconsistent structured codes fail closed at durable save/CAS/load boundaries rather than falling back to legacy text.
- Existing structured-code precedence is preserved: stale/conflicting legacy error text cannot override a valid populated structured code.
- Legacy migration remains intentionally allowlist-only; arbitrary legacy text does not become structured authority.
- Serverless lifecycle remains allowlisted rather than heuristic; diagnostic text cannot manufacture a terminal state.
- Dispatch-signing/result-decryption private material remain client-local; worker/deployment plaintext receives only validated canonical public identities.
- Existing encrypted transport, authenticated associated data, signed binding, cancellation/recovery, exact-once ingestion, Tavily/Nemotron behavior, browser safety, and Windows permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler exists in this execution environment, so recent .NET/WPF/Worker changes still require a real restore/build/test/run before they can be considered compile-verified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real `embeddinggemma` ranking quality still needs a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- `NebiusServerlessJobSnapshotParser` still independently enforces the same 128-character/control-character failure-code rules rather than delegating code canonicalization to `ProviderFailureCodeTrust`. Downstream durable state is protected, but parser-level policy duplication remains.
- Only `NotEnoughResources` and `Quota` are allowlisted because those are the failure classifications verified from first-party evidence. Do not guess additional action classifications without current provider evidence or a real contract capture.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, remove the remaining parser-level provider-code validation duplication by routing `NebiusServerlessJobSnapshotParser` code fields through `ProviderFailureCodeTrust` while keeping the independent 1024-character provider-message bound, then add parser-to-persistence contract tests proving one invariant from untrusted Nebius JSON through durable provenance.
