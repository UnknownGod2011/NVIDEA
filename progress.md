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
- **Skills / permissions:** capability registry, declared data/tool permissions, monotonic risk, exact-scope approvals, last-mile tool enforcement and append-only audit events.
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
- Last-mile `CapabilityToolExecutor` re-checks permission/risk immediately before real tool execution and consumes exact single-use approval grants.
- Durable resumable jobs foundation under `src/Nvidea.Core/Jobs`.
- Credential-injected Nebius Serverless REST client contract for create/list/cancel using current official endpoints; no live success is fabricated.
- Contract tests under `tests/Nvidea.Core.Tests` cover inference, memory, research, browser policy/execution, capability policy, scoped approval, last-mile tool execution, audit behavior, resumable jobs and Serverless REST request semantics.
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

### 2026-09-06 — Safe browser-agent foundation
- Added typed browser observations/actions/locators, accessibility-first contracts, untrusted-page evidence, browser hard-safety policy, deny-by-default approval fallback, bounded observe -> classify -> approve -> act -> observe -> verify execution, cancellation/recovery, conservative verification, and immutable action receipts.
- Browser policy blocks unsafe schemes and autonomous credential/OTP/payment/private-key entry, and requires approval for uploads and consequential send/submit/publish/delete/purchase/account-like actions.
- Added mocked tests for credential blocking, high-impact approvals, unsafe navigation, denied approval, verified state changes, stop-on-unverified behavior, action budgets and untrusted page labeling.

### 2026-09-06 — Capability, permission and audit security layer
- Added typed capability descriptors/data permissions/risk levels, monotonic least-privilege policy, exact action approval scopes, bounded single-use approval grants and append-only JSONL audit events.
- Integrated capability enforcement into browser execution without allowing system policy to weaken the browser hard-safety floor.
- Added tests for permission escalation, risk downgrade resistance, exact-scope approval, untrusted-source injection, append-only audit, approval expiry/single-use and browser-policy composition.

### 2026-09-06 — Durable resumable jobs foundation
- Added explicit Pending/Running/WaitingForApproval/RetryScheduled/Completed/Failed/Cancelled states, durable checkpoints, bounded retries/backoff, exact approval scopes and local-vs-Nebius execution metadata.
- `ConservativeJobExecutionPolicy` forces private OS data local and only routes non-private background/containerizable work to Nebius Serverless.
- Added atomic JSON job persistence, cancellation, checkpoint/resume and lifecycle audit events.
- Added tests for private/local routing, Nebius routing, checkpoint completion, exact approval resume, retry exhaustion and persistence.

### 2026-09-06 — Last-mile tool authorization + Nebius Serverless REST adapter
Completed:
- Added `Capabilities/CapabilityToolExecutor.cs`:
  - evaluates `ICapabilityPermissionPolicy` immediately before every concrete tool call;
  - persisted checkpoint/job state is never accepted as authorization;
  - denied invocations never reach the backend;
  - consequential/high-risk actions require an exact live `ApprovalGrant`;
  - grants are single-use and consumed at execution attempt time;
  - failed/ambiguous consequential calls deliberately do not restore approval, so retries require fresh consent rather than risking duplicate side effects;
  - execution start/success/failure/cancellation and denial/approval-wait states are written to the shared audit trail.
- Added `CapabilityToolExecutorTests` for no-approval blocking, grant replay prevention, checkpoint-not-authorization and undeclared-permission denial/audit.
- Verified current official Nebius Serverless semantics before implementation:
  - create: `POST https://api.nebius.cloud/ai/v1/jobs`;
  - list: `GET https://api.nebius.cloud/ai/v1/jobs?parentId=<project_ID>`;
  - cancel: `POST https://api.nebius.cloud/ai/v1/jobs/cancel` with `{ "id": "<job_ID>" }`;
  - CLI equivalents remain `nebius ai job create/list/get/cancel`.
- Added `Jobs/NebiusServerlessJobClient.cs`:
  - bearer-auth REST client with strict HTTPS/host validation;
  - typed create/list/cancel contract;
  - project-scoped job creation and listing;
  - cancellation and per-request timeout;
  - bounded retry only for timeouts/network failures/429/5xx, not permanent 4xx client errors;
  - successful non-empty responses must parse as JSON;
  - likely secret-bearing environment variable names are rejected before network transmission, directing future secret use toward Nebius SecretStash/env-secret instead of plaintext job environment variables;
  - response remains raw JSON intentionally so we do not invent unstable/undocumented operation/status schemas.
- Added mocked Serverless contract tests for create endpoint/body/auth, project-scoped list, exact cancel body, plaintext-secret rejection, transient-vs-permanent retry behavior and endpoint pinning.
- Source review found and fixed an initial bug that would have retried permanent 4xx `HttpRequestException`s.

External-platform evidence:
- Current Nebius docs describe Serverless Jobs as finite non-interactive container workloads suitable for batch/data-processing/AI work and show the REST create/list/cancel operations above.
- Current docs also expose `nebius ai job get <id>` / lower-level job get, but this client intentionally avoids guessing a REST get/status response shape until we verify and model the response contract needed for durable remote-state mapping.

Validation / evidence:
- Repository target was re-verified as exactly `UnknownGod2011/NVIDEA` before every mutation.
- Attempted local verification after the changes: this runtime still has no `dotnet` executable and cannot resolve `github.com` from the container, so a fresh local clone/build/test run could not be executed.
- Therefore all newly added tests remain source-reviewed/mocked contract tests and MUST NOT be reported as green until a .NET-capable runner executes them.

Unverified / risks:
- `CapabilityToolExecutor` is now the correct last-mile boundary, but every future concrete job/skill handler must be wired through it; merely having the type in the repository does not magically prevent a badly written handler from bypassing it.
- Job approval UX still needs to create a visible exact-scope `ApprovalGrant` and supply it only to the immediate consequential tool call; approvals should remain ephemeral rather than durable checkpoint data.
- Nebius Serverless create/list/cancel requests are contract-tested against documented semantics, but no live credentials were used and no remote job was launched.
- Remote job state/result/checkpoint mapping is still missing; implement only after verifying the exact current status representation instead of guessing it.
- Full solution compilation remains the largest verification risk.
- Audit storage is append-only at API level but not tamper-evident/encrypted; memory persistence is not yet OS-encrypted at rest.
- No concrete Playwright .NET/CDP/browser-extension driver exists yet.

Next highest-value task:
- Implement a concrete **Playwright .NET browser driver** behind the existing provider-neutral browser contracts, using accessibility/user-facing locators, strict browser-context boundaries and no credential bypass. Route every write action through `CapabilityToolExecutor` and add mocked/fake-page tests where possible. In parallel, add an ephemeral job approval execution context so resumable handlers can receive a fresh single-use grant without ever persisting authorization into checkpoints. After that, start Windows shell integration.
