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
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, wired WPF Judge Evidence to authoritative browser/session evidence, repaired build/API contracts, serialized canonical browser product publication, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, qualified issued-facade operation leasing, wrapped every public `BrowserProductRuntime` operation in a lifetime lease, atomically bound the product facade to root shutdown authority before publication, and added operation leasing to ambiguous recovery.

## Latest run — root-bound ambiguous recovery
Files changed:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `progress.md`

Completed:
- Closed the previously documented recovery-vs-root-disposal gap by binding each newly created `BrowserAmbiguousRecoveryService` to the composition root's `_lifetime` before returning it to callers.
- Binding happens while the factory itself still holds the same root acquisition lease, so there is no publication window in which an externally visible recovery facade can operate against only its compatibility-local gate.
- The recovery service's existing operation lease now therefore linearizes its complete durable read/reconcile/persist/evidence transaction against root shutdown.
- Preserved lazy Playwright startup, caller cancellation, the public factory signature, and the existing least-authority API surface; no credentials, URLs, typed values, approval material, browser receipts, or provider payload were added to lifetime state.
- Re-read `progress.md`, the current composition root, and the existing recovery lifetime contract before editing. Repository identity was explicitly reverified before each GitHub mutation as exactly `UnknownGod2011/NVIDEA`; no other repository was touched.

Validation/evidence:
- Static inspection confirms `CreateBrowserAmbiguousRecoveryServiceAsync` acquires `_lifetime`, obtains the canonical browser host under that lease, constructs the unpublished service, invokes `recovery.BindCompositionLifetime(_lifetime)`, and only then returns it.
- This matches the already-established product-facade publication pattern and removes the specific unbound recovery path recorded by the previous run.
- The connector environment cannot execute .NET 8/WPF/Chromium, so compile/test PASS is not claimed. No paid/live provider or browser operation was triggered.

## Security / privacy / failure review
- Root disposal cannot cross a bound recovery operation's lifetime lease and tear down browser authority midway through durable reconciliation; post-disposal recovery acquisition fails closed through `CompositionLifetimeGate`.
- Recovery's existing fail-closed rules for downloads/uploads and crash-ambiguous actions are unchanged; this is lifetime hardening only.
- Evidence remains downstream of trusted-host reconciliation and durable session persistence.
- Caller cancellation continues through factory acquisition and recovery-operation acquisition/dependencies.
- No new secret storage, telemetry, network endpoint, browser permission, or consequential-action bypass was introduced.

## Known blockers / risks
- New Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- `BrowserGoalAgent` still outlives its acquisition lease; naively leasing all public methods would deadlock because `ResumeAsync` and `ApproveAndContinueAsync` call `RunUntilPauseAsync` internally. It needs public lease wrappers around private non-leasing cores (or an equivalent non-reentrant design).
- Browser startup remains intentionally inside the root lifetime lease; Windows timing qualification is required.
- Judge Evidence browser initialization can incur Playwright startup latency; live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.

## Single Best Next Task
Finish issued-facade shutdown linearization for `BrowserGoalAgent`: refactor the public Run/Resume/Approve entry points into exactly-one-lease wrappers over private non-leasing cores, bind the agent to `_lifetime` before publication in `CreateBrowserGoalAgentAsync`, and add deterministic in-flight-disposal/post-disposal qualification without requiring Chromium. Then run the full .NET/Windows/Chromium suite in the first capable environment and fix findings without weakening authority boundaries.
