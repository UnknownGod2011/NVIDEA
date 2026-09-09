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
- Layered personal memory with privacy-aware writes and retrieval.
- Tavily Search + query-focused advanced Extract research pipeline with canonical URL deduplication, bounded extraction, credit accounting, deterministic authority/freshness/diversity ranking, uncertainty warnings, and Nemotron synthesis.
- Source text remains untrusted data; source-quality metadata is separate and explicitly heuristic; `[src:SOURCE_ID]` citations are machine-validated.
- Durable research is staged at remote-work boundaries: request -> Nemotron plan -> Tavily Search/Extract + ranking -> prepared evidence -> Nemotron synthesis -> completed report.
- `ResearchJobHandler` persists versioned checkpoints. Prepared evidence includes exact ranked provenance, quality metadata, warnings, and cumulative Tavily credits so normal resume does not repeat Search/Extract or rerank evidence.
- Research checkpoint payloads are bounded to 2 MiB UTF-8. `JsonAgentJobStore` and `JsonLinesAuditTrail` use Windows CurrentUser DPAPI by default on Windows.
- `ResearchJobRuntime` is the trusted production host for durable local research. Every durable research mutation runs under an OS-backed `StateDirectoryLease` spanning the full remote provider stage and checkpoint persistence; read-only status/report access remains lease-free.
- Research has explicit interrupted-Running recovery. A stale local `Running` research record with a known checkpoint can be explicitly re-armed to `Pending`; re-arm performs no provider call, preserves the checkpoint, emits audit evidence, warns about possible duplicate cost, and requires a second deliberate action before retry.
- Generic `ResumableJobOrchestrator` behavior remains fail-closed for `Running` jobs; browser/consequential jobs are not auto-replayed.
- WPF surfaces durable research create/resume/cancel/recovery/report flows with privacy-safe stage telemetry and emergency-stop integration.
- Playwright browser agent includes persistent Chromium profile, popup/new-tab tracking, iterative verification, prompt-injection/tool-output trust boundaries, permission gates, durable download quarantine, explicit download recovery, emergency stop, and crash recovery.
- `StateDirectoryLease` provides process-local plus OS-backed file locking with bounded owner metadata, stale-file recovery, and reparse-point rejection. Browser ownership begins before Playwright transport startup.
- Capability registry, least-privilege permission policy, exact single-use approvals, protected hash-chained/segmented audit trail, durable jobs, and Nebius Serverless contracts exist.
- **Nebius Serverless Jobs control plane is now refreshed against the current API contract:** create requires explicit subnet and disk, GET-by-job-id is supported for status polling, create-operation `resourceId` and direct `status.state` have conservative parsers, and secret-bearing environment variables can be supplied through Nebius MysteryBox references instead of plaintext values.
- Research execution is still truthfully displayed as local. A real remote worker/dispatcher is not yet connected and no serverless execution claim is made.

## Persistent Progress History

### 2026-09-06 to 2026-09-08 — Core platform and safe browser state
Added Nebius/Nemotron inference, layered memory, Tavily research, capability/approval/audit infrastructure, durable jobs, initial Nebius Serverless contracts, Playwright execution, Windows shell, DPAPI state protection, persistent Chromium sessions, durable verified downloads, crash recovery, and OS-backed single-owner browser state. Representative commits include `2de5457b0ad514aa46cc0a0e645a3e8bcd0bcdbf`, `f32619c68f06d1fbc99eb69cea7af8948dba4c1f`, `15d905c52a472face95fdce4dc7f91e88f171a7e`, and `2f3f1f33423f5bfbd33abcc174fafa0052278355`.

### 2026-09-09 — Tavily evidence quality and resumable research
Added advanced Tavily Extract enrichment, fail-soft fallback, exact credit accounting, deterministic authority/freshness/diversity ranking, stale/unknown-current-event warnings, deceptive-subdomain hardening, staged `ResearchEngine` boundaries, exact prepared-evidence checkpoints, and resume-from-evidence without repeated Search/Extract/reranking. Representative commits: `9b45da151dedd946625962f485713a4543543cd6`, `9f16c1fb658394c0718d391f7fbead07d9166d45`, `067c3b94b83fd9eebe48a39c4b7aa47dcec922af`, `5affb9d37266b3d137fe2adb4c84dc367bdfcadb`, `d72172bc6e7377df90ec4ab7e9536ac02334afdc`.

### 2026-09-09 — Production durable research UX and crash recovery
Added privacy-safe `ResearchJobStatus`, trusted `ResearchJobRuntime`, composition-root/WPF wiring, restart recovery from saved evidence, Windows create/resume/cancel/report UI, emergency-stop participation, explicit interrupted-stage recovery, and generic non-research fail-closed regression coverage. Representative commits: `e6409e2260deb7f9938596d372e89adc7bee5978`, `ac305dd8158518274de5779c681f6f29c6ed5b71`, `7dc10b7293769ff4b89c3886a8a0ca13d4e47a86`, `6c3d86d8637f5dae55784b140ba046a0dc5a0d2b`, `93cd4c84faa5c7aee528357dca0ee702b9f17907`, `8227c91c4affceb0333f525f6da571a1347c1441`.

### 2026-09-09 — Cross-process research mutation ownership
Added mutation-scoped `StateDirectoryLease` ownership around research creation, stage execution, re-arm, and cancellation. The lease spans remote provider work plus persistence. A concurrency fixture verifies a second runtime can read status but cannot mutate while the first owns the research state. Representative commits: `43712cf72dc673775620f8b93046cba9ff01c54d`, `98d3d4355028f0bc541b75aca1905bcbb456f661`, `771656a5c8300707cdbd45ad82809a0bdde59875`, `69e3b9fc5ac4ba09439c62766e82d4110305c0bf`.

### 2026-09-09 — Current run: harden Nebius Serverless Jobs control plane
Completed:
- Re-read this file completely at run start and inspected recent commits, the repository tree, `ResearchJobRuntime`, `JobContracts`, the existing `NebiusServerlessJobClient`, its tests, and the current public Nebius API definitions before changing code.
- Re-verified current Nebius Serverless documentation and the official `nebius/api` `JobSpec`/`JobService` definitions rather than relying on the older local client assumptions.
- Found a material contract drift: current `JobSpec` requires `disk` and `subnet_id`; the local client treated subnet as optional and did not send a disk at all. The official API also supports MysteryBox secret references for environment variables and exposes direct job GET/status states.
- Updated `src/Nvidea.Core/Jobs/NebiusServerlessJobClient.cs`:
  - added explicit `NebiusServerlessDiskSpec`;
  - requires non-empty `SubnetId` and positive explicit disk size before any network call;
  - serializes the required `disk` object;
  - added `GetAsync(remoteJobId)` against `/ai/v1/jobs/{job_ID}` for status polling;
  - added conservative response helpers for create-operation `resourceId` and direct `status.state`, returning unknown instead of guessing when absent;
  - added `NebiusMysteryBoxSecretRef` and `SecretEnvironmentVariables`, serialized as `mysteryboxSecret` references;
  - plaintext secret-like environment names remain rejected before network I/O;
  - duplicate plaintext+secret definitions for the same variable fail closed;
  - retained endpoint allow-listing, bearer auth, retry, timeout, cancellation, and JSON validation behavior.
- Updated `tests/Nvidea.Core.Tests/NebiusServerlessJobClientTests.cs`:
  - current required create payload including disk/subnet;
  - create `resourceId` parsing;
  - direct GET endpoint and `RUNNING` state parsing;
  - MysteryBox reference serialization with no plaintext secret value;
  - rejection of missing disk/subnet before network I/O;
  - rejection of plaintext secret-like variables and duplicate plaintext/secret definitions;
  - existing retry/permanent-error and endpoint allow-list behavior retained.
- Static review caught and corrected an overcomplicated environment-variable validator and a nullable test assertion before finalizing.

Commits this run:
- `60a5f58b4ec414e400ac2ed88a614c18c34f57ac` — refresh Serverless Jobs contract and MysteryBox support.
- `2e6dfcf114737080ef1ba08dae04a6b8c02ed63b` — simplify environment-name validation.
- `5c1ab2457cc42254c5ff7c592334d533baeff347` — update current Serverless contract tests.
- `3b177a8438cbb6b1c6081dc8e2340a44e42ae8ff` — correct missing-disk regression fixture.
- `efbc75a953fe94875779c0a990ebc7435125b121` — nullable warnings-as-errors hardening.

Validation / evidence:
- Repository identity was explicitly verified as exactly `UnknownGod2011/NVIDEA` before every GitHub mutation.
- Official Nebius quickstart currently documents REST create at `POST https://api.nebius.cloud/ai/v1/jobs`, direct status retrieval at `GET /ai/v1/jobs/<job_ID>`, required subnet/disk examples, and create `resourceId`.
- Official `nebius/api` `JobSpec` marks `disk` and `subnet_id` required and defines MysteryBox-backed environment variables; `JobStatus.State` includes PROVISIONING/STARTING/IMAGE_PULLING/RUNNING/CANCELLING/COMPLETED/FAILED/CANCELLED/ERROR.
- No GitHub Actions workflow was triggered merely to obtain a green result.
- This execution environment still exposes no verified .NET SDK/compiler signal, so compilation and test execution are **not claimed**.
- No other repository was mutated.

Security / privacy / permissions / cost review:
- Serverless job specs no longer silently omit required infrastructure fields; callers must make disk/subnet allocation explicit.
- Disk size must be positive and explicit, avoiding a hidden default storage allocation/cost choice inside NVIDEA.
- Secret-like environment values cannot be supplied plaintext through this client; MysteryBox references provide the intended control-plane path for Tavily/Nebius credentials in a future worker.
- The remote control plane still must not receive private OS context by default. No serverless research dispatcher was enabled in this run, so local execution-location claims remain truthful.
- Create/cancel operation JSON remains opaque except for narrowly parsed fields; no speculative success state is inferred.

## Known Blockers / Risks
- This environment still lacks a verified .NET 8/Windows execution signal. Source/tests are not substitutes for `dotnet build`, `dotnet test`, XAML load, DPAPI execution, and real WPF interaction.
- The refreshed Serverless client and tests are statically reviewed but have not been compiled/executed here.
- The local REST cancel path (`POST /ai/v1/jobs/cancel`) is retained from the existing contract and CLI/proto cancellation support, but the current public quickstart exposes REST create/get/delete more explicitly than the cancel REST transcoding path. It should be verified with a real Nebius account or generated client before relying on cancellation in a demo.
- There is still no NVIDEA Serverless research worker image, protected remote payload transport, remote-result/checkpoint ingestion, or production dispatcher. Therefore durable research remains correctly local.
- Passing a user's research question directly in container args/environment would make it control-plane-visible and is not acceptable as the default privacy design. A remote dispatcher needs explicit cloud disclosure plus a bounded protected payload channel.
- A stale local `Running` stage may already have reached a remote provider before a crash; explicit retry can duplicate Nemotron/Tavily cost/latency.
- WPF research currently favors the newest nonterminal job rather than a polished multi-job selector/history.
- Local voice/transcription and a verified production embedding adapter remain absent.

## Single Best Next Task
First obtain a real Windows/.NET 8 build + tests + WPF launch signal and repair every issue found. If that execution signal remains unavailable, continue the Serverless path **without faking completion**: implement the privacy/permission-aware Nebius research-dispatch contract and worker protocol around an explicit cloud-disclosure decision, opaque job/work-item identifiers, MysteryBox credential references, typed remote state polling/cancellation, and durable remote-job provenance. Do not send private OS context or silently place user questions/secrets in job args/environment. Only after that protocol exists should `ResearchJobRuntime` create records with `JobExecutionLocation.NebiusServerless` and the WPF UI expose serverless execution.
