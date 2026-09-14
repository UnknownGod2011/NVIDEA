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
- Remote research uses encrypted opaque work items, two-phase dispatch, lifecycle reconciliation, crash-resumable cancellation, exact-once protected-result ingestion, cancellation-vs-terminal race handling, Nebius Object Storage, and Serverless-mounted worker transport.
- `DurableJobAuditOutbox` couples exact validated audit intents to the same job CAS as dispatch reservation, remote-id attachment, result application, cancellation requests, and terminal failure/cancellation/expiry.
- `PendingExternalAction` / `DurableJobExternalActionIntent` persist cancellation-redrive ambiguity independently from success state; transport success is never interpreted as provider cancellation success.
- `PendingProtectedPayloadCleanup` / `DurableProtectedPayloadCleanupIntent` persist cleanup obligations with result/terminal state and retry partial deletes idempotently after restart.
- Signed worker dispatch bindings are restart-recoverable from exact durable `Dispatched` / `CancelRequested` provenance before provider observation or cancellation; conflicting substituted bindings fail closed.
- **Dispatch-binding V2 now cryptographically commits to the exact protected work-item envelope.** The trusted client computes a canonical SHA-256 commitment, stores it in protected CAS-versioned job state before Nebius Create, and signs `{opaque id, remote job id/name, envelope digest, lifetime}`. Restart recovery signs only the original durable digest and never re-hashes mutable shared transport state.
- The hardened worker requires V2 for execution, verifies the staged encrypted envelope against the signed digest before decryption/provider work, and pins that verified envelope in memory so downstream execution cannot re-read a substituted object from the shared mount.
- Legacy V1 bindings remain verifiable only for bounded pre-upgrade lifecycle/cancellation compatibility; they do not authorize the hardened worker execution path.
- Worker dispatch-binding consumption uses bounded exponential retry for absent bindings and transient mounted-volume `IOException`, capped by configured wait budget and work-item expiry, and never retries malformed or cryptographically invalid content.
- Worker protected work-item bootstrap has a separate bounded retry layer: only absent-object propagation and mounted-volume `IOException` are retried; protocol/identity/lifetime/transport-validation failures fail closed.
- Remote worker process cancellation is threaded through bootstrap, binding recovery, and execution. POSIX SIGTERM gets a bounded 20-second cooperative grace window before forced exit; repeated SIGTERM exits immediately.
- Deterministic judging tools include `Nvidea.PersonalAiDemoEval`, `Nvidea.PersonalAiAdversarialEval`, `Nvidea.JudgingEvidenceVerifier`, `Nvidea.DemoPackageValidator`, and `Nvidea.NebiusModelCatalogCheck`.

## Persistent Progress History

### 2026-09-06 to 2026-09-12 — Core product and judging infrastructure
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, capability permissions/audit, durable jobs, Playwright browser execution, DPAPI state protection, persistent browser sessions/downloads, crash recovery, encrypted Nebius remote execution, two-phase dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, local voice, semantic-memory migration UX, judging/evaluator tooling, protocol trust, endpoint/redirect trust, deployment preflight, and open-source/demo documentation.

### 2026-09-13 to 2026-09-14 — Exact-once, privacy, and lifecycle hardening
- Browser executed-but-unverified actions remain durable `Running` and require fresh verification; they are never automatically replayed.
- Added audit payload/event trust validation before approval, durable-state, and external-effect boundaries.
- Quarantined browser/provider diagnostics and credential-bearing provider failure details.
- Remote dispatch reserves provenance before creation and binds protected results to exact opaque id, remote id, checkpoint, protocol, and authenticated result envelope.
- Cancellation is durable/crash-resumable; only freshly verified provider `Cancelled` becomes cancellation success.
- Durable audit-outbox protection covers result application, cancellation requests, terminal states, dispatch reservation, and remote-id attachment.
- Durable external-action intent protects cancellation-redrive ambiguity; durable payload-cleanup intent protects partial result/work-item deletion across restart.
- Signed dispatch bindings are recoverable from durable job provenance before provider observation or cancellation.

Selected lifecycle commits:
- `e66381b78752c6141e8d9ac192ea307e48cf0968` — crash-resumable Nebius cancellation.
- `8bed3481f54fba21c46d604d9f924eb56d05ac6a` / `9028621ca8430bbeaf8a2f6bc00230d104ca98a2` / `1f4a15b12ef35fc6cb0764425cf10fb9e06a9ea5` — audit-outbox foundation and lifecycle integration.
- `6278afd91b96f3611e471b49de9dbba065627ec7` / `bd4083d078586a9234dbecccaba3a3f8aa2e90d8` — durable external actions and cancellation-redrive integration.
- `338fe56587eaa96faa8942e10cbcbea3c894b3b7` / `862a4be206d4c442396fed6e54ac6f23d5cf5d77` / `a0ba803e075acfb523bae037e177d426c4ea7753` — protected-payload cleanup durability.
- `d9ad5246e3d63a394c9e65dc5300352aadafdc1d` / `dbde246acea66af694be7eebfe97ece30bac5ee3` — crash-recoverable dispatch reservation/attachment audits.
- `ae1a2dbe19066d94b1b0ac4ef9c987d0f1953112` / `74b9203d5c80875fec7cdcccf6015bd1c57a55b9` — signed binding restart recovery before provider effects.

### 2026-09-14 — Bounded worker binding recovery and shutdown
- Binding publication and mounted-volume `IOException` use bounded exponential backoff.
- Effective binding deadline is `min(start + configured max wait, protected work-item expiry)` and is checked before/after reads.
- Signed binding lifetime may not exceed the associated work-item lifetime.
- Process cancellation propagates through worker operations; SIGTERM has a 20-second watchdog.

Selected commits:
- `3f766515c1403ad56a33941bccf10cf056a10731` — bounded binding backoff/lifetime enforcement.
- `04cf3ab09fd8a84d0d473b358c331ee99c5bf0af` — binding wait capped by staged work-item expiry.
- `517a493a9d019e5e2740601073800d3cc9dc159c` — late-binding regressions.
- `aae28e3e930dd3499df9717f56e938e713bb3e2d` — process cancellation propagation.
- `21cbdb56a04f3b903a3d466fa63f33f43b8bcdf4` / `451368b4e74be5a18755b6b68d308753a0065109` — mounted binding I/O resilience and tests.
- `a2b63b02dee5a692199a380091fdc182556562e6` — bounded SIGTERM shutdown.

### 2026-09-15 — Bounded protected work-item bootstrap
- Added `WorkerProtectedResearchWorkItemLoader` with absolute deadline and exponential backoff.
- Retries only not-yet-visible objects and transport `IOException`; malformed/substituted/expired content fails closed.
- Added `NVIDEA_WORK_ITEM_POLL_SECONDS` / `NVIDEA_WORK_ITEM_WAIT_SECONDS`, validated before worker/provider secret reads.
- Replaced worker one-shot work-item read with bounded loading before binding recovery.

Selected commits:
- `bd7a22e94961f0024d7704312b1c873bb2fd4a1a` — bounded bootstrap loader.
- `6fd474d39e28d5e1e2ddb123db794413e4c5032d` — bootstrap timing configuration.
- `ca673e38732b011d2bfd38ef8260d526a408ba28` — production worker integration.
- `b8a1b54311e07487f18f90e9ee931296e89a6f43` — bootstrap resilience regressions.
- `ffce8a7c2b6e16192d35458572b97320933eba7f` / `c5d14d30174ae09fbbb9f98392367511818ff86a` — timing/credential-order tests and static helper correction.

### 2026-09-15 — Envelope-bound dispatch authority (current run)
Completed:
- Re-read this ledger first and inspected current dispatch, binding recovery, worker bootstrap/execution, job-store CAS, lifecycle reconciliation, and binding tests before changing code.
- Added `ResearchWorkItemEnvelopeCommitment`, a serializer-independent SHA-256 commitment over a domain/version plus length-prefixed protocol, opaque id, wrapped key, nonce, ciphertext, authentication tag, creation time, and expiry. Digest representation is strict lowercase 64-character hex; comparisons use fixed-time byte equality.
- Added protected durable `AgentJobRecord.RemoteWorkItemEnvelopeSha256` and included it in `JsonAgentJobStore` CAS identity so stale concurrent writers cannot erase or replace an established commitment.
- Added `DurableResearchEnvelopeCommitment`. It accepts only an audit-settled local `DispatchReserved` research stage, binds the exact reserved opaque id/protocol, is idempotent for the same digest, and fails closed on a substituted envelope.
- Updated the production two-phase dispatcher so the trusted client commits the exact encrypted envelope **before any Nebius Serverless Create call**. A failed commitment leaves the existing durable reservation/ciphertext intact and no provider job has been created.
- Added dispatch-binding protocol V2 (`nvidea.research.dispatch-binding.v2`). V2 RSA-PSS signatures include the exact canonical work-item envelope digest in addition to opaque id, authoritative remote job id/name, and lifetime.
- Kept V1 verification/signing only for bounded compatibility with already-persisted pre-upgrade jobs. New production dispatch/recovery uses V2 whenever the durable digest exists.
- Updated `ResearchDispatchBindingRecovery` so restart publication uses only the original protected durable digest. It explicitly never recomputes a digest from the writable shared mount after a crash.
- Updated `NebiusResearchClientRuntime` production composition to install the durable commitment layer, route binding recovery through the V2-aware component, and withhold the legacy binding publisher from the lifecycle reconciler so production recovery cannot accidentally downgrade a new stage to V1.
- Hardened worker execution: `ResearchDispatchBindingWaiter` now has an envelope-aware overload that requires V2 and verifies the exact staged envelope before execution/decryption.
- Removed the worker verify-then-re-read TOCTOU. After V2 verification, the already-verified envelope is pinned in an in-memory read-only transport passed to `NebiusResearchWorker`; downstream execution no longer re-reads the mutable shared work-item object.
- Added adversarial regressions proving deterministic commitments, ciphertext substitution changes the digest, a same-opaque-id substituted envelope fails V2 verification, legacy V1 cannot authorize the hardened worker path, create-once V2 publication rejects a different digest, and the durable commitment is immutable once attached.
- Performed a static constructor/syntax cleanup pass after implementation: canonical digest validation uses explicit character comparisons and the new tests use fully named record arguments to avoid compiler ambiguity.

Files / architecture changed:
- `src/Nvidea.Core/Jobs/ResearchWorkItemEnvelopeCommitment.cs` — new canonical digest and protected durable commitment boundary.
- `src/Nvidea.Core/Jobs/JobContracts.cs` — protected durable envelope digest on job state.
- `src/Nvidea.Core/Jobs/JsonAgentJobStore.cs` — digest participates in CAS version identity.
- `src/Nvidea.Core/Jobs/ResearchDispatchBinding.cs` — V2 signed envelope commitment, V2 publication/recovery verification, worker envelope-aware wait path.
- `src/Nvidea.Core/Jobs/ResearchDispatchBindingRecovery.cs` — restart signing from protected durable digest only.
- `src/Nvidea.Core/Jobs/TwoPhaseNebiusResearchDispatcher.cs` — commitment before Nebius Create and V2 binding publication.
- `src/Nvidea.Core/Jobs/NebiusResearchClientRuntime.cs` — production V2-aware composition and recovery ordering.
- `src/Nvidea.Worker/Program.cs` — V2 requirement plus pinned verified envelope to remove shared-mount re-read TOCTOU.
- `tests/Nvidea.Core.Tests/ResearchWorkItemEnvelopeCommitmentTests.cs` — focused sender-authenticity/substitution/create-once regressions.

Engineering commits before this ledger update:
- `4dbc5ab1299b6175f98b6952998482a0d19862dc` — canonical/durable envelope commitment primitive.
- `b030bc4b4e7a3cda81473dd660d153cc18a54c37` / `553ce4ca132bd385621b27e13793869ba2cd23b4` — persist and CAS-version the commitment.
- `1b6c7bd398ae2eb71af685c46dc0eaac5ecfb2bb` — V2 signed binding authority over exact envelope.
- `1ea7873e3c5083303763facc79ee711c2d6cf36f` — commit exact envelope before Nebius Create.
- `45b0c91e8c7952c372d129c1c79266dd0b6d8649` / `3c1c313f1a1e9a918c4d7f4ba7af18dbfaf643c9` — V2 restart recovery and production composition.
- `c95962b434c7bb4228249501fdf3ef665833c167` — hardened worker V2 verification and pinned envelope execution.
- `07faa5fd05641b50cf0ad1d22d090ade8f093322` — sender-authenticity/substitution regressions.
- `f63504411f232745d77fe1b8119bcc934fcc7480` / `58cbabc5ebb55b9ecd0db3c939beccc1be71c5b2` — static digest/test compile-clarity cleanup.

Validation / evidence this run:
- Before every GitHub mutation, repository metadata reported exact full name `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head was `207b49316b020cccfd248d894d1b8ad226ef82ce`.
- Before this ledger commit, GitHub compare reported **11 commits ahead / 0 behind**, with changes restricted to nine intended implementation/test files: `JobContracts.cs`, `JsonAgentJobStore.cs`, `NebiusResearchClientRuntime.cs`, `ResearchDispatchBinding.cs`, `ResearchDispatchBindingRecovery.cs`, new `ResearchWorkItemEnvelopeCommitment.cs`, `TwoPhaseNebiusResearchDispatcher.cs`, `Nvidea.Worker/Program.cs`, and new `ResearchWorkItemEnvelopeCommitmentTests.cs`.
- Static trust-order review confirms production order is: encrypted work-item preparation/upload -> audited durable reservation -> protected durable envelope commitment -> Nebius Create -> audited remote-id attachment -> V2 signed binding publication.
- Static worker review confirms the shared mount is read once through the bounded bootstrap loader, then the exact staged envelope is V2-verified and pinned for downstream decryption/execution. A same-id replacement on the mount after verification is not re-read.
- Static recovery review confirms V2 republishing uses `RemoteWorkItemEnvelopeSha256` from protected durable job state, not transport bytes.
- **Executable validation remains unavailable:** this environment still has no usable `dotnet`, `csc`, or `msbuild`, and direct container clone has been DNS-blocked. No compilation, xUnit, Worker, WPF, evaluator, or live-integration PASS is claimed.
- No GitHub Actions workflow and no live/paid Nebius, Tavily, Object Storage, Serverless, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- The previous writable-mount sender-authenticity gap is materially closed for new production dispatches: V2 signatures commit to the exact encrypted work-item envelope and the worker requires that commitment before model/provider execution.
- The digest covers encrypted protocol material only; it does not expose plaintext prompts, evidence, credentials, or decrypted state.
- Fixed-time digest comparison is used at cryptographic equality boundaries.
- V2 publication remains create-once/idempotent only for the same remote id and same envelope digest. A pre-existing binding with a different digest fails closed.
- Restart recovery never signs transport state observed after the crash. This prevents a writable shared transport from turning the trusted desktop into an oracle that blesses a substituted envelope.
- Legacy V1 can remain useful for pre-upgrade cancellation/lifecycle recovery but is rejected by the hardened worker execution path. This is an intentional fail-safe compatibility boundary rather than a silent downgrade.
- The verified staged envelope is pinned in memory for execution, closing the prior verify-then-re-read race against the shared mount.
- Existing external-action, audit-outbox, cancellation-race, terminal-state, result-ingestion, payload-cleanup, endpoint/redirect trust, and provider-diagnostic protections remain unchanged.

## Known Blockers / Risks
- No usable .NET 8 executable/compiler is available in this execution environment; current Core/Worker/test changes are statically reviewed but unexecuted.
- Direct container `git clone` has been DNS-blocked, so local repository compilation cannot currently be bootstrapped through clone either.
- Live Nebius Serverless/Object Storage mounted-volume behavior, authenticated worker execution, SIGTERM delivery/grace behavior, provider catalog drift, real Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- **Narrow remaining crash-consistency gap:** the audited `DispatchReserved` CAS currently commits the opaque id/checkpoint/expiry first, then the new envelope digest is attached in a second local CAS before provider Create. A crash between those two CASes is safe (no Nebius Create has occurred and recovery will not sign mutable mount state) but can leave a reservation unable to progress automatically. The clean follow-up is to carry the digest inside `RemoteResearchDispatchReservation` and persist it in the same audited reservation CAS.
- Pre-upgrade in-flight jobs without a durable envelope digest cannot satisfy the hardened V2 worker execution requirement. They retain bounded V1 lifecycle/cancellation compatibility but should be re-created rather than silently upgraded from mutable transport bytes.
- Binding publication remains an idempotent external write rather than a separately persisted pending/completed marker; hardened client lifecycle paths reconstruct it before provider use.
- `ResearchDispatchBindingCleanup` remains best-effort after terminal settlement. The binding contains no research payload and is signed/TTL-bounded, but cleanup is not represented as its own durable obligation.

## Single Best Next Task
Eliminate the final local reservation/commitment crash gap by moving `RemoteWorkItemEnvelopeSha256` into `RemoteResearchDispatchReservation` and the **same audited `DispatchReserved` compare-and-swap**. Then add fault-injection tests proving that either `{reservation + exact audit + exact envelope digest}` are all durable together or none are, that Nebius Create cannot occur without that atomic trust root, and that restart can publish V2 without ever reading mutable shared transport state.
