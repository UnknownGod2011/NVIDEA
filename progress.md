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
- `DurableJobAuditOutbox` couples exact validated audit intents to the same job CAS as dispatch reservation, remote-id attachment, result application, cancellation requests, and terminal failure/cancellation/expiry.
- `PendingExternalAction` / `DurableJobExternalActionIntent` persist cancellation-redrive ambiguity independently from success state; transport success is never interpreted as provider cancellation success.
- `PendingProtectedPayloadCleanup` / `DurableProtectedPayloadCleanupIntent` persist cleanup obligations with result/terminal state and retry partial deletes idempotently after restart.
- Signed worker dispatch bindings are restart-recoverable from exact durable `Dispatched` / `CancelRequested` provenance before provider observation or cancellation; conflicting substituted bindings fail closed.
- Worker-side dispatch binding consumption now supports bounded exponential retry for legitimately late publication, is capped by both configured wait budget and work-item expiry, rejects bindings that outlive the work item, and receives real process cancellation through the worker entrypoint.
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
- `8bed3481f54fba21c46d604d9f924eb56d05ac6a` / `8145c7bb812777b557608ddb1eef2ce59cfd49fb` / `567aa999f6b5db181820c424942144ddf52eb0ad` — audit-outbox foundation and result integration.
- `9028621ca8430bbeaf8a2f6bc00230d104ca98a2` / `ebf3f30cae8007da8e5ac452de786c9f7486f7cb` — cancellation-request audit outbox.
- `1f4a15b12ef35fc6cb0764425cf10fb9e06a9ea5` / `61cbce3f289f935bb3666757b0fa14d7d4140f64` — remote terminal audit outbox and recovery tests.
- `d9ad5246e3d63a394c9e65dc5300352aadafdc1d` / `8fc33ef3194a4fdb19bc403851a1f566b183b6e6` / `dbde246acea66af694be7eebfe97ece30bac5ee3` — crash-recoverable dispatch reservation/attachment audits and payload ownership.

### 2026-09-14 — Durable external-action and cleanup recovery
- Added `DurableExternalActionKind`, `PendingExternalAction`, CAS identity coverage, and `DurableJobExternalActionIntent` with restart-stable action reuse, audit-first ordering, exact provider-target validation, and fail-closed binding checks.
- Wired Nebius cancellation redrive to one durable action identity. Active provider state reuses the same action; `Cancelling`/terminal provider truth clears it.
- Added `PendingProtectedPayloadCleanup` and `DurableProtectedPayloadCleanupIntent`; result application and terminal settlement persist exact cleanup obligations and retry partial deletes idempotently after restart.

Selected commits:
- `1a68d07f95a7b1597ef2ebaffe4f71ccaf831a64` / `38ff800354826a15ac17081726e38db0f6e13a65` — action contracts and CAS identity.
- `6278afd91b96f3611e471b49de9dbba065627ec7` / `51af3518cf18cd0a36f924c2aafae6237939be24` — durable action coordinator and restart-stable reuse.
- `bd4083d078586a9234dbecccaba3a3f8aa2e90d8` / `53eda5e196dc95bb9d64f2d3a2313ec37975da5c` — production cancellation-redrive integration and fault tests.
- `1a2d82ac63da3b10c4eb706fa3e6552b62ad4133` / `ecc6a89501090fd7d37aa7ac374fbd86b50bcc9e` — action/audit binding recovery hardening.
- `ed287a3e6864ee11e88bb9d478bffce2ac57d473` / `338fe56587eaa96faa8942e10cbcbea3c894b3b7` / `53f324be012adacce3c8b77f37ec52c238cdcd54` — durable cleanup state/coordinator/CAS identity.
- `862a4be206d4c442396fed6e54ac6f23d5cf5d77` / `0f05aeb2742528491c222878ea19f1cc039953f6` — result cleanup recovery and tests.
- `a0ba803e075acfb523bae037e177d426c4ea7753` / `6504c61199796315e6780ff70064e92de060d345` / `1bad131d138cf1459134e9150e7c5f898ad96693` — terminal/partial cleanup recovery.

### 2026-09-14 — Signed dispatch-binding restart recovery
- Added provider-independent `ResearchDispatchBindingRecovery`, which reconstructs the exact signed `OpaqueWorkItemId -> RemoteJobId` binding from durable `Dispatched` / `CancelRequested` provenance and rejects conflicts.
- Wired client reconciliation/cancellation paths to repair/verify binding before any Nebius observation or cancellation side effect.
- Added idempotency, substituted-id, publication-failure, and provider-gating regressions.

Selected commits:
- `ae1a2dbe19066d94b1b0ac4ef9c987d0f1953112` — durable-state signed binding recovery.
- `74b9203d5c80875fec7cdcccf6015bd1c57a55b9` — binding recovery before remote lifecycle actions.
- `05b75cf601532398c9e792cf110035e890b3f801` — crash-safe binding recovery regressions.
- `8d2bb3a00d5852fd76275fd210f5ac4376235437` — persisted recovery ledger.

### 2026-09-14 — Bounded worker-side late-binding recovery (latest run)
Completed:
- Re-read this ledger first, inspected recent commits and the current worker bootstrap, signed-binding protector/publisher/waiter, runtime timing configuration, directory transport, and existing binding/trust tests before changing code.
- Hardened `ResearchDispatchBindingWaiter` with bounded exponential retry for an initially absent signed binding. Retry starts at the configured poll interval, doubles without overflow, and caps at 30 seconds.
- Added an overload that accepts the protected work-item expiry as an absolute upper bound. Effective deadline is `min(start + configured max wait, work-item expiry)`; transport-visible expiry can only shorten waiting and can never extend trust or the configured budget.
- Enforced the deadline both before and after each transport read so a slow read cannot cause the worker to accept a binding after its execution window has expired.
- A cryptographically valid signed binding is now also rejected if its signed `ExpiresAt` exceeds the associated protected work-item expiry. This prevents control-plane authority from outliving the payload lifecycle supplied to the worker.
- Updated `Nvidea.Worker` bootstrap to read the staged protected work-item envelope before binding resolution, fail fast if it is unavailable, and pass its expiry only as a shortening deadline. Payload authenticity is still established later by the existing decrypt/authentication boundary; the transport-visible expiry is explicitly not treated as trust.
- Found and fixed a production wiring gap: the waiter and worker APIs accepted cancellation, but `Program` passed `CancellationToken.None`. One process cancellation token is now threaded through staged-envelope read, binding retry, and `NebiusResearchWorker.ExecuteOneStageAsync`; Ctrl+C cancels in-flight retry/work rather than waiting for timeout.
- Added deterministic focused regressions for delayed binding appearance after multiple reads, permanent absence bounded by work-item expiry, a signed binding outliving its work item, and cancellation during retry without another transport read.

Files / architecture changed:
- `src/Nvidea.Core/Jobs/ResearchDispatchBinding.cs` — lifetime-aware bounded exponential worker wait and binding/work-item lifetime trust check.
- `src/Nvidea.Worker/Program.cs` — staged envelope expiry bound plus process cancellation propagation.
- `tests/Nvidea.Core.Tests/ResearchDispatchBindingTests.cs` — delayed publication, expiry, lifetime mismatch, and cancellation regressions.

Engineering commits this run before this ledger update:
- `3f766515c1403ad56a33941bccf10cf056a10731` — harden worker dispatch binding wait lifetime/backoff.
- `04cf3ab09fd8a84d0d473b358c331ee99c5bf0af` — bound worker binding recovery by staged work-item expiry.
- `517a493a9d019e5e2740601073800d3cc9dc159c` — add bounded worker dispatch-binding regressions.
- `aae28e3e930dd3499df9717f56e938e713bb3e2d` — thread worker process cancellation through binding recovery and execution.

Validation / evidence this run:
- Before every successful GitHub mutation, repository metadata reported exact full name `UnknownGod2011/NVIDEA`; no other repository was mutated. One initial core-file write was rejected with HTTP 409 due a stale blob SHA and changed nothing; the current blob was re-read before retrying.
- Starting head was `8d2bb3a00d5852fd76275fd210f5ac4376235437`; pre-ledger head is `aae28e3e930dd3499df9717f56e938e713bb3e2d`, four commits ahead / zero behind.
- Effective pre-ledger diff is restricted to three intended files: `ResearchDispatchBinding.cs` (+77/-8), `Program.cs` (+27/-2), and `ResearchDispatchBindingTests.cs` (+132).
- Static review confirms unsigned/substituted binding behavior remains fail-closed because every observed binding still passes `ResearchDispatchBindingProtector.Verify(...)` before use.
- Static review confirms an untrusted transport expiry cannot make the worker wait longer: it is used only when earlier than the configured deadline. A maliciously shortened expiry can cause denial-of-service, but cannot increase authority or execution lifetime.
- Static review confirms cancellation is checked before transport reads and passed to both transport operations and backoff delay, and production now supplies a cancellable token instead of `CancellationToken.None`.
- `dotnet`, `csc`, `msbuild`, and `mcs` are unavailable in this execution environment. **No compilation, xUnit, WPF, Worker, evaluator, or live integration PASS is claimed.**
- No GitHub Actions workflow and no live/paid Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- Late-binding retry handles only signed control-plane metadata; it does not expose prompts, evidence, user context, provider credentials, or decrypted research payloads.
- Binding signature/protocol/opaque-id/remote-id/deterministic-name checks remain mandatory. Retry never converts invalid signed content into a transient condition; an observed invalid/conflicting binding fails immediately.
- Work-item expiry is transport-visible before decryption and therefore is not authenticated at bootstrap. NVIDEA uses it only as a stricter upper bound; increasing it cannot exceed the independent configured wait budget, while decreasing it only fails closed earlier.
- A binding whose own signed lifetime exceeds the staged work-item lifetime fails closed even if its signature is valid.
- Deadline is checked after each transport read, so a blocking/slow read cannot return late and still authorize execution.
- Backoff is bounded and cancellation-aware, preventing tight polling during delayed publication and allowing shutdown to interrupt delay/provider work.
- Existing exact external-action, protected-payload cleanup, dispatch-audit, cancellation-race, and terminal-state protections remain unchanged.
- Generic low-level `JsonAgentJobStore.SaveAsync(...)` remains trusted infrastructure; product paths should continue preferring constrained CAS/lifecycle APIs.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler is available here; the new Core/Worker/test changes are statically reviewed but unexecuted.
- Live Nebius Serverless/Object Storage behavior, mounted-volume visibility/latency, authenticated worker execution, signed binding reads, provider catalog drift, real Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- `Console.CancelKeyPress` gives the worker a real cancellation path for interactive/process console cancellation, but explicit Serverless/container `SIGTERM` handling has not yet been proven or wired with `PosixSignalRegistration`; abrupt platform termination may therefore still bypass graceful cancellation depending on host behavior.
- Binding polling retries an absent binding, but an actual transient mounted-volume `IOException` currently propagates immediately rather than being classified/retried. Invalid/malformed/cryptographically conflicting binding data must continue to fail immediately and must never be retried as transient.
- Binding publication remains an idempotent external write rather than a separately persisted pending/completed marker; hardened client lifecycle paths reconstruct it before provider use.
- `ResearchDispatchBindingCleanup` remains best-effort after terminal settlement. The binding contains no research payload and is signed/TTL-bounded, but cleanup is not represented as its own durable obligation.

## Single Best Next Task
Harden **remote-worker shutdown and mounted-transport resilience** without weakening trust: add explicit bounded `SIGTERM`/process-shutdown cancellation for the Serverless worker, classify only genuinely transient binding-read I/O failures for retry under the existing absolute deadline/backoff, and add deterministic tests proving cancellation interrupts retry, transient I/O can recover, permanent/cryptographic failures fail closed immediately, and no retry extends past work-item expiry.
