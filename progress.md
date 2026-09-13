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
- `ProviderFailureCodeTrust` is now the dedicated evidence trust boundary for structured provider failure codes: maximum 128 characters, surrounding-whitespace canonicalization, control-character rejection, unknown-code preservation, and no remediation authority.
- `JsonAgentJobStore` already routes save, CAS replacement, and load through `RemoteResearchFailureProvenanceMigration`; that migration now applies `ProviderFailureCodeTrust`, so malformed pre-existing/populated structured codes fail closed at durable-state boundaries and valid unknown codes are canonicalized.
- `NebiusFailureRemediationPolicy` is separate from evidence validation. Only verified local allowlist entries currently map to guidance: `NotEnoughResources` and `Quota`. Provider messages and unknown codes cannot authorize retry/resubmit/cancel, resize resources, change projects, spend money, or cause any other side effect.
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

### 2026-09-13 — Shared provider failure-code trust boundary
Completed in this run:
- Added `ProviderFailureCodeTrust` as the single evidence validator/canonicalizer for structured provider failure classifications. It accepts absent values, trims bounded values, preserves well-formed unknown/future codes, rejects values over 128 characters, and rejects control characters.
- Kept evidence validation deliberately separate from remediation authorization: accepting an unknown code as evidence does **not** grant UI guidance or execution authority.
- Hardened `RemoteResearchFailureProvenanceMigration` so already-populated structured codes are validated/canonicalized instead of bypassing migration. Malformed populated codes fail closed with `InvalidDataException` rather than silently falling back to legacy `LastError` text.
- Because `JsonAgentJobStore.SaveAsync`, CAS replacement, and load already call `RemoteResearchFailureProvenanceMigration`, the structured-code invariant is now enforced at all three durable job-store boundaries without duplicating store logic.
- Updated `NebiusFailureRemediationPolicy.Classify` to reuse `ProviderFailureCodeTrust` rather than maintaining a separate length/control-character validator. The remediation allowlist remains narrower and side-effect-free.
- Added `ProviderFailureCodeTrustTests` covering absent values, canonicalization, bounded unknown evidence, oversize/control-character rejection, unknown-code preservation without remediation authority, malformed structured-provenance rejection, and continued legacy allowlist migration.
- Added `ProviderFailureCodePersistenceBoundaryTests` covering save/reload canonicalization of unknown structured evidence and rejection-before-persistence for oversized/control-character-bearing structured codes.

Engineering commits before this ledger update:
- `485d3d1257d6020278abbb997948de816074588d` — add shared provider failure code trust boundary.
- `a00deacfb784f47cc95008482774e6f29140489e` — enforce provider failure code trust at durable migration boundary.
- `82649c96a12bf4fcf589e8df725f2c17c00fe725` — reuse shared provider failure code trust in remediation policy.
- `118b915f37040f87bc3389a806ad4d9c6de90e55` — test shared provider failure code trust boundary.
- `f6771c353eaa405dfc95a690a0b6be903fea8342` — test provider failure code persistence boundary.

Validation / evidence this run:
- Re-read `progress.md` completely before implementation and inspected current recent commits, `NebiusResearchLifecycleReconciler`, `JsonAgentJobStore`, `RemoteResearchFailureProvenanceMigration`, `NebiusFailureRemediationPolicy`, `ResearchJobStatus`, and existing persistence tests.
- Verified before every GitHub mutation that the write target was exactly `UnknownGod2011/NVIDEA`.
- GitHub compare from prior ledger head `8f83b260e72b4e7b4c3d561544c1f89d4b41c24c` to engineering head `f6771c353eaa405dfc95a690a0b6be903fea8342`: **5 commits ahead / 0 behind**, with changes limited to the shared trust primitive, migration/remediation policy, and focused tests.
- Static review confirms `JsonAgentJobStore.SaveAsync`, `CompareExchangeAsync`, and load all pass records through `RemoteResearchFailureProvenanceMigration`, so the new trust primitive covers new writes, CAS replacements, and restored pre-existing structured records.
- Static review confirms product remediation remains separate: unknown but well-formed evidence is retained while `NebiusFailureRemediationPolicy` returns no guidance for it.
- This environment still exposes neither `dotnet` nor `csc`; focused tests were committed but could not be executed here.
- **No compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider failure `message` remains non-authoritative and is not promoted into structured provenance or product guidance.
- `ProviderFailureCode` remains classification evidence only. Unknown/future bounded codes cannot trigger retries, resubmission, cancellation, resizing, project changes, billing actions, or other side effects.
- Malformed structured codes now fail closed at durable save/CAS/load boundaries instead of bypassing the original live-parser bound.
- Existing structured-code precedence is preserved: stale/conflicting legacy error text cannot override a populated valid structured code.
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
- `NebiusServerlessJobSnapshotParser` still enforces the same 128-character/control-character rules locally for diagnostic codes rather than delegating that field directly to `ProviderFailureCodeTrust`. Downstream durable state is protected, but parser-level policy duplication should be removed when safely editing/rebuilding that file.
- Only `NotEnoughResources` and `Quota` are allowlisted because those are the failure classifications verified from first-party evidence. Do not guess additional action classifications without current provider evidence or a real contract capture.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, remove the remaining parser-level provider-code validation duplication by routing `NebiusServerlessJobSnapshotParser` code fields through `ProviderFailureCodeTrust` while keeping the independent 1024-character provider-message bound, then add parser-to-persistence contract tests proving one invariant from untrusted Nebius JSON through durable provenance.
