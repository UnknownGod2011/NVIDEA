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
- Remote research has bidirectional encrypted payload transport, one-stage `NebiusResearchWorker`, two-phase `DispatchReserved -> Nebius Create -> remote-id attachment`, exact-once result ingestion, deterministic lifecycle reconciliation across bounded complete job listings, durable cancellation, terminal/result-expiry handling, mounted encrypted transport, non-root worker image, and a signed authoritative remote-ID binding protocol.
- The signed binding producer is now wired into normal two-phase dispatch, crash-window reservation recovery, and dispatched reconciliation when local composition supplies `ResearchDispatchBindingPublisher`.
- Production remains truthfully local until a live Nebius/Object Storage/MysteryBox/container end-to-end probe succeeds.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, Playwright execution, Windows shell, DPAPI protection, persistent browser sessions, durable verified downloads, crash recovery, and single-owner browser state.

### 2026-09-09 — Research quality + durability
Added Tavily Extract enrichment, deterministic evidence quality/staleness/diversity handling, staged research boundaries, restart-safe synthesis, privacy-safe status, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership.

### 2026-09-09 — Nebius Serverless privacy/control plane
Refreshed the Jobs client to current subnet/disk requirements, MysteryBox secret refs, secret rejection, List/Get/Create/Cancel, retries and endpoint allow-listing. Added encrypted opaque-ID dispatch, protected result return, one-stage worker primitive, durable remote provenance, exact CAS result ingestion, two-phase dispatch reservation, deterministic crash reconciliation, typed provider lifecycle parsing, durable `CancelRequested -> Cancelled` handling, bounded pagination, explicit terminal/result-expiry reconciliation, mounted encrypted transport, deployable non-root worker image, and signed authoritative resource-ID bindings.

### 2026-09-09 — Current run: durable authoritative binding publication
Completed:
- Re-read `progress.md` fully, inspected the current NVIDEA repo state and relevant dispatch/reconciliation/tests, and verified the repository identity before every GitHub mutation.
- Updated `TwoPhaseNebiusResearchDispatcher` so a configured `ResearchDispatchBindingPublisher` is invoked only after `RemoteResearchResultIngestor.AttachDispatchAsync` has durably CAS-attached the authoritative Nebius resource ID.
- Reused `ResearchDispatchBindingProtector.GetDeterministicRemoteJobName` from the dispatcher/lifecycle paths to reduce duplicate naming logic.
- Updated `NebiusResearchLifecycleReconciler.ReconcileReservedAsync` so crash-window recovery attaches the uniquely recovered/direct-GET-verified resource ID first, then publishes the exact signed binding.
- Updated `ReconcileDispatchedAsync` to idempotently ensure the binding exists before provider lifecycle reads. A conflicting validly signed binding for another resource therefore blocks reconciliation before provider-state mutation.
- Added `ResearchDispatchBindingLifecycleIntegrationTests.cs` covering: publication only after durable attachment; crash recovery publishing exactly the recovered resource ID; repeated dispatched reconciliation producing one create-once binding; and conflicting bindings blocking provider reads.
- Updated `docs/nebius-research-worker.md` so the documented control-plane sequence matches the implemented producer/consumer lifecycle.

Commits this run:
- `6624fa198a9e3a3c5281be733695a0bf5b9f0926` — wire binding publication after authoritative dispatch attachment.
- `96bf0f75a1f12204a44de32517e7846bf6434273` — publish/re-publish authoritative bindings during lifecycle reconciliation.
- `803be06fcf9fbc31138acfe5c2c42013d9d006ee` — add binding lifecycle integration regression tests.
- `6f2195ff489bcf86516c8d74e129eb55995de597` — document automatic authoritative-ID handoff.

Validation / evidence:
- Repository metadata reported `full_name = UnknownGod2011/NVIDEA` immediately before every mutation; no other repository was mutated.
- Static review confirms normal dispatch cannot publish a binding before the authoritative remote ID is durably attached.
- Static review confirms crash recovery publishes only the ID that passed bounded exact-name search plus direct GET identity/state verification.
- Static review confirms repeated reconciliation uses publisher idempotency instead of overwriting create-once bindings.
- Static review confirms a conflicting binding fails before the lifecycle reconciler performs a provider GET.
- `dotnet` is not present in the execution environment, so compilation and unit-test execution are **not claimed**.
- No GitHub Actions workflow was triggered merely to manufacture a green signal.

Security / privacy / failure review:
- The client RSA private signing key is still accepted only by local `ResearchDispatchBindingPublisher`; it is not written into job records, Nebius job specs, worker config, or shared transport.
- Bindings contain opaque/control-plane identity only, not research questions, evidence, selected text, clipboard context, Tavily/Nebius keys, or OS-private data.
- Publication happens after durable attachment. If publication then fails, the durable `Dispatched` provenance remains recoverable and later reconciliation can retry idempotently.
- A pre-existing conflicting signed binding is never overwritten or ignored.
- One cleanup gap remains: terminal cleanup currently deletes protected work-item/result objects but does not explicitly delete the corresponding dispatch-binding object. Binding expiry and provider bucket lifecycle can bound retention, but explicit terminal deletion should be added after checking worker/result race semantics.

## Known Blockers / Risks
- No verified .NET 8/Windows/container execution signal is available here; new code/tests are statically reviewed but not compiled/executed.
- No live Object Storage bucket, registry image, MysteryBox keys, subnet, or Serverless job has been provisioned/validated in this environment.
- WPF/`ResearchJobRuntime` still deliberately avoid claiming production Serverless execution until the live contract succeeds.
- Local composition must supply the same client-side `ResearchDispatchBindingPublisher` to dispatcher/reconciler; constructors remain backward-compatible with no publisher for existing local/test call sites.
- Terminal cleanup does not yet delete signed dispatch bindings.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
Close the remaining remote transport lifecycle and prove the cloud path: add explicit safe dispatch-binding cleanup after terminal success/failure/cancellation with race-aware tests, add a small local composition boundary that constructs dispatcher + reconciler + publisher from one client-only signing key without exposing that key to Serverless configuration, then run the narrow container/Nebius contract probe when credentials/tooling are available (`opaque dispatch -> signed authoritative binding -> worker -> one Nemotron/Tavily stage -> encrypted result -> exact-once local ingestion`). Only after that succeeds should WPF expose Nebius Serverless research.
