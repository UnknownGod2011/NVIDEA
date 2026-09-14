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
- `DurableJobAuditOutbox` protects remote dispatch reservation, remote-id attachment, result application, initial cancellation requests, and terminal failure/cancellation/expiry by coupling the exact validated audit intent to the same job CAS as its state transition.
- A Nebius Serverless Create cannot start until `research.remote_dispatch_reserved` is proven durable. If reservation CAS succeeds but audit persistence fails, the durable reservation owns the encrypted work item and cleanup must not remove it.
- `PendingExternalAction` / `DurableJobExternalActionIntent` persist cancellation-redrive ambiguity independently from success state, bind one exact audit event, survive restart/provider ambiguity, validate provider target/audit binding, and clear only after fresh provider reconciliation proves replay unnecessary.
- `PendingProtectedPayloadCleanup` / `DurableProtectedPayloadCleanupIntent` persist protected remote-research cleanup obligations in the same CAS as result application or terminal settlement. Audit settles first, deletes retry idempotently, and the marker clears only after every required transport delete succeeds.
- Signed worker dispatch bindings are now restart-recoverable from exact durable `Dispatched` / `CancelRequested` provenance before provider observation or cancellation. Recovery never creates/lists/gets Nebius work and rejects conflicting substituted bindings cryptographically.
- Lifecycle reconciliation drains stranded audit and cleanup obligations locally before provider replay where applicable.
- Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-12 — Core product and judging infrastructure
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, capability permissions/audit, durable jobs, Playwright browser execution, DPAPI state protection, persistent browser sessions/downloads, crash recovery, encrypted Nebius remote execution, two-phase dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, local voice, semantic-memory migration UX, judging/evaluator tooling, protocol trust, endpoint/redirect trust, deployment preflight, and open-source/demo documentation.

### 2026-09-13 to 2026-09-14 — Exact-once, privacy, and lifecycle hardening
- Browser executed-but-unverified actions remain durable `Running` and require fresh verification; they are never automatically replayed.
- Added audit payload/event trust validation before approval, durable-state, and external-effect boundaries.
- Quarantined browser/provider diagnostics, credential-bearing URL mismatch details, Tavily provider failures, and Nebius transport/cancellation diagnostics.
- Remote dispatch reserves provenance before creation and binds protected results to exact opaque id, remote id, checkpoint, protocol, and authenticated envelope.
- Cancellation is durable and crash-resumable. Only freshly verified provider `Cancelled` becomes cancellation success. Failed/completed cancellation races converge truthfully.
- Added durable audit-outbox protection for result application, cancellation requests, terminal failure/cancellation/expiry, dispatch reservation, and remote-id attachment, with restart/fault-injection coverage.

Selected lifecycle commits:
- `e66381b78752c6141e8d9ac192ea307e48cf0968` — crash-resumable Nebius cancellation.
- `fd763848104e9c5420b4175e5246fbd1e887e8da` — truthful failed cancellation race.
- `563e3e06187720fb0b175eadb50e79df8ede8b2d` / `cd0946c41d1124cc495fea455cdbdd610afa8b19` / `1092b7d548dc20aa650b5defb5119803744bd235` — completed-after-cancel race handling.
- `8bed3481f54fba21c46d604d9f924eb56d05ac6a` / `8145c7bb812777b557608ddb1eef2ce59cfd49fb` / `567aa999f6b5db181820c424942144ddf52eb0ad` — audit-outbox foundation and result integration.
- `9028621ca8430bbeaf8a2f6bc00230d104ca98a2` / `ebf3f30cae8007da8e5ac452de786c9f7486f7cb` — cancellation-request audit outbox.
- `1f4a15b12ef35fc6cb0764425cf10fb9e06a9ea5` / `61cbce3f289f935bb3666757b0fa14d7d4140f64` — remote terminal audit outbox and recovery tests.
- `d9ad5246e3d63a394c9e65dc5300352aadafdc1d` / `8fc33ef3194a4fdb19bc403851a1f566b183b6e6` / `dbde246acea66af694be7eebfe97ece30bac5ee3` — crash-recoverable dispatch reservation/attachment audits and payload ownership.

### 2026-09-14 — Durable external-action and cleanup recovery
- Added `DurableExternalActionKind`, `PendingExternalAction`, CAS identity coverage, and `DurableJobExternalActionIntent` with restart-stable action reuse, audit-first ordering, exact provider-target validation, and fail-closed binding checks.
- Wired Nebius cancellation redrive to one durable action identity. Active provider state reuses the same action; `Cancelling`/terminal provider truth clears it; transport success is never interpreted as cancellation success.
- Added `PendingProtectedPayloadCleanup` and `DurableProtectedPayloadCleanupIntent`; result application and terminal settlement now persist exact cleanup obligations with the state transition and retry partial deletes idempotently after restart.

Selected commits:
- `1a68d07f95a7b1597ef2ebaffe4f71ccaf831a64` / `38ff800354826a15ac17081726e38db0f6e13a65` — action contracts and CAS identity.
- `6278afd91b96f3611e471b49de9dbba065627ec7` / `51af3518cf18cd0a36f924c2aafae6237939be24` — durable action coordinator and restart-stable reuse.
- `bd4083d078586a9234dbecccaba3a3f8aa2e90d8` / `53eda5e196dc95bb9d64f2d3a2313ec37975da5c` — production cancellation-redrive integration and fault tests.
- `1a2d82ac63da3b10c4eb706fa3e6552b62ad4133` / `ecc6a89501090fd7d37aa7ac374fbd86b50bcc9e` — action/audit binding recovery hardening.
- `ed287a3e6864ee11e88bb9d478bffce2ac57d473` / `338fe56587eaa96faa8942e10cbcbea3c894b3b7` / `53f324be012adacce3c8b77f37ec52c238cdcd54` — durable cleanup state/coordinator/CAS identity.
- `862a4be206d4c442396fed6e54ac6f23d5cf5d77` / `0f05aeb2742528491c222878ea19f1cc039953f6` — result cleanup recovery and tests.
- `a0ba803e075acfb523bae037e177d426c4ea7753` / `6504c61199796315e6780ff70064e92de060d345` / `1bad131d138cf1459134e9150e7c5f898ad96693` — terminal/partial cleanup recovery.

### 2026-09-14 — Signed dispatch-binding restart recovery (latest run)
Completed:
- Re-read this ledger first and inspected the current signed-binding publisher, two-phase dispatcher, client runtime, lifecycle reconciler, product cloud coordinator, and binding lifecycle tests before changing code.
- Added `ResearchDispatchBindingRecovery`, a provider-independent recovery boundary that reads the exact protected durable job record and republishes only the persisted `OpaqueWorkItemId -> RemoteJobId` binding. It accepts only active `NebiusServerless` research in `Dispatched` or `CancelRequested`, requires any durable audit marker to already be settled, preserves the persisted TTL, and never invents provider identity.
- Wired `NebiusResearchClientRuntime.ReconcileDispatchedAsync(...)` to repair the binding before lifecycle reconciliation can read Nebius. The existing reconciler still independently guarantees the binding, so the runtime adds an earlier fail-closed boundary rather than weakening the lower layer.
- Hardened `RequestCancellationAsync(...)`: it first drains a stranded dispatch audit locally, then reconstructs/verifies the exact signed binding, and only then allows the cancellation state transition / Nebius `CancelAsync` path. A missing or conflicting binding therefore blocks provider cancellation instead of moving the lifecycle forward without worker authority.
- Hardened `ReconcileCancellationAsync(...)`: after local audit/result recovery, an active `CancelRequested` job must also have its exact binding reconstructed/verified before Nebius GET or cancellation redrive.
- Added focused regressions proving idempotent repeated recovery produces one authoritative binding, a conflicting signed binding with a substituted remote id fails closed without changing durable provenance, successful cancellation observes the repaired binding before provider cancellation, and binding publication failure leaves the job durably `Dispatched` with zero provider cancellation calls.

Files / architecture changed:
- `src/Nvidea.Core/Jobs/ResearchDispatchBindingRecovery.cs` — new local restart-recovery boundary for signed dispatch bindings.
- `src/Nvidea.Core/Jobs/NebiusResearchClientRuntime.cs` — binding recovery is now a prerequisite for dispatched reconciliation and cancellation control-plane paths.
- `tests/Nvidea.Core.Tests/ResearchDispatchBindingRecoveryTests.cs` — focused restart/idempotency/substitution/provider-gating regressions.

Engineering commits this run before this ledger update:
- `ae1a2dbe19066d94b1b0ac4ef9c987d0f1953112` — add durable-state signed binding recovery.
- `74b9203d5c80875fec7cdcccf6015bd1c57a55b9` — enforce binding recovery before remote lifecycle actions.
- `05b75cf601532398c9e792cf110035e890b3f801` — add crash-safe binding recovery regressions.

Validation / evidence this run:
- Before every GitHub mutation, repository metadata reported exact full name `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head was `9ec90333124bc06bf98aa63c2271b2b833023708`; pre-ledger head was `05b75cf601532398c9e792cf110035e890b3f801`, three commits ahead / zero behind.
- Effective pre-ledger diff versus the starting head is limited to three intended files: `NebiusResearchClientRuntime.cs` (+22/-4), new `ResearchDispatchBindingRecovery.cs` (+58), and new `ResearchDispatchBindingRecoveryTests.cs` (+320).
- Static review confirms binding recovery has no `INebiusServerlessJobClient` dependency at all; it cannot accidentally create/list/get/cancel provider work.
- Static review confirms recovery signs only the durable provenance remote id and uses `ResearchDispatchBindingPublisher`, whose existing idempotent GET/PUT race handling and cryptographic verification reject a conflicting authoritative id.
- Static review confirms cancellation cannot reach `CancelAsync` after a binding publication failure because recovery occurs before `NebiusResearchLifecycleReconciler.RequestCancellationAsync(...)`.
- Project targets .NET 8 with `LangVersion=latest` and warnings-as-errors.
- `dotnet`, `csc`, `msbuild`, and `mcs` are unavailable in this execution environment. **No compilation, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.**
- No GitHub Actions workflow and no live/paid Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- Binding recovery reads only the existing protected durable job record and writes only the existing signed opaque-id/remote-id binding; it introduces no plaintext payload, provider credential, model prompt, OS-private context, or new secret store.
- Recovery refuses non-research jobs, empty ids, non-Nebius execution, terminal/local states, unresolved durable audit markers, and lifecycle states other than `Dispatched` / `CancelRequested`.
- A substituted pre-existing signed binding cannot overwrite durable provenance: publisher verification fails closed before any provider action.
- Cancellation now has stricter ordering: dispatch audit durable -> exact worker binding present/verified -> durable cancellation request/audit -> provider cancel.
- Recovery is idempotent and provider-independent, so repeated restart repair cannot replay Serverless Create or generate a second remote job.
- Existing exact external-action, protected-payload cleanup, audit-event identity, cancellation-race, and terminal-state protections remain unchanged.
- Generic low-level `JsonAgentJobStore.SaveAsync(...)` remains trusted infrastructure; product paths should continue preferring constrained CAS/lifecycle APIs.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler is available here; the new Core/test changes are statically reviewed but unexecuted.
- Live Nebius Serverless/Object Storage behavior, authenticated worker execution, signed binding reads, provider catalog drift, real Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- Binding publication itself is an idempotent external write rather than a separately persisted `pending/completed` job marker. The hardened lifecycle paths now reconstruct/verify it before provider use, but a caller that abandons the failed dispatch without ever invoking reconciliation will not proactively repair the binding until the next lifecycle operation.
- `ResearchDispatchBindingCleanup` remains best-effort after terminal settlement. The binding contains no research payload and is signed/TTL-bounded, but cleanup is not represented as its own durable obligation.
- The public `CleanupProtectedPayloadsAsync(...)` compatibility helper remains best-effort; hardened result/terminal paths use durable cleanup instead.

## Single Best Next Task
Harden the worker side of the same crash window: inspect the remote worker bootstrap/binding-resolution path and make startup tolerate a legitimately late signed binding publication with a **bounded, cancellation-aware retry/backoff that never accepts an unsigned/substituted binding and never extends past the work-item/binding expiry**. Add deterministic tests for binding initially absent then appearing, permanent absence/expiry, conflicting binding, cancellation, and bounded retry behavior. This closes the client-recovery half and worker-consumption half of late binding publication without weakening cryptographic authority or provider-side exact-once guarantees.
