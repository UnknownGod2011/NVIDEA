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
- Contract tests under `tests/Nvidea.Core.Tests` for inference, memory, research, browser policy/execution, capability policy, scoped approval, and audit behavior.
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
Completed:
- Added `Capabilities/CapabilityContracts.cs` with typed capability descriptors, data permissions, risk levels, invocation envelopes, permission decisions, registry and policy interfaces.
- Added `CapabilityRegistry` and `CapabilityPermissionPolicy`:
  - denies permissions not predeclared by the registered skill/capability;
  - computes effective risk monotonically so an invocation can never downgrade the capability's registered risk;
  - always requires approval for consequential permission classes such as email send, browser write, file write, calendar write and local process control;
  - raises untrusted-source write operations to high risk/approval rather than accepting content-provided authorization;
  - builds approval scope from exact capability + action id + permission set.
- Added `ScopedApprovalAuthorizer`:
  - grants only decisions that are allowed and explicitly require approval;
  - caps grant lifetime at 15 minutes;
  - validates exact approval scope;
  - defaults to single-use grants so one approval cannot silently authorize later actions.
- Added `JsonLinesAuditTrail`:
  - append-only JSONL event model with stable event ids and capability/action identifiers;
  - rejects duplicate event ids rather than treating a second write as an update;
  - preserves ordered events for later inspection/export.
- Added `BrowserCapabilityGuard` and integrated it into `BrowserAgentExecutor` as an optional enforcement layer:
  - the existing browser safety decision executes first and remains the hard floor;
  - blocked browser actions cannot be relaxed by capability policy;
  - browser action kinds map to declared browser/file permissions;
  - undeclared browser permissions become blocked actions;
  - higher capability risk or approval requirements can only make the browser decision stricter, never weaker.
- Added tests:
  - undeclared permission escalation is denied;
  - high-risk registered skills cannot be downgraded by low-risk invocation claims;
  - approvals differ by exact action scope;
  - untrusted page/tool text cannot grant missing permissions;
  - audit event identity is append-only and order is preserved;
  - exact-scope grants are single use, expire, and cannot authorize a different action;
  - browser hard blocks survive capability composition;
  - capability risk can elevate browser approval requirements;
  - a read-only browser capability cannot execute a write action.

Validation / evidence:
- Rechecked this automation runtime for `dotnet`, `csc`, and `msbuild`; none are installed. New code/tests are source-reviewed but NOT compiled or executed here. Do not describe them as green until a .NET-capable runner validates the solution.
- Existing browser constructor call sites remain source-compatible because the new capability guard parameter is optional.
- Repository target was verified as exactly `UnknownGod2011/NVIDEA` before every mutation in this run.

Unverified / risks:
- Full solution compilation remains the highest immediate verification risk because this runtime lacks the .NET toolchain.
- `JsonLinesAuditTrail` is append-only at the application API level but is not yet cryptographically tamper-evident or OS-encrypted; local filesystem modification outside the process remains possible.
- Approval grants currently live in memory; desktop UX must bind user confirmation to the exact `ApprovalScope` and action preview.
- Browser actions are now capability-aware, but research/memory/desktop/email/calendar execution paths still need to be routed through the same policy before tool invocation.
- No concrete Playwright/CDP/browser-extension driver exists yet.
- Memory persistence remains JSON and is not yet OS-encrypted at rest.

Next highest-value task:
- Build the **resumable task/job orchestration layer** with durable checkpoints, cancellation, retries/backoff, explicit task state transitions, approval-paused states, and local-vs-Nebius execution policy. Keep OS-private actions local and define a clean Nebius serverless adapter for long-running research/agent work without faking cloud execution. Route all tool execution through the capability policy/audit boundary. After that, add a concrete Playwright .NET driver when package/toolchain verification is available.
