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
- `JsonAgentJobStore` and `JsonLinesAuditTrail` use Windows CurrentUser DPAPI by default on Windows.
- `ResearchJobRuntime` is the trusted local production host. Mutations use an OS-backed `StateDirectoryLease`; read-only status/report access remains lease-free. Explicit interrupted-stage recovery is research-only; generic Running jobs remain fail-closed.
- WPF surfaces durable research create/resume/cancel/recovery/report flows and participates in emergency stop.
- Playwright browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit recovery, emergency stop, crash recovery, and pre-transport single-owner browser-state locking.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Nebius Serverless Jobs client follows the current required subnet/disk shape, direct status GET/polling, conservative resource-id/state parsing, MysteryBox-backed secret references, plaintext-secret rejection, retry/timeout/cancellation, and endpoint allow-listing.
- Privacy-safe Serverless dispatch exists: `NebiusResearchDispatcher` protects research checkpoints with AES-256-GCM and wraps per-item keys to a pinned worker RSA public key using OAEP-SHA256. Serverless args receive only an opaque work-item id plus non-secret protocol metadata; per-stage cloud authorization must match exact job + checkpoint + disclosure version; private OS-local data is rejected from remote execution.
- **New bidirectional boundary:** `ResearchResultProtector` protects worker results back to the originating client using a separate client RSA key pair. Result checkpoint payloads are encrypted with AES-256-GCM; the data key is OAEP-SHA256-wrapped to the client public key. Opaque work-item id, remote Nebius job id, and lifecycle timestamps are authenticated associated data so provenance substitution is detected.
- **New worker execution primitive:** `NebiusResearchWorker` accepts only an opaque work-item id plus remote-job provenance, retrieves/decrypts one bounded work item, constructs a constrained research job, executes exactly one `ResearchJobHandler` stage, refuses approval-bearing results, encrypts the stage result for the client, and publishes it through `IProtectedResearchResultTransport`.
- Production execution remains truthfully local: there is still no concrete shared cloud transport, built/published worker executable/image, client result-ingestion/CAS path, or live Nebius validation.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, initial Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and OS-backed single-owner browser state. Representative commits include `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily quality, resumable research, Windows UX, recovery
Added Tavily Extract enrichment, exact credit accounting, deterministic evidence quality/staleness/diversity handling, staged research boundaries, exact prepared-evidence checkpoints, restart-safe synthesis without repeat Search/Extract, privacy-safe status, protected local persistence, WPF durable-research UX, emergency stop, explicit interrupted-stage recovery, generic non-research fail-closed behavior, and mutation-scoped cross-process research ownership. Representative commits include `9b45da151dedd946625962f485713a4543543cd6`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`, `7dc10b7293769ff4b89c3886a8a0ca13d4e47a86`, `6c3d86d8637f5dae55784b140ba046a0dc5a0d2b`, `69e3b9fc5ac4ba09439c62766e82d4110305c0bf`.

### 2026-09-09 — Nebius Serverless control plane and encrypted dispatch
Refreshed the Serverless client to required subnet/disk, direct status GET, conservative parsing, MysteryBox refs, secret rejection, and endpoint allow-listing. Added `NebiusResearchDispatcher` + `ResearchWorkItemProtector` for exact per-stage cloud authorization and encrypted opaque-ID-only research dispatch. Representative commits: `60a5f58b4ec414e400ac2ed88a614c18c34f57ac`, `5c1ab2457cc42254c5ff7c592334d533baeff347`, `59aab5d2258adb61e0e25dd04fd86365fad32f34`, `feb410eeb2ee7af6b2e664bc7bb234f9e8a262e7`, `8a61b5895824807b163e314a2724c8deea97db2c`.

### 2026-09-09 — Current run: protected result return + one-stage worker primitive
Completed:
- Re-read `progress.md` fully at run start and inspected the current head, recent commits, dispatch protocol, `ResearchJobHandler`, job contracts, capability contracts, research engine constructor, and existing dispatch tests before implementation.
- Added `src/Nvidea.Core/Jobs/NebiusResearchResultProtocol.cs`:
  - `RemoteResearchStageResult` and `ProtectedResearchResultEnvelope` with exact local-job, input-stage, opaque-work-item, remote-job, completion/expiry provenance;
  - `IProtectedResearchResultTransport` abstraction for shared result publication/retrieval/deletion;
  - `ResearchResultProtector` using random AES-256-GCM data keys wrapped to a pinned client RSA public key via OAEP-SHA256;
  - authenticated associated data binds protocol version + opaque work-item id + remote Nebius job id + lifecycle timestamps, so remote-job/work-item substitution fails cryptographic verification;
  - 2 MiB plaintext cap and 24-hour maximum result lifetime; malformed/expired/oversized envelopes fail before acceptance;
  - decrypted result provenance is checked against authenticated envelope metadata before returning it to the client;
  - `NebiusResearchWorker` that retrieves one opaque work item, decrypts it with the worker private key, rejects OS-private content, executes exactly one `ResearchJobHandler` stage, rejects approval-bearing output, protects the resulting checkpoint to the client public key, and publishes it.
- Added `tests/Nvidea.Core.Tests/NebiusResearchResultProtocolTests.cs` covering:
  - encrypted result round-trip and absence of plaintext question/evidence in serialized envelope;
  - remote Nebius job-id tamper rejection;
  - opaque work-item-id substitution rejection;
  - expiry rejection before decryption/ingestion.
- Did **not** switch `ResearchJobRuntime` or WPF execution location to `NebiusServerless`; no production claim is made until shared transport, worker packaging, client-side result ingestion/CAS, and live Nebius validation exist.

Commits this run:
- `22c1e7103a5cbc11bde38d69e6e8fd795d29f861` — add protected Nebius research result protocol and one-stage worker primitive.
- `8b6269d85ee715933afdb34b9c1bedb716791bce` — add protected result-provenance regression tests.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- The new path uses the existing staged `ResearchJobHandler`; it does not invent a second research implementation.
- Cryptographic/provenance tests require no cloud credentials and are deterministic apart from generated keys/nonces.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- This environment still has no verified `dotnet`, `msbuild`, or `csc` execution signal. Source/tests were statically reviewed, but **compilation and test execution are not claimed**.
- No other repository was mutated.

Security / privacy / permissions / failure / cost review:
- User research content and returned checkpoint payloads remain encrypted in both directions; neither path requires plaintext question/evidence in Serverless arguments or ordinary environment variables.
- Worker result provenance binds the exact opaque work item and Nebius remote job id cryptographically, reducing result-substitution risk.
- The worker executes one research stage only and cannot emit approval-bearing remote actions; this keeps consequential-action authority on the trusted client side.
- `ContainsPrivateOsData` is checked again worker-side even though dispatch authorization already rejects it, preserving defense in depth.
- Payload and lifetime caps bound accidental retention/cost; result expiry is fail-closed.
- A concrete shared transport still needs authenticated write-once/idempotent semantics. The current interfaces intentionally do not pretend to provide those guarantees.

## Known Blockers / Risks
- No verified .NET 8/Windows execution signal is available here; `dotnet build`, `dotnet test`, XAML load, DPAPI, and live WPF behavior remain unverified.
- New result/worker code and tests are statically reviewed but not compiled/executed.
- No production transport is accessible by both Windows and Nebius Serverless. It needs authenticated, bounded, TTL-backed, preferably write-once/idempotent work-item/result semantics.
- No built/published NVIDEA worker executable/image exists yet. The new `NebiusResearchWorker` is a core execution primitive, not a deployable entry point.
- Client-side result ingestion is not yet implemented. It must verify remote job id + opaque id + expected local job id + expected input checkpoint before atomically replacing the local durable checkpoint; stale/duplicate results must fail closed.
- Remote job polling/cancellation/result provenance is not yet persisted on `AgentJobRecord`; `ResearchJobRuntime` therefore correctly remains local.
- The retained REST cancel path still needs live-account verification before a judged cancellation demo.
- A provider may have accepted work before a crash; retries can duplicate Nemotron/Tavily cost/latency.
- WPF research still favors the newest nonterminal job instead of a polished multi-job selector/history.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that remains unavailable, implement the **client-side remote result ingestion/CAS boundary plus durable remote provenance**: persist opaque work-item id + Nebius remote-job id + expected input checkpoint on the local job, poll remote status, retrieve/decrypt the protected result, verify exact local-job/stage/job-id provenance, atomically apply the returned `JobStepResult` once, reject stale/duplicate/substituted results, and support explicit remote cancellation. In parallel or immediately after, add a minimal deployable worker project/entry point and a real authenticated shared transport adapter. Only after live Nebius validation should production set `JobExecutionLocation.NebiusServerless` or the WPF UI advertise real Serverless execution.
