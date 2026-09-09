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
- Durable research is staged at remote-work boundaries: request -> Nemotron plan -> Tavily Search/Extract + ranking -> prepared evidence -> Nemotron synthesis -> completed report.
- `ResearchJobHandler` persists versioned checkpoints up to 2 MiB UTF-8. Prepared evidence includes exact ranked provenance, quality metadata, warnings, and cumulative Tavily credits so resume does not repeat Search/Extract or rerank evidence.
- `JsonAgentJobStore` and `JsonLinesAuditTrail` use Windows CurrentUser DPAPI by default on Windows.
- `ResearchJobRuntime` is the trusted production host for durable local research. Every mutation runs under an OS-backed `StateDirectoryLease` spanning provider work plus checkpoint persistence; read-only status/report access remains lease-free.
- Research has explicit interrupted-Running recovery: stale local research can be explicitly re-armed without provider work, then requires a second deliberate action before retry. Generic non-research `Running` jobs remain fail-closed.
- WPF surfaces durable research create/resume/cancel/recovery/report flows with privacy-safe stage telemetry and emergency-stop integration.
- Playwright browser agent includes persistent Chromium state, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit recovery, emergency stop, and crash recovery.
- `StateDirectoryLease` provides process-local plus OS-backed file locking with bounded owner metadata, stale-file recovery, reparse-point rejection, and pre-Playwright ownership.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- Nebius Serverless Jobs control-plane support is refreshed to the current API shape: required subnet/disk, direct job GET/status polling, conservative `resourceId`/state parsing, MysteryBox-backed secret environment references, retry/timeout/cancellation, and endpoint allow-listing.
- **New remote research boundary:** `NebiusResearchDispatcher` and `ResearchWorkItemProtector` now define an explicit privacy-aware Serverless dispatch protocol. Research checkpoints are hybrid-encrypted with AES-256-GCM and a random per-item data key wrapped to a pinned worker RSA public key via OAEP-SHA256. Nebius job arguments receive only a random opaque work-item ID and non-secret protocol metadata; user questions/checkpoint payloads are not placed in args or plaintext environment variables.
- Remote dispatch requires exact per-job/per-checkpoint cloud authorization and rejects private OS-local data even when approval is supplied.
- Serverless credentials remain expected through MysteryBox references. There is still no production shared transport adapter or built/published worker image, so `ResearchJobRuntime` remains truthfully local and the WPF UI does not yet claim Serverless execution.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, initial Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and OS-backed single-owner browser state. Representative commits: `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily evidence quality and resumable research
Added advanced Tavily Extract enrichment, fail-soft fallback, exact credit accounting, deterministic authority/freshness/diversity ranking, stale/unknown-current-event warnings, deceptive-subdomain hardening, staged `ResearchEngine` boundaries, exact prepared-evidence checkpoints, and resume-from-evidence without repeated Search/Extract/reranking. Representative commits: `9b45da151dedd946625962f485713a4543543cd6`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `5affb9d37266b3d137fe2adb4c84dc367bdfcadb`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`.

### 2026-09-09 — Production durable research UX, recovery, and ownership
Added privacy-safe `ResearchJobStatus`, trusted `ResearchJobRuntime`, composition-root/WPF wiring, restart recovery from saved evidence, Windows create/resume/cancel/report UI, emergency-stop participation, explicit interrupted-stage recovery, generic non-research fail-closed regression coverage, and mutation-scoped OS-backed single-owner research state. Representative commits: `e6409e2260deb7f9938596d372e89adc7bee5978`, `ac305dd8158518274de5779c681f6f29c6ed5b71`, `7dc10b7293769ff4b89c3886a8a0ca13d4e47a86`, `6c3d86d8637f5dae55784b140ba046a0dc5a0d2b`, `93cd4c84faa5c7aee528357dca0ee702b9f17907`, `69e3b9fc5ac4ba09439c62766e82d4110305c0bf`.

### 2026-09-09 — Nebius Serverless control-plane hardening
Refreshed the client against current Nebius Serverless Jobs requirements: explicit subnet/disk, direct status GET, conservative `resourceId`/state parsing, MysteryBox references, plaintext-secret rejection, duplicate-env rejection, endpoint allow-listing, and contract tests. Representative commits: `60a5f58b4ec414e400ac2ed88a614c18c34f57ac`, `5c1ab2457cc42254c5ff7c592334d533baeff347`, `efbc75a953fe94875779c0a990ebc7435125b121`, `1e7dff5ce05567567f08523e14860d1da42704c7`.

### 2026-09-09 — Current run: privacy-safe Nebius remote research protocol
Completed:
- Re-read this file completely at run start and inspected the current `NVIDEA` head, recent commits, `ResearchJobRuntime`, `JobContracts`, `ResearchJobHandler`, `NebiusServerlessJobClient`, Serverless contract tests, README status, and current Nebius/NVIDIA-related public material before implementation.
- Added `src/Nvidea.Core/Jobs/NebiusResearchDispatch.cs` with:
  - `RemoteResearchWorkItem`, `ProtectedResearchWorkItemEnvelope`, exact `ResearchCloudAuthorization`, Serverless dispatch options/receipt, and `IProtectedResearchWorkItemTransport`;
  - hybrid payload protection: random 256-bit AES-GCM data key, 96-bit nonce, 128-bit tag, worker RSA public-key wrapping via OAEP-SHA256, bounded 2 MiB plaintext, 24-hour maximum lifetime, random URL/CLI-safe opaque work-item IDs, and authenticated lifecycle metadata;
  - worker-side `Unprotect` path with protocol/version, expiry, cryptographic-dimension, opaque-ID, metadata-integrity, and private-key validation;
  - cloud authorization validation scoped to the exact local job id + checkpoint step + disclosure version, with explicit approval required;
  - hard rejection of `ContainsPrivateOsData` for remote research even when approval is supplied;
  - `NebiusResearchDispatcher` that uploads only the encrypted envelope to an injected transport, then creates a Serverless Job whose args contain only `--work-item-id=<opaque-id>` plus non-secret protocol metadata;
  - existing MysteryBox secret references flow through to the Serverless job spec, so Tavily/Nebius/worker private-key secrets need not appear as plaintext env values;
  - failed Serverless creation performs best-effort encrypted-work-item cleanup; a successful/2xx create that does not expose `resourceId` intentionally retains the encrypted payload until expiry because remote creation is ambiguous and deleting it could break a job that actually started.
- Added `tests/Nvidea.Core.Tests/NebiusResearchDispatchTests.cs` covering:
  - encrypt/decrypt round trip and absence of plaintext question/evidence from the envelope representation;
  - authenticated metadata tamper detection;
  - exact per-job/per-stage cloud authorization before any upload/control-plane call;
  - private OS-data rejection before upload;
  - opaque-ID-only Serverless args and protocol metadata;
  - worker decryption recovering the exact original checkpoint;
  - encrypted payload cleanup when create fails;
  - encrypted payload retention when create succeeds but the resource id is ambiguous.
- No `ResearchJobRuntime` or WPF execution-location switch was made. This is intentional: the protocol exists, but a real shared transport adapter, worker executable/image, result-ingestion channel, and live Nebius validation are still missing.

Commits this run:
- `59aab5d2258adb61e0e25dd04fd86365fad32f34` — add privacy-safe Nebius research dispatch protocol.
- `feb410eeb2ee7af6b2e664bc7bb234f9e8a262e7` — add privacy-safe research dispatch regression tests.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation.
- Current Nebius ecosystem material continues to distinguish Token Factory credentials from Nebius IAM credentials and recommends non-committed secret configuration; the existing client already models MysteryBox references rather than plaintext secret-like environment values.
- Tests were written against pure cryptographic and contract fakes; no real cloud credentials are required.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- This execution environment still exposes no usable `dotnet`, `msbuild`, or `csc` signal; source and tests were statically reviewed but **compilation/test execution are not claimed**.
- No other repository was mutated.

Security / privacy / permissions / cost review:
- User questions and research checkpoint JSON are encrypted before entering the remote work-item transport and are never placed in Nebius job arguments or plaintext env variables by the dispatcher.
- Per-stage authorization is explicit and narrowly scoped; approval for one job/checkpoint cannot authorize another.
- OS-private data remains local by policy even if a caller tries to provide cloud approval.
- Envelope metadata is authenticated with AES-GCM associated data, making opaque-ID/lifecycle tampering fail decryption.
- Per-item encryption keys are random and zeroed after use; payload plaintext is zeroed after protection/decryption where practical in managed memory.
- Encrypted payload lifetime is bounded to 24 hours and plaintext size to 2 MiB to constrain accidental retention/cost.
- Failed create attempts clean up encrypted work items best-effort. Ambiguous successful creates retain ciphertext until expiry to avoid breaking a potentially running remote job.
- The transport abstraction is intentionally not presented as a finished cloud integration; production must supply a real authenticated shared transport and worker image.

## Known Blockers / Risks
- This environment still lacks a verified .NET 8/Windows execution signal. Source/tests are not substitutes for `dotnet build`, `dotnet test`, XAML load, DPAPI execution, and real WPF interaction.
- New dispatch/protector tests are statically reviewed but have not been compiled/executed here.
- There is no production `IProtectedResearchWorkItemTransport` adapter accessible from both Windows and Nebius Serverless yet.
- There is no built/published NVIDEA remote worker image that retrieves an opaque item, reads its private key via MysteryBox, decrypts/validates the checkpoint, executes exactly one research stage, encrypts a result, and publishes it back.
- There is no result-envelope/result-ingestion protocol yet, so remote checkpoints cannot safely flow back into the local durable store.
- Remote job polling/cancellation provenance is not yet attached to `AgentJobRecord`; `ResearchJobRuntime` therefore correctly remains local.
- The retained REST cancel path should still be verified with a real Nebius account/generated client before a judged live cancellation demo.
- A stale local/remote stage may already have reached a provider before a crash; explicit retry can duplicate Nemotron/Tavily cost/latency.
- WPF research currently favors the newest nonterminal job rather than a polished multi-job selector/history.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that execution signal remains unavailable, continue the remote path without overstating completion: implement the **bidirectional protected worker/result protocol** and a real shared transport adapter, then add a minimal NVIDEA worker entry point that accepts only an opaque work-item id, retrieves/decrypts one bounded checkpoint using a MysteryBox-supplied private key, executes exactly one `ResearchJobHandler` stage, protects the result for the originating Windows client, and publishes it back with TTL/idempotency/provenance. Only after that is live-tested should `ResearchJobRuntime` persist `JobExecutionLocation.NebiusServerless` and the WPF UI expose real Serverless execution.
