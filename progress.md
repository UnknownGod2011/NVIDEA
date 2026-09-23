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

## Latest run — browser-goal composition API regression qualification
Files changed:
- `tests/Nvidea.Core.Tests/BrowserGoalCompositionApiSurfaceTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, the current composition root, existing browser goal API tests, and current test tree before mutation.
- Added deterministic provider/Chromium-free API regression coverage requiring `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` to publish exactly `Task<IBrowserGoalAgent>` rather than raw `BrowserGoalAgent`.
- Locked `IBrowserGoalAgent` to the four intended durable user transactions: Run, Resume, Approve-and-Continue, and Cancel; each must remain asynchronous and cancellation-aware.
- Added a public-surface guard preventing `NvideaCompositionRoot` from exposing raw `BrowserGoalAgent` or `CompositionLifetimeGate` through method parameters/return types, including nested generic/array shapes.
- This makes accidental future erosion of the least-authority composition boundary visible without starting Playwright or invoking Nebius.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static repository inspection confirms the production root currently returns `Task<IBrowserGoalAgent>`, creates an ordinary evidence-observing host, wraps the raw agent in `LifetimeBoundBrowserGoalAgent`, binds `_lifetime` before publication, and does not compose `LifetimeBoundBrowserGoalHost` on this path.
- The new tests use reflection only and require no API key, browser, network, or provider call.
- Existing `BrowserGoalTransactionLifetimeTests` / `LifetimeBoundBrowserGoalAgentTests` cover the underlying lease primitive and facade fail-closed behavior; this run adds the missing composition/API contract layer.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows test suite.
- No paid/live provider call, browser action, workflow rerun, issue, PR, or repository-setting mutation was triggered.

## Security / privacy / failure review
- Public composition remains least-authority: callers receive `IBrowserGoalAgent`, not the raw agent or root lifetime gate.
- Regression coverage contains no browser payload, URL, typed value, approval material, credential, secret, or personal state.
- The tests intentionally avoid browser/provider startup, keeping CI lean and deterministic.
- Existing approval, prompt-injection, crash-consistency, durable verification and evidence-observation semantics are unchanged.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- The new reflection tests are strong API guards but do not themselves instantiate `NvideaCompositionRoot`; a composition-level concurrency harness still needs a browser-independent construction seam to prove an issued facade delays actual root disposal without Playwright startup.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Introduce the narrowest test-only/internal browser-host factory seam in `NvideaCompositionRoot` (without weakening production authority) so deterministic tests can construct an issued goal facade without Playwright. Then prove end-to-end that an in-flight issued Run/Resume/Approve/Cancel transaction delays root disposal and that a post-disposal call fails before planner/store/browser authority executes. Run the full .NET/Windows/Chromium suite in the first capable environment and fix any compile/interface findings without weakening the exactly-one-lease boundary.
