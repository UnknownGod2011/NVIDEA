# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Dispatch-binding V2 signs authoritative remote id + canonical SHA-256 of the exact encrypted work-item envelope; worker verifies and pins the envelope before execution.
- Protected local CAS state supports durable binding-publication, audit and protected-payload-cleanup obligations with independent restart recovery.

## Persistent history
### 2026-09-06 to 2026-09-12
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment preflight and judging/evaluator tooling.

### 2026-09-13 to 2026-09-15
Hardened exact-once browser behavior and remote dispatch: durable external-action/cleanup/audit intents, exact remote provenance, crash-resumable cancellation, envelope commitment, V2 sender authenticity, pinned-envelope worker execution, bounded worker transport retry/SIGTERM, atomic reservation + audit + digest CAS, provider-delivery ambiguity reconciliation, shared reservation trust validation and final pre-Create durable authority revalidation.

### 2026-09-15 — Durable V2 binding publication and race hardening
Added `PendingResearchDispatchBinding` and `DurableResearchDispatchBindingObligation`: exact V2 publication intent is CAS-staged before shared transport I/O, publication is idempotent, and restart/reconciliation consumes the protected obligation. Fresh and resumed production dispatch use the same coordinator. Added bounded post-publication CAS completion, an internal-only deterministic race observer, authority-substitution coverage for remote id/opaque id/envelope digest/expiry, failed-first-publication restart recovery, and a real cancellation pipeline race through durable audit + provider cancellation.

### 2026-09-15 — Terminal binding-completion semantics
Split dispatch-binding validation into strict pre-publication authority and post-publication bookkeeping semantics. After exact signed publication, completion accepts only lifecycle-consistent state triples while exact obligation identity, remote id, opaque id, envelope digest and expiry remain unchanged. Legitimate `ResultApplied`, `Cancelled`, `RemoteFailed`, and `Expired` transitions can converge without weakening pre-publication authority; inconsistent state/provenance pairs fail closed.

### 2026-09-15 — Real result-ingestion and interrupted recovery
Added real `RemoteResearchResultIngestor.IngestAsync` races at the post-publication/pre-completion seam. Successful ingestion covers Pending/Completed ResultApplied convergence. Audit-failure injection proves result CAS, pending audit, cleanup debt and binding debt survive independently; restart flushes audit before cleanup and binding recovery does not republish or reapply the result.

### 2026-09-15 — Cleanup failure and ambiguous post-delete recovery
Added pre-delete cleanup failure and successful-delete/lost-acknowledgement recovery coverage. Cleanup remains durable until deletion returns successfully; restart retries an already-absent result idempotently, preserves exactly-once audit/result application, and independently converges the outstanding V2 binding without republishing.

### 2026-09-15 — Published-binding multi-artifact cleanup recovery
Added the real combined result + work-item cleanup race: result deletion succeeds, work-item deletion fails, restart repeats both deletes idempotently, cleanup clears only after both transports succeed, and binding recovery remains independently exactly-once.

### 2026-09-15 — Ambiguous work-item deletion with published binding (latest run)
Completed:
- Re-read this ledger completely and inspected the current combined cleanup regression before mutation.
- Verified immediately before each GitHub mutation that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Extended `DurableResearchDispatchBindingMultiArtifactCleanupRaceTests` with the work-item analogue of successful-side-effect/lost-acknowledgement recovery.
- Refactored the two closely related scenarios through one test harness without changing production code: (1) work-item delete fails before deletion, and (2) work-item delete actually removes the encrypted object then throws before acknowledgement.
- In the ambiguous case, the crash boundary explicitly requires: result absent, work item absent, result audit settled exactly once, result checkpoint applied, cleanup marker still durable, binding marker still durable, and exactly one signed V2 binding publication.
- Restart reconstructs the production ingestor and deliberately repeats both deletes. Both already-absent result and work-item deletion must be idempotent; only then may the shared cleanup obligation clear.
- Fresh binding recovery then clears only the independently owned V2 bookkeeping obligation and requires binding transport publication count to remain exactly one.

Files changed:
- `tests/Nvidea.Core.Tests/DurableResearchDispatchBindingMultiArtifactCleanupRaceTests.cs`
- `progress.md`

Commits this run before ledger:
- `100a777d64c257557d3bda5e02bc78cfd15e9791` — test ambiguous work-item cleanup recovery with published binding.

Validation/evidence:
- Static review confirms the fault is injected after `WorkItemExists = false`, so it models a real successful external delete whose acknowledgement is lost rather than another pre-side-effect failure.
- The intermediate assertions distinguish the two failure modes: the ordinary failure leaves the work item present; the ambiguous failure requires it already absent while cleanup debt remains durable.
- Recovery requires two result-delete calls and two work-item-delete calls in both cases, proving retry behavior rather than silently treating local object absence as completion.
- Audit remains exactly once, checkpoint remains at `ResearchJobHandler.EvidenceStep`, and binding transport remains one put after independent binding reconciliation.
- Executable validation remains unavailable: no usable `dotnet`, `csc` or `msbuild` is available here, so no compilation/xUnit/WPF/Worker PASS is claimed.
- No GitHub Actions and no live/paid Nebius, Object Storage, Serverless, Tavily, Playwright, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Cleanup acknowledgement is deliberately conservative: even when an external work-item delete probably succeeded, NVIDEA retains the durable cleanup obligation until a retry receives success.
- Both encrypted transports are now regression-specified as idempotent-delete dependencies in the combined published-binding scenario.
- Audit settlement remains prior to cleanup and is not replayed during deletion recovery.
- Result application, cleanup and V2 binding publication remain independent obligations; recovery of one cannot silently clear or duplicate another.
- No production API was broadened and no plaintext credentials, tokens, private keys or user research content were committed.

## Known blockers / risks
- No .NET 8 compiler/runtime in this environment; current changes are statically reviewed but unexecuted.
- Live Nebius mounted-volume/Serverless behavior, worker auth, Windows UX, authenticated Playwright, Tavily and semantic ranking remain environment-validation items.
- Combined cleanup recovery now covers both work-item failure-before-delete and work-item successful-delete/lost-acknowledgement after result cleanup has succeeded.
- The next unproven durability boundary is local cleanup-marker CAS failure/contention after both remote deletions have returned success; repeated recovery must not create unbounded retry behavior or interfere with independently pending binding/audit obligations.

## Single Best Next Task
Add deterministic coverage for cleanup-marker completion CAS contention/failure after both protected transports have returned successful deletion: simulate a concurrent legitimate state transition, prove cleanup completion retries/converges without replaying audit/result application, and ensure an independently outstanding V2 binding obligation remains exact-once and authority-locked.
