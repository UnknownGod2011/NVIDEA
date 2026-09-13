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
- Layered personal memory with privacy-aware writes, provenance, semantic/recency/importance retrieval, local embeddings, migration/re-indexing, and maintenance UX.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, evidence quality/freshness/diversity ranking, citations/provenance, resumable checkpoints, explicit untrusted-evidence handling, and diagnostic quarantine on Extract fallback.
- Safe Playwright browser agent with persistent Chromium state, popup tracking, plan-act-observe-verify, prompt-injection detection, consequential-action approvals, quarantined downloads, emergency stop, crash recovery, and no automatic replay after ambiguous side effects.
- Protected local state uses Windows CurrentUser DPAPI by default; job state uses durable CAS, leases, and hash-chained/segmented audit.
- Remote research uses encrypted opaque work items, signed resource-ID bindings, two-phase dispatch, lifecycle reconciliation, durable cancellation, exact-once result ingestion, race-safe cleanup, Nebius Object Storage, and Serverless-mounted worker transport.
- Trust boundaries include `ProviderFailureCodeTrust`, `JobFailureDiagnostic`, `DesktopUiFailureProjector`, `DesktopDisplayTextTrust`, `BrowserProductOutcomeTrust`, `BrowserGoalEvidenceTrust`, `CapabilityIdentityTrust`, `AuditPayloadTrust`, and `AuditEventTrust`.
- Exact browser approval scopes exclude typed values, uploads, rationale, page/source URLs, and tool arguments.
- `JsonLinesAuditTrail` enforces identity and semantic payload trust on append, protected/hash-chained reload, and legacy migration; `BoundedSegmentedAuditTrail` also enforces byte/retention ceilings.
- `ResumableJobOrchestrator`, Nebius lifecycle reconciliation, remote-result ingestion, coordinator dispatch, two-phase dispatch, browser download handoff/discard, and generic capability execution validate prospective audit events before the durable/external/approval transition they describe.
- Remote dispatch ordering is: exact cloud authorization -> no-mutation reservation/audit preflight -> encrypt/upload -> durable `DispatchReserved` -> Nebius create -> durable remote-id attachment -> optional binding publication.
- Browser capability execution and the legacy browser executor quarantine raw driver/site exception text from receipts; user-facing/durable flows receive only fixed NVIDEA-authored browser diagnostics.
- Tavily Extract failure fallback preserves already-collected search evidence but no longer copies provider/network exception messages into `ResearchBatch.Warnings`, preventing those diagnostics from entering resumable research checkpoints.
- Windows voice invocation is local/review-first. Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-12 — Core platform and judging infrastructure
Added Nebius/Nemotron inference, layered memory, Tavily research, capability approvals/audit, durable jobs, Playwright execution, Windows shell, DPAPI state protection, persistent browser sessions/downloads, crash recovery, encrypted Nebius remote execution, two-phase dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, local voice, semantic-memory migration UX, judging/evaluator tooling, protocol trust, endpoint/redirect trust, RSA role separation, deployment preflight policy reuse, and open-source/demo documentation.

### 2026-09-13 — Browser exact-once/privacy hardening
Added provider-failure provenance and diagnostic quarantine, privacy-safe desktop/browser projections, safe legacy goal migration, parent/child checkpoint minimization, terminal checkpoint scrubbing, and explicit ambiguous execution handling. Executed-but-unverified browser actions stay `Running`, receive no automatic retry, and require fresh verification. Capability/action/tool authority became bounded canonical ASCII and reject-only.

### 2026-09-13 — Durable audit and generic job ordering hardening
Added `AuditPayloadTrust` and `AuditEventTrust`; enforced them in `JsonLinesAuditTrail` append/reload/legacy migration and bounded segmented audit. `ResumableJobOrchestrator` validates audits before creation, approval transitions, cancellation/failure, and ambiguous recovery. Malformed handler-produced audit/approval data after execution starts leaves the job `Running`, preventing replay.

### 2026-09-13 — Nebius remote-research ordering hardening
Hardened lifecycle cancellation/terminal transitions, quarantined provider diagnostic messages, and validated reserve/attach/result audit events before CAS. Added product-level and lower-level pre-dispatch audit preflight so malformed durable authority cannot reach encrypted upload or Nebius creation in deterministic cases. `RemoteResearchResultIngestor.PreflightDispatchReservationAsync(...)` is no-mutation; `ReserveDispatchAsync(...)` still reloads and revalidates immediately before CAS.

### 2026-09-14 — Consequential browser approval/audit ordering
Hardened browser download handoff/discard and generic `CapabilityToolExecutor` so exact start-audit data is validated before single-use approvals are consumed. The same prevalidated event instance is appended before the external operation/backend call.

### 2026-09-14 — Browser diagnostic privacy hardening
Hardened the current browser capability path and legacy browser executor so raw driver/site `Exception.Message` content cannot cross into browser receipts. Added adversarial bearer-token regressions requiring only fixed NVIDEA-authored quarantine diagnostics.

### 2026-09-14 — Tavily Extract diagnostic privacy hardening (latest run)
Completed:
- Re-read this ledger, inspected the current head/recent commits, durable research checkpoint path, Nebius client diagnostics, Tavily Search/Extract implementation, and existing Tavily enrichment regressions.
- Identified a concrete durable-state privacy leak: `TavilyResearchClient.EnrichAsync(...)` appended `ex.Message` to `ResearchBatch.Warnings` when Extract degraded to existing search evidence. `ResearchJobHandler` serializes prepared research evidence into resumable checkpoints, so a transport/provider diagnostic could become durable local/cloud job state.
- Replaced the raw exception interpolation with a fixed NVIDEA-authored warning: Extract is unavailable, search evidence is retained, and raw provider/network diagnostics are quarantined. Cancellation requested by the caller is still rethrown and fallback behavior is otherwise unchanged.
- Added `Enrich_quarantines_transport_exception_diagnostics_from_warnings`, injecting a bearer-like secret and credential-bearing URL through `HttpRequestException`. The regression requires retained search evidence/credit accounting while forbidding the token, `Bearer`, and `token=` from the warning and requiring the fixed quarantine signal.

Engineering commits this run before this ledger update:
- `703f2477d751d3fd9ab21e7e0ad74ad348f91492` — quarantine Tavily Extract transport/provider diagnostics from research warnings.
- `33dd8774287990699a61468115f9e23095ac11f8` — focused Tavily Extract diagnostic privacy regression.

Validation / evidence this run:
- Before every GitHub mutation, repository metadata reported exactly `repository_full_name: UnknownGod2011/NVIDEA`, default branch `main`.
- Starting head was `82e4037a3a1c581c3da83cd917ee397276a248fe`.
- Static data-flow review confirms Extract fallback no longer copies `Exception.Message` into `ResearchBatch.Warnings`; `ResearchJobHandler` persists prepared evidence, so the change closes that durable-checkpoint path at its source.
- The new regression injects `Bearer super-secret-tavily-token` plus a `?token=` URL and requires both to be absent from warnings while the existing search evidence survives.
- This run did not mutate `UnknownGod2011/keyboard.wtf` or any other repository.
- No GitHub Actions workflow, live Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or paid inference operation was triggered.
- Runtime inspection still reports `dotnet`, `csc`, `msbuild`, and `mcs` absent, so **no compile, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed**. Changes were statically reviewed only.

## Security / Privacy / Failure Review
- Raw browser driver/site diagnostics no longer cross the hardened browser receipt boundaries.
- Tavily Extract fallback no longer persists raw provider/network exception messages through research warnings/checkpoints; availability degradation still preserves collected search evidence.
- Single-use capability approvals cannot be deterministically consumed by malformed start-audit authority before a tool call begins.
- Backend/provider/site exception text remains excluded from durable audit summaries.
- Audit append/storage I/O can still fail after approval consumption because approval state and audit storage are not one atomic transaction. Prevalidation removes deterministic semantic rejection from that window but cannot make independent storage I/O transactional.
- Browser ambiguous-execution recovery, emergency stop, exact approval gating, encrypted research transport, Tavily source provenance, and local/cloud separation were not weakened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler is available in this environment; recent Core/WPF/Worker changes still require a real restore/build/test/run before compile confidence is justified.
- Latest Tavily diagnostic privacy regression is statically reviewed but unexecuted.
- Audit append/storage I/O is not transactionally coupled to approval consumption or the job store.
- Other direct `AuditEvent` / `IAuditTrail.AppendAsync` producers may still need ordering review.
- Other provider/browser/network exception-to-state paths should continue to be audited for embedded secret leakage even when bounded/control-normalized.
- A state race after remote dispatch preflight but before reservation can still upload an encrypted work item requiring best-effort cleanup; Nebius creation remains blocked unless durable reservation succeeds.
- Provider catalogs can change; a real Nebius inference smoke test remains required.
- Prompt-injection detection remains heuristic; capability gates and approvals remain mandatory defense in depth.
- Real Windows UX, embedding ranking, Playwright authenticated-session behavior, Tavily live behavior, and Nebius Object Storage/Serverless execution still require live environment validation.

## Single Best Next Task
If a real .NET 8 Windows build environment becomes available, immediately run restore/build/Core tests/WPF build/Worker build and record exact failures. Otherwise continue the **privacy + direct-audit-producer boundary audit**, next tracing Nebius/other provider diagnostic fields (including any consumer of raw response excerpts) and remaining direct audit producers to ensure raw diagnostics cannot enter durable/UI state and deterministic audit rejection cannot happen after approval, durable mutation, or external side effects. Avoid duplicating policy where the existing sink is already sufficient.
