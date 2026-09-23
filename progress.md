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
Added a private least-authority `ICrashConsistentBrowserGoalHost` factory seam so Chromium-free root qualification is possible without changing production Playwright ownership. `CreateBrowserGoalAgentAsync` resolves that host only under root lifetime authority, preserves evidence observation/Nemotron planning/durable sessions, and publishes only the lifetime-bound `IBrowserGoalAgent` facade. Added reflection guards for API privacy and seam ownership/immutability, deterministic post-disposal qualification for all four public browser-goal transactions, and actual-root concurrency qualification proving an issued Run transaction holds `NvideaCompositionRoot.DisposeAsync` behind the full transaction lease. Replaced the temporary reflective test fixture with an assembly-internal deterministic root factory that accepts only fake inference, the least-authority browser-goal host factory, and an isolated state directory. Locked that factory with API-surface regression tests requiring it to remain non-public/static, return the real root, accept exactly the least-authority inference/host/state/cancellation inputs, and remain absent from the public composition API.

## Latest run — deterministic factory authority regression
Files changed:
- `tests/Nvidea.Core.Tests/NvideaCompositionRootDeterministicApiTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, current root-level disposal-race tests and the deterministic composition implementation before mutation.
- Added API-surface regression coverage for `CreateDeterministicAsync`.
- The tests require the seam to remain assembly-internal (`IsAssembly`), static, non-public and to return `Task<NvideaCompositionRoot>`.
- Parameter-shape qualification requires exactly `IAgentInferenceClient`, `Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>`, isolated state-directory string, and optional `CancellationToken` in that order.
- Public API qualification rejects accidental publication of `CreateDeterministicAsync` and rejects public browser-goal host/factory injection parameters anywhere on `NvideaCompositionRoot`.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the production implementation currently matches the locked contract: `CreateDeterministicAsync` is `internal static`, production `CreateFromEnvironmentAsync` remains separate, and deterministic construction reuses the real root/lifetime/store implementation.
- The new suite is provider-, network- and Chromium-free and triggers no workflow or paid service.
- No Playwright startup, provider key, live Nebius/Tavily request, workflow rerun, issue, PR or repository-setting mutation was triggered.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows test suite.

## Security / privacy / failure review
- The deterministic seam is now guarded against accidental promotion into a second production construction API.
- Regression coverage prevents future addition of public `ICrashConsistentBrowserGoalHost` authority injection through the root surface without an explicit test failure/review.
- No credentials, browser payloads, approval scopes, personal data or secrets are accepted or persisted by the deterministic factory.
- Production ownership, exact approval gates, prompt-injection boundaries, crash consistency, durable verification and exactly-one-lease transaction semantics are unchanged.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Actual-root in-flight concurrency is dynamically specified for Run; equivalent Resume/Approve/Cancel in-flight races remain to be qualified at root level. Post-disposal semantics for all four transactions are already qualified directly at the issued boundary.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- Reflection is used only by the API-shape regression test to inspect visibility/signatures; deterministic runtime qualification no longer uses constructor reflection.

## Single Best Next Task
Extend actual-root in-flight disposal qualification across Resume, Approve-and-Continue and Cancel using the deterministic root factory, including authority counters that prove disposal waits for the complete transaction and no planner/browser authority escapes after shutdown. Run the full .NET/Windows/Chromium suite in the first capable environment and fix any compile/interface findings without weakening the exactly-one-lease boundary.
