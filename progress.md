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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, and atomically bound the product facade to root shutdown authority before publication.

## Latest run — ambiguous-recovery facade lifetime boundary
Files changed:
- `src/Nvidea.Core/Desktop/BrowserAmbiguousRecovery.cs`
- `progress.md`

Completed:
- `BrowserAmbiguousRecoveryService` now has an assembly-internal one-time `BindCompositionLifetime` boundary matching the already-qualified browser-product pattern.
- `RecoverAsync` acquires its facade lifetime before reading the durable goal session, inspecting/reconciling browser child state, persisting recovery state, or publishing session evidence. This also serializes concurrent recovery attempts on standalone/test-created service instances instead of allowing overlapping state reconciliation.
- Binding is least-authority: no provider credentials, raw browser runtime, approval material, URLs, typed values, receipts, or other payload is added to the lifetime boundary.
- Existing public constructor behavior is preserved with a private compatibility-local gate, so direct unit/integration construction remains source-compatible.
- Re-read `progress.md`, current composition root, goal-agent public operation flow, recovery implementation, and product lifetime-binding pattern before editing. Repository identity was explicitly reverified before each GitHub mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- Static inspection confirms the recovery lease is acquired before any store/host/evidence authority is touched and is held across the complete recovery transaction.
- The one-time binding guard rejects accidental rebinding, mirroring the product facade's authority discipline.
- The connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- Recovery remains fail-closed for downloads/uploads and for ambiguous actions without deterministic proof; lifetime hardening does not weaken reconciliation rules.
- Evidence remains downstream of trusted-host reconciliation and durable session persistence.
- Caller cancellation still flows through lifetime acquisition and every recovery dependency.
- Important limitation: `NvideaCompositionRoot.CreateBrowserAmbiguousRecoveryServiceAsync` still constructs the service without invoking the new root-lifetime binding. Therefore this run establishes the operation boundary and local serialization but does NOT yet claim recovery-vs-root-disposal is closed.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Ambiguous recovery now supports root lifetime binding but the composition root must bind it before publication.
- `BrowserGoalAgent` still outlives its acquisition lease; naively leasing all public methods would deadlock because `ResumeAsync` and `ApproveAndContinueAsync` call `RunUntilPauseAsync` internally. It needs public lease wrappers around private non-leasing cores (or an equivalent non-reentrant design).
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required.
- Judge Evidence browser initialization can incur Playwright startup latency; live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Finish issued-facade shutdown linearization: bind `BrowserAmbiguousRecoveryService` to `_lifetime` inside `CreateBrowserAmbiguousRecoveryServiceAsync` before publication, then refactor `BrowserGoalAgent` so each external operation holds exactly one root lifetime lease without nested acquisition when Resume/Approve delegate into the run loop. Add deterministic post-disposal/in-flight-disposal qualification for both facades, then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
