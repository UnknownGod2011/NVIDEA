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
- Safe browser-agent policy/execution foundation under `src/Nvidea.Core/Browser` plus a concrete Playwright .NET driver.
- System-wide capability/permission/audit foundation under `src/Nvidea.Core/Capabilities`.
- `CapabilityToolExecutor` re-checks permission/risk immediately before real tool execution and consumes exact single-use approval grants.
- Durable resumable jobs foundation under `src/Nvidea.Core/Jobs`.
- Credential-injected Nebius Serverless REST client contract for create/list/cancel using current official endpoints; no live success is fabricated.
- Contract tests under `tests/Nvidea.Core.Tests` cover inference, memory, research, browser policy/execution, capability policy, scoped approval, last-mile tool execution, audit behavior, resumable jobs and Serverless REST request semantics.
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
Completed:
- Re-verified repository target before every mutation: only `UnknownGod2011/NVIDEA` was written.
- Verified current Playwright .NET guidance before implementation. Playwright continues to recommend user-facing locators such as `GetByRole`, `GetByLabel`, `GetByText`, and explicit test IDs over brittle CSS/XPath chains. Current NuGet package checked during this run: `Microsoft.Playwright` 1.62.0 (published 2026-08-11).
- Added `Microsoft.Playwright` 1.62.0 to `Nvidea.Core`.
- Added `Browser/PlaywrightBrowserDriver.cs`, a real `IBrowserDriver` implementation backed by an injected Playwright `IPage`.
- Observation path:
  - bounded interactive-element enumeration (`MaxObservedElements`, default 250);
  - bounded visible page text (`MaxObservationCharacters`, default 12k);
  - role/name/label/placeholder-oriented metadata for agent observations;
  - per-observation stable page references using a dedicated `data-nvidea-ref` attribute;
  - password input values are never returned in observations;
  - visible page text is explicitly flagged when common prompt-injection patterns are detected;
  - observations reject non-HTTP(S) pages and optionally enforce an exact host allowlist.
- Action path:
  - supports bounded Navigate/Back/Refresh/Click/Type/Select/Upload/Download operations behind the existing browser action vocabulary;
  - role-and-name, label, text, test-id and accessibility-reference locators are supported;
  - CSS remains an explicit fallback only;
  - XPath is deliberately disabled in the concrete driver to avoid brittle/opaque selectors;
  - per-action timeouts and .NET cancellation are enforced;
  - navigation is restricted to absolute HTTP(S) URLs and optional host allowlists.
- Current Playwright ARIA enum spelling was checked against official docs and the initial `ComboBox` spelling was corrected to `AriaRole.Combobox` before closing this run.

Security/reliability notes:
- This driver does **not** replace `BrowserSafetyPolicy`, `BrowserCapabilityGuard`, or `CapabilityToolExecutor`; callers must keep the existing safety -> capability -> exact approval chain in front of driver writes. The driver intentionally cannot grant itself authorization.
- DOM/page content remains untrusted evidence. Assigning local `data-nvidea-ref` attributes only creates bounded local action references; page text cannot create capability approvals.
- Password values are redacted from observations, but the existing browser safety policy remains responsible for blocking autonomous credential/OTP/payment/private-key entry altogether.
- Host allowlists are exact-host in this first concrete driver. Explicit subdomain policy can be added later, but should not be silently inferred.
- Downloads currently model the triggering click; durable download artifact tracking/verification still needs a dedicated download coordinator before claiming end-to-end verified downloads.

Validation / evidence:
- Current Playwright documentation and NuGet package metadata were checked live during this run rather than relying on stale API assumptions.
- Source review caught and fixed an invalid Playwright enum spelling before the final commit.
- This execution environment still has no `dotnet`, `csc`, or `msbuild`, so the Playwright code and existing tests could not be compiled/executed here. They MUST NOT be reported as green until a .NET-capable runner executes them.
- Playwright browser binaries were not installed or launched in this runtime; no live browser success is fabricated.

Unverified / risks:
- Full solution compilation remains the largest immediate verification risk, especially because this run adds the first external runtime package.
- The concrete driver is not yet wired through a real browser capability backend/end-to-end application composition root; a future handler must ensure every browser write passes `CapabilityToolExecutor` before `IBrowserDriver.ExecuteAsync`.
- No end-to-end Playwright test currently launches Chromium against a deterministic local fixture.
- Accessibility observations are DOM/ARIA-derived rather than using Playwright's newer AI-mode ARIA snapshot references. This was chosen to keep action references explicit and bounded; evaluate the native snapshot API later if it improves fidelity without weakening determinism.
- Popup/new-tab ownership, file-download receipts, authenticated session lifecycle and browser-context teardown still need concrete handling.
- Job approvals are still resumed by persisted scope string; a fresh ephemeral `ApprovalGrant` execution context still needs to be passed to the immediate resumed tool call without ever being serialized into checkpoints.
- Audit storage is append-only at API level but not tamper-evident/encrypted; memory persistence is not yet OS-encrypted at rest.

Next highest-value task:
- First add an **ephemeral resumed-job approval execution context** so a user approval creates a fresh single-use `ApprovalGrant` that exists only in memory and is supplied only to the immediate capability-secured tool call; never serialize the grant/token into `AgentJobRecord` or checkpoint payloads. Then wire a browser capability backend through `CapabilityToolExecutor` into `PlaywrightBrowserDriver`, add deterministic local-page Playwright integration tests when a .NET/browser-capable runner is available, and begin the Windows shell/composition-root integration.
