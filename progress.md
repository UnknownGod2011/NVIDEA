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
- NVIDIA Nemotron through Nebius Token Factory with structured tool calling, retries/timeouts/cancellation, response-schema support, and fast/deep model routing.
- Layered personal memory with privacy-aware writes, provenance, semantic/recency/importance retrieval, local embeddings, migration/re-indexing, retention/edit/delete controls, and maintenance UX.
- Tavily Search + Extract research with multi-query planning, canonical deduplication, source quality/freshness/diversity ranking, citations/provenance, resumable checkpoints, explicit untrusted-evidence handling, and provider-diagnostic quarantine.
- Safe Playwright browser agent with persistent Chromium state, popup tracking, plan-act-observe-verify, prompt-injection detection, consequential-action approvals, quarantined downloads, emergency stop, crash recovery, and no automatic replay after ambiguous side effects.
- Protected local state uses Windows CurrentUser DPAPI by default; jobs use durable compare-and-swap and audit uses protected hash-chained storage/tail sealing.
- Remote research uses encrypted opaque work items, signed resource-ID bindings, two-phase dispatch, lifecycle reconciliation, crash-resumable cancellation, exact-once protected-result ingestion, cancellation-vs-terminal race handling, Nebius Object Storage, and Serverless-mounted worker transport.
- `DurableJobAuditOutbox` protects remote-result application, initial remote cancellation requests, and remote terminal lifecycle finalization by coupling exact validated audit intent to the same job CAS as the state transition.
- Product/runtime reconciliation drains stranded result/cancellation/terminal audits before provider replay and blocks protected-payload cleanup until required audit durability is established.
- `PendingExternalAction` / `DurableJobExternalActionIntent` can persist an external side-effect intent independently from success state, bind it to one exact audit event, keep it durable after audit settlement, reuse that same identity after restart, validate provider target/kind/audit binding, and clear it only after fresh reconciliation proves replay is unnecessary.
- Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-12 — Core product and judging infrastructure
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, capability permissions/audit, durable jobs, Playwright browser execution, DPAPI state protection, persistent browser sessions/downloads, crash recovery, encrypted Nebius remote execution, two-phase dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, local voice, semantic-memory migration UX, judging/evaluator tooling, protocol trust, endpoint/redirect trust, deployment preflight, and open-source/demo documentation.

### 2026-09-13 to 2026-09-14 — Exact-once, privacy, and lifecycle hardening
- Browser executed-but-unverified actions remain durable `Running` and require fresh verification; they are never automatically replayed.
- Added audit payload/event trust validation before many approval, durable-state, and external-effect boundaries.
- Quarantined browser/provider diagnostics, credential-bearing URL mismatch details, Tavily provider failures, and Nebius transport/cancellation diagnostics.
- Remote dispatch reserves provenance before creation and binds protected results to exact opaque id, remote id, checkpoint, protocol, and authenticated envelope.
- Cancellation is durable and crash-resumable. Only verified provider `Cancelled` becomes cancellation success. Failed/completed cancellation races converge truthfully.
- Added durable audit outbox protection for result application, cancellation requests, and terminal failure/cancellation/expiry transitions, with restart recovery and fault-injection regressions.

Selected lifecycle commits before the current run:
- `e66381b78752c6141e8d9ac192ea307e48cf0968` — crash-resumable Nebius cancellation.
- `fd763848104e9c5420b4175e5246fbd1e887e8da` — truthful failed cancellation race.
- `563e3e06187720fb0b175eadb50e79df8ede8b2d` / `cd0946c41d1124cc495fea455cdbdd610afa8b19` / `1092b7d548dc20aa650b5defb5119803744bd235` — completed-after-cancel race handling.
- `8bed3481f54fba21c46d604d9f924eb56d05ac6a` / `8145c7bb812777b557608ddb1eef2ce59cfd49fb` / `567aa999f6b5db181820c424942144ddf52eb0ad` — audit outbox foundation and result integration.
- `9028621ca8430bbeaf8a2f6bc00230d104ca98a2` / `ebf3f30cae8007da8e5ac452de786c9f7486f7cb` — cancellation-request audit outbox.
- `1f4a15b12ef35fc6cb0764425cf10fb9e06a9ea5` / `61cbce3f289f935bb3666757b0fa14d7d4140f64` — remote terminal audit outbox and recovery tests.

### 2026-09-14 — Durable external action intent foundation
Completed:
- Inspected the cancellation-redrive path and confirmed that active-provider redrive still used direct `research.remote_cancel_redriven` audit append followed by `CancelAsync`, with no durable external-action record surviving a process/provider-call ambiguity.
- Added `DurableExternalActionKind` and `PendingExternalAction` to durable job contracts. The intent stores a unique action id, explicit action kind, provider target id, exact bound audit event id, and creation time. Presence means only “delivery may still require reconciliation”; it is never provider-success evidence.
- Extended `JsonAgentJobStore` compare-and-swap identity so pending external-action content participates in version equivalence. A stale writer therefore cannot silently erase or replace a durable external action.
- Added `DurableJobExternalActionIntent`, which stages an exact external-action intent together with its validated audit event, flushes the audit while deliberately keeping the action intent durable, refuses to clear the action while its audit is pending, and clears only one exact action id after higher-level provider reconciliation proves replay unnecessary.
- Added strict target-id validation to keep control characters/oversized provider identifiers out of durable action metadata.
- Added `DurableJobExternalActionIntentTests` covering stage -> audit flush -> action clear ordering, refusal to clear before audit durability, CAS protection against stale erasure, and invalid provider target rejection.

Engineering commits:
- `1a68d07f95a7b1597ef2ebaffe4f71ccaf831a64` — add durable external action intent to job records.
- `38ff800354826a15ac17081726e38db0f6e13a65` — version durable external actions in job CAS.
- `6278afd91b96f3611e471b49de9dbba065627ec7` — add durable external-action coordinator.
- `470243b945b5fd9230325d9fd3e55306fefc6501` — add focused durable-action regressions.
- `465d4715e5cd3f42ac371b4ed51d06356b8b3af5` — record durable external-action foundation.

### 2026-09-14 — Restart-stable external action reuse hardening (latest run)
Completed:
- Re-read this ledger first and inspected current cancellation recovery plus the durable external-action coordinator.
- Added `StageOrReuseAndFlushAsync(...)` to `DurableJobExternalActionIntent`. A previously persisted matching action now wins over any newly prepared retry audit, so process restart or an ambiguous provider failure cannot manufacture a second action/audit identity for the same pending delivery.
- Added `ValidatePending(...)` as an explicit fail-closed trust boundary. Pending action kind, exact provider target id, non-empty action/audit ids, creation timestamp, and pending-audit binding are validated before higher-level provider delivery/clearing code can rely on the durable action.
- Kept CAS ordering strict for new actions: `PendingAuditEvent + PendingExternalAction` must commit together before the bound audit can be flushed. A CAS conflict fails rather than rebasing a stale action onto changed job state.
- Added restart-oriented regressions proving that an existing settled action is reused without a second audit; a stranded bound audit is recovered without replacing the action; remote-id substitution fails without mutating durable state or appending the attacker-controlled retry audit; corrupt action kind/audit binding fails closed.

Engineering commits this run before this ledger update:
- `51af3518cf18cd0a36f924c2aafae6237939be24` — harden durable external action reuse semantics.
- `71675a08d2e2159779ab9e2f5a43faedd0899096` — test restart reuse and substitution guards.

Validation / evidence this run:
- Before every GitHub mutation, repository metadata reported exact full name `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head was `465d4715e5cd3f42ac371b4ed51d06356b8b3af5`.
- Static review confirms a persisted matching action is never treated as provider success, and a newly prepared audit is ignored when restart recovery finds the original durable action identity.
- Static review confirms target substitution and mismatched audit binding throw before action reuse/clear can proceed.
- `dotnet`, `csc`, `msbuild`, and `mcs` remain unavailable in this execution environment. **No compilation, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.**
- No GitHub Actions workflow and no live/paid Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- External-action intent remains inside the existing protected job record; no plaintext sidecar or new secret store was introduced.
- The durable action contains provider resource identity already required by remote provenance, not credentials or request bodies.
- Audit and action are separate durable concepts but are explicitly bound by exact `AuditEventId`; audit must settle before action clearing is allowed.
- CAS identity includes the complete pending action, preventing stale concurrent writers from erasing delivery ambiguity.
- Existing durable actions are now restart-stable: recovery reuses the original action/audit identity rather than creating another retry identity.
- Provider target and action kind are validated exactly; substitution or corrupt bindings fail closed.
- The primitive intentionally does not auto-clear after an attempted provider call. A transport success/failure by itself is insufficient proof; higher-level reconciliation must inspect fresh provider state.
- The new primitive is **still not wired into `NebiusResearchLifecycleReconciler` redrive**. The existing direct redrive audit -> provider call remains the active production path. This run therefore removes restart/substitution ambiguity inside the durability primitive but does not claim the production redrive crash window is fully closed.
- Protected-payload cleanup remains idempotent but lacks an explicit durable cleanup completion marker.
- Remote reserve/attach transitions and generic job transitions still have independent state/write and audit operations unless separately hardened.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler is available here; recent Core/WPF/Worker changes still require real restore/build/test/run validation.
- The new external-action regressions are statically reviewed but unexecuted.
- Cancellation redrive still writes `research.remote_cancel_redriven` directly before `CancelAsync`; production integration with `DurableJobExternalActionIntent` remains outstanding.
- `NebiusResearchLifecycleReconciler` is a large lower-level component and the available GitHub mutation API replaces whole files rather than applying patches; do not risk blind rewrites. Product-facing composition must continue preserving recovery/order boundaries.
- Protected-payload cleanup has no explicit durable completion marker after audit settlement.
- Blind low-level `JsonAgentJobStore.SaveAsync(...)` remains trusted infrastructure; product paths should prefer constrained CAS/lifecycle APIs.
- Live Nebius Serverless/Object Storage behavior, provider catalog drift, real Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking still need environment validation.

## Single Best Next Task
Integrate `DurableJobExternalActionIntent.StageOrReuseAndFlushAsync(...)` and `ValidatePending(...)` into **Nebius cancellation redrive** end-to-end. When fresh provider state is `Pending`/`Running`, persist or reuse the exact `research.remote_cancel_redriven` action, prove its bound audit durable, then attempt `CancelAsync` while leaving that same action durable. On restart, validate and reuse the original action before any provider delivery; if fresh provider state is already `Cancelling`, `Cancelled`, `Completed`, or `Failed`, clear that exact validated action without duplicate cancellation before continuing truthful lifecycle handling. Add lifecycle-level fault-injection tests for audit failure, provider-call failure, restart after ambiguous delivery, repeated active reconciliation, and remote-id/action-id substitution. Never report cancellation success without freshly verified provider `Cancelled`.
