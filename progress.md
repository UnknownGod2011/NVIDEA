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
- `DurableJobAuditOutbox` couples validated audit intents to the same job CAS as critical research state transitions.
- `PendingExternalAction` / `DurableJobExternalActionIntent` persist ambiguous cancellation delivery; transport success is never provider-state proof.
- `PendingProtectedPayloadCleanup` / `DurableProtectedPayloadCleanupIntent` persist cleanup obligations and retry partial deletes idempotently after restart.
- Signed dispatch-binding V2 cryptographically commits to the exact protected work-item envelope via canonical SHA-256. Restart recovery signs only protected durable digest state and never mutable shared transport bytes.
- Hardened worker execution requires V2, verifies the exact staged envelope, pins that verified envelope in memory, uses bounded bootstrap/binding retry for only absent objects and transient `IOException`, and fails closed on malformed/substituted/cryptographically invalid state.
- Remote worker process cancellation is threaded through bootstrap, binding recovery, and execution; POSIX SIGTERM has a bounded cooperative grace window.
- Deterministic judging/evaluation tools cover demo, adversarial, package, evidence, and model-catalog checks.

## Persistent Progress History

### 2026-09-06 to 2026-09-12 — Core product and judging infrastructure
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, capability permissions/audit, durable jobs, Playwright browser execution, DPAPI state protection, persistent browser sessions/downloads, crash recovery, encrypted Nebius remote execution, two-phase dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, local voice, semantic-memory migration UX, judging/evaluator tooling, protocol trust, endpoint/redirect trust, deployment preflight, and open-source/demo documentation.

### 2026-09-13 to 2026-09-14 — Exact-once, privacy, lifecycle, and remote-dispatch hardening
- Browser executed-but-unverified actions remain durable and are never automatically replayed.
- Added audit/event trust validation, provider diagnostic quarantine, exact remote provenance, crash-resumable cancellation, durable external-action intent, durable protected-payload cleanup, and restart-safe signed-binding recovery.
- Dispatch reservation and remote-id attachment audits use the durable audit-outbox pattern.
- Key commits: `e66381b78752c6141e8d9ac192ea307e48cf0968`, `8bed3481f54fba21c46d604d9f924eb56d05ac6a`, `6278afd91b96f3611e471b49de9dbba065627ec7`, `338fe56587eaa96faa8942e10cbcbea3c894b3b7`, `d9ad5246e3d63a394c9e65dc5300352aadafdc1d`, `ae1a2dbe19066d94b1b0ac4ef9c987d0f1953112`.

### 2026-09-14 to 2026-09-15 — Worker recovery and envelope-bound authority
- Binding publication/mounted-volume reads use bounded retry capped by authenticated work-item lifetime; malformed/cryptographic failures never retry.
- Work-item bootstrap has its own bounded retry layer and shutdown cancellation propagates through all worker stages.
- Added canonical serializer-independent work-item envelope commitment and protected durable `RemoteWorkItemEnvelopeSha256` participating in CAS identity.
- Dispatch-binding V2 signs `{opaque id, authoritative remote id/name, envelope digest, lifetime}`. V1 remains only for bounded pre-upgrade lifecycle/cancellation compatibility and cannot authorize hardened worker execution.
- Worker verifies V2 against the staged encrypted envelope and executes from the pinned verified envelope, eliminating verify-then-re-read TOCTOU.
- Restart binding recovery uses only protected durable digest provenance; it never recomputes from the writable mount.
- Key commits: `3f766515c1403ad56a33941bccf10cf056a10731`, `a2b63b02dee5a692199a380091fdc182556562e6`, `bd7a22e94961f0024d7704312b1c873bb2fd4a1a`, `4dbc5ab1299b6175f98b6952998482a0d19862dc`, `1b6c7bd398ae2eb71af685c46dc0eaac5ecfb2bb`, `c95962b434c7bb4228249501fdf3ef665833c167`.

### 2026-09-15 — Atomic dispatch reservation trust root
Completed:
- Added `AtomicRemoteResearchDispatchReservation` in `ResearchWorkItemEnvelopeCommitment.cs`.
- The atomic coordinator validates the research job, checkpoint, opaque work-item identity, protocol, exact encrypted work-item expiry, and approval state before mutation.
- It computes the canonical SHA-256 over the exact protected envelope and commits **all of the following in one job compare-and-swap**: `DispatchReserved` provenance, exact opaque id/checkpoint/lifetime, `RemoteWorkItemEnvelopeSha256`, and the exact validated `research.remote_dispatch_reserved` audit intent.
- It settles that exact pending audit before returning. If audit persistence fails after the CAS, it raises `RemoteResearchDispatchReservationAuditPendingException`, preserving ciphertext and the complete durable trust root for recovery.
- Updated `TwoPhaseNebiusResearchDispatcher` with an optional atomic reservation coordinator. Production uses the atomic path; legacy/lower-level compositions retain the former `ReserveDispatchAsync` + `DurableResearchEnvelopeCommitment` path for compatibility.
- Nebius `CreateAsync` remains strictly after successful atomic reservation/audit settlement. The second commitment CAS is skipped on the atomic production path.
- Updated `NebiusResearchClientRuntime.Create` to wire `AtomicRemoteResearchDispatchReservation(store, auditTrail)` into production while retaining the legacy commitment object only for compatibility paths.
- Added `AtomicRemoteResearchDispatchReservationTests` covering successful atomic provenance/audit/digest commit, audit-append failure leaving the full trust root durable with pending audit, and envelope-expiry substitution rejection with no state mutation.

Key commits:
- `05bf4bb41f062f447f410b18f36b550fbfd33add` — make dispatch reservation trust root atomic.
- `b36ce6810f0cb7922a113cc0e89f76a51e7da2bc` — use atomic reservation in dispatcher path.
- `f8ef286b48c6d8130bb61dc338a5559ecfd047b8` — wire atomic trust root into production runtime.
- `202035f0ae7239d6d68da11d7a3cf9804e606010` — atomic reservation fault/security regressions.
- `8683370875327aa29ddc0551c7690fdc44a9b38c` — persist the atomic trust-root progress ledger.

### 2026-09-15 — Crash-safe reservation-audit recovery and duplicate-create boundary (latest run)
Completed:
- Re-read `progress.md` first and inspected the current dispatcher, atomic reservation coordinator, audit outbox, result ingestor, client runtime, lifecycle reconciler, and existing dispatch tests before modifying anything.
- Added `RemoteResearchDispatchReservationRecovery`. It reloads the `DispatchReserved` trust root from the protected job store, verifies research/job/protocol/checkpoint/approval state plus canonical `RemoteWorkItemEnvelopeSha256`, settles the exact `research.remote_dispatch_reserved` outbox event, then verifies the envelope commitment did not change while audit recovery was happening.
- Recovery deliberately performs **no shared work-item transport read, upload, or re-hash**. Authority comes only from the protected durable atomic reservation.
- Added `AtomicRemoteResearchDispatchReservation.RecoverAsync(...)` so restart recovery uses the same protected store/audit composition as reservation creation rather than a caller-supplied in-memory record.
- Hardened `TwoPhaseNebiusResearchDispatcher.ResumeReservedAsync(...)`: callers provide only the local job id; the dispatcher first reloads and settles the atomic reservation through the coordinator, then validates exact checkpoint, unexpired protected-work-item lifetime, protocol, local/Running state, no approval, no remote id, no pending audit, and canonical digest before any Nebius Create call.
- Centralized provider-create request construction so fresh dispatch and safe restart use the same deterministic Nebius job name/spec and remote-id parsing.
- Added dispatcher-level fault injection proving that if atomic reservation CAS succeeds but reservation-audit append fails, the dispatcher performs **zero** Nebius `CreateAsync` calls, preserves the exact ciphertext, and keeps the complete trust root + pending audit durable.
- Added restart regression proving the pending audit is settled before Create, the original envelope digest is preserved, the provider is created once, the exact remote id is attached/audited, and restart performs zero shared work-item `Get` and zero additional `Put` operations.
- During review, identified a critical ambiguity distinction: once the reservation audit marker is already cleared, a crash could have occurred before, during, or after provider Create. Therefore a marker-cleared `DispatchReserved` record is **not** safe replay authority.
- Added `RemoteResearchDispatchReservationRecoveryNotRequiredException` and tightened recovery so direct Create replay is allowed only while the exact reservation audit is still pending. That pending marker proves atomic `ReserveAsync` never returned to the original dispatcher, hence the original control flow never reached Create.
- Updated production `NebiusResearchClientRuntime.ReconcileReservedAsync(...)`: it first attempts this narrowly safe pending-audit recovery; if the reservation audit is already settled, it falls back to the existing conservative deterministic provider list/GET reconciliation instead of issuing another Create.
- Added `RemoteResearchDispatchReservationRecoverySafetyTests` proving an already audit-settled `DispatchReserved` record cannot trigger direct provider replay: `CreateAsync == 0`, shared transport reads == 0, and shared transport writes == 0.

Files changed this run:
- `src/Nvidea.Core/Jobs/RemoteResearchDispatchReservationRecovery.cs` (new)
- `src/Nvidea.Core/Jobs/ResearchWorkItemEnvelopeCommitment.cs`
- `src/Nvidea.Core/Jobs/TwoPhaseNebiusResearchDispatcher.cs`
- `src/Nvidea.Core/Jobs/NebiusResearchClientRuntime.cs`
- `tests/Nvidea.Core.Tests/TwoPhaseNebiusResearchDispatcherTests.cs`
- `tests/Nvidea.Core.Tests/RemoteResearchDispatchReservationRecoverySafetyTests.cs` (new)
- `progress.md`

Commits before this ledger update:
- `809a7bd3fff05fa869ea1b1f0b973dfa363380bd` — add durable remote dispatch reservation recovery.
- `3cf8f6a1038666505aa0bdbe867cc002331df71f` — resume atomic dispatch from durable reservation.
- `e94e7502711a5010bb7d26c98fe91bee7fcd1641` — cover atomic dispatch audit failure and restart recovery.
- `839fe4f8f37458eb67b665e8621118a3d9cdf069` — bind dispatch restart recovery to atomic coordinator.
- `ecb0741d12ab12e42abeb4c15b45fb332d53065a` — require atomic recovery before restarted provider create.
- `42b4339085fd04849b6f490fe83cae49d28079dc` — route restart test through atomic durable recovery.
- `2ea5cb4b665798f3c950df1596349ee34caef7f3` — distinguish safe pending-audit dispatch recovery.
- `a11d3d9c05ccd23d8bd02a5a1d74680c27aca300` — recover pending dispatch audit before provider reconciliation.
- `fa605b6607b698e59b6df9222294b15e47ef547b` — prove marker-cleared reservations never replay provider create.

Validation / evidence:
- Before every GitHub mutation, repository metadata reported exact `repository_full_name: UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head for this run was `8683370875327aa29ddc0551c7690fdc44a9b38c`.
- GitHub compare before this ledger update reported **9 commits ahead / 0 behind** and only the six intended implementation/test files above.
- Static trust-order review now distinguishes two restart states:
  1. **Pending exact reservation audit:** safe to settle the existing audit and issue the first provider Create because `ReserveAsync` never returned to the original dispatcher.
  2. **Reservation audit already settled:** provider-create delivery is ambiguous; never replay Create directly. Use deterministic provider list/GET reconciliation.
- Static review confirms the safe restart path reads the protected local job store/audit only; mutable Object Storage/mounted work-item bytes are neither read nor re-hashed before provider Create.
- Static failure review confirms audit settlement failure propagates before provider Create and preserves the atomic trust root/ciphertext.
- Static duplicate-work review confirms marker-cleared reservations cannot enter direct replay through `ResumeReservedAsync`; production runtime deliberately routes them to the existing conservative reconciliation path.
- **Executable validation remains unavailable:** this environment has no usable `dotnet`, `csc`, or `msbuild`, and direct container GitHub clone remains DNS-blocked. No compilation, xUnit, Worker, WPF, evaluator, or live-integration PASS is claimed.
- No GitHub Actions workflow and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- The atomic trust root now has an explicit crash-recovery protocol rather than merely preserving stranded state.
- Recovery never trusts writable shared work-item transport bytes. It relies on protected local checkpoint/provenance and the canonical envelope commitment already committed in the atomic reservation CAS.
- Direct replay is deliberately narrower than generic `DispatchReserved` recovery: only the still-pending reservation audit proves the original dispatcher could not have reached provider Create.
- A marker-cleared reservation remains ambiguous and therefore uses provider reconciliation rather than replay, protecting against duplicate paid/background Nebius work after an uncertain Create delivery.
- The exact checkpoint step + saved timestamp, work-item lifetime, protocol, local execution state, approval state, remote-id absence, pending audit type, and canonical envelope digest are validated before safe replay.
- Existing V2 envelope-bound signed binding, cancellation ambiguity, result exact-once, durable payload cleanup, endpoint/redirect trust, prompt-injection, permissions, and worker fail-closed protections remain intact.

## Known Blockers / Risks
- No .NET 8 compiler/runtime is available in this execution environment, so the latest Core/test changes are statically reviewed but unexecuted.
- Direct container `git clone` cannot currently resolve GitHub, preventing local compilation bootstrap.
- Live Nebius Serverless/Object Storage mounted-volume behavior, authenticated worker execution, SIGTERM delivery, provider catalog drift, Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- The production runtime's marker-cleared fallback still depends on Nebius deterministic-name list/GET reconciliation. Existing lifecycle tests cover that reconciler, but a new end-to-end client-runtime regression should explicitly prove the fallback never invokes Create and correctly attaches exactly one verified matching provider job.
- Reservation eligibility/checkpoint/digest validation is now repeated across atomic reservation, recovery, dispatcher provider-create authority, lifecycle reconciliation, and binding recovery. This should be centralized into pure shared validators without weakening the single-CAS trust root or ambiguity boundary.
- Binding publication remains an idempotent external write rather than a separately persisted pending/completed obligation; lifecycle recovery reconstructs it from durable state before provider use.
- Signed-binding cleanup is still best-effort after terminal settlement. It is signed/TTL-bounded and contains no research payload, but cleanup itself is not yet represented as a durable obligation.

## Single Best Next Task
Add a **client-runtime end-to-end ambiguity regression** for the marker-cleared `DispatchReserved` state: prove `NebiusResearchClientRuntime.ReconcileReservedAsync` performs zero `CreateAsync` calls, resolves exactly one deterministic provider match through bounded list + verified GET, attaches that exact remote id, and publishes/reconstructs the V2 binding from the protected durable envelope commitment. Then centralize the repeated reservation trust validation helpers across atomic reservation, recovery, dispatcher, reconciler, and binding recovery without weakening any fail-closed boundary.
