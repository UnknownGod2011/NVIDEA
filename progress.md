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
Added a private least-authority `ICrashConsistentBrowserGoalHost` factory seam so Chromium-free root qualification is possible without changing production Playwright ownership. `CreateBrowserGoalAgentAsync` resolves that host only under root lifetime authority, preserves evidence observation/Nemotron planning/durable sessions, and publishes only the lifetime-bound `IBrowserGoalAgent` facade. Added reflection guards for API privacy and seam ownership/immutability, deterministic post-disposal qualification for all four public browser-goal transactions, and actual-root concurrency qualification proving an issued Run transaction holds `NvideaCompositionRoot.DisposeAsync` behind the full transaction lease. Replaced the temporary reflective test fixture with an assembly-internal deterministic root factory that accepts only fake inference, the least-authority browser-goal host factory, and an isolated state directory.

## Latest run — deterministic composition construction
Files changed:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `tests/Nvidea.Core.Tests/NvideaCompositionRootBrowserGoalDisposalRaceTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, recent commits, repository tree, current composition root and root-level disposal-race fixture before mutation.
- Added `NvideaCompositionRoot.CreateDeterministicAsync`, an assembly-internal factory for provider/Chromium/network-free lifetime qualification.
- The factory accepts only `IAgentInferenceClient`, `Func<CancellationToken, Task<ICrashConsistentBrowserGoalHost>>`, an isolated state directory and cancellation. It does not accept Nebius/Tavily credentials, Playwright runtime, cloud transports, audit authorities, approval material or raw provider clients.
- Deterministic construction uses the same production root type, memory/session composition, browser-goal store, lifetime gate and disposal implementation. Startup-owned resources are transferred through `StartupResourceLease` exactly as in production construction.
- Removed constructor reflection and `System.Reflection` from `NvideaCompositionRootBrowserGoalDisposalRaceTests`; the actual-root disposal qualification now uses the explicit internal seam.
- The fake browser host factory is cancellation-aware and remains least-authority.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms `CreateDeterministicAsync` is `internal static`, so it is available to the friend test assembly but absent from the public application API.
- Production `CreateFromEnvironmentAsync` is unchanged in signature and continues to own real Nebius/Tavily/cloud/browser construction.
- Existing actual-root tests still exercise the real `CreateBrowserGoalAgentAsync` and `DisposeAsync`; only external inference/browser authority is substituted.
- No Playwright startup, provider key, network call, paid service, workflow rerun, issue, PR or repository-setting mutation was triggered.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows test suite.

## Security / privacy / failure review
- The testability seam narrows rather than widens authority compared with constructor reflection: qualification code cannot inject arbitrary owned resources or bypass root lifetime composition.
- No credentials, browser payloads, approval scopes, personal data or secrets are accepted or persisted by the deterministic factory.
- Production ownership, exact approval gates, prompt-injection boundaries, crash consistency, durable verification and exactly-one-lease transaction semantics are unchanged.
- The factory creates an ordinary no-redirect HTTP client solely to preserve root ownership shape; deterministic tests make no provider request through it.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Actual-root in-flight concurrency is dynamically specified for Run; equivalent Resume/Approve/Cancel in-flight races remain to be qualified at root level. Post-disposal semantics for all four transactions are already qualified directly at the issued boundary.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The internal deterministic factory should remain test-only in effective reach; API-surface tests should lock that invariant against future accidental promotion to public.

## Single Best Next Task
Add API-surface regression coverage proving `CreateDeterministicAsync` remains internal and least-authority, then extend actual-root in-flight disposal qualification across Resume, Approve-and-Continue and Cancel. Run the full .NET/Windows/Chromium suite in the first capable environment and fix any compile/interface findings without weakening the exactly-one-lease boundary.
