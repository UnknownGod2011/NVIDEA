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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, bound ambiguous recovery to root lifetime before publication, and qualified a least-authority lifetime-bound goal-host decorator.

## Latest run — browser goal-agent lifetime contract qualification
Files changed:
- `tests/Nvidea.Core.Tests/BrowserGoalAgentLifetimeDesignTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, the current `BrowserGoalAgent`, `NvideaCompositionRoot`, and repository tree before mutation.
- Added Chromium-free reflection contract tests that lock the complete public goal transaction surface to `RunUntilPauseAsync`, `ResumeAsync`, `ApproveAndContinueAsync`, and `CancelAsync`.
- Added a contract that every public goal transaction remains async and cancellation-aware, so the upcoming lifetime wrapper cannot accidentally turn shutdown-sensitive work into an uncancellable/synchronous boundary.
- Added a least-authority contract preventing composition lifetime binding/gate authority from becoming public API.
- Confirmed from current implementation that Resume and Approve delegate back into RunUntilPause, so independently leasing all public methods would deadlock on the non-reentrant `CompositionLifetimeGate`.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- Static inspection of the current default branch confirms exactly four public `Task<BrowserGoalSession>` transaction methods and confirms the current nested delegation that motivates private non-leasing cores.
- The new tests are deterministic and do not require Chromium/provider credentials.
- This connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- The new tests inspect only type metadata; they contain no URL, page content, typed value, approval material, provider credential, browser receipt, or secret.
- The contract deliberately keeps lifetime authority internal rather than making shutdown control available to callers.
- No production behavior was weakened or removed in this run.
- The actual multi-phase goal transaction is still not atomic against root shutdown: disposal may linearize between planner/store/host phases until the agent itself owns one outer lease per public transaction.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- `LifetimeBoundBrowserGoalHost` is qualified but not wired into `CreateBrowserGoalAgentAsync`; wiring it under an outer agent lease would nest the same non-reentrant gate and deadlock, so it must not be composed that way without a distinct strategy.
- `BrowserGoalAgent` still needs exactly-one-lease public wrappers over private non-leasing cores, followed by internal lifetime binding while `CreateBrowserGoalAgentAsync` holds the root lease.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Refactor `BrowserGoalAgent` so each of the four now-contract-locked public transactions acquires exactly one composition-lifetime lease and delegates only to private non-leasing cores; bind the root lifetime before `CreateBrowserGoalAgentAsync` publishes/returns the agent. Do not wrap its host with the same lifetime gate under that outer lease. Add deterministic in-flight-disposal, post-disposal, and cancellation tests without Chromium, then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
