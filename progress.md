# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence surface projects real provider readiness plus payload-free, production-observed session milestones.

## Persistent history
### 2026-09-06 to 2026-09-17
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling.

### 2026-09-17 to 2026-09-19 — browser safety and qualification
Hardened browser transport to HTTPS or loopback HTTP and WSS or loopback WS; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Added real-Chromium qualification, TRX validation, SHA-256 evidence receipts, independent verification, clean exact-HEAD provenance, release gate and judge-recording gate. Producer rechecks HEAD and tracked/untracked cleanliness after Chromium so source drift invalidates evidence.

### 2026-09-19 to 2026-09-20 — memory embedding and persistence integrity
- Persisted embedding state is untrusted: malformed vector/provenance pairs, empty/dimensionally inconsistent vectors, missing provider/model identity and NaN/Infinity are stripped while user-authored memory survives.
- Semantic comparison repeats finite/provenance checks; lexical/recency/importance retrieval remains available after semantic corruption.
- Migration treats malformed semantic state as stale, remains local-provider-only, preserves Sensitive/Restricted opt-ins, revalidates candidates, validates provider vectors/provenance and skips concurrent edits.
- Adversarial tests cover corrupt semantic state, valid preservation, lexical fallback, sensitivity policy, invalid provider vectors and multi-batch provider failure/cancellation without partial active-batch application.
- JSON persistence serializes access and uses same-directory write-through temp replacement. Tests cover replacement, malformed/truncated fail-closed reads, cancellation preservation and non-interleaved concurrent snapshots.
- Added one bounded `.bak` last-known-good generation. Only demonstrably readable current state can become backup; corrupt current bytes never displace known-good backup. Normal reads never silently fall back.
- `RecoverLastKnownGoodAsync` requires explicit intent, validates/decrypts backup under the same protection context before replacement, then restores exact persisted bytes. Tests cover explicit-only fallback, previous-generation restoration, missing backup, corrupt-primary backup preservation, temp cleanup, protected-envelope confidentiality and wrong-context fail-closed behavior.

### 2026-09-20 — explicit Windows memory recovery UX
- Added a startup recovery surface in `src/Nvidea.Windows/App.xaml.cs` for the bounded last-known-good memory generation.
- Recovery is never automatic. The prompt appears only after startup reports `InvalidDataException`, the default production `memory.json` and `.bak` both exist, and the current personal-memory primary itself independently fails validation. This prevents unrelated protected-subsystem failures from opportunistically triggering memory rollback.
- The dialog clearly states that recovery rolls back exactly one generation, recent memory changes may be lost, no memory content is displayed, no cloud provider is contacted, and Cancel leaves files unchanged. Cancel is the default choice.
- On explicit OK, recovery uses the production `JsonFileMemoryStore` and therefore the same Windows DPAPI/protected-envelope validation and atomic replacement path. Failed backup validation is credential-safe/payload-free and does not intentionally replace the primary.
- After successful recovery the app closes and requires a clean restart instead of continuing with partially initialized provider/runtime state.

### 2026-09-20 — testable, non-mutating recovery eligibility
- Extracted startup rollback eligibility into `MemoryRecoveryAvailabilityProbe`, used by the WPF host. It authorizes offering recovery only when both generations exist and the primary specifically fails memory validation with `InvalidDataException`; missing files, path/IO/permission failures and a healthy primary fail closed.
- Added `JsonFileMemoryStore.ValidatePrimaryAsync`, a deliberately non-mutating validation path. Unlike normal `ReadAllAsync`, it cannot migrate legacy plaintext, rotate the backup or rewrite durable state merely because startup is deciding whether to show a recovery dialog.
- Cancellation propagates instead of being converted into authorization or an ordinary negative result.
- Added `MemoryRecoveryAvailabilityProbeTests` covering corrupt-primary eligibility, healthy-primary rejection, missing-generation rejection, cancellation, invalid paths, byte-for-byte preservation of both primary and backup during eligibility checks, and absence of temp-file residue.

Files changed in latest run:
- `src/Nvidea.Core/Memory/JsonFileMemoryStore.cs`
- `src/Nvidea.Core/Memory/MemoryRecoveryAvailabilityProbe.cs`
- `src/Nvidea.Windows/App.xaml.cs`
- `tests/Nvidea.Core.Tests/MemoryRecoveryAvailabilityProbeTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected the current WPF startup recovery flow, `JsonFileMemoryStore`, existing JSON-store tests and Core test layout before implementation.
- Recovery eligibility is now independently testable without WPF/MessageBox automation while the actual destructive recovery operation remains in the production store and behind explicit UI consent.
- Eligibility does not read/validate the backup before consent and does not mutate either generation; backup protection-context validation still happens only inside `RecoverLastKnownGoodAsync` after explicit approval.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Browser validation remains fail closed and the release/judge trust chain remains unchanged.
- Memory recovery is explicit, one-generation bounded and protection-context validated. The backup contains the same protected at-rest representation as primary; no plaintext recovery copy is created.
- Recovery eligibility is fail closed and non-mutating. A healthy memory primary cannot authorize rollback for an unrelated startup `InvalidDataException`, and merely checking eligibility cannot rotate/migrate persisted memory.
- The recovery UI defaults to Cancel. Successful recovery requires restart, avoiding reuse of partially initialized services.
- Migration remains local-only; Sensitive/Restricted opt-ins remain explicit. Provider failure/cancellation cannot partially apply an unvalidated migration batch.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release qualification wrapper, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- Persisted-memory, migration, JSON-store/recovery/probe tests and the WPF recovery path still need compile/runtime execution under .NET 8 on Windows.
- `File.Move(..., overwrite: true)` is relied upon as the final same-volume replacement step; crash/power-loss durability semantics depend on host filesystem/OS and need Windows validation.
- Recovery is intentionally one snapshot deep, not a journal/database/user backup. A filesystem failure affecting both sibling files remains unrecoverable here.
- Production recovery assumes the normal default Windows state directory used by `CreateFromEnvironmentAsync()`; test-only/custom state-directory startup is not exposed by the WPF entry point.
- SHA-256 browser receipts are integrity bindings, not signatures; freshness depends on producer host clock.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Execute `MemoryRecoveryAvailabilityProbeTests`, `JsonFileMemoryStoreTests`, `MemoryEmbeddingMigrationTests`, `PersistedMemoryEmbeddingIntegrityTests`, build the WPF host, and exercise the explicit recovery prompt on Windows/.NET 8 as soon as an executable environment is available. Verify Cancel is the default and leaves both files byte-identical, unrelated `InvalidDataException` does not offer memory recovery when the primary validates, wrong DPAPI context cannot restore, successful recovery requires restart, and recovered memory initializes normally. If connector-only execution remains unavailable, next audit startup cancellation/shutdown behavior so cancellation cannot fall through to a misleading generic failure or recovery path. Keep the clean Windows checkout as the required path for browser release qualification and mandatory judge-recording gate.
