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
- Worker-side dispatch binding consumption uses bounded exponential retry for absent bindings and transient mounted-volume `IOException`, is capped by both configured wait budget and work-item expiry, and never retries malformed or cryptographically invalid content.
- Remote worker process cancellation is threaded through staged-envelope reads, binding recovery, and worker execution. Linux/macOS/FreeBSD SIGTERM now receives a bounded 20-second cooperative grace window before forced exit; repeated SIGTERM exits immediately.
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
- Durable audit-outbox protection now covers result application, cancellation requests, terminal failure/cancellation/expiry, dispatch reservation, and remote-id attachment.
- Durable external-action intent protects cancellation redrive ambiguity; durable protected-payload cleanup intent protects partial result/work-item deletion across restart.
- Signed dispatch bindings are recoverable from durable job provenance before provider observation or cancellation.

Selected lifecycle commits:
- `e66381b78752c6141e8d9ac192ea307e48cf0968` — crash-resumable Nebius cancellation.
- `8bed3481f54fba21c46d604d9f924eb56d05ac6a` / `9028621ca8430bbeaf8a2f6bc00230d104ca98a2` / `1f4a15b12ef35fc6cb0764425cf10fb9e06a9ea5` — audit-outbox foundation and lifecycle integration.
- `6278afd91b96f3611e471b49de9dbba065627ec7` / `bd4083d078586a9234dbecccaba3a3f8aa2e90d8` — durable external actions and cancellation-redrive integration.
- `338fe56587eaa96faa8942e10cbcbea3c894b3b7` / `862a4be206d4c442396fed6e54ac6f23d5cf5d77` / `a0ba803e075acfb523bae037e177d426c4ea7753` — protected-payload cleanup durability.
- `d9ad5246e3d63a394c9e65dc5300352aadafdc1d` / `dbde246acea66af694be7eebfe97ece30bac5ee3` — crash-recoverable dispatch reservation/attachment audits.
- `ae1a2dbe19066d94b1b0ac4ef9c987d0f1953112` / `74b9203d5c80875fec7cdcccf6015bd1c57a55b9` — signed binding restart recovery before provider effects.

### 2026-09-14 — Bounded worker-side late-binding recovery
- `ResearchDispatchBindingWaiter` retries initially absent binding publication with bounded exponential backoff, capped at 30 seconds per delay.
- Effective worker binding deadline is `min(start + configured max wait, protected work-item expiry)`; transport-visible expiry can only shorten waiting.
- Deadline is checked before and after transport reads, preventing a slow read from authorizing execution after expiry.
- Signed binding lifetime may not exceed the associated work-item lifetime.
- Worker bootstrap now threads a real process cancellation token through staged-envelope read, binding retry, and worker execution.
- Focused regressions cover delayed publication, work-item expiry, lifetime mismatch, and cancellation during retry.

Selected commits:
- `3f766515c1403ad56a33941bccf10cf056a10731` — bounded binding backoff/lifetime enforcement.
- `04cf3ab09fd8a84d0d473b358c331ee99c5bf0af` — bind wait to staged work-item expiry.
- `517a493a9d019e5e2740601073800d3cc9dc159c` — late-binding regressions.
- `aae28e3e930dd3499df9717f56e938e713bb3e2d` — process cancellation propagation.

### 2026-09-14 — Mounted transport resilience and bounded SIGTERM shutdown (latest run)
Completed:
- Re-read this ledger first and inspected current `ResearchDispatchBindingWaiter`, `DirectoryProtectedResearchTransport`, worker bootstrap, tests, project target framework, and latest commits before changing code.
- Verified via current Microsoft guidance that explicit `PosixSignalRegistration` is the appropriate low-level mechanism when application code needs direct termination-signal handling rather than a higher-level host lifetime.
- Hardened `ResearchDispatchBindingWaiter` so only `IOException` thrown by the dispatch-binding transport read is treated as transient. The same absolute deadline and exponential backoff are reused; a mount outage therefore cannot extend authorization or worker lifetime.
- Cancellation has precedence over retry: if shutdown races a transport `IOException`, the cancellation token is re-checked immediately and the worker stops without another read.
- Malformed transport state (`InvalidOperationException`) and protocol/signature/identity failures remain outside the I/O catch and fail closed immediately; invalid signed content is never converted into a transient retry.
- Added deterministic regressions for transient I/O recovery, persistent I/O bounded by work-item expiry, permanent transport-validation failure with exactly one read, cryptographic substitution with exactly one read, and shutdown racing transient I/O.
- Added explicit POSIX SIGTERM handling in `Nvidea.Worker`. First SIGTERM suppresses immediate termination, cancels the existing process token, and starts a 20-second watchdog. Normal cooperative completion cancels that watchdog; a second SIGTERM or grace-period expiry forces exit code 143. Windows continues to use `Console.CancelKeyPress` for this executable path.
- Added explicit `OperationCanceledException` handling for process shutdown so cancellation is reported as worker cancellation rather than a generic provider/worker failure.

Files / architecture changed:
- `src/Nvidea.Core/Jobs/ResearchDispatchBinding.cs` — narrow transient-I/O classification plus deadline/cancellation-preserving retry helper.
- `tests/Nvidea.Core.Tests/ResearchDispatchBindingTests.cs` — mounted-volume I/O, permanent-failure, cryptographic fail-closed, expiry, and shutdown-race regressions.
- `src/Nvidea.Worker/Program.cs` — bounded POSIX SIGTERM handling and shutdown-specific cancellation outcome.

Engineering commits this run before this ledger update:
- `21cbdb56a04f3b903a3d466fa63f33f43b8bcdf4` — retry transient worker binding I/O within deadline.
- `451368b4e74be5a18755b6b68d308753a0065109` — mounted binding transport resilience regressions.
- `a2b63b02dee5a692199a380091fdc182556562e6` — bounded SIGTERM shutdown for remote worker.

Validation / evidence this run:
- Before every GitHub mutation, repository metadata reported exact full name `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head was `486b7ff52ff9b4e3bd5c9d49044f4b8a14977505`; pre-ledger head is `a2b63b02dee5a692199a380091fdc182556562e6`, three commits ahead / zero behind.
- Effective pre-ledger diff is restricted to three intended files: `ResearchDispatchBinding.cs`, `ResearchDispatchBindingTests.cs`, and `Program.cs`.
- Static review confirms retry catches only `IOException` from `_transport.GetAsync(...)`; signature/protocol/lifetime verification occurs after the catch and therefore still fails immediately.
- Static review confirms every retry remains under the original absolute deadline and cancellation token. Persistent I/O cannot extend past work-item expiry.
- Static review confirms first SIGTERM cancels the same token consumed by binding reads/backoff and worker execution; forced termination is bounded to 20 seconds if a dependency ignores cancellation.
- `src/Nvidea.Worker/Nvidea.Worker.csproj` targets `net8.0`, where `PosixSignalRegistration` is available.
- `dotnet`, `csc`, `msbuild`, and `mcs` remain unavailable in this execution environment. **No compilation, xUnit, Worker, WPF, evaluator, or live integration PASS is claimed.**
- No GitHub Actions workflow and no live/paid Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- I/O retry handles only signed control-plane binding reads; it does not expose prompts, evidence, user context, credentials, or decrypted research payloads.
- Retry classification is intentionally narrow: `IOException` only at the transport read boundary. JSON/size/protocol/signature/identity failures are not retried.
- Binding signature/protocol/opaque-id/remote-id/deterministic-name/lifetime checks remain mandatory after any successful read.
- Work-item expiry remains transport-visible and unauthenticated at bootstrap; it can only shorten the wait budget, never extend authority.
- SIGTERM does not introduce a separate execution path. It cancels the same token already used by staged-envelope read, binding wait, Tavily/Nebius work, and worker execution.
- The watchdog bounds cooperative shutdown if a downstream dependency ignores cancellation, while a repeated SIGTERM gives operators an immediate escape hatch.
- Existing external-action, protected-payload cleanup, audit-outbox, cancellation-race, and terminal-state protections remain unchanged.
- Generic low-level `JsonAgentJobStore.SaveAsync(...)` remains trusted infrastructure; product paths should continue preferring constrained CAS/lifecycle APIs.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler is available here; the new Core/Worker/test changes are statically reviewed but unexecuted.
- Live Nebius Serverless/Object Storage behavior, mounted-volume latency/failure modes, authenticated worker execution, actual SIGTERM delivery/grace behavior, signed binding reads, provider catalog drift, real Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- The initial protected work-item envelope read in `Nvidea.Worker/Program.cs` is still a one-shot mounted-volume read before the binding waiter. A legitimate mount-propagation delay or transient `IOException` at that earlier boundary can still terminate the worker even though binding reads are now resilient.
- Binding publication remains an idempotent external write rather than a separately persisted pending/completed marker; hardened client lifecycle paths reconstruct it before provider use.
- `ResearchDispatchBindingCleanup` remains best-effort after terminal settlement. The binding contains no research payload and is signed/TTL-bounded, but cleanup is not represented as its own durable obligation.

## Single Best Next Task
Harden **initial protected work-item acquisition** with a dedicated bounded, cancellation-aware worker-side loader: tolerate only absent-object propagation and transient `IOException` under an independent/configured bootstrap deadline, reject malformed/oversized envelopes immediately, preserve the rule that transport-visible expiry cannot extend authority, and add deterministic tests proving delayed appearance/transient I/O recovery, cancellation, deadline exhaustion, and fail-closed malformed envelope behavior before binding resolution begins.
