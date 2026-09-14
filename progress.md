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

### 2026-09-15 — Atomic dispatch reservation trust root (latest run)
Completed:
- Re-read `progress.md` first and inspected current `RemoteResearchResultIngestor`, `TwoPhaseNebiusResearchDispatcher`, durable audit outbox, envelope commitment layer, production composition, and existing commitment tests.
- Added `AtomicRemoteResearchDispatchReservation` in `ResearchWorkItemEnvelopeCommitment.cs`.
- The atomic coordinator validates the research job, checkpoint, opaque work-item identity, protocol, exact encrypted work-item expiry, and approval state before mutation.
- It computes the canonical SHA-256 over the exact protected envelope and commits **all of the following in one job compare-and-swap**: `DispatchReserved` provenance, exact opaque id/checkpoint/lifetime, `RemoteWorkItemEnvelopeSha256`, and the exact validated `research.remote_dispatch_reserved` audit intent.
- It settles that exact pending audit before returning. If audit persistence fails after the CAS, it raises `RemoteResearchDispatchReservationAuditPendingException`, preserving ciphertext and the complete durable trust root for recovery.
- Updated `TwoPhaseNebiusResearchDispatcher` with an optional atomic reservation coordinator. Production uses the atomic path; legacy/lower-level compositions retain the former `ReserveDispatchAsync` + `DurableResearchEnvelopeCommitment` path for compatibility.
- Nebius `CreateAsync` remains strictly after successful atomic reservation/audit settlement. The second commitment CAS is skipped on the atomic production path.
- Updated `NebiusResearchClientRuntime.Create` to wire `AtomicRemoteResearchDispatchReservation(store, auditTrail)` into production while retaining the legacy commitment object only for compatibility paths.
- Added `AtomicRemoteResearchDispatchReservationTests` covering successful atomic provenance/audit/digest commit, audit-append failure leaving the full trust root durable with pending audit, and envelope-expiry substitution rejection with no state mutation.

Files changed this run:
- `src/Nvidea.Core/Jobs/ResearchWorkItemEnvelopeCommitment.cs`
- `src/Nvidea.Core/Jobs/TwoPhaseNebiusResearchDispatcher.cs`
- `src/Nvidea.Core/Jobs/NebiusResearchClientRuntime.cs`
- `tests/Nvidea.Core.Tests/AtomicRemoteResearchDispatchReservationTests.cs`
- `progress.md`

Commits before this ledger update:
- `05bf4bb41f062f447f410b18f36b550fbfd33add` — make dispatch reservation trust root atomic.
- `b36ce6810f0cb7922a113cc0e89f76a51e7da2bc` — use atomic reservation in dispatcher path.
- `f8ef286b48c6d8130bb61dc338a5559ecfd047b8` — wire atomic trust root into production runtime.
- `202035f0ae7239d6d68da11d7a3cf9804e606010` — atomic reservation fault/security regressions.

Validation / evidence:
- Before every GitHub mutation, repository metadata reported exact `full_name: UnknownGod2011/NVIDEA`; no other repository was mutated.
- Starting head for this run was `2589f71f6f11ac2fbb55bfe402fdf5fe495b977d`.
- GitHub compare before this ledger update reported **4 commits ahead / 0 behind** and only the four intended implementation/test files above.
- Static trust-order review now gives the production order: encrypted work-item preparation/upload -> **one CAS containing reservation + audit intent + exact envelope digest** -> audit settlement -> Nebius Create -> audited remote-id attachment -> V2 signed binding publication.
- Static failure review confirms an audit append failure after the reservation CAS cannot trigger ciphertext cleanup because it is surfaced as `RemoteResearchDispatchReservationAuditPendingException`; provider Create is not reached.
- Static substitution review confirms a reservation expiry different from the exact envelope expiry fails before any job mutation.
- **Executable validation remains unavailable:** this environment has no usable `dotnet`, `csc`, or `msbuild`, and direct container GitHub clone remains DNS-blocked. No compilation, xUnit, Worker, WPF, evaluator, or live-integration PASS is claimed.
- No GitHub Actions workflow and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama, or inference operation was triggered.

## Security / Privacy / Failure Review
- The former two-CAS reservation/commitment crash gap is closed for production composition: no provider creation authority exists unless reservation provenance, exact audit intent, and exact encrypted-envelope commitment committed together.
- The envelope digest covers encrypted protocol material only; no plaintext prompt, research evidence, credential, or decrypted state is exposed.
- A writable shared mount cannot cause restart recovery to bless substituted work-item bytes because V2 recovery signs only the protected durable digest.
- Legacy APIs remain available to avoid breaking working lower-level tests/compositions, but production composition explicitly selects the stronger atomic path.
- Existing cancellation ambiguity, result exact-once, terminal cleanup, audit-outbox, endpoint/redirect trust, prompt-injection, permission, and worker fail-closed protections remain intact.

## Known Blockers / Risks
- No .NET 8 compiler/runtime is available in this execution environment, so latest Core/test changes are statically reviewed but unexecuted.
- Direct container `git clone` cannot currently resolve GitHub, preventing local compilation bootstrap.
- Live Nebius Serverless/Object Storage mounted-volume behavior, authenticated worker execution, SIGTERM delivery, provider catalog drift, Windows UX, authenticated Playwright sessions, Tavily live behavior, and semantic ranking remain environment-validation items.
- The atomic reservation implementation duplicates a small amount of dispatch-eligibility/audit construction logic from `RemoteResearchResultIngestor`; future cleanup should centralize those pure validation/event-building helpers without weakening the atomic CAS boundary.
- Binding publication is still an idempotent external write rather than a separately persisted pending/completed obligation; lifecycle recovery reconstructs it from durable state before provider use.
- Signed-binding cleanup is still best-effort after terminal settlement. It is signed/TTL-bounded and contains no research payload, but cleanup itself is not yet represented as a durable obligation.

## Single Best Next Task
Add an end-to-end dispatcher fault-injection regression around the **production atomic path** proving the Nebius Serverless client receives zero `CreateAsync` calls when reservation-audit settlement fails, then test crash/restart recovery from the persisted atomic reservation through audit settlement and provider reconciliation without re-uploading or re-hashing mutable transport bytes. After that, centralize shared reservation validation helpers to remove duplication while preserving the single-CAS trust root.
