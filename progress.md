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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, bound ambiguous recovery to root lifetime before publication, qualified a least-authority lifetime-bound goal-host decorator, locked the goal-agent public lifetime refactor contract, composed the host decorator into issued goal agents as defense-in-depth, added `BrowserGoalTransactionLifetime`, added/qualified `LifetimeBoundBrowserGoalAgent` for exactly-one-lease transactions, and introduced the least-authority `IBrowserGoalAgent` API seam.

## Latest run — issued browser-goal transaction lifetime boundary
Files changed:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, current composition root, goal-agent wrapper/API seam, repository tree, and browser API tests before mutation.
- Changed `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` to return least-authority `IBrowserGoalAgent` instead of publishing raw `BrowserGoalAgent`.
- Removed `LifetimeBoundBrowserGoalHost` from the issued composition path, preventing same-gate re-entry once the outer full-transaction lease is active.
- Composition now builds the ordinary evidence-observing crash-consistent host, creates the lifetime-unaware raw `BrowserGoalAgent`, wraps it in `LifetimeBoundBrowserGoalAgent`, binds `_lifetime` before publication, and returns only the interface.
- This moves root shutdown atomicity to the complete Run/Resume/Approve/Cancel transaction, including planner/store-only phases between browser calls, while preserving the raw agent's internal Resume/Approve -> Run delegation under one outer lease.
- An intermediate contents-API update accidentally replaced the composition-root file with a placeholder. It was immediately repaired from the exact prior commit and the intended production change was applied in the next commit; current default-branch content was re-fetched and verified. No other repository was touched.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static re-fetch confirms `CreateBrowserGoalAgentAsync` now returns `Task<IBrowserGoalAgent>`, no longer constructs `LifetimeBoundBrowserGoalHost`, binds `LifetimeBoundBrowserGoalAgent` to `_lifetime`, and publishes only after binding.
- Existing deterministic `BrowserGoalTransactionLifetimeTests` and `LifetimeBoundBrowserGoalAgentTests` cover unbound fail-closed behavior, duplicate binding, disposal waiting behind an in-flight transaction, post-disposal rejection before transaction work, and cancellation waiting behavior; executable PASS is not claimed in this connector environment.
- Existing evidence-observing host remains in the path, so production browser session evidence behavior is preserved.
- No paid/live provider call, browser action, workflow rerun, issue, PR, or repository-setting mutation was triggered.

## Security / privacy / failure review
- Public callers receive only `IBrowserGoalAgent`; root lifetime/gate binding remains assembly-internal.
- Exactly one root lease now surrounds each complete goal transaction; the inner host is intentionally not wrapped in the same gate, avoiding non-reentrant deadlock.
- No browser payload, URL, typed value, approval material, provider credential or secret is added to lifetime state.
- Existing approval, prompt-injection, crash-consistency, durable verification and evidence-observation semantics are preserved.
- The accidental intermediate placeholder commit is superseded by the repaired current head; it remains visible in history and is documented here rather than hidden.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- API consumers/tests that explicitly expected `Task<BrowserGoalAgent>` may require compile-time adjustment to the new least-authority interface; static repository review found the dedicated interface seam but the full suite cannot be compiled here.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Add deterministic composition/API regression tests that lock `CreateBrowserGoalAgentAsync` to `IBrowserGoalAgent`, prohibit raw `BrowserGoalAgent`/`LifetimeBoundBrowserGoalHost` publication, and prove through the issued facade that an in-flight full transaction delays root disposal while a post-disposal transaction fails before planner/store/browser work. Then run the full .NET/Windows/Chromium suite in the first capable environment and fix any interface-call-site findings without weakening the exactly-one-lease authority boundary.
