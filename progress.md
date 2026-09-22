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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, and bound ambiguous recovery to root lifetime before publication.

## Latest run — browser goal host lifetime qualification
Files changed:
- `src/Nvidea.Core/Desktop/LifetimeBoundBrowserGoalHost.cs`
- `tests/Nvidea.Core.Tests/LifetimeBoundBrowserGoalHostTests.cs`
- `progress.md`

Completed:
- Added an internal least-authority `LifetimeBoundBrowserGoalHost` decorator that acquires `CompositionLifetimeGate` before every privileged `ICrashConsistentBrowserGoalHost` operation and releases it only after that operation completes.
- Covered Observe, Start/Create/Advance/Get, approval rearm/consume, and cancellation authority. Exact approval scope validation remains fail-closed before authority invocation.
- Added deterministic Chromium-free qualification for three shutdown invariants: in-flight host work holds disposal behind it; calls after disposal fail before inner browser authority runs; cancellation while waiting does not invoke inner authority or corrupt later gate use.
- Kept the boundary internal so the lifetime gate is not exposed through the public goal API.
- Re-read `progress.md`, `BrowserGoalAgent`, `NvideaCompositionRoot`, browser contracts, and recent commits before editing. Repository identity was explicitly reverified before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- Static contract inspection confirms all eight `ICrashConsistentBrowserGoalHost` operations route through the same `ExecuteAsync` lease boundary.
- Corrected the new test fixture after checking the current five-required-argument `BrowserObservation` contract; the final fixture matches the repository contract.
- The connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- The decorator carries only authority/lifetime references; it stores no URL, page content, typed value, approval grant, provider credential, browser receipt, or secret.
- Shutdown cannot dispose BrowserHostRuntime during an individual decorated host call; post-disposal calls fail closed at the composition gate.
- Caller cancellation propagates both while waiting for lifetime authority and into the inner browser operation.
- This deliberately does not claim multi-call goal transactions are atomic. Disposal can still linearize between BrowserGoalAgent's planner/store/host phases until the agent itself receives one outer lease per public operation.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- `LifetimeBoundBrowserGoalHost` is qualified but not yet wired into `CreateBrowserGoalAgentAsync`; wiring it alone would protect individual host calls but still would not make an entire Run/Resume/Approve/Cancel transaction atomic against root shutdown.
- `BrowserGoalAgent` still needs public exactly-one-lease wrappers around private non-leasing cores. Naively leasing `ResumeAsync`, `ApproveAndContinueAsync`, and `RunUntilPauseAsync` independently would deadlock because the former methods delegate to the latter.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Refactor `BrowserGoalAgent` into exactly-one-lifetime-lease public Run/Resume/Approve/Cancel wrappers over private non-leasing cores, bind that lifetime while `CreateBrowserGoalAgentAsync` still holds the root lease, and use the qualified host decorator only as defense-in-depth if it does not create nested-gate deadlock. Add deterministic in-flight-disposal and post-disposal tests without Chromium, then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
