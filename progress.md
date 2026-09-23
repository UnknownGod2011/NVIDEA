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

## Latest run — browser-goal composition seam
Files changed:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `tests/Nvidea.Core.Tests/BrowserGoalCompositionSeamTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, recent commits, the full current composition root, repository tree, and existing browser-goal lifetime/API qualification before mutation.
- Added a narrow optional browser-goal host factory seam at the private composition-root constructor boundary. Production `CreateFromEnvironmentAsync` does not supply it, so production continues to lazily create and own `BrowserHostRuntime` exactly as before.
- Added `GetBrowserGoalHostUnderLeaseAsync`, which resolves only the least-authority `ICrashConsistentBrowserGoalHost` contract and is called only after the root lifetime lease has been acquired.
- Updated `CreateBrowserGoalAgentAsync` to consume that least-authority host, preserve evidence observation, Nemotron planning, durable goal storage, and the outer exactly-one-lease `LifetimeBoundBrowserGoalAgent` publication boundary.
- Kept browser product and ambiguous-recovery construction on the canonical `BrowserHostRuntime` path; the seam cannot silently replace those broader privileged surfaces.
- Added deterministic reflection guards requiring the seam field and resolver to stay private, cancellation-aware, and typed only to `ICrashConsistentBrowserGoalHost`; public composition APIs must not expose the seam or goal-host authority.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`.

Validation/evidence:
- Static inspection confirms the production factory still invokes the private constructor without a host-factory argument; therefore normal application behavior remains the Playwright-backed path.
- The new seam is nullable, private, constructor-injected, and immutable after root construction; there is no public setter, service locator, or runtime swapping mechanism.
- `CreateBrowserGoalAgentAsync` still acquires `_lifetime` before resolving either production or injected host authority and binds the returned lifetime facade before publication.
- The new regression tests are provider/Chromium/network-free and inspect only API/composition shape.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows test suite.
- No paid/live provider call, browser action, workflow rerun, issue, PR, or repository-setting mutation was triggered.

## Security / privacy / failure review
- The seam deliberately accepts only `ICrashConsistentBrowserGoalHost`; it cannot inject raw Playwright objects, provider credentials, the root lifetime gate, or product-runtime authority.
- Factory invocation occurs while the root lifetime lease is held, so disposal cannot complete while host construction is in progress.
- A null factory result fails closed before agent publication.
- No browser payload, URL, typed value, approval material, credential, secret, or personal state is retained by the seam itself.
- Existing approval, prompt-injection, crash-consistency, durable verification, evidence-observation and exactly-one-lease transaction semantics are unchanged.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- The seam now makes Chromium-free root qualification architecturally possible, but a deterministic test root constructor/factory still needs to be added so tests can supply fake inference/memory/session dependencies without reflection-heavy setup or live provider configuration.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Add an assembly-internal deterministic `NvideaCompositionRoot` test construction path that accepts fake inference plus the new least-authority browser-goal host factory while preserving production ownership semantics. Then add composition-level concurrency tests proving an issued Run/Resume/Approve/Cancel transaction delays actual root disposal and a post-disposal call fails before planner/store/browser authority executes. Run the full .NET/Windows/Chromium suite in the first capable environment and fix any compile/interface findings without weakening the exactly-one-lease boundary.
