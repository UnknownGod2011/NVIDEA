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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, added operation leasing to ambiguous recovery, bound ambiguous recovery to root lifetime before publication, qualified a least-authority lifetime-bound goal-host decorator, locked the goal-agent public lifetime refactor contract, and composed the host decorator into issued goal agents as defense-in-depth.

## Latest run — goal transaction lifetime primitive
Files changed:
- `src/Nvidea.Core/Desktop/BrowserGoalTransactionLifetime.cs`
- `tests/Nvidea.Core.Tests/BrowserGoalTransactionLifetimeTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, the current tree, `BrowserGoalAgent`, its lifetime-design tests, and composition state before mutation.
- Added `BrowserGoalTransactionLifetime`, an internal least-authority boundary designed for exactly one lease around each complete public goal transaction. It fails closed if composition forgets to bind it and rejects a second binding.
- Added deterministic Chromium-free qualification for unpublished/unbound failure, one-time binding, in-flight transaction vs disposal ordering, post-disposal fail-closed behavior, and cancellation while waiting without executing stale authority.
- The boundary passes the caller cancellation token through to the operation and deliberately contains no browser payload, URL, typed value, approval scope, provider credential, or secret.
- Repository identity was explicitly reverified immediately before every mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- Static inspection confirms the primitive delegates lifetime linearization to the already-qualified `CompositionLifetimeGate` rather than introducing a second semaphore or disposal flag.
- The new tests exercise the concurrency contract without Playwright/Chromium or provider credentials.
- This connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- The transaction boundary is assembly-internal and cannot be used by callers to manufacture lifetime authority.
- Unbound publication is fail-closed: an operation delegate is not invoked until a bound root lifetime lease has been acquired.
- Root disposal waits behind an in-flight transaction; after disposal linearizes, new transactions fail before their bodies execute.
- This run intentionally does not wire the primitive into `BrowserGoalAgent` yet; doing so safely still requires refactoring public Run/Resume/Approve/Cancel methods into wrappers over private non-leasing cores so internal delegation never reacquires the non-reentrant gate.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- `BrowserGoalAgent` still needs exactly-one-lease public wrappers over private non-leasing cores and one-time binding before publication.
- When the outer agent lease is integrated, `CreateBrowserGoalAgentAsync` must pass the plain evidence-observing host rather than `LifetimeBoundBrowserGoalHost`; retaining both would re-enter the same root gate and deadlock.
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required. Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Integrate `BrowserGoalTransactionLifetime` into `BrowserGoalAgent`: add an internal one-time composition binding, refactor `RunUntilPauseAsync`, `ResumeAsync`, `ApproveAndContinueAsync`, and `CancelAsync` into exactly-one-lease public wrappers over private non-leasing cores, and redirect all internal delegation to those cores. Then update `CreateBrowserGoalAgentAsync` to bind before return and remove `LifetimeBoundBrowserGoalHost` from that composition path to prevent same-gate reentrancy. Extend deterministic tests to prove full transaction-vs-disposal behavior and no nested deadlock, then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
