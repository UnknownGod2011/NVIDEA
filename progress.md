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
Hardened browser transport to HTTPS or loopback HTTP and WSS or loopback WS; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Added real-Chromium qualification, TRX validation, SHA-256 evidence receipts, independent verification, clean exact-HEAD provenance, release gate and judge-recording gate. Producer rechecks HEAD and tracked/untracked cleanliness after Chromium so source drift invalidates evidence.

### 2026-09-19 to 2026-09-20 — memory embedding and persistence integrity
Persisted embedding state is treated as untrusted: malformed vector/provenance pairs, empty/dimensionally inconsistent vectors, missing provider/model identity and NaN/Infinity are stripped while user-authored memory survives. Semantic comparison repeats finite/provenance checks; lexical/recency/importance retrieval remains available after semantic corruption. Migration remains local-provider-only, preserves Sensitive/Restricted opt-ins, revalidates candidates, validates provider vectors/provenance, skips concurrent edits, and has adversarial invalid-vector/failure/cancellation coverage.

JSON memory persistence serializes access and uses same-directory write-through replacement. Added one bounded `.bak` last-known-good generation; only demonstrably readable current state can become backup and corrupt current bytes never displace known-good backup. Normal reads never silently fall back. Explicit recovery validates/decrypts the backup under the same protection context before replacement. Coverage includes malformed/truncated state, cancellation, concurrent snapshots, previous-generation restoration, protected-envelope confidentiality and wrong-context fail-closed behavior.

### 2026-09-20 — explicit, fail-closed Windows memory recovery
Added startup recovery UX that is never automatic and defaults to Cancel. Recovery is offered only when both memory generations exist and the memory primary itself independently fails validation; unrelated startup failures cannot authorize rollback. Added non-mutating `ValidatePrimaryAsync` plus `MemoryRecoveryAvailabilityProbe`, so merely checking eligibility cannot migrate/rotate durable memory. Successful recovery requires a clean restart. Startup and recovery cancellation are handled separately from corruption/configuration failure and never trigger rollback or misleading provider diagnostics.

### 2026-09-20 — shutdown disposal hardening
- Audited `App.OnExit` and `NvideaCompositionRoot.DisposeAsync` after startup/recovery cancellation hardening.
- Found that WPF synchronously called `DisposeAsync().GetResult()` without a failure boundary. Any cleanup exception could escape `OnExit`, skip `base.OnExit`, and turn an otherwise safe normal/cancelled shutdown into an unhandled teardown failure.
- `App.OnExit` now detaches `_root` before disposal so re-entrant/duplicate exit paths cannot dispose the same authority graph twice.
- Composition-root disposal is wrapped in a last-resort payload-free failure boundary. Cleanup exceptions are not displayed/logged with provider/path details, cleanup is not retried against a partially disposed graph, and `base.OnExit(e)` is guaranteed through `finally`.
- This does not weaken browser, memory, permission, provider, recovery, or cloud policy. It only makes process teardown deterministic when resource cleanup itself fails.

Files changed in latest run:
- `src/Nvidea.Windows/App.xaml.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, inspected current recent commits, WPF startup/exit flow, and `NvideaCompositionRoot.DisposeAsync` before implementation.
- Static control-flow review confirms `_root` is cleared before disposal, duplicate exit cannot reuse it, cleanup exceptions cannot bypass `base.OnExit`, and no exception detail is projected to UI.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Memory recovery remains explicit, one-generation bounded, protection-context validated, fail closed and non-mutating during eligibility checks. Startup/recovery cancellation remains distinct from corruption/configuration failure.
- Shutdown cleanup failures are now credential-safe/payload-free and cannot prevent WPF's base exit path. Root authority is detached before cleanup to prevent duplicate disposal attempts.
- Migration remains local-only; Sensitive/Restricted opt-ins remain explicit. Provider failure/cancellation cannot partially apply an unvalidated migration batch.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release qualification wrapper, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- Persisted-memory, migration, JSON-store/recovery/probe tests and the WPF recovery/cancellation/shutdown paths still need compile/runtime execution under .NET 8 on Windows.
- `NvideaCompositionRoot.CreateFromEnvironmentAsync` still constructs several disposable resources before returning the root. If startup fails after those allocations but before return, the WPF host never receives an owner to dispose. This partial-construction cleanup path needs explicit factory-level ownership hardening.
- `File.Move(..., overwrite: true)` is relied upon as the final same-volume replacement step; crash/power-loss durability semantics depend on host filesystem/OS and need Windows validation.
- Recovery is intentionally one snapshot deep, not a journal/database/user backup. A filesystem failure affecting both sibling files remains unrecoverable here.
- SHA-256 browser receipts are integrity bindings, not signatures; freshness depends on producer host clock.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Harden `NvideaCompositionRoot.CreateFromEnvironmentAsync` partial-construction ownership. Ensure Nebius/Tavily HTTP clients, memory store/provider/service, Object Storage/serverless resources and any successfully created runtime are disposed exactly once if initialization, memory load, Tavily setup, cloud preflight, or final composition throws before a root is returned. Add focused fault-injection/API-surface coverage where feasible without live providers. Then execute the accumulated memory/startup/shutdown suites on Windows/.NET 8 when an executable environment is available. Keep the clean Windows checkout as the required path for browser release qualification and the mandatory judge-recording gate.
