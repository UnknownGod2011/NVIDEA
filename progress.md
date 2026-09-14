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
- Signed dispatch-binding V2 commits to the exact protected work-item envelope via canonical SHA-256. Restart recovery signs only protected durable digest state and never mutable shared transport bytes.
- Hardened worker execution requires V2, verifies and pins the exact staged envelope, bounds bootstrap/binding retry by authenticated lifetime, retries only absence/transient I/O, and fails closed on malformed/substituted/cryptographic state.
- Remote worker cancellation propagates through bootstrap, binding recovery, and execution; POSIX SIGTERM has a bounded cooperative grace window.
- Deterministic judging/evaluation tooling covers demo, adversarial, package, evidence, and model-catalog checks.

## Persistent Progress History

### 2026-09-06 to 2026-09-12 — Core product and judging infrastructure
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, persistent browser sessions/downloads, crash recovery, encrypted Nebius remote execution, two-phase dispatch, signed resource binding, exact-once ingestion, lifecycle/cancellation reconciliation, local voice, semantic-memory migration UX, judging/evaluator tooling, protocol trust, endpoint/redirect trust, deployment preflight, and open-source/demo documentation.

### 2026-09-13 to 2026-09-14 — Exact-once and remote-dispatch hardening
- Browser executed-but-unverified actions remain durable and are never automatically replayed.
- Added audit/event trust validation, provider diagnostic quarantine, exact remote provenance, crash-resumable cancellation, durable external-action intent, durable protected-payload cleanup, and restart-safe binding recovery.
- Dispatch reservation and remote-id attachment audits use the durable audit-outbox pattern.
- Representative commits: `e66381b7`, `8bed3481`, `6278afd9`, `338fe565`, `d9ad5246`, `ae1a2dbe`.

### 2026-09-14 to 2026-09-15 — Worker recovery and envelope-bound authority
- Binding publication/mounted-volume reads use bounded retry capped by work-item lifetime; malformed/cryptographic failures never retry.
- Work-item bootstrap has bounded retry and shutdown cancellation propagates through all worker stages.
- Added serializer-independent protected-envelope commitment and durable `RemoteWorkItemEnvelopeSha256` participating in CAS identity.
- Dispatch-binding V2 signs `{opaque id, authoritative remote id/name, envelope digest, lifetime}`. V1 is compatibility-only and cannot authorize hardened worker execution.
- Worker verifies V2 against the staged encrypted envelope and executes from the pinned verified envelope, removing verify-then-re-read TOCTOU.
- Restart binding recovery uses protected durable digest provenance only.
- Representative commits: `3f766515`, `a2b63b02`, `bd7a22e9`, `4dbc5ab1`, `1b6c7bd3`, `c95962b4`.

### 2026-09-15 — Atomic dispatch trust root and crash recovery
- `AtomicRemoteResearchDispatchReservation` validates job/checkpoint/approval/protocol/lifetime/opaque identity and commits `DispatchReserved` provenance, exact envelope SHA-256, and `research.remote_dispatch_reserved` audit intent in one CAS before Nebius Create.
- Audit append failure preserves ciphertext and the complete trust root; Create remains blocked.
- `RemoteResearchDispatchReservationRecovery` reloads authority only from protected durable state and never shared mutable work-item bytes.
- Pending exact reservation audit is safe for first Create replay because the original reservation call could not have returned; a marker-cleared reservation is provider-delivery ambiguous and must never directly replay Create.
- `NebiusResearchClientRuntime.ReconcileReservedAsync` therefore attempts narrowly safe pending-audit recovery first and otherwise falls back to deterministic provider list + verified GET reconciliation.
- Dispatcher/fault tests cover audit failure, restart recovery, no mutable transport re-hash, and zero direct Create for marker-cleared reservations.
- Representative commits: `05bf4bb4`, `b36ce681`, `f8ef286b`, `809a7bd3`, `e94e7502`, `2ea5cb4b`, `a11d3d9c`, `fa605b66`, `44fcbc57`.

### 2026-09-15 — Client-runtime marker-cleared ambiguity regression (latest run)
Completed:
- Re-read this ledger and inspected the current client runtime, lifecycle reconciler, reservation-recovery safety tests, binding recovery implementation, and provider reconciliation behavior before changing anything.
- Added `NebiusResearchClientRuntimeReservationAmbiguityTests` to exercise the **production runtime**, not merely the lower-level dispatcher.
- The regression creates an atomic, audit-settled `DispatchReserved` trust root with a durable envelope commitment, then simulates exactly one matching deterministic Nebius provider job plus an unrelated job.
- It proves `NebiusResearchClientRuntime.ReconcileReservedAsync` performs **zero `CreateAsync` calls**, one bounded provider list read, one verified GET of the exact matched remote id, and attaches that exact authoritative remote id.
- It uses a work-item transport whose `GetAsync`/`PutAsync` throw and asserts both counters remain zero, proving marker-cleared ambiguity recovery does not re-read, re-upload, or re-hash mutable shared work-item state.
- It verifies the runtime subsequently publishes an envelope-bound **V2** dispatch binding, signed for the exact reconciled remote id and carrying the exact protected durable `RemoteWorkItemEnvelopeSha256` committed before provider creation.
- It re-reads the durable job and asserts the attached remote id, cleared audit intent, and original envelope commitment remain intact.

Files changed this run:
- `tests/Nvidea.Core.Tests/NebiusResearchClientRuntimeReservationAmbiguityTests.cs` (new)
- `progress.md`

Commits:
- `ad01d8c197cc15108304b0e0165e722cccbc95fd` — lock down marker-cleared client-runtime recovery.
- This ledger update commit follows it.

Validation / evidence:
- Before each GitHub mutation, repository metadata reported exact `repository_full_name: UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head: `44fcbc57f4a55068cd82f16957a81542bb259ea6`.
- Static review confirms the regression traverses the real runtime fallback: safe direct-resume rejection -> deterministic provider list/verified GET -> audited remote-id attachment -> protected-state V2 binding recovery.
- Static review confirms the fake provider counts Create/List/Get independently and the work-item transport fails immediately if mutable shared state is touched.
- **Executable validation remains unavailable:** this environment has no usable `dotnet`, `csc`, or `msbuild`; no compilation/xUnit/Worker/WPF PASS is claimed.
- No GitHub Actions workflow and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- Pending exact reservation audit and marker-cleared reservation remain deliberately different trust states; only the former can authorize a first Create replay.
- Marker-cleared ambiguity resolution obtains authority from deterministic provider identity plus verified GET and never mutable work-item bytes.
- The V2 binding produced after reconciliation is derived from the durable pre-provider envelope commitment, so provider recovery cannot bless a substituted shared envelope.
- Exact remote id, checkpoint, lifetime, protocol, approval state, audit ordering, and envelope digest remain fail-closed trust boundaries.
- Existing cancellation ambiguity, result exact-once, durable payload cleanup, endpoint/redirect trust, prompt-injection, permissions, and worker protections remain intact.

## Known Blockers / Risks
- No .NET 8 compiler/runtime is available in this execution environment, so current Core/test changes are statically reviewed but unexecuted.
- Direct container `git clone` cannot currently resolve GitHub, preventing local compilation bootstrap.
- Live Nebius Serverless/Object Storage mounted-volume behavior, authenticated worker execution, SIGTERM delivery, provider catalog drift, Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- Reservation eligibility/checkpoint/digest validation is repeated across atomic reservation, recovery, dispatcher create authority, lifecycle reconciliation, and binding recovery. Drift between those checks is now the highest-value local reliability/security debt.
- Binding publication is idempotent but not itself modeled as a durable pending/completed obligation; lifecycle recovery reconstructs it from durable state before provider use.
- Signed-binding cleanup after terminal settlement remains best-effort. The binding is signed/TTL-bounded and contains no research payload, but cleanup is not yet represented as a durable obligation.

## Single Best Next Task
Centralize the repeated **remote research reservation trust validation** into one small, pure, fail-closed validator used by atomic reservation/recovery, dispatcher provider-create authority, lifecycle reconciliation, and binding recovery where their invariants overlap. Add focused mutation/adversarial tests for checkpoint timestamp/step, opaque id, protocol, approval state, execution state/location, lifetime, remote-id absence/presence, and canonical envelope digest so one component cannot accidentally accept a state another rejects. Preserve the explicit ambiguity rule: a cleared reservation-audit marker must never become direct Create authority.
