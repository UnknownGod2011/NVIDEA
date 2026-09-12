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
- Token Factory, Tavily, Serverless, Object Storage, worker, model-catalog, and contract-probe credential paths have explicit endpoint/redirect trust boundaries.
- Worker envelope public/private keys and client result-envelope public/private keys use bounded PEM, RSA >=2048, strict role separation, OAEP-SHA256 capability proofs, canonicalization, and temporary-buffer zeroization.
- Client dispatch-signing and result-envelope RSA purposes are separated end-to-end; production live composition rejects identity reuse before provider credential reads.
- `NebiusResearchWorkerRuntimeConfiguration` is the single worker environment boundary. Credential-free topology/model/timing validation happens before worker-private-key and provider-secret reads.
- Nebius Serverless lifecycle interpretation is explicitly allowlisted. Bounded `state_details` diagnostics are untrusted evidence only and never determine lifecycle transitions.
- Recognized Nebius failure codes map through a fixed local remediation allowlist. **Durable remote provenance contains a structured optional `ProviderFailureCode`; newly verified remote failures now place the already bounded diagnostic code into terminal provenance before CAS, so returned in-memory and persisted records agree. Product guidance still treats the code as evidence and only acts on its fixed local allowlist.**
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

### 2026-09-13 — Serverless lifecycle + bounded diagnostics
Audited current Nebius Serverless lifecycle vocabulary and added exhaustive fail-closed state coverage. Added bounded `state_details.code/message` parsing with 128/1024-character limits, control-character rejection, duplicate-shape ambiguity rejection, generic fallback, and explicit `Provider diagnostic (untrusted)` evidence. Provider diagnostics never feed lifecycle parsing.

### 2026-09-13 — Fixed local Nebius remediation
Added `NebiusFailureRemediationPolicy`. Only verified exact codes currently allowlisted from first-party Nebius evidence are `NotEnoughResources` (capacity guidance) and `Quota` (quota guidance). Provider `message` cannot select guidance, authorize retry/resubmit/cancel, change resources/projects, or mutate durable state. `ResearchJobStatus` exposes locally authored guidance only for durable `Failed + RemoteFailed` jobs.

### 2026-09-13 — Structured durable remote failure provenance
- Extended `RemoteResearchProvenance` with trailing optional `ProviderFailureCode`, preserving existing constructor call sites/legacy JSON compatibility while creating a dedicated structured field for bounded provider classification.
- Added `RemoteResearchFailureProvenanceMigration`. It migrates only `RemoteFailed` records with no structured code, reads only NVIDEA's own legacy failure-evidence `code=...`, and promotes a code only when the fixed local remediation allowlist recognizes it. Provider `message` is never copied or parsed for authority.
- Hardened `ResearchJobStatus`: when `ProviderFailureCode` is present it is authoritative even if unknown to this version. A conflicting legacy `LastError` cannot override it. Legacy text parsing is used only when the structured field is absent.
- Integrated migration at the durable job-store boundary: `JsonAgentJobStore.SaveAsync`, CAS replacement, and load all canonicalize recognized legacy failure codes. Loaded legacy records are rewritten durably when migration changes them.
- Included `ProviderFailureCode` in job-store CAS version equivalence so concurrent/stale updates cannot silently ignore a changed structured failure classification.
- Added focused/adversarial tests for recognized migration, unknown-code refusal, preservation of an already structured code, structured-vs-legacy conflict precedence, malicious provider-message non-disclosure, and fixed-guidance projection.
- Added `RemoteResearchFailureProvenancePersistenceTests` exercising the real protected `JsonAgentJobStore` save/reload path: a legacy `Quota` failure is reloaded with structured `ProviderFailureCode=Quota`, provider message remains only in legacy error evidence, and Windows-safe status emits fixed local guidance without exposing that provider message.

Engineering commits:
- `2cbb03def912821b4fcd4c5cfc9bfdfad64cf663` — persist bounded remote failure code in provenance.
- `e63d6b9d2907309a4b71b24cdd7fe27a0eb8e87c` — prefer structured remote failure provenance in UI.
- `04e1859d0d6d34826a482021121095da676b4c91` — add remote failure provenance migration.
- `a03b6a349cd451852919e20be98050eb282d9dbf` — make structured failure provenance authoritative.
- `678e24f1ae93ab3f3dbbed06c3dbeae7257d3a40` — migrate remote failure codes at durable store boundary.
- `de479adec7d8c17c2381100c365856cd34f0f466` — test structured remote failure provenance migration.
- `1fe442cab7adb6914d39b661a126b5e3fd94fb24` — test durable remote failure provenance migration.

### 2026-09-13 — Direct lifecycle failure-code capture
Completed in this run:
- `NebiusResearchLifecycleReconciler` now carries the already parsed/bounded `state_details.code` directly into `RemoteResearchProvenance.ProviderFailureCode` when and only when the verified remote lifecycle independently resolves to `FAILED`/`ERROR`.
- Terminal provenance is built with the code **before** the job-store compare-and-swap. The record returned by reconciliation therefore matches the durable record instead of relying on a later load/migration pass.
- Non-`RemoteFailed` terminal paths explicitly clear `ProviderFailureCode`, preventing provider failure classification from leaking into cancelled/expired terminal provenance.
- Unknown/future bounded provider codes are preserved as untrusted evidence, but they do not gain remediation authority: `NebiusFailureRemediationPolicy` still returns guidance only for its fixed local allowlist.
- Added `NebiusResearchLifecycleFailureProvenanceTests` covering immediate-vs-durable equality for both an allowlisted `Quota` code and an unknown future code, plus a rejected oversized diagnostic proving malformed diagnostics do not populate structured provenance.

Engineering commits before this ledger update:
- `1073f5de4717a8a4f850367022071ec9f227abdf` — persist provider failure code during lifecycle reconciliation.
- `034ca5809f2f893f2ba3aa9b15c30f41f330de43` — test direct remote failure provenance capture.

Validation / evidence this run:
- Re-read `progress.md` completely before implementation and inspected the current reconciler, durable provenance model, job-store migration/CAS behavior, remediation policy, and existing lifecycle-test patterns.
- GitHub comparison from prior ledger head `920d3d0634f18e51b39a0cf7dd351e559ba4ac7b` before this ledger commit: **2 commits ahead / 0 behind**, with net diff limited to `NebiusResearchLifecycleReconciler.cs` and the new focused lifecycle-provenance test file.
- Static review confirms the structured code originates only from `NebiusServerlessJobSnapshotParser`, which already bounds code length to 128 characters and rejects control characters/non-string/ambiguous diagnostic payloads before reconciliation.
- Focused regression code is committed but **not executed here** because no usable .NET compiler/runtime is available in this execution environment.
- **No compile, xUnit, WPF, Worker, evaluator, or tool PASS is claimed.**
- No GitHub Actions workflow was triggered merely to manufacture a green signal.
- No live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was performed.

## Security / Privacy / Failure Review
- Provider failure `message` remains non-authoritative and is not promoted into structured provenance or product guidance.
- `ProviderFailureCode` is classification evidence only. Even an unknown/future bounded code cannot itself trigger retries, resubmission, cancellation, resizing, project changes, billing actions, or any other side effect.
- Only a provider response whose independently allowlisted lifecycle state is `FAILED`/`ERROR` can populate `ProviderFailureCode`; diagnostic text cannot manufacture a terminal transition.
- Once a structured code exists, legacy formatted error text cannot override it. Unknown/future structured codes fail to generic UI rather than falling back to a conflicting old string.
- Legacy migration remains intentionally allowlist-only: unknown legacy codes remain unstructured instead of being promoted from text.
- CAS version equivalence includes the structured code, protecting against stale state replacement across this field.
- Dispatch-signing/result-decryption private material remain client-local; worker/deployment plaintext receives only validated canonical public identities.
- Serverless lifecycle remains allowlisted rather than heuristic; undocumented/future states cannot mutate durable local job state.
- Existing encrypted transport, authenticated associated data, signed binding, cancellation/recovery, exact-once ingestion, Tavily/Nemotron behavior, browser safety, and Windows permission UX were not removed or weakened.

## Known Blockers / Risks
- No usable .NET 8 executable exists in this execution environment, so all recent .NET/WPF/Worker changes still require a real restore/build/test/run before they can be considered compile-verified.
- Real Windows execution remains mandatory before treating WPF voice/readiness/maintenance behavior and generated judging evidence as judge-ready.
- Provider catalogs can change; model listing does not prove quota, inference success, tool calling, context length, or every capability. A real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense-in-depth.
- Real `embeddinggemma` ranking quality still needs a local Ollama evaluation corpus.
- No live Object Storage bucket/static key, digest-pinned registry image, MysteryBox refs, subnet, Serverless token, or real Serverless job has been provisioned/validated here.
- Exact provider acceptance of Serverless Object Storage `Source` / `SourcePath` still requires a real job.
- The steady-state lifecycle mismatch for newly observed remote failures is closed: reconciliation now returns the same structured failure code that CAS persists. Legacy `LastError` parsing remains only for old records that predate structured provenance.
- A pre-existing/corrupt record can still contain an already-populated arbitrary `ProviderFailureCode`; product remediation fails closed on malformed/unknown values, but the durable-model invariant that this field is bounded is not yet enforced by one shared trust primitive at every persistence boundary.
- Only `NotEnoughResources` and `Quota` are allowlisted because those are the failure classifications currently verified from first-party Nebius evidence. Do not add timeout/start/container classifications until current provider evidence or a real contract capture confirms exact codes.
- Lower-level `NebiusResearchClientRuntime.Create(...)` still retains a same-key compatibility fallback for legacy unit/contract callers; production live composition and deployment preflight are stricter. Remove only after executable migration coverage exists.

## Single Best Next Task
Obtain a real .NET 8-capable Windows restore/build/test signal and fix every compile/runtime defect exposed by recent security/reliability migrations. If executable validation remains unavailable, introduce one shared bounded `ProviderFailureCode` trust/canonicalization primitive and apply it to parser output plus durable provenance persistence/load so the 128-character/control-character invariant is enforced consistently even for pre-existing structured records, while keeping remediation allowlisting separate from evidence validation.
