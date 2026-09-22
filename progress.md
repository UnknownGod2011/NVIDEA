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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, and wrapped every public `BrowserProductRuntime` operation in a lifetime lease.

## Latest run — atomic root-to-product lifetime binding
Files changed:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `progress.md`

Completed:
- `GetBrowserProductAsync` now creates the product while holding the root lifetime lease, binds the unpublished product exactly once to the root `_lifetime`, then publishes it to `_browserProduct`.
- Cached product publication remains canonical and serialized; no product can be returned from the root while still using its compatibility-local lifetime gate.
- Browser product operations and root disposal now share the same `CompositionLifetimeGate`: an in-flight product operation can hold disposal behind it, while operations beginning after disposal wins fail before touching host/evidence/download authority.
- Preserved lazy browser startup and the existing least-authority public API. No raw host, durable runtime, receipt store/publisher, or lifetime authority was made public.
- Re-read `progress.md`, the current root/product implementation, tree, and lifetime qualification before editing. Repository identity was explicitly reverified before every GitHub mutation as exactly `UnknownGod2011/NVIDEA`.
- An intermediate malformed contents update during this run was immediately corrected in the next commit; current default-branch head contains the restored composition root plus the intended lifetime binding. No other repository was touched.

Validation/evidence:
- Static inspection confirms publication order is now create -> `BindCompositionLifetime(_lifetime)` -> assign `_browserProduct` -> return, all under the root lifetime lease.
- Existing deterministic `CompositionLifetimeGate` / issued-facade qualification establishes the required semantics without Chromium: in-flight leases delay disposal; post-disposal acquisition fails closed; waiting cancellation does not execute authority.
- Connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- Lifetime binding carries no provider/browser payload and persists no credentials, prompts, URLs, locators, downloads, or receipts.
- Binding occurs before publication, preventing callers from observing a product temporarily attached to the wrong shutdown authority.
- Root disposal remains non-cancellable after its linearization point and product operations retain caller cancellation.
- Exact consequential-action approval/evidence ordering is unchanged.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Goal-agent and ambiguous-recovery issued facades still outlive their acquisition lease and need the same least-authority operation lifetime treatment.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required.
- Judge Evidence browser initialization can incur Playwright startup latency; live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Extend root lifetime authority to issued `BrowserGoalAgent` and `BrowserAmbiguousRecoveryService` operations without exposing the gate or creating nested-acquisition deadlocks. Add deterministic post-disposal and in-flight-disposal qualification for those facades, then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
