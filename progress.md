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
- Worker dispatch-binding consumption uses bounded exponential retry for absent bindings and transient mounted-volume `IOException`, capped by both configured wait budget and work-item expiry, and never retries malformed or cryptographically invalid content.
- Worker protected work-item bootstrap now also uses a separate bounded retry layer: only absent-object propagation and mounted-volume `IOException` are retried; protocol/identity/lifetime/transport-validation failures fail closed.
- Remote worker process cancellation is threaded through work-item bootstrap, binding recovery, and worker execution. POSIX SIGTERM gets a bounded 20-second cooperative grace window before forced exit; repeated SIGTERM exits immediately.
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
- Durable audit-outbox protection covers result application, cancellation requests, terminal failure/cancellation/expiry, dispatch reservation, and remote-id attachment.
- Durable external-action intent protects cancellation-redrive ambiguity; durable protected-payload cleanup intent protects partial result/work-item deletion across restart.
- Signed dispatch bindings are recoverable from durable job provenance before provider observation or cancellation.

Selected lifecycle commits:
- `e66381b78752c6141e8d9ac192ea307e48cf0968` — crash-resumable Nebius cancellation.
- `8bed3481f54fba21c46d604d9f924eb56d05ac6a` / `9028621ca8430bbeaf8a2f6bc00230d104ca98a2` / `1f4a15b12ef35fc6cb0764425cf10fb9e06a9ea5` — audit-outbox foundation and lifecycle integration.
- `6278afd91b96f3611e471b49de9dbba065627ec7` / `bd4083d078586a9234dbecccaba3a3f8aa2e90d8` — durable external actions and cancellation-redrive integration.
- `338fe56587eaa96faa8942e10cbcbea3c894b3b7` / `862a4be206d4c442396fed6e54ac6f23d5cf5d77` / `a0ba803e075acfb523bae037e177d426c4ea7753` — protected-payload cleanup durability.
- `d9ad5246e3d63a394c9e65dc5300352aadafdc1d` / `dbde246acea66af694be7eebfe97ece30bac5ee3` — crash-recoverable dispatch reservation/attachment audits.
- `ae1a2dbe19066d94b1b0ac4ef9c987d0f1953112` / `74b9203d5c80875fec7cdcccf6015bd1c57a55b9` — signed binding restart recovery before provider effects.

### 2026-09-14 — Bounded worker binding recovery and shutdown
- `ResearchDispatchBindingWaiter` retries absent publication and only transient `IOException` under exponential backoff.
- Effective binding deadline is `min(start + configured max wait, protected work-item expiry)` and is checked before and after reads.
- Signed binding lifetime may not exceed the associated work-item lifetime.
- Process cancellation propagates through worker operations; first SIGTERM cancels cooperatively with a 20-second watchdog, second SIGTERM exits immediately.
- Regressions cover delayed binding publication, transient/permanent mounted I/O, expiry, lifetime mismatch, cryptographic substitution, and cancellation races.

Selected commits:
- `3f766515c1403ad56a33941bccf10cf056a10731` — bounded binding backoff/lifetime enforcement.
- `04cf3ab09fd8a84d0d473b358c331ee99c5bf0af` — binding wait capped by staged work-item expiry.
- `517a493a9d019e5e2740601073800d3cc9dc159c` — late-binding regressions.
- `aae28e3e930dd3499df9717f56e938e713bb3e2d` — process cancellation propagation.
- `21cbdb56a04f3b903a3d466fa63f33f43b8bcdf4` / `451368b4e74be5a18755b6b68d308753a0065109` — mounted binding I/O resilience and tests.
- `a2b63b02dee5a692199a380091fdc182556562e6` — bounded SIGTERM shutdown.

### 2026-09-15 — Bounded protected work-item bootstrap (latest run)
Completed:
- Re-read this ledger first and inspected the current worker startup path, protected work-item transport, envelope protocol, binding waiter, runtime configuration, tests, tree, and recent commits before changing code.
- Added `WorkerProtectedResearchWorkItemLoader`, a dedicated worker bootstrap boundary with its own absolute deadline and exponential backoff capped at 15 seconds.
- The loader retries **only** two availability-class outcomes: `null` (create-once object not yet visible through the mount) and `IOException` from the transport read. Cancellation is re-checked immediately after I/O failure and before any retry.
- Successful reads are rejected immediately when protocol version is wrong, opaque work-item identity is substituted, lifetime is non-positive/over 24 hours, or transport-visible expiry is already elapsed. Existing transport malformed/oversized JSON errors remain outside the retry catch and therefore fail closed.
- Preserved the trust model explicitly: transport-visible lifecycle metadata is not treated as authenticated research payload state. It can reject or shorten later waiting, never establish plaintext authenticity; AES-GCM work-item authentication/decryption remains downstream in `ResearchWorkItemProtector.Unprotect`.
- Extended `NebiusResearchWorkerRuntimeConfiguration` with `NVIDEA_WORK_ITEM_POLL_SECONDS` and `NVIDEA_WORK_ITEM_WAIT_SECONDS`, defaulting to 1 second / 30 seconds. Validation occurs before worker/provider secret reads, with a 100 ms–30 second poll bound and a maximum five-minute bootstrap window.
- Kept the positional runtime-record constructor unchanged for compatibility; work-item timings are added as init properties.
- Replaced `Nvidea.Worker`'s previous one-shot work-item `GetAsync` with the bounded loader, using the existing process cancellation token. Binding recovery starts only after a usable staged envelope is acquired.
- Added focused loader regressions for delayed appearance, transient I/O recovery, cancellation racing I/O, deadline exhaustion, permanent transport validation failure, opaque-id substitution, invalid protocol, and expired metadata.
- Added runtime configuration regressions proving the new timing values parse correctly and invalid/excessive windows fail before worker private key, Nebius API key, or Tavily API key are read.
- Static review caught an invalid `IReadOnlyList<T>.IndexOf` use in the new timing-test helper; replaced it with an explicit loop before finalizing the run.

Files / architecture changed:
- `src/Nvidea.Core/Jobs/WorkerProtectedResearchWorkItemLoader.cs` — new bounded, cancellation-aware protected-envelope bootstrap boundary.
- `src/Nvidea.Core/Jobs/NebiusResearchWorkerRuntimeConfiguration.cs` — independent work-item retry configuration and pre-secret validation.
- `src/Nvidea.Worker/Program.cs` — production integration before signed dispatch-binding resolution.
- `tests/Nvidea.Core.Tests/WorkerProtectedResearchWorkItemLoaderTests.cs` — bootstrap propagation/I/O/security/deadline regressions.
- `tests/Nvidea.Core.Tests/NebiusResearchWorkerBootstrapTimingTests.cs` — timing and credential-ordering regressions.

Engineering commits this run before this ledger update:
- `bd7a22e94961f0024d7704312b1c873bb2fd4a1a` — add bounded worker work-item bootstrap loader.
- `6fd474d39e28d5e1e2ddb123db794413e4c5032d` — configure bounded worker work-item bootstrap.
- `ca673e38732b011d2bfd38ef8260d526a408ba28` — route production worker startup through the loader.
- `b8a1b54311e07487f18f90e9ee931296e89a6f43` — worker bootstrap resilience regressions.
- `ffce8a7c2b6e16192d35458572b97320933eba7f` / `c5d14d30174ae09fbbb9f98392367511818ff86a` — timing/credential-order tests and static helper correction.

Validation / evidence this run:
- Before every GitHub mutation, repository metadata reported exact full name `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head was `47db2edba06327fcf30ce7d55384cd1ee3334a8a`; pre-ledger head is `c5d14d30174ae09fbbb9f98392367511818ff86a`, six linear commits later.
- Recursive tree inspection confirms the five intended implementation/test files and their current blobs on `main`.
- Static review confirms `WorkerProtectedResearchWorkItemLoader` catches only `IOException` from `_transport.GetAsync`; malformed/oversized transport errors, protocol mismatch, identity substitution, invalid lifetime, and expired envelopes do not enter retry handling.
- Static review confirms every retry is bounded by one absolute bootstrap deadline and process cancellation; no retry path extends authorization.
- Static review confirms the subsequent binding waiter retains its own independent configured deadline and can only be shortened by the staged envelope expiry.
- Container validation remains unavailable: `dotnet`, `csc`, and `msbuild` are not installed, and direct `git clone` failed because the container could not resolve `github.com`. **No compilation, xUnit, Worker, WPF, evaluator, or live-integration PASS is claimed.**
- No GitHub Actions workflow and no live/paid Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- The new retry layer handles only encrypted envelope transport availability; it does not log or expose research plaintext, prompts, evidence, API keys, or decrypted state.
- Retry classification is intentionally narrow: absent object and `IOException` only. JSON/size/protocol/identity/lifetime failures fail immediately.
- Cancellation has precedence over retry, including an `IOException` race during SIGTERM/Ctrl+C shutdown.
- Transport-visible `CreatedAt`/`ExpiresAt` remain explicitly unauthenticated until decryption. Manipulation can at most cause fail-closed denial/shortening at bootstrap; it cannot extend the independent configured binding or work-item bootstrap deadline.
- The client-signed dispatch binding authenticates `OpaqueWorkItemId -> RemoteJobId` and lifetime, but currently does **not** cryptographically commit to the exact protected work-item envelope bytes. This means the next security review should examine mount-content substitution / TOCTOU between staged-envelope observation and downstream decryption rather than assuming the signed control-plane binding authenticates envelope content.
- Existing external-action, protected-payload cleanup, audit-outbox, cancellation-race, terminal-state, signed remote-id binding, and provider-diagnostic protections remain unchanged.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler is available in this execution environment; current Core/Worker/test changes are statically reviewed but unexecuted.
- Direct container `git clone` is presently DNS-blocked, so local repository compilation cannot be bootstrapped through clone either.
- Live Nebius Serverless/Object Storage mounted-volume propagation/reconnect behavior, authenticated worker execution, real SIGTERM delivery/grace behavior, provider catalog drift, real Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- The signed dispatch binding protects the authoritative Nebius remote job ID but does not include a digest of the encrypted work-item envelope. The envelope is AES-GCM authenticated to the worker after decryption, but a writable transport plus knowledge of the worker public key deserves a dedicated sender-authenticity/TOCTOU review.
- Binding publication remains an idempotent external write rather than a separately persisted pending/completed marker; hardened client lifecycle paths reconstruct it before provider use.
- `ResearchDispatchBindingCleanup` remains best-effort after terminal settlement. The binding contains no research payload and is signed/TTL-bounded, but cleanup is not represented as its own durable obligation.

## Single Best Next Task
Harden **protected work-item sender authenticity and mount TOCTOU**: determine the cleanest backward-compatible way for the client-signed dispatch binding (or a separate signed manifest) to commit to a canonical SHA-256 digest of the exact encrypted `ProtectedResearchWorkItemEnvelope`; make the worker verify that commitment immediately before execution/decryption, ensure restart/republication preserves the same digest, and add adversarial tests proving a writable shared transport cannot substitute a different validly encrypted envelope for the same opaque ID without detection.
