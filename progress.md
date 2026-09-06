# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI edition inspired by keyboard.wtf for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest Windows interaction ideas while materially upgrading the intelligence/runtime into an NVIDIA/Nebius-first agent system with durable memory, research, complex browser automation, skills, permissioning, verification, and long-running execution.

Target track: **Personal AI**. Secondary prize target: **Best Use of Tavily**. Overall ambition: top-three / Grand Prize quality as a complete product rather than a model wrapper.

## HARD REPOSITORY BOUNDARY
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material. Never commit, edit, delete, open PRs/issues, change settings, rerun workflows, or otherwise mutate it.
- Never write to ANY other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not delete existing NVIDEA functionality merely to simplify implementation; migrate/refactor carefully and keep working paths unless a tested replacement exists.

## Required Work Loop
Every run must:
1. Read this file first.
2. Inspect current NVIDEA state and recent changes.
3. Choose the highest-value unfinished engineering task.
4. Verify current official platform/model/API assumptions when they can be stale.
5. Implement real code/config/tests/docs in NVIDEA; do not spend a run only planning when safe implementation is possible.
6. Validate as far as available tooling permits; never claim unverified behavior works.
7. Review correctness, privacy, permissions, prompt injection/tool trust, secrets, recovery/cancellation, UX, latency/cost and hackathon fit.
8. Update this file with completed work, evidence, unverified items, risks, blockers and the single best next task.
9. Continue improving on future runs while meaningful product/engineering work remains.

## Product gaps inherited from keyboard.wtf that NVIDEA must solve
### Memory
keyboard.wtf currently has bounded explicit key/value intent memory. NVIDEA must provide real working/session, episodic, semantic/profile, project/entity and skill/workflow memory with retrieval, provenance, sensitivity/retention, user controls and compaction.

### Research
Build Tavily-backed multi-query research with source collection/extraction, freshness, quality ranking, deduplication, citations/provenance, explicit uncertainty and resumable long tasks.

### Browser automation
Build DOM/accessibility-first browser agency with navigation/read/click/type/select/upload/download where permissioned, robust locators, plan-act-observe-verify, state verification, retries/cancellation/recovery, authentication boundaries, prompt-injection defenses, audit evidence and confirmation gates for consequential actions.

## Target Architecture
### Desktop shell
Windows-first hotkeys, voice/text, orb/status, active-app/selected-text/clipboard context, permission UX, local speech where useful and emergency stop.

### Agent core
Task planner/state machine; structured tool calls; iterative plan -> act -> observe -> verify; bounded autonomy; cancellation; retries; explicit completion criteria; human approvals.

### NVIDIA/Nebius backend
NVIDIA open models through Nebius Token Factory / Nebius AI Cloud are the genuine core runtime. Use a provider abstraction, verified model IDs, structured output/tool calling, timeout/rate-limit handling and later telemetry/model routing. Gemini/OpenAI/Claude must not be hidden primary dependencies.

### Memory
Layered typed memory with privacy-aware writes, deterministic persistence, semantic/lexical retrieval, recency/importance weighting, provenance/confidence/sensitivity/retention and user deletion controls. Add compaction/summarization and a verified production embedding adapter later.

### Skills / permissions
Capability registry with name/version, tools, data permissions, risk class, confirmation requirements, inputs/outputs and evaluation fixtures. Least privilege; consequential send/submit/publish/delete/purchase/financial/account/security actions require approval.

### Nebius execution
Use Nebius serverless/cloud execution where it adds real value to long-running/background work; keep OS-private actions on the user's machine.

## Hackathon product bar
Every major feature should strengthen technological implementation, design, potential impact or quality/originality of idea. Final <=3 minute demo should prove: invocation anywhere on Windows, context awareness, durable memory affecting later behavior, Tavily research with sources, complex multi-step browser work with verification, approval before a consequential action, meaningful background Nebius execution, and a concise architecture view proving Nemotron/Nebius/Tavily are core.

## Engineering priorities
1. Clean solution/package/build/test baseline.
2. Nebius Token Factory + NVIDIA Nemotron core backend.
3. Layered memory + retrieval + tests.
4. Tavily research + citations/provenance + tests.
5. Safe browser agent/runtime with observation/action/verification loop.
6. Skills + permission/risk engine + audit trail.
7. Resumable jobs + Nebius serverless where justified.
8. Windows desktop UX integration using useful keyboard.wtf patterns read-only.
9. Reliability/evals, cancellation/retries/offline/error states.
10. Security/privacy threat model and hardening.
11. Packaging/onboarding/demo environment.
12. Hackathon README, architecture diagrams, setup, attribution, significant-changes documentation, demo scenario and final rubric audit.

## Current State
- Standalone .NET 8 core at `src/Nvidea.Core`.
- NVIDIA/Nebius-first Token Factory inference client; no Gemini/OpenAI/Claude dependency required by core.
- Verified default model ID: `nvidia/nemotron-3-super-120b-a12b`; fast/deep tier IDs remain configuration-only until individually verified.
- Layered personal-memory foundation now exists under `src/Nvidea.Core/Memory`.
- Contract tests exist under `tests/Nvidea.Core.Tests` for inference and memory behavior.
- Root README + MIT LICENSE exist.
- keyboard.wtf has only been inspected read-only; no mutation has been performed there or in any repository other than NVIDEA.

## Progress Log
### 2026-09-06 — Initialization
- Established immutable repository-safety boundary.
- Captured keyboard.wtf strengths/gaps, Personal AI architecture and judging bar.
- Defined memory, research, browser automation, skills, security, Nebius/NVIDIA and demo workstreams.

### 2026-09-06 — Nebius/Nemotron core foundation
Completed:
- Verified current Nebius Token Factory OpenAI-compatible API, tool/function support, structured JSON support and documented Nemotron 3 Super model ID using official sources.
- Added `NebiusTokenFactoryClient` + `IAgentInferenceClient` with bearer auth, structured tools/tool-call parsing, optional JSON schema, cancellation/timeouts, bounded retries, HTTPS/Nebius endpoint validation and secret-safe errors.
- Added conservative fast/standard/deep routing without guessing undocumented model IDs.
- Added contract tests for request shape, routing, auth, tool calls, retries, failures and endpoint safety.
- Added root README and MIT LICENSE.

Validation / known limitations:
- Runtime uses framework libraries only for inference.
- Contract tests need no real API key.
- Execution environment did not provide `dotnet`, `csc`, `mcs` or `msbuild`; compile/test execution remains unverified until a .NET-capable environment runs them.
- Structured-output behavior still needs real Token Factory integration testing.
- `Retry-After`, usage/cost telemetry and verified fast/deep model IDs remain future hardening.

### 2026-09-06 — Layered personal-memory foundation
Completed:
- Added `MemoryModels.cs` with typed layers: Working, Episodic, Semantic, Project and Skill.
- Added typed provenance, confidence, importance, sensitivity (Public/Personal/Sensitive/Restricted), retention (Session/7d/30d/Indefinite), expiry and optional embedding metadata.
- Added `IMemoryEmbeddingProvider`, `IMemoryStore` and `IMemoryWritePolicy` boundaries so storage/embedding backends are replaceable and testable without cloud credentials.
- Added `DefaultMemoryWritePolicy`:
  - rejects empty/oversized/invalid confidence/importance writes;
  - refuses likely private keys, bearer tokens, JWTs and common credential assignments even if the caller asks to save them;
  - requires explicit user approval for Sensitive/Restricted memories;
  - requires explicit approval for indefinite retention.
- Added `JsonFileMemoryStore` with async read/write, serialized enums, process-level write gate, atomic temp-file replacement and no plaintext secret-specific behavior in the memory layer.
- Added `PersonalMemoryService` with:
  - explicit initialization and expiry cleanup;
  - update-in-place for matching layer + key rather than uncontrolled duplicates;
  - session memory that remains in process and is deliberately excluded from persistence;
  - 7-day, 30-day and indefinite retention behavior;
  - default retrieval limited to Public + Personal sensitivity unless the caller explicitly expands scope;
  - hybrid scoring: semantic similarity when embeddings exist, lexical relevance, recency, importance and confidence;
  - deterministic lexical fallback when embeddings are unavailable;
  - embedding failures degrade to lexical retrieval rather than breaking memory operations;
  - strict relevance gate so recent/important but unrelated memories are not returned merely due to recency;
  - tag/layer/sensitivity filters;
  - deletion by id, deletion by layer, expiry purge and snapshots;
  - access timestamps and immediate persistence of expiry cleanup.
- Added memory tests covering:
  - credential-like material rejected even with explicit approval;
  - sensitive and indefinite retention approval requirements;
  - session memory never persisted and disappears across service restart;
  - semantic ranking with deterministic embeddings;
  - sensitive memory excluded from default search and available only when explicitly scoped in;
  - seven-day expiry/purge;
  - matching layer/key update instead of duplicate creation;
  - JSON store round-trip;
  - no unrelated results in lexical-only mode;
  - lexical fallback when embedding provider is unavailable.
- Updated README to document the memory contract and current implemented status.

Validation / evidence:
- Performed manual source review after writing the subsystem and fixed a C# regex-literal syntax risk in the secret detector.
- Performed a second retrieval review and fixed a logic flaw where recency/importance alone could make unrelated memory appear relevant without embeddings.
- Attempted to clone the public repository into the local execution container for an additional static/toolchain check, but that container has no DNS/network route to GitHub. This does not affect the GitHub connector writes.
- As in the previous run, no .NET SDK/compiler is available in the execution environment, so the new code/tests have NOT been compiled/executed here. Do not claim green tests until a .NET-capable runner verifies them.

Unverified / risks:
- Full compilation remains the highest immediate verification risk because the automation runtime lacks a .NET toolchain.
- The current JSON persistence is intentionally a deterministic baseline, not yet OS-encrypted storage. Before desktop integration, sensitive persistent memory should use an encrypted-at-rest store or an encryption wrapper backed by Windows DPAPI/key protection.
- Embeddings are an interface only. Do not guess a Nebius embedding model; verify a current supported production embedding endpoint/model before implementing the adapter.
- Memory compaction/summarization, salience promotion/demotion, contradiction handling and user-facing memory-management UX are not yet implemented.
- `SnapshotAsync` is a management/debug primitive and can expose all loaded sensitivity classes; UI callers must not use it as ambient prompt context. Normal `SearchAsync` defaults to Public + Personal only.
- Embedding failures currently degrade silently to lexical retrieval; future telemetry should record degradation without exposing memory contents.

Next highest-value task:
- Implement the Tavily-backed research subsystem in `Nvidea.Core` with a provider abstraction and current official Tavily API verification: query planning contract, search/extract result models, URL canonicalization/deduplication, source quality/freshness metadata, provenance/citation objects, bounded retries/timeouts/cancellation, synthesis input that clearly marks web content as untrusted, and contract tests with mocked HTTP. Keep actual Nemotron synthesis behind the existing inference abstraction so research can later become a resumable long-running skill rather than a monolithic API call.
