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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, and integrated `CompositionLifetimeGate` into `NvideaCompositionRoot` as the acquisition/disposal linearization authority. Deterministic qualification proved the same gate can safely protect already-issued facade operations.

## Latest run — browser product operation leasing
Files changed:
- `src/Nvidea.Core/Desktop/BrowserProductRuntime.cs`
- `progress.md`

Completed:
- Every public `BrowserProductRuntime` operation now acquires an async lifetime lease before touching host, durable-verification, download, approval, cancellation, or evidence authority and holds it through settlement.
- Added an assembly-internal one-time `BindCompositionLifetime` hook so trusted composition can replace the facade's unpublished local compatibility gate with the root shutdown authority without exposing that authority publicly.
- Consequential approval/session evidence remains downstream of successful host completion; cancellation and rejected exact scopes cannot create success evidence.
- Re-read current root/host/product implementation and recent lifetime history before editing. Repository identity was explicitly reverified before every mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was mutated.

Validation/evidence:
- Static inspection confirms all nine public product operations now lease before authority access.
- The existing qualified `CompositionLifetimeGate` semantics remain the basis: queued/new acquisition fails after disposal wins; in-flight lease holds disposal behind it; waiting cancellation does not invoke authority.
- Connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- The lease carries no browser/provider payload and persists no credentials, prompts, URLs, locators or receipts.
- Product callers still cannot obtain `BrowserHostRuntime`, `BrowserDurableActionRuntime`, receipt publisher/store, or the composition lifetime authority.
- Exact approval and evidence ordering are unchanged; only operation lifetime is wrapped.
- IMPORTANT: trusted `NvideaCompositionRoot` does not yet call `BindCompositionLifetime(_lifetime)` before publishing the product. Until that final composition binding is made, product operations use their private compatibility gate and therefore do not yet block root disposal. This run intentionally does not falsely mark the production race closed.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Final root-to-product lifetime binding remains required to close the actual operation-vs-root-disposal race.
- Goal-agent and ambiguous-recovery issued facades still need the same least-authority operation lifetime treatment.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required.
- Judge Evidence browser initialization can incur Playwright startup latency; live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Complete the production binding atomically in `NvideaCompositionRoot.GetBrowserProductAsync`: create the unpublished product, bind it exactly once to `_lifetime`, then publish it. Add a deterministic composition contract proving the returned product is root-bound and post-disposal calls fail before host authority. Then extend operation leasing to goal-agent and ambiguous-recovery facades without nested acquisition. Run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
