# NVIDEA Hackathon Progress

## Mission
Build a competition-grade, open-source Personal AI edition inspired by keyboard.wtf for the Nebius x NVIDIA Global AI Hackathon. Preserve the strongest Windows interaction ideas while materially upgrading the intelligence/runtime into an NVIDIA/Nebius-first agent system with durable memory, research, complex browser automation, skills, permissioning, verification, and long-running execution.

Target track: **Personal AI**. Secondary target: **Best Use of Tavily**. Overall ambition: top-three / Grand Prize quality as a complete product rather than a model wrapper.

## HARD REPOSITORY BOUNDARY
- WRITE ONLY to `UnknownGod2011/NVIDEA`.
- `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material. Never commit, edit, delete, open PRs/issues, change settings, rerun workflows, or otherwise mutate it.
- Never write to ANY other repository.
- Before every GitHub mutation, verify the repository target is exactly `UnknownGod2011/NVIDEA`.
- Do not delete working NVIDEA functionality merely to simplify implementation; migrate/refactor carefully and keep working paths unless a tested replacement exists.

## Required Work Loop
Every run must read this file first, inspect current NVIDEA state, choose the highest-value unfinished engineering task, verify current platform/API assumptions where stale behavior matters, implement real code/tests/docs only in NVIDEA, validate as far as tooling permits, review safety/correctness, update this ledger, and continue improving while meaningful work remains.

## Target Architecture
- **Desktop shell:** Windows hotkeys, voice/text, orb/status, active-app/selected-text/clipboard context, permission UX, local speech where useful and emergency stop.
- **Agent core:** task planner/state machine, structured tool calls, bounded iterative execution, verification, cancellation/retries and approvals.
- **NVIDIA/Nebius:** NVIDIA open models through Nebius Token Factory / AI Cloud as genuine core runtime; no hidden Gemini/OpenAI/Claude dependency.
- **Memory:** layered typed working/episodic/semantic/project/skill memory with privacy-aware writes, semantic/lexical retrieval, recency/importance, provenance/confidence/sensitivity/retention and deletion controls.
- **Research:** Nemotron planning -> Tavily evidence -> untrusted-content boundary -> Nemotron synthesis -> validated source IDs.
- **Browser:** accessibility/DOM observation -> typed action -> browser hard-safety floor -> system capability policy -> exact approval -> driver execution -> fresh observation -> verification -> receipt.
- **Skills / permissions:** capability registry, declared data/tool permissions, monotonic risk, exact-scope approvals, and append-only audit events.
- **Jobs:** durable state/checkpoints, cancellation, retry/backoff, approval-paused states and explicit local-vs-Nebius execution selection.
- **Nebius execution:** cloud/serverless only where long-running background work benefits; private OS actions stay local.

## Hackathon product bar
Final <=3 minute demo should prove invocation anywhere on Windows, context awareness, durable memory affecting later behavior, Tavily research with sources, complex browser work with verification, approval before consequential actions, meaningful background Nebius execution, and an architecture view proving Nemotron/Nebius/Tavily are core.

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
- NVIDIA/Nebius-first Token Factory inference client with verified default `nvidia/nemotron-3-super-120b-a12b`, structured tools/output, cancellation/timeouts, bounded retries and endpoint validation.
- Layered privacy-aware personal memory under `src/Nvidea.Core/Memory`.
- Tavily provider + Nemotron research engine under `src/Nvidea.Core/Research`.
- Provider-neutral safe browser-agent foundation under `src/Nvidea.Core/Browser`.
- System-wide capability/permission/audit foundation under `src/Nvidea.Core/Capabilities`.
- Durable resumable jobs foundation under `src/Nvidea.Core/Jobs`.
- Contract tests under `tests/Nvidea.Core.Tests` for inference, memory, research, browser policy/execution, capability policy, scoped approval, audit behavior and resumable jobs.
- Root README + MIT LICENSE.
- keyboard.wtf has only been inspected read-only; no repository other than NVIDEA has been mutated.

## Progress Log
### 2026-09-06 — Initialization + Nebius/Nemotron foundation
- Added `NebiusTokenFactoryClient`, `IAgentInferenceClient`, conservative model routing, bearer auth, structured tools/tool-call parsing, cancellation/timeouts, bounded retries and endpoint validation.
- Added contract tests for request shape, routing, auth, tool calls, retries, failures and endpoint safety.

### 2026-09-06 — Layered personal-memory foundation
- Added Working/Episodic/Semantic/Project/Skill layers with provenance, confidence, importance, sensitivity, retention, expiry and optional embeddings.
- Added replaceable embedding/store/write-policy boundaries, secret detection, sensitive-write approval, session-only memory, atomic JSON persistence, expiry cleanup, deletion and hybrid semantic/lexical/recency/importance retrieval.
- Added tests for privacy policy, retention, expiry, sensitive retrieval, semantic ranking, lexical fallback, duplicate prevention and persistence.

### 2026-09-06 — Tavily research + provenance foundation
- Added typed `IResearchProvider`, strict Tavily endpoint/auth handling, bounded multi-query search, retries/timeouts/cancellation, date/domain/topic support, usage reporting, URL deduplication, provenance/source IDs and an explicit untrusted-web-content boundary.
- Added `ResearchEngine` with structured Nemotron planning, Tavily evidence collection, Nemotron synthesis, required `[src:SOURCE_ID]` markers and validation of cited IDs.
- Added mocked contract tests for request shape, retries, prompt-injection boundaries, planning/synthesis and citation validation.
- Remaining research gaps: Tavily Extract, richer authority/freshness/corroboration scoring, sentence-level entailment checking and live-key validation.

### 2026-09-06 — Safe browser-agent foundation
- Added typed browser observations/actions/locators, accessibility-first contracts, untrusted-page evidence, browser hard-safety policy, deny-by-default approval fallback, bounded observe -> classify -> approve -> act -> observe -> verify execution, cancellation/recovery, conservative verification, and immutable action receipts.
- Browser policy blocks unsafe schemes and autonomous credential/OTP/payment/private-key entry, and requires approval for uploads and consequential send/submit/publish/delete/purchase/account-like actions.
- Added mocked tests for credential blocking, high-impact approvals, unsafe navigation, denied approval, verified state changes, stop-on-unverified behavior, action budgets and untrusted page labeling.

### 2026-09-06 — Capability, permission and audit security layer
- Added typed capability descriptors/data permissions/risk levels, monotonic least-privilege policy, exact action approval scopes, bounded single-use approval grants and append-only JSONL audit events.
- Integrated capability enforcement into browser execution without allowing system policy to weaken the browser hard-safety floor.
- Added tests for permission escalation, risk downgrade resistance, exact-scope approval, untrusted-source injection, append-only audit, approval expiry/single-use and browser-policy composition.

### 2026-09-06 — Durable resumable jobs foundation
Completed:
- Added `Jobs/JobContracts.cs` with explicit `Pending`, `Running`, `WaitingForApproval`, `RetryScheduled`, `Completed`, `Failed`, and `Cancelled` states.
- Added typed durable checkpoints, attempt counters, retry timestamps, exact approval scopes, execution-location metadata and job definitions carrying capability permissions/risk/privacy characteristics.
- Added `ConservativeJobExecutionPolicy`:
  - any job containing private OS data is forced to `Local` execution;
  - only non-private work that actually benefits from background execution is eligible for `NebiusServerless`;
  - ordinary non-private foreground work remains local rather than being pushed to cloud gratuitously.
- Added atomic `JsonAgentJobStore` with one-record-per-job upsert semantics and temp-file replacement to avoid partial writes.
- Added `ResumableJobOrchestrator`:
  - creates and persists jobs before execution;
  - runs one bounded step at a time so work can checkpoint/resume rather than disappear on process interruption;
  - persists checkpoint payloads after each successful partial step;
  - pauses on consequential actions with an exact approval scope and refuses mismatched approval;
  - supports user cancellation and cancellation-token propagation;
  - schedules exponential retry with a bounded delay and terminal failure after `MaxAttempts`;
  - emits job lifecycle events through the existing append-only audit abstraction;
  - preserves explicit local-vs-Nebius execution choice on the durable record.
- Added tests for local routing of private data, Nebius routing of suitable background work, checkpointed multi-step completion, exact-scope approval resumption, retry exhaustion, and JSON checkpoint round-tripping.

Current external-platform evidence:
- Nebius Serverless AI jobs are currently documented as non-interactive containerized workloads that terminate/release resources on completion and are intended for AI/data-processing/batch workloads. This supports using them only for genuinely background/containerizable work rather than private desktop actions.
- Serverless AI jobs/endpoints currently inherit Compute quotas/pricing, so execution policy should remain cost-aware instead of routing all work to cloud.

Validation / evidence:
- Repository target was re-verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- Source review checked that job state is persisted before/after execution, approval scopes are exact string matches, retries are bounded, and private OS data cannot select the Nebius execution location through the default policy.
- This environment still does not provide the .NET SDK/compiler used by the solution, so the new code/tests are source-reviewed but NOT compiled or executed here. Do not report them as green until a .NET-capable runner validates them.

Unverified / risks:
- `JobExecutionLocation.NebiusServerless` is currently an execution decision/contract, not a fake cloud implementation; a real Nebius Serverless adapter still needs to submit/query/cancel container jobs using current Nebius APIs/CLI/SDK and map remote state back to durable checkpoints.
- The orchestrator emits lifecycle audit events but the actual job handler/tool invocation path still needs to evaluate `CapabilityPermissionPolicy` immediately before each tool operation, so a resumed job cannot rely solely on permission decisions made before suspension.
- Approval resume currently verifies exact scope equality; desktop UX still needs to obtain the scope from a visible action preview and use `ScopedApprovalAuthorizer` for a single-use grant at actual tool-execution time.
- Full solution compilation remains a major verification risk until a .NET-capable environment runs all tests.
- Audit storage is append-only at API level but not yet tamper-evident/encrypted, and memory persistence is still not OS-encrypted at rest.
- No concrete Playwright/CDP/browser-extension driver exists yet.

Next highest-value task:
- Add the **real job execution boundary**: `ICapabilityToolExecutor`/handler wrapper that re-evaluates declared permissions and risk immediately before every tool call, consumes exact single-use approvals, writes audit events, and never trusts a resumed checkpoint as authorization. Then implement a concrete but credential-free **Nebius Serverless job adapter contract/client** from current official API/CLI semantics (with mocked contract tests, no fake successful cloud calls). After that, prioritize a Playwright .NET driver and Windows shell integration.
