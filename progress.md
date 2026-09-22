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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, bound ambiguous recovery to root lifetime before publication, qualified a least-authority lifetime-bound goal-host decorator, locked the goal-agent public lifetime refactor contract, composed the host decorator into issued goal agents as defense-in-depth, added `BrowserGoalTransactionLifetime`, and added/qualified `LifetimeBoundBrowserGoalAgent` for exactly-one-lease transactions.

## Latest run — least-authority goal-agent API seam
Files changed:
- `src/Nvidea.Core/Desktop/IBrowserGoalAgent.cs`
- `src/Nvidea.Core/Desktop/LifetimeBoundBrowserGoalAgent.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, current composition root, goal agent, lifetime wrapper, recent commits and repository tree before mutation.
- Added public `IBrowserGoalAgent`, exposing only the four durable goal transactions callers need: Resume, RunUntilPause, ApproveAndContinue and Cancel. It exposes no composition gate, browser host, provider client or secret-bearing authority.
- Made internal `LifetimeBoundBrowserGoalAgent` implement that public least-authority contract while keeping its binding operation internal.
- This removes the API-shape blocker to returning the lifetime-enforced facade from composition without making the wrapper or lifetime authority public.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- Static inspection confirms the interface signatures match the wrapper's four transaction methods and preserve cancellation tokens.
- The wrapper still routes every interface operation through `BrowserGoalTransactionLifetime`; no browser payload, URL, typed value, approval material, provider credential or secret is added to lifetime state.
- This connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed.
- No paid/live provider call, browser action, workflow rerun, issue, PR, or repository-setting mutation was triggered.

## Security / privacy / failure review
- The new public contract is intentionally capability-minimal; trusted lifetime binding remains assembly-internal.
- The inner `BrowserGoalAgent` remains lifetime-unaware, preserving exactly-one outer acquisition across its Resume/Approve -> Run internal delegation.
- Existing composition still uses `LifetimeBoundBrowserGoalHost`, so the new interface must not yet be returned by composition until that same-gate host decorator is removed; otherwise nested acquisition could deadlock.
- No safety behavior, approval semantics, prompt-injection boundary, or durable verification rule was weakened.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` still returns raw `BrowserGoalAgent`; the new least-authority interface and transaction facade are not yet on the issued composition path.
- The current composition path still uses `LifetimeBoundBrowserGoalHost`; it must be removed in the same change that wires the outer transaction facade to avoid same-gate re-entry.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Change `NvideaCompositionRoot.CreateBrowserGoalAgentAsync` to return `IBrowserGoalAgent`: compose the ordinary evidence-observing host without `LifetimeBoundBrowserGoalHost`, create the raw `BrowserGoalAgent`, wrap it in `LifetimeBoundBrowserGoalAgent`, bind the wrapper to `_lifetime` before return, and add deterministic composition/API tests proving an in-flight full transaction delays disposal while a post-disposal transaction fails before planner/store/browser work. Then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
