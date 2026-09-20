# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17 — product foundation
Implemented the Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-19 — browser safety and qualification
Hardened browser transport to HTTPS or loopback HTTP and WSS or loopback WS; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Added real-Chromium qualification, TRX validation, SHA-256 evidence receipts, independent verification, clean exact-HEAD provenance, release gate and judge-recording gate.

### 2026-09-19 to 2026-09-20 — memory integrity and explicit recovery
Persisted embedding state is treated as untrusted; malformed vector/provenance state is stripped while user-authored memory survives. Migration remains local-provider-only, preserves Sensitive/Restricted opt-ins, revalidates candidates, validates vectors/provenance, skips concurrent edits, and has invalid-vector/failure/cancellation coverage. JSON persistence serializes access, uses same-directory write-through replacement, and maintains one bounded `.bak` last-known-good generation. Recovery is explicit, protection-context validated, fail-closed and never automatic. Startup recovery eligibility uses non-mutating primary validation and cannot be authorized by unrelated startup failure.

### 2026-09-20 — startup/shutdown lifecycle hardening
WPF startup/recovery cancellation is distinct from corruption/configuration failure and cannot authorize rollback. `App.OnExit` detaches the root before disposal, contains payload-bearing cleanup failures, prevents duplicate disposal and guarantees WPF base shutdown.

### 2026-09-20 — partial-construction ownership hardening
- Added `StartupResourceLease`, a construction-time ownership primitive with reverse-order, exactly-once best-effort cleanup and explicit successful ownership transfer.
- Integrated the lease into `NvideaCompositionRoot.CreateFromEnvironmentAsync`. Nebius HTTP, memory store, optional local embedding provider, memory service, Tavily HTTP, and successfully transferred Nebius Object Storage/Serverless HTTP resources are now owned immediately after acquisition and cannot be orphaned by a later startup exception or cancellation.
- Preserved the existing inner cloud-provider failure boundary: Object Storage/Serverless resources are disposed inside the provider factory only when acquisition/preflight has not escaped. They are registered with the outer startup lease only after `CreateAfterDestinationPreflight` succeeds, avoiding overlapping owners/double-disposal.
- Root ownership is transferred only after the complete `NvideaCompositionRoot` constructor succeeds. Any failure before that point leaves the lease authoritative; after `ReleaseAll`, the normal root `DisposeAsync` path remains authoritative.

Files changed in latest run:
- `src/Nvidea.Core/Desktop/NvideaCompositionRoot.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected `NvideaCompositionRoot.CreateFromEnvironmentAsync`, `StartupResourceLease`, `PersonalMemoryService` disposal, and local embedding-provider construction before implementation.
- Static ownership review confirms acquisition/cleanup order is coherent: memory service is disposed before its provider/store; cloud Serverless HTTP is disposed before Object Storage on startup unwind; Nebius HTTP is last. No provider resource is registered with two active startup owners.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Memory recovery remains explicit, one-generation bounded, protection-context validated, fail closed and non-mutating during eligibility checks. Migration remains local-only and Sensitive/Restricted opt-ins remain explicit.
- Startup/shutdown cleanup is payload-free: cleanup failures cannot replace the authoritative startup/exit exception or be projected to UI.
- The construction lease stores only disposable references and never logs, serializes or inspects provider credentials/state. Cancellation follows the same cleanup path as every other failed construction.

## Known blockers / risks
- Factory-level fault-injection tests are still needed to prove the newly integrated lease at the composition boundary, especially failures after memory initialization, after Tavily acquisition, after successful cloud-provider transfer, and during final composition.
- Real-Chromium fixtures and the release/judge qualification scripts still need execution on Windows with .NET 8, PowerShell 7 and matching Playwright Chromium.
- Persisted-memory, migration, JSON-store/recovery/probe, startup/shutdown and ownership-lease tests still need compile/runtime execution under .NET 8 on Windows.
- `File.Move(..., overwrite: true)` crash/power-loss durability semantics depend on host filesystem/OS and need Windows validation.
- Recovery is intentionally one snapshot deep, not a journal/database/user backup.
- SHA-256 browser receipts are integrity bindings, not signatures; freshness depends on producer host clock.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Add a narrow factory seam/fault-injection harness around composition-root acquisitions and write deterministic tests proving failures after memory initialization, Tavily acquisition, successful cloud-provider transfer and immediately before ownership release dispose every acquired resource exactly once while preserving the original exception/cancellation. Then execute the accumulated lifecycle/memory suites on Windows/.NET 8 when an executable environment is available.
