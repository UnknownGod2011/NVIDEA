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
- System-wide capability/permission/audit foundation under `src/Nvidea.Core/Capabilities`.
- `CapabilityToolExecutor` re-checks permission/risk immediately before real tool execution and consumes exact single-use approval grants.
- Durable resumable jobs foundation under `src/Nvidea.Core/Jobs` now includes an in-memory resumed-job approval handoff; `ApprovalGrant` values are never serialized into `AgentJobRecord` or checkpoints.
- Credential-injected Nebius Serverless REST client contract for create/list/cancel using current official endpoints; no live success is fabricated.
- Contract tests under `tests/Nvidea.Core.Tests` cover inference, memory, research, browser policy/execution, capability policy, scoped approval, last-mile tool execution, audit behavior, resumable jobs, ephemeral approval behavior and Serverless REST request semantics.
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
Completed:
- Re-verified before every write that the target repository was exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.
- Added `Jobs/EphemeralJobApprovalContext.cs`:
  - `EphemeralJobApprovalStore` keeps grants only in process memory and removes them on `Take`;
  - `JobExecutionContext` exposes only exact-scope `TakeApproval`, at most once per resumed execution step;
  - cancellation/failure/revocation paths discard any leftover approval.
- Extended `IAgentJobHandler` with a context-aware execution overload using a default interface implementation, preserving existing handler source compatibility while allowing consequential handlers to receive the ephemeral context.
- Updated `ResumableJobOrchestrator`:
  - exact scope is still checked against the paused persisted action;
  - successful explicit resume creates a two-minute single-use grant using the shared `ScopedApprovalAuthorizer`;
  - the persisted record is changed back to `Pending` with `ApprovalScope = null` before execution;
  - the grant is stored only in `EphemeralJobApprovalStore` and transferred only to the next actual execution step;
  - retry/cancel/failure paths revoke remaining grants;
  - approval audit events preserve the exact approved scope without persisting the grant/token.
- Tightened `ScopedApprovalAuthorizer`: exact-scope minting is internal-only, while normal external grant creation still requires an allowed `PermissionDecision` that requires approval. This reduces the API surface for forged grants.
- Added `EphemeralJobApprovalTests.cs` covering:
  - the next resumed step receives the exact grant;
  - the grant remains single-use under `ScopedApprovalAuthorizer`;
  - JSON job persistence contains no `GrantId`, `GrantedAt`, or `ExpiresAt` material and clears persisted `ApprovalScope` after resume;
  - wrong-scope resume attempts throw and mint no ephemeral grant.

Security / correctness reasoning:
- A checkpoint/scope string remains descriptive state, never authorization.
- A process restart intentionally destroys all resumed grants; this can inconvenience the user but is fail-closed and forces fresh approval rather than replaying stale consent.
- The grant is removed from the ephemeral store when the resumed step begins. If the handler never uses it, it is lost rather than carried to a later step.
- Exact-scope grant consumption by the capability executor remains single-use. A failed consequential call does not restore consent, preventing accidental duplicate sends/submissions on retry.
- No grant identifiers/timestamps/tokens are written to job persistence or checkpoint payloads.

Validation / evidence:
- Source-level review checked existing `ScopedApprovalAuthorizer`, `CapabilityToolExecutor`, job contracts/orchestrator, and current job tests before implementation.
- New tests were added, but this runtime still does not provide `dotnet`, `csc`, or `msbuild`; they MUST NOT be claimed as passing until a .NET-capable runner executes them.
- No CI workflow was added or triggered, avoiding unnecessary GitHub Actions usage.

Unverified / risks:
- Full solution compilation remains the immediate verification risk, including C# default-interface-method compatibility across the existing test project.
- The new context is not yet used by a concrete browser job handler that routes a resumed browser action through `CapabilityToolExecutor` and then `PlaywrightBrowserDriver`.
- No deterministic Chromium integration test exists yet.
- Popup/new-tab ownership, durable download receipts and authenticated browser-context lifecycle remain incomplete.
- Audit storage is append-only at API level but not tamper-evident/encrypted; persistent memory is not yet OS-encrypted at rest.

Next highest-value task:
- Build the **concrete browser capability backend + browser job handler** so a planned browser action is transformed into a `CapabilityInvocation`, checked by `BrowserSafetyPolicy`/system capability policy, supplied the exact grant from `JobExecutionContext` only when required, executed through `CapabilityToolExecutor`, then applied by `PlaywrightBrowserDriver` with fresh observation verification. Add mocked end-to-end contract tests for read-only actions, approval-paused consequential actions, wrong/missing grant behavior, untrusted-page content and failed post-action verification. After that, begin the Windows composition root/shell integration and obtain a real .NET/Playwright compile-and-run signal as soon as tooling permits.
