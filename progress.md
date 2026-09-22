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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, bound ambiguous recovery to root lifetime before publication, qualified a least-authority lifetime-bound goal-host decorator, and locked the goal-agent public lifetime refactor contract.

## Latest run — goal-host composition hardening
Files changed:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, current recent commits/tree, `BrowserGoalAgent`, and `NvideaCompositionRoot` before mutation.
- Wired the already-qualified `LifetimeBoundBrowserGoalHost` into `CreateBrowserGoalAgentAsync` after the evidence-observing host boundary and before the agent is returned.
- Every browser-authority call made by an issued goal agent now acquires the same root `CompositionLifetimeGate`; in-flight host calls hold root disposal behind them, while host calls attempted after disposal fail closed before touching browser authority.
- Preserved evidence observation, durable goal-session storage, Nemotron planning, exact approval behavior, and lazy browser startup; no existing functionality was removed.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- `LifetimeBoundBrowserGoalHost` already has deterministic Chromium-free concurrency qualification for in-flight disposal, post-disposal failure, and cancellation cleanup.
- Static composition inspection confirms the new boundary is constructed with the root `_lifetime` and wraps the observed crash-consistent host.
- This connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- The composition change stores no URL, page body, typed value, approval material, provider credential, browser receipt, or secret in lifetime state.
- Browser authority can no longer be invoked by the issued goal agent after root disposal has linearized, and an in-flight host call cannot overlap browser-host teardown.
- This is defense-in-depth, not full transaction atomicity: disposal can still linearize during planner/store-only work between host calls. A later host call then fails closed rather than using stale authority, but the whole multi-phase public transaction is not yet one shutdown lease.
- The lifetime-bound host must be removed when the agent gains its own outer root lease; nesting the same non-reentrant gate would deadlock.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- `BrowserGoalAgent` still needs exactly-one-lease public wrappers over private non-leasing cores to make Run/Resume/Approve/Cancel transactions atomic against root disposal.
- When that outer agent lease is implemented, `CreateBrowserGoalAgentAsync` must bind it before return and must pass the plain evidence-observing host rather than `LifetimeBoundBrowserGoalHost`, avoiding same-gate reentrancy.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Implement the full `BrowserGoalAgent` outer lifetime boundary: add internal one-time root-lifetime binding; refactor `RunUntilPauseAsync`, `ResumeAsync`, `ApproveAndContinueAsync`, and `CancelAsync` into exactly-one-lease public wrappers over private non-leasing cores; make all internal delegation target only the private cores; then change `CreateBrowserGoalAgentAsync` to bind before return and remove the same-gate `LifetimeBoundBrowserGoalHost` wrapper. Add deterministic in-flight-disposal, post-disposal, cancellation, and no-nested-deadlock tests without Chromium, then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
