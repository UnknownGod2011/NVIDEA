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
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`; deployable remote worker in `src/Nvidea.Worker`.
- NVIDIA Nemotron through Nebius Token Factory with structured reasoning/tool boundaries, retries, timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + Extract research with canonical deduplication, exact credit accounting, evidence quality/freshness/diversity, provenance, untrusted-evidence handling, and validated citations.
- Durable staged research checkpoints make synthesis restart-safe without repeating Tavily retrieval.
- Protected local state uses Windows CurrentUser DPAPI by default, job-store CAS, hash-chained/segmented audit, and OS-backed single-owner mutation leases.
- Safe browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, emergency stop, and explicit crash recovery.
- Remote research now has bidirectional encrypted payload transport, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation across bounded complete job listings, durable remote cancellation, explicit terminal/result-expiry handling, a mounted encrypted transport, a non-root worker image, and a signed authoritative remote-ID binding protocol.
- Production remains truthfully local until client lifecycle code automatically publishes/re-publishes the signed binding and a live Nebius end-to-end probe succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, MysteryBox secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, explicit terminal/result-expiry reconciliation, mounted encrypted transport, and a deployable non-root worker image.

### 2026-09-09 — Current run: signed authoritative dispatch binding
Completed:
- Re-read `progress.md` completely and inspected the current repo identity, recent commits/tree, mounted transport, two-phase dispatcher, worker entry point, worker/result protocol, and lifecycle reconciler before mutation.
- Verified every mutation target was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `ResearchDispatchBinding.cs` with `ProtectedResearchDispatchBinding`, transport contract, RSA-PSS/SHA-256 signing/verification, deterministic job-name validation, protocol/lifetime bounds, and opaque/resource-ID validation.
- Added `ResearchDispatchBindingPublisher`. It keeps the client private key on the originating side, derives the public key locally, publishes create-once signed bindings, and treats duplicate publication as idempotent only when the existing signed object targets the same authoritative Nebius resource ID. Conflicts fail closed.
- Added `ResearchDispatchBindingWaiter`, which polls only within bounded intervals/timeouts, supports cancellation, and verifies the client signature/opaque ID/deterministic name/expiry before returning a remote resource ID.
- Extended `DirectoryProtectedResearchTransport` with a separate `dispatch-bindings/` namespace alongside encrypted `work-items/` and `results/`. Existing filename validation, bounded reads/writes, create-once semantics, temp-file publication, and explicit deletion apply to bindings too.
- Updated `Nvidea.Worker` to remove the impossible pre-create `NVIDEA_REMOTE_JOB_ID` requirement. The worker now waits for a signed binding using the already-pinned `NVIDEA_CLIENT_PUBLIC_KEY_PEM`, then passes only the verified authoritative resource ID into `NebiusResearchWorker`.
- Added optional bounded worker configuration `NVIDEA_BINDING_POLL_SECONDS` and `NVIDEA_BINDING_WAIT_SECONDS`; defaults are 2 seconds / 5 minutes with hard bounds of 100 ms–30 seconds polling and at most 15 minutes total wait.
- Added `ResearchDispatchBindingTests` covering remote-ID substitution rejection, expiry rejection, same-resource idempotent publication, conflicting-resource rejection, and verified waiter resolution through the real directory-backed transport.
- Updated `docs/nebius-research-worker.md` to document the signed binding protocol and explicitly state that the client private key must never enter the Serverless worker.

Commits this run:
- `bc8b6695278e14f39441af734a9491bebe719611` — add signed dispatch-binding protocol.
- `055e9c7e48303a823e8fd194f050a0dc3744c704` — persist signed bindings in mounted transport.
- `736dc48712faf55c9caabe9a880e8c37ed632ca7` — resolve authoritative job ID from signed binding in worker.
- `23e8c7053ad70f1ed42b08bd7b6f167b7cb1e3c6` — add idempotent binding publisher.
- `b8abc40dfac1a0c957fb80b48c68e63e8a5bc22b` — add substitution/expiry/idempotency/waiter regression tests.
- `a8aef3fc277cf343841c4ef21b3315656c170f21` — document binding deployment contract.

Validation / evidence:
- GitHub repository metadata reported `full_name = UnknownGod2011/NVIDEA` immediately before every write.
- Static review confirms the Serverless worker no longer needs an authoritative ID before job creation; it obtains that ID only from a client-signed create-once binding after creation.
- Static review confirms research question/evidence/checkpoint data are not added to the binding; only opaque/control-plane identity and bounded timestamps are signed.
- Static review confirms an attacker who changes remote job ID, deterministic job name, opaque ID, or signed timestamps invalidates the RSA-PSS signature.
- Static review confirms a conflicting pre-existing binding is not overwritten.
- No GitHub Actions workflow was triggered merely to obtain a green signal.
- This environment still lacks a verified usable .NET/Windows/container toolchain, so **compilation, unit-test execution, Docker build, WPF launch, crypto runtime execution, and live Nebius execution are not claimed**.

Security / privacy / failure review:
- Client private signing material remains client-side; the worker needs only the pinned client public key that it already used for result encryption.
- Bindings contain no user research payload, clipboard/selected-text context, API keys, or private OS-local data.
- Binding lifetime is at most 24 hours and worker waiting is separately bounded to at most 15 minutes.
- Unknown protocol versions, malformed IDs/signatures, expired bindings, deterministic-name substitution, remote-resource substitution, and conflicting create-once objects fail closed.
- Worker failure logs still emit exception type only, not exception message/provider body/research content.
- A process crash after remote attachment but before binding publication is still recoverable in principle because durable remote provenance already exists; producer wiring in lifecycle reconciliation remains the missing step.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- `ResearchDispatchBindingPublisher` is implemented but not yet automatically invoked by `TwoPhaseNebiusResearchDispatcher` after `AttachDispatchAsync` or by `NebiusResearchLifecycleReconciler` after crash recovery / for a dispatched record whose binding is absent.
- Until that producer wiring exists, the worker can correctly consume a binding but normal production dispatch does not yet guarantee that one will be published.
- No live Object Storage bucket, registry image, MysteryBox keys, subnet, or Serverless job has been provisioned/validated in this environment.
- `ResearchJobRuntime` still deliberately rejects non-local records; WPF Serverless controls remain intentionally absent until cloud execution is real and validated.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Wire `ResearchDispatchBindingPublisher` into the durable client lifecycle: publish immediately after successful `AttachDispatchAsync`, publish after `ReconcileReservedAsync` recovers a crash-window remote resource ID, and idempotently ensure the binding exists at the start of `ReconcileDispatchedAsync`. Carry the client signing key through a local-only composition boundary without storing it in job records or Serverless config. Add tests proving no binding is published before authoritative attachment, crash recovery publishes exactly the recovered resource ID, a conflicting binding blocks execution, and repeated reconciliation is idempotent. Then run the narrow container/Serverless contract probe when credentials/tooling are available. Only after that succeeds should WPF expose Nebius Serverless research.
