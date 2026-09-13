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
- `ProviderFailureCodeTrust` is the single evidence-shape trust boundary for provider failure codes: maximum 128 characters, surrounding-whitespace canonicalization, control-character rejection, unknown-code preservation, and no remediation authority. The raw Serverless snapshot parser, durable migration/store boundaries, and remediation classification all consume that shared canonicalization contract.
- Provider diagnostic `message` remains separately bounded to 1024 characters and is display/audit evidence only; it is never promoted into structured failure classification.
- `ProviderFailureCode` is valid only when `RemoteResearchProvenance.State == RemoteFailed`; inconsistent structured failure metadata is rejected at durable boundaries.
- `NebiusFailureRemediationPolicy` remains narrower than evidence validation. Only verified local allowlist entries currently map to guidance: `NotEnoughResources` and `Quota`. Provider messages and unknown codes cannot authorize retry/resubmit/cancel, resize resources, change projects, spend money, or cause any other side effect.
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
- Added bounded `state_details.code/message` parsing, duplicate-shape ambiguity rejection, generic fallback, and explicit `Provider diagnostic (untrusted)` evidence.
- Added fixed local remediation for `NotEnoughResources` and `Quota`; provider messages never select guidance or actions.
- Added optional `RemoteResearchProvenance.ProviderFailureCode`, durable migration from recognized legacy failure evidence, CAS equivalence over the structured field, and product UI precedence for structured provenance.
- Lifecycle reconciliation places parsed failure classification into terminal provenance before CAS, keeping returned and persisted records consistent.
- Added `ProviderFailureCodeTrust` and enforced the state invariant that structured failure codes are valid only on `RemoteFailed` provenance.

### 2026-09-13 — Unified raw-provider to durable failure-code trust boundary
Completed in this run:
- Removed the duplicate 128-character/control-character provider-code policy from `NebiusServerlessJobSnapshotParser` and routed raw `stateDetails.code` / `state_details.code` through `ProviderFailureCodeTrust.TryCanonicalize(...)`.
- Kept the provider diagnostic message policy intentionally independent at 1024 characters; unsafe code or message still discards the complete diagnostic without affecting lifecycle parsing.
- Preserved lifecycle authority: provider diagnostics cannot turn an unknown state into success/failure, and unknown well-formed codes remain evidence-only.
- Added parser-level parity tests proving whitespace canonicalization and oversized-code rejection are exactly governed by the shared trust primitive.
- Added an end-to-end raw-JSON → parser → reconciler → CAS job store regression. A whitespace-padded unknown code becomes canonical `FutureProviderCode` in both the immediately returned record and the reloaded durable record, while `NebiusFailureRemediationPolicy` still returns no guidance for it.

Engineering commits before this ledger update:
- `41eaf88ee0f2636f73348032df889fa86243614b` — unify Nebius failure-code trust boundary.
- `275ab1a77eca5bd9ab3e87f06eb5cde607f8f1f1` — test Nebius failure-code trust end to end.

Validation / evidence this run:
- Re-read `progress.md` completely before implementation and inspected the repository tree, `NebiusResearchLifecycleReconciler`, `NebiusServerlessJobSnapshotParser`, `ProviderFailureCodeTrust`, and existing diagnostic/provenance tests.
- Verified before every GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`.
- GitHub compare from prior ledger head `fe1665d3b9e23e06e7ae2b9c696f601b507355b3` to engineering head `275ab1a77eca5bd9ab3e87f06eb5cde607f8f1f1`: **2 commits ahead / 0 behind**, touching only `NebiusResearchLifecycleReconciler.cs` and `NebiusServerlessFailureDiagnosticTests.cs`.
- Static review confirms raw provider codes now cross the same canonicalization primitive used at durable boundaries, while the independent provider-message bound and fail-closed lifecycle parser remain unchanged.
- `dotnet` and `csc` are unavailable in this execution environment; focused tests were committed but could not be executed here.
- **No compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider failure `message` remains non-authoritative and is not promoted into structured provenance or product guidance.
- `ProviderFailureCode` remains classification evidence only. Unknown/future bounded codes cannot trigger retries, resubmission, cancellation, resizing, project changes, billing actions, or other side effects.
- One shared code-shape trust primitive now covers raw provider ingestion and durable structured state, reducing policy drift between network and persistence boundaries.
- Structured failure metadata has both shape and state invariants: it must be bounded/control-character-free and belong to `RemoteFailed` provenance.
- Malformed or semantically inconsistent structured codes fail closed rather than falling back to legacy text.
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
- Only `NotEnoughResources` and `Quota` are allowlisted because those are the failure classifications verified from first-party evidence. Do not guess additional action classifications without current provider evidence or a real contract capture.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, audit all remaining consumers of provider diagnostic text and legacy `LastError` for accidental control-flow/remediation authority, then add adversarial taint/provenance regressions wherever untrusted provider strings can still cross a product or audit boundary.
