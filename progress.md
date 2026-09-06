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
Every run must read this file first, inspect current NVIDEA state, choose the highest-value unfinished engineering task, verify current platform/API assumptions where stale behavior matters, implement real code/tests/docs only in NVIDEA, validate as far as tooling permits, review safety/correctness, update this ledger, and continue improving while meaningful work remains.

## Product gaps inherited from keyboard.wtf
- **Memory:** replace bounded key/value intent memory with layered working, episodic, semantic/profile, project/entity and skill/workflow memory, retrieval, provenance, sensitivity/retention and user controls.
- **Research:** Tavily-backed multi-query research with freshness/quality, source provenance, citations, explicit uncertainty and resumable tasks.
- **Browser automation:** DOM/accessibility-first plan-act-observe-verify agency with robust locators, recovery, permissions, prompt-injection defenses and audit evidence.

## Target Architecture
- **Desktop shell:** Windows hotkeys, voice/text, orb/status, active-app/selected-text/clipboard context, permission UX, local speech where useful and emergency stop.
- **Agent core:** task planner/state machine, structured tool calls, bounded iterative execution, verification, cancellation/retries and approvals.
- **NVIDIA/Nebius:** NVIDIA open models through Nebius Token Factory / AI Cloud as genuine core runtime; no hidden Gemini/OpenAI/Claude dependency.
- **Memory:** layered typed memory with privacy-aware writes, semantic/lexical retrieval, recency/importance, provenance/confidence/sensitivity/retention and deletion controls.
- **Research:** Nemotron planning -> Tavily evidence -> untrusted-content boundary -> Nemotron synthesis -> validated source IDs.
- **Browser:** provider-neutral accessibility/DOM observations -> typed action -> risk/approval -> driver execution -> fresh observation -> verification -> immutable receipt.
- **Skills / permissions:** capability registry, declared data/tool permissions, risk class, confirmation policy and evaluation fixtures.
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
- NVIDIA/Nebius-first Token Factory inference client with verified default `nvidia/nemotron-3-super-120b-a12b`.
- Structured tools/output, cancellation/timeouts, bounded retries and endpoint validation.
- Layered privacy-aware personal memory under `src/Nvidea.Core/Memory`.
- Tavily research provider + Nemotron research engine under `src/Nvidea.Core/Research`.
- Provider-neutral safe browser-agent foundation under `src/Nvidea.Core/Browser`.
- Contract tests under `tests/Nvidea.Core.Tests` for inference, memory, research and browser policy/execution.
- Root README + MIT LICENSE.
- keyboard.wtf has only been inspected read-only; no repository other than NVIDEA has been mutated.

## Progress Log
### 2026-09-06 — Initialization + Nebius/Nemotron foundation
- Established immutable repository-safety boundary.
- Added `NebiusTokenFactoryClient`, `IAgentInferenceClient`, conservative model routing, bearer auth, structured tools/tool-call parsing, cancellation/timeouts, bounded retries and endpoint validation.
- Added contract tests for request shape, routing, auth, tool calls, retries, failures and endpoint safety.

### 2026-09-06 — Layered personal-memory foundation
- Added Working/Episodic/Semantic/Project/Skill memory layers with provenance, confidence, importance, sensitivity, retention, expiry and optional embeddings.
- Added replaceable embedding/store/write-policy boundaries, secret detection, explicit approval for sensitive/indefinite writes, session-only memory, atomic JSON persistence, expiry cleanup, deletion and hybrid semantic/lexical/recency/importance retrieval.
- Added tests for privacy policy, retention, expiry, sensitive retrieval, semantic ranking, lexical fallback, duplicate prevention and persistence.

### 2026-09-06 — Tavily research + provenance foundation
- Verified current official Tavily Search contract before implementation.
- Added typed `IResearchProvider`, strict Tavily endpoint/auth handling, bounded multi-query search, retries/timeouts/cancellation, date/domain/topic support, usage reporting, canonical URL deduplication, provenance/source IDs and an explicit untrusted-web-content boundary.
- Added `ResearchEngine` with structured Nemotron planning, Tavily evidence collection, Nemotron synthesis, required `[src:SOURCE_ID]` markers and validation of cited IDs.
- Added mocked contract tests for request shape, retry behavior, prompt-injection boundaries, planning/synthesis and citation validation.
- Remaining research gaps: Tavily Extract, richer authority/freshness/corroboration scoring, sentence-level entailment checking and live-key validation.

### 2026-09-06 — Safe browser-agent foundation
Completed:
- Re-verified current Playwright design guidance before implementing transport-neutral browser policy. Current Playwright documentation favors accessibility snapshots/user-facing locators and performs actionability checks before interaction, reinforcing the decision to avoid coordinate-driven core contracts.
- Added `src/Nvidea.Core/Browser/BrowserContracts.cs`:
  - typed browser actions (`Read`, `Navigate`, `Click`, `Type`, `Select`, `Upload`, `Download`, `Back`, `Refresh`);
  - locator hierarchy supporting accessibility references, role/name, labels, text, test IDs and CSS as a last-resort transport detail;
  - typed accessible elements and observations with URL/title/text/snapshot identity;
  - explicit untrusted-page evidence envelope that states webpage text cannot redefine agent policy, authorize tools, or request credentials;
  - provider-neutral `IBrowserDriver`, `IBrowserApprovalGate` and `IBrowserActionVerifier` boundaries;
  - immutable action decision/receipt records suitable for later audit persistence.
- Added `BrowserSafetyPolicy`:
  - blocks non-HTTP(S) navigation such as script/data/file schemes;
  - blocks autonomous typing into password/OTP/payment/private-key/API-key-like targets;
  - always requires explicit approval for uploads;
  - requires approval for consequential send/submit/publish/delete/purchase/financial/account/security-like controls;
  - keeps ordinary click/type/select/navigation medium risk and read/back/refresh low risk;
  - preserves the untrusted-content boundary even when page content contains prompt-injection-like text.
- Added `BrowserAgentExecutor` implementing a bounded **observe -> classify -> approve if required -> act -> observe -> verify** loop:
  - stops a plan immediately after a failed or unverified action instead of blindly continuing;
  - propagates cancellation;
  - records best-effort post-failure observations without masking the original driver error;
  - includes a deny-by-default approval gate for integrations that have not connected real user approval UX;
  - includes a conservative verifier that checks exact navigation destinations, explicit expected state, or otherwise requires observable state change rather than trusting driver success.
- Added `BrowserAgentTests.cs` covering blocked credential entry, high-impact approval gating, unsafe navigation schemes, approval denial without driver execution, successful expected-state verification, stop-on-unverified behavior, action-budget enforcement and untrusted webpage labeling.
- Source review found and fixed a C# member-name collision between the generated `Role` record property and locator factory method; the factory is now `ByRole`.

Validation / evidence:
- Current Playwright accessibility snapshot, best-practice locator and actionability documentation was checked on 2026-09-06 before implementation.
- The browser core is intentionally independent of Playwright/CDP/extensions; a concrete Windows transport can later implement `IBrowserDriver` without moving safety policy into the transport layer.
- Rechecked the execution container for `dotnet`, `csc` and `msbuild`; none are installed. Browser code/tests are therefore source-reviewed but NOT compiled/executed in this automation environment. Do not describe them as green until a .NET-capable runner verifies them.

Unverified / risks:
- Full solution compilation is still the highest immediate verification risk because this runtime lacks a .NET toolchain.
- No concrete Playwright/CDP/browser-extension driver exists yet; the current layer is the policy/state-machine contract plus mocked driver tests.
- The verifier is deliberately conservative but generic. Production flows need action-specific assertions and richer DOM state fingerprints to avoid both false positives and false negatives.
- Prompt-injection detection is currently a flag supplied by the observation producer plus an immutable trust boundary; a future driver/agent layer should add deterministic suspicious-content heuristics and model-independent policy tests.
- Immutable receipts exist, but durable encrypted audit persistence is not yet implemented.
- There is no cross-skill permission registry yet; browser risk terms currently live in browser policy and should ultimately compose with the system-wide capability policy.
- Memory persistence remains JSON and is not yet OS-encrypted at rest.

Next highest-value task:
- Build the **system-wide skill/capability + permission/risk engine and durable audit abstraction** so research/browser/desktop skills declare required tools/data permissions, risk class and confirmation policy in one place. Integrate browser actions with that engine without weakening the current hard browser safety floor. Add tests proving high-impact actions cannot be downgraded by a skill, approvals are scoped to the exact action, audit events are append-only, and untrusted tool/page content cannot grant itself permissions. After that, add a concrete Playwright .NET driver behind `IBrowserDriver` once package/toolchain validation can be performed safely.
