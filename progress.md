# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon. Target **Personal AI**, **Best Use of Tavily**, and top-three / Grand Prize quality. NVIDEA must be independently stronger than keyboard.wtf in NVIDIA/Nebius-first reasoning, memory, research, browser automation, long-running work, verification, privacy, and safety.

## Hard Repository Boundary
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate it.
- Never write to any other repository.
- Before every GitHub mutation, verify the target is exactly `UnknownGod2011/NVIDEA`.
- Do not remove working functionality merely to simplify implementation.

## Current Architecture / Product State
- .NET 8 core in `src/Nvidea.Core`; WPF host in `src/Nvidea.Windows`.
- Nebius Token Factory / NVIDIA Nemotron inference abstraction with structured output/tool handling, retry/timeout/cancellation, and conservative routing.
- Layered personal memory with privacy-aware writes and hybrid retrieval.
- Tavily Search + advanced Extract research with canonical deduplication, credit accounting, authority/freshness/diversity ranking, uncertainty warnings, provenance, untrusted-evidence boundaries, and machine-validated `[src:SOURCE_ID]` citations.
- Durable research stages: request -> Nemotron plan -> Tavily Search/Extract + ranking -> prepared evidence -> Nemotron synthesis -> completed report. Exact prepared evidence is checkpointed so resume does not repeat Search/Extract or rerank evidence.
- `JsonAgentJobStore` and `JsonLinesAuditTrail` use Windows CurrentUser DPAPI by default on Windows. `JsonAgentJobStore` now also exposes a strong compare-and-swap replacement path used by remote-result ingestion.
- `ResearchJobRuntime` is the trusted local production host. Mutations use an OS-backed `StateDirectoryLease`; read-only status/report access remains lease-free. Explicit interrupted-stage recovery is research-only; generic Running jobs remain fail-closed.
- WPF surfaces durable research create/resume/cancel/recovery/report flows and participates in emergency stop.
- Playwright browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit recovery, emergency stop, crash recovery, and pre-transport single-owner browser-state locking.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Nebius Serverless Jobs client follows the current required subnet/disk shape, direct status GET/polling, conservative resource-id/state parsing, MysteryBox-backed secret references, plaintext-secret rejection, retry/timeout/cancellation, and endpoint allow-listing.
- Privacy-safe Serverless dispatch exists: `NebiusResearchDispatcher` protects research checkpoints with AES-256-GCM and wraps per-item keys to a pinned worker RSA public key using OAEP-SHA256. Serverless args receive only an opaque work-item id plus non-secret protocol metadata; per-stage cloud authorization must match exact job + checkpoint + disclosure version; private OS-local data is rejected from remote execution.
- Bidirectional privacy boundary exists: `ResearchResultProtector` encrypts worker results back to the originating client using a separate client RSA key pair, authenticating opaque work-item id + remote Nebius job id + lifecycle metadata.
- `NebiusResearchWorker` accepts only opaque work-item id + remote-job provenance, retrieves/decrypts one bounded work item, executes exactly one `ResearchJobHandler` stage, refuses approval-bearing results, encrypts the result to the client, and publishes it through `IProtectedResearchResultTransport`.
- **New client ingestion boundary:** `RemoteResearchProvenance` is persisted on `AgentJobRecord`, and `RemoteResearchResultIngestor` attaches exact dispatch provenance then applies one protected worker result only if local job id, remote job id, opaque id, input checkpoint step, input checkpoint timestamp, state, and cryptographic envelope still match. Result application uses job-store CAS, rejects stale/duplicate/substituted/approval-bearing results, returns execution location to Local, and best-effort deletes bounded encrypted transport objects after commit.
- Production execution remains truthfully local: there is still no concrete shared cloud transport, built/published worker executable/image, crash-safe pre-dispatch reservation, or live Nebius validation.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, initial Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and OS-backed single-owner browser state. Representative commits include `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily quality, resumable research, Windows UX, recovery
Added Tavily Extract enrichment, exact credit accounting, deterministic evidence quality/staleness/diversity handling, staged research boundaries, exact prepared-evidence checkpoints, restart-safe synthesis without repeat Search/Extract, privacy-safe status, protected local persistence, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership. Representative commits include `9b45da151dedd946625962f485713a4543543cd6`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`, `7dc10b7293769ff4b89c3886a8a0ca13d4e47a86`, `6c3d86d8637f5dae55784b140ba046a0dc5a0d2b`, `69e3b9fc5ac4ba09439c62766e82d4110305c0bf`.

### 2026-09-09 — Nebius Serverless control plane and encrypted dispatch
Refreshed the Serverless client to required subnet/disk, direct status GET, conservative parsing, MysteryBox refs, secret rejection, and endpoint allow-listing. Added `NebiusResearchDispatcher` + `ResearchWorkItemProtector` for exact per-stage cloud authorization and encrypted opaque-ID-only research dispatch. Added protected worker-result return and the one-stage `NebiusResearchWorker`. Representative commits: `60a5f58b4ec414e400ac2ed88a614c18c34f57ac`, `5c1ab2457cc42254c5ff7c592334d533baeff347`, `59aab5d2258adb61e0e25dd04fd86365fad32f34`, `feb410eeb2ee7af6b2e664bc7bb234f9e8a262e7`, `8a61b5895824807b163e314a2724c8deea97db2c`, `22c1e7103a5cbc11bde38d69e6e8fd795d29f861`, `8b6269d85ee715933afdb34b9c1bedb716791bce`, `d16505842cd30d688b6c4ca81418d702f6ea9a2f`.

### 2026-09-09 — Current run: durable remote provenance + exact-once result CAS
Completed:
- Re-read `progress.md` completely at run start and inspected current NVIDEA head, recent commits, `ResearchJobRuntime`, `JobContracts`, `JsonAgentJobStore`, `NebiusResearchDispatch`, `NebiusResearchResultProtocol`, orchestrator transition semantics, and existing result tests before changing code.
- Extended `AgentJobRecord` with optional `RemoteResearchProvenance`; older persisted records deserialize with null and existing positional callers remain source-compatible because the field is optional and last.
- Added `JsonAgentJobStore.CompareExchangeAsync(expected, replacement)`:
  - performs load/compare/replace under the store semaphore and one atomic temp-file replacement;
  - compares durable state, execution location, attempt, update/retry timestamps, approval/error state, immutable definition, full checkpoint identity including payload, and full remote provenance;
  - rejects stale expected versions instead of blindly overwriting newer local work.
- Added `src/Nvidea.Core/Jobs/RemoteResearchResultIngestor.cs`:
  - durable `RemoteResearchProvenance` with protocol, opaque work-item id, remote Nebius job id, exact input checkpoint step/timestamp, dispatch time, lifecycle state and result-application time;
  - `AttachDispatchAsync` moves the exact pending local checkpoint into a durable Running/NebiusServerless record using CAS and emits an audit event;
  - sequential remote stages are supported only after the prior stage reached `ResultApplied`; unfinished remote provenance cannot be replaced;
  - `IngestAsync` retrieves the expected opaque result, cryptographically unprotects it, rejects implausible future completion times, checks exact local/remote/work-item/checkpoint provenance, rejects any approval-bearing result, and requires a durable output checkpoint;
  - result application uses CAS, changes the job to Pending or Completed according to the worker `JobStepResult`, returns execution location to Local, records `ResultApplied`, and cannot be applied twice;
  - encrypted result/work-item cleanup happens only after local CAS succeeds and is best-effort because transport objects are TTL-bounded.
- Added `tests/Nvidea.Core.Tests/RemoteResearchResultIngestorTests.cs` covering verified one-time ingestion, persisted execution/provenance transitions, duplicate rejection, changed-local-checkpoint rejection, audit emission, and direct stale-CAS rejection.
- During review, strengthened CAS to include checkpoint payload and all relevant state fields, and corrected the first provenance design so one completed remote stage does not permanently block dispatching a later stage.
- Did **not** wire production WPF/runtime to advertise Serverless. The dispatch/worker/ingestion primitives now form a much stronger protocol, but real transport, packaging, crash-safe dispatch reservation and live cloud validation still have to exist first.

Commits this run:
- `38e5bfc1c68a28d9d5229b6d633e0c8a7ae26f40` — persist remote research provenance on durable job records.
- `980b838ec7ea46af611d558fb9eaccb90050a158` — add remote research result ingestor and provenance lifecycle.
- `9138464ab26c5dc565e1b30541052e75bbc41976` — add atomic job compare-and-swap persistence.
- `2f95cf921950e16e4f2eb7f88290f12a412a2040` — add remote-result ingestion/CAS regression coverage.
- `5720b5ce98471358f284ab1d49ce93354db76669` — harden CAS version equivalence against checkpoint/state changes.
- `0467e36c3fa1dfb54734353225c409b725ba9f2d` — permit sequential remote stages only after verified prior application.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- No mutation was made to keyboard.wtf or any other repository.
- New tests are deterministic and require no Nebius/Tavily credentials; they exercise the real JSON job store and crypto result envelope path with generated RSA keys.
- This environment still exposes no `dotnet`, `msbuild`, or `csc`; **compilation and test execution are not claimed**.
- No GitHub Actions workflow was triggered merely to obtain a green signal.

Security / privacy / permissions / failure / cost review:
- Durable remote provenance contains opaque identifiers/checkpoint identity only; user question/evidence remains inside the already-protected local checkpoint and encrypted transport payload.
- Result authority is data-only: remote workers cannot return approval scope, and successful ingestion never creates a client-side approval grant.
- CAS prevents a late or duplicate cloud result from replacing a checkpoint that changed locally after dispatch.
- Both transport provenance and decrypted protected-result provenance must match the DPAPI-backed durable expectation.
- After a verified stage is applied, NVIDEA returns execution location to Local rather than leaving a stale Serverless claim.
- Cleanup occurs after durable commit so a cleanup error cannot cause replay/rollback; TTL remains the fallback retention bound.
- A remaining crash gap exists between remote job creation and `AttachDispatchAsync`: if the process dies after Nebius accepts creation but before the receipt is durably attached, the remote job may exist without local provenance. Production dispatch must reserve/persist an opaque pending dispatch before creation, then CAS in the remote job id after create succeeds.

## Known Blockers / Risks
- No verified .NET 8/Windows execution signal is available here; `dotnet build`, `dotnet test`, XAML load, DPAPI, and live WPF behavior remain unverified.
- New provenance/CAS/ingestion code and tests are statically reviewed but not compiled/executed.
- No production transport is accessible by both Windows and Nebius Serverless. It needs authenticated, bounded, TTL-backed, preferably create-once/write-once semantics.
- No built/published NVIDEA worker executable/image exists yet. `NebiusResearchWorker` is a core execution primitive, not a deployable entry point.
- There is a crash window after Nebius job creation but before receipt attachment. Add a pre-dispatch reservation state to close it before production remote execution.
- Remote status polling and explicit remote cancellation are not integrated with the durable provenance lifecycle yet; cancellation needs fail-closed state transitions and live verification of the retained REST cancel path.
- `ResearchJobRuntime` still deliberately rejects non-local records and therefore is not yet the production Serverless controller.
- A provider may have accepted work before a crash; retries can duplicate Nemotron/Tavily cost/latency.
- WPF research still favors the newest nonterminal job instead of a polished multi-job selector/history.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that remains unavailable, close the **dispatch crash gap and remote lifecycle**: persist a CAS-protected `DispatchReserved` provenance record containing the opaque work-item id and exact input checkpoint *before* calling Nebius, CAS the returned remote job id into that reservation after creation, recover/expire ambiguous reservations safely, poll typed remote status, and add explicit remote cancellation that cannot silently replay or delete uncertain work. Then add a minimal deployable worker entry point/image plus authenticated shared transport. Only after live Nebius validation should `ResearchJobRuntime`/WPF advertise `JobExecutionLocation.NebiusServerless` as a real production execution path.
