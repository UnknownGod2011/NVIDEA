# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, authoritative serialized browser-verification state, production-observed session milestones, and a fail-closed recording gate.

## Persistent history
### 2026-09-06 to 2026-09-21 — product foundation and hardening
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling. Hardened browser transport, prompt-injection/consequential-action gates, emergency stop, protected browser receipts, durable research lineage and crash-ambiguous reconciliation.

### 2026-09-22 — evidence and composition lifetime hardening
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, bound ambiguous recovery to root lifetime before publication, qualified a least-authority lifetime-bound goal-host decorator, locked the goal-agent public lifetime refactor contract, composed the host decorator into issued goal agents as defense-in-depth, and added `BrowserGoalTransactionLifetime` for exactly-one-lease transactions.

## Latest run — exactly-one-lease goal-agent boundary
Files changed:
- `src/Nvidea.Core/Desktop/LifetimeBoundBrowserGoalAgent.cs`
- `tests/Nvidea.Core.Tests/LifetimeBoundBrowserGoalAgentTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, current tree, `BrowserGoalAgent`, composition root, and current inference contracts before mutation.
- Added `LifetimeBoundBrowserGoalAgent`, an assembly-internal transaction boundary that wraps an ordinary `BrowserGoalAgent` and routes Run, Resume, Approve-and-Continue, and Cancel through `BrowserGoalTransactionLifetime`.
- The wrapped agent remains lifetime-unaware, so its existing Resume/Approve -> Run internal delegation executes inside the already-held outer transaction lease rather than reacquiring the non-reentrant composition gate.
- The boundary is one-time bindable and fail-closed when unpublished/unbound.
- Added deterministic tests for unbound fail-closed behavior, post-disposal rejection before browser authority executes, and duplicate-binding rejection.
- Corrected the test fake to the repository's current `IAgentInferenceClient` / `AgentCompletion` contract after inspecting the actual Nebius abstraction.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- Static inspection confirms all four privileged goal transactions delegate through the already-qualified `BrowserGoalTransactionLifetime`; the wrapper itself contains no browser payload, URL, typed value, approval scope, provider credential, or secret.
- Tests are Chromium-free and provider-free. This connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed.
- No paid/live provider call, browser action, workflow rerun, issue, PR, or repository-setting mutation was triggered.

## Security / privacy / failure review
- Exactly one outer lease can cover planner work, durable session persistence, browser authority, verification and approval handling for a transaction once composition uses this boundary.
- The inner agent does not receive `CompositionLifetimeGate`, preventing accidental nested acquisition through its existing internal method delegation.
- Unbound publication fails before the inner transaction executes; post-disposal transactions fail before browser authority executes.
- The boundary is intentionally internal until composition/API shape is finalized, avoiding premature public lifetime authority or a duplicate product surface.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` still returns the raw `BrowserGoalAgent`; the new transaction boundary is therefore production code but not yet on the public composition path.
- The current composition path still uses `LifetimeBoundBrowserGoalHost`; once the outer transaction boundary is wired, retaining that same-gate host decorator would deadlock and must be removed from that path.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Wire the exactly-one-lease goal transaction boundary into the composition path without creating a second public browser API: choose the smallest compatible API refactor that makes every issued goal agent transaction pass through `LifetimeBoundBrowserGoalAgent`, bind it to the root lifetime before publication/return, and remove `LifetimeBoundBrowserGoalHost` from that path so the same non-reentrant gate is never acquired twice. Add deterministic composition tests proving in-flight full transactions delay disposal and post-disposal transactions fail before planner/store/browser work, then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
