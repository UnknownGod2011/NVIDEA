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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, bound ambiguous recovery to root lifetime before publication, qualified a least-authority lifetime-bound goal-host decorator, locked the goal-agent public lifetime refactor contract, composed the host decorator into issued goal agents as defense-in-depth, added `BrowserGoalTransactionLifetime`, added/qualified `LifetimeBoundBrowserGoalAgent` for exactly-one-lease transactions, introduced the least-authority `IBrowserGoalAgent` API seam, and moved issued goal agents to the full-transaction lifetime facade without same-gate host re-entry.

### 2026-09-23 — browser-goal composition qualification
Added a private least-authority `ICrashConsistentBrowserGoalHost` factory seam so Chromium-free root qualification is possible without changing production Playwright ownership. `CreateBrowserGoalAgentAsync` resolves that host only under root lifetime authority, preserves evidence observation/Nemotron planning/durable sessions, and publishes only the lifetime-bound `IBrowserGoalAgent` facade. Added reflection guards for API privacy and seam shape.

## Latest run — seam ownership regression hardening
Files changed:
- `tests/Nvidea.Core.Tests/BrowserGoalCompositionOwnershipTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, current composition-root implementation, recent commits, the existing seam tests, and `InternalsVisibleTo` configuration before mutation.
- Added deterministic regression coverage requiring the browser-goal host override to remain constructor-only, readonly, optional, and the final private-constructor parameter.
- Added a production-factory guard proving `CreateFromEnvironmentAsync` cannot accept or expose `ICrashConsistentBrowserGoalHost` or its factory, preventing test authority from becoming an application/plugin injection surface.
- Added a mutation-surface guard preventing any writable composition-root property from exposing the host or host-factory authority after construction.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms `NvideaCompositionRoot` has one private constructor and `_browserGoalHostFactory` is a readonly field populated only there.
- Production `CreateFromEnvironmentAsync` still constructs the root without supplying the optional seam, preserving lazy Playwright-backed ownership.
- Tests are provider/Chromium/network-free and use reflection only to lock architecture; no live provider/browser operation is required.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows test suite.
- No paid/live provider call, browser action, workflow rerun, issue, PR, or repository-setting mutation was triggered.

## Security / privacy / failure review
- The test seam remains least-authority and immutable: no raw Playwright objects, provider credentials, root lifetime gate, browser product runtime, URL, typed value, approval material, secret, or personal state is exposed through it.
- Production callers cannot select a substitute browser-goal host through the public factory or mutate one after construction.
- Existing approval, prompt-injection, crash-consistency, durable verification, evidence-observation and exactly-one-lease transaction semantics are unchanged.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Chromium-free root qualification is architecturally possible, but a deterministic assembly-internal root construction path still needs to supply fake inference/memory/session dependencies without reflection-heavy construction or live provider configuration.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Add an assembly-internal deterministic `NvideaCompositionRoot` test construction path that accepts fake inference plus the least-authority browser-goal host factory while preserving production ownership semantics. Then add composition-level concurrency tests proving an issued Run/Resume/Approve/Cancel transaction delays actual root disposal and a post-disposal call fails before planner/store/browser authority executes. Run the full .NET/Windows/Chromium suite in the first capable environment and fix any compile/interface findings without weakening the exactly-one-lease boundary.
