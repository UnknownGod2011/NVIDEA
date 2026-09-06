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
- **Browser:** accessibility/DOM observation -> typed action -> browser hard-safety floor -> system capability policy -> exact approval -> Playwright driver execution -> fresh observation -> verification -> receipt.
- **Skills / permissions:** capability registry, declared data/tool permissions, monotonic risk, exact-scope approvals, last-mile tool enforcement and append-only audit events.
- **Jobs:** durable state/checkpoints, cancellation, retry/backoff, approval-paused states, ephemeral approval grants, and explicit local-vs-Nebius execution selection.
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
- Safe browser-agent policy/execution foundation under `src/Nvidea.Core/Browser` plus a concrete Playwright .NET driver.
- End-to-end browser capability execution now routes fresh observation -> browser safety -> capability policy -> exact single-use approval -> `CapabilityToolExecutor` -> browser driver -> fresh observation -> verification.
- `BrowserActionJobHandler` now connects durable jobs to that browser execution path while keeping approval grants ephemeral and action IDs stable across resume.
- System-wide capability/permission/audit foundation under `src/Nvidea.Core/Capabilities`.
- `CapabilityToolExecutor` re-checks permission/risk immediately before real tool execution and consumes exact single-use approval grants.
- Durable resumable jobs foundation under `src/Nvidea.Core/Jobs` includes an in-memory resumed-job approval handoff; `ApprovalGrant` values are never serialized into `AgentJobRecord` or checkpoints.
- Credential-injected Nebius Serverless REST client contract for create/list/cancel using current official endpoints; no live success is fabricated.
- Contract tests under `tests/Nvidea.Core.Tests` cover inference, memory, research, browser policy/execution, capability policy, scoped approval, last-mile tool execution, audit behavior, resumable jobs, ephemeral approval behavior, end-to-end browser jobs and Serverless REST request semantics.
- Root README + MIT LICENSE.
- keyboard.wtf has only been inspected read-only; no repository other than NVIDEA has been mutated.

## Progress Log
### 2026-09-06 — Core foundations
- Initialized the NVIDEA-only repository contract and persistent engineering ledger.
- Added `NebiusTokenFactoryClient`, `IAgentInferenceClient`, conservative model routing, bearer auth, structured tools/tool-call parsing, cancellation/timeouts, bounded retries and endpoint validation.
- Added layered Working/Episodic/Semantic/Project/Skill memory with provenance, confidence, importance, sensitivity, retention, expiry, optional embeddings, secret detection, explicit sensitive-write approval, session-only memory and hybrid semantic/lexical/recency/importance retrieval.
- Added Tavily-backed research contracts and `ResearchEngine`: bounded planning, multi-query search, retries, URL deduplication, source IDs, untrusted-web-content isolation, Nemotron synthesis and citation-ID validation.
- Added provider-neutral browser observations/actions/locators, browser hard-safety policy, deny-by-default approval, bounded observe -> classify -> approve -> act -> observe -> verify execution and immutable action receipts.
- Added system capability descriptors/data permissions/risk levels, monotonic least privilege, exact-scope approvals, single-use grants and append-only audit events.
- Added durable resumable job states/checkpoints/retries/cancellation/approval pauses and conservative local-vs-Nebius execution selection.
- Added last-mile `CapabilityToolExecutor`: every concrete tool call is re-authorized immediately before execution; checkpoint data is never authorization; ambiguous failed side effects consume approval and require fresh consent before retry.
- Added Nebius Serverless REST client for documented create/list/cancel operations, strict HTTPS/host validation, bounded transient retries, project scoping and plaintext-secret environment rejection.

### 2026-09-07 — Concrete Playwright browser driver
- Added `Microsoft.Playwright` 1.62.0 and `Browser/PlaywrightBrowserDriver.cs` backed by an injected `IPage`.
- Added bounded DOM/ARIA observations, password-value redaction, untrusted-content flags, exact-host allowlists and stable local page references.
- Added bounded Navigate/Back/Refresh/Click/Type/Select/Upload/Download-trigger operations, user-facing locator priority, CSS fallback and intentionally disabled XPath.
- Kept browser authorization outside the driver: safety -> capability policy -> exact approval -> driver remains mandatory.
- Source review fixed the Playwright `AriaRole.Combobox` spelling before commit.
- Still unverified by compilation/browser launch because this runtime has no .NET SDK or Playwright browser binaries.

### 2026-09-07 — Ephemeral resumed-job approval execution context
- Added `EphemeralJobApprovalStore` and `JobExecutionContext`; grants exist only in process memory, can be taken only for the exact scope, and are removed before the immediate consequential step.
- Extended `IAgentJobHandler` with context-aware execution while keeping legacy handlers source-compatible.
- Updated `ResumableJobOrchestrator` so explicit resume mints a two-minute exact single-use grant, clears persisted approval scope before execution, and revokes leftovers on failure/cancel.
- Tightened `ScopedApprovalAuthorizer` so arbitrary capability consumers cannot mint exact-scope grants directly.
- Added regression tests proving no grant identifiers/timestamps are serialized and wrong-scope resumes cannot mint or consume approval.

### 2026-09-07 — End-to-end capability-secured browser jobs
Completed:
- Re-verified before every GitHub mutation that the target was exactly `UnknownGod2011/NVIDEA`; no other repository was written.
- Added `Browser/BrowserCapabilityExecution.cs`:
  - `BrowserCapabilityBackend` is the typed `ICapabilityToolBackend` bridge to `IBrowserDriver`;
  - `BrowserCapabilityExecutionService` performs a fresh observation, applies `BrowserSafetyPolicy`, evaluates system capability policy, calls `CapabilityToolExecutor`, performs the concrete browser action only after last-mile authorization, observes again, and verifies the post-action state;
  - blocked browser actions never reach the tool backend;
  - capability policy can raise risk/approval requirements but cannot weaken the browser safety floor;
  - untrusted-page provenance is propagated into capability/audit evaluation;
  - an optional stable action id lets approval scope survive a durable pause/resume without making the id itself authorization.
- During implementation, found and fixed a critical approval-scope mismatch: generating a new action id after resume would have invalidated the exact grant. Browser jobs now deliberately use the stable job id as their capability action id across pause/resume.
- Added `Jobs/BrowserActionJobHandler.cs`:
  - browser actions are persisted only as descriptive checkpoint payloads;
  - the handler first executes through the capability boundary without approval to derive the exact scope;
  - when paused, only the exact scope is persisted;
  - after explicit resume, `JobExecutionContext.TakeApproval(scope)` retrieves the single ephemeral grant and supplies it only to the immediate capability call;
  - if policy scope changes, the handler fails closed and asks for fresh approval rather than translating/reusing consent;
  - executed-but-unverified actions become job failures/retries instead of being reported as successful.
- Added `BrowserActionJobHandlerTests.cs` covering:
  - read-only browser jobs completing without approval;
  - consequential actions pausing before driver execution, resuming with exact ephemeral approval, and avoiding replay;
  - wrong-scope approval rejection;
  - credential/password typing blocked before the driver;
  - failed post-action verification causing a retry state;
  - prompt-injection-like page content being marked as untrusted and unable to bypass approval.
- Source review also tightened nullable handling in the new tests because the test project treats warnings as errors.

Security / correctness reasoning:
- Durable checkpoint data and stable action IDs are identifiers/descriptive state only; neither can authorize execution.
- `CapabilityToolExecutor` still re-evaluates the invocation at the final execution boundary.
- The grant is exact to capability + stable action id + permission set and remains single-use.
- A webpage can influence observations but cannot mint permissions, create grants, lower risk, or bypass approval.
- Credential-style autonomous typing remains blocked before concrete driver execution.
- A side effect that executes but cannot be verified is not marked complete; consequential approval is not restored for an ambiguous retry.

Validation / evidence:
- Source-level review covered `BrowserSafetyPolicy`, browser contracts/executor, capability registry/policy, scoped approvals, job contracts/orchestrator, Playwright driver and warning-as-error test settings before/after implementation.
- Runtime tool check again found no `dotnet`, `csc`, or `msbuild`; therefore the new code/tests MUST NOT be claimed as compiled or passing.
- No GitHub Actions workflow was added or triggered, avoiding unnecessary Actions usage.

Unverified / risks:
- Full .NET compilation is still the highest immediate technical risk.
- The new browser job tests use a deterministic fake `IBrowserDriver`; a real Chromium/Playwright integration test still does not exist.
- Browser action checkpoint payloads are currently plain JSON; general job-store-at-rest encryption is not yet implemented.
- Popup/new-tab ownership, authenticated browser-context lifecycle, downloads as durable artifacts, and navigation race handling remain incomplete.
- `BrowserCapabilityBackend` must only be exposed through the trusted composition root; direct construction by untrusted plugin code would bypass the intended outer policy architecture.
- Audit storage remains append-only at the API level but not tamper-evident/encrypted.

Next highest-value task:
- Build the **Windows composition root + minimal desktop shell integration** so the existing hotkey/context/orb patterns can invoke the real Nemotron/Nebius agent core, memory, research and browser-job stack through one trusted dependency graph. In parallel, add a deterministic Playwright Chromium integration harness that can run locally without secrets and obtain the first real `dotnet test` + browser execution signal as soon as a .NET-capable environment is available. Then harden authenticated browser session ownership, popup/download handling and encrypted local persistence.
