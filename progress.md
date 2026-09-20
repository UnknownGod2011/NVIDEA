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

### 2026-09-17 to 2026-09-18 — browser safety and privacy
Hardened browser transport to HTTPS or HTTP loopback and WSS or WS loopback; rejected embedded URI credentials; added request/WebSocket routing, Service Worker blocking, post-action location enforcement, download quarantine, credential/prompt-injection/consequential-action gates, authenticated-state restart coverage, redirect/WebSocket no-dispatch fixtures, emergency-stop coverage and canonical page admission. Expanded credential classification across passwords, OTP, PIN, recovery material, API/access/refresh tokens, identity-number labels and standard security/payment autocomplete tokens. Production snapshots suppress password/OTP/payment values before reading DOM values. Hermetic real-Chromium coverage includes sensitive fields, benign controls, accessibility-reference safety and SPA in-place field repurposing.

### 2026-09-18 to 2026-09-19 — executable browser qualification
Added `scripts/run-browser-integration.ps1`: .NET 8 enforcement, restore/build, project-pinned Playwright Chromium install, process-scoped integration opt-in, curated security suite, deterministic failure propagation, fail-closed TRX validation, and payload-free schema-v2 qualification receipts bound to exact TRX bytes with SHA-256. Added independent verifier with canonical fixture pinning, exact receipt/TRX agreement, source provenance, clean-source policy, strict schema/type checks, timestamp validation and freshness support. Added hermetic verifier regression coverage for canonical evidence plus malformed provenance/type/suite/lookalike/TRX-tampering/time cases.

### 2026-09-19 — release and judge recording trust chain
Added release browser verification pinned to the expected GitHub origin, exact clean HEAD, canonical suite and fresh evidence. Integrated qualification into live-demo readiness. Added `judge-recording-gate.ps1`: Windows/PowerShell 7, mandatory browser evidence, verifier self-test, cloud-research readiness and build validation are required. Diagnostic skip switches can run troubleshooting checks but can never mint a recording PASS. Added `run-release-browser-qualification.ps1` so wrong-repository, ambiguous-HEAD, or dirty-source states fail before expensive Chromium execution. Producer-side qualification rechecks exact HEAD and complete tracked/untracked cleanliness after Chromium succeeds, so source drift during the run invalidates evidence.

### 2026-09-19 to 2026-09-20 — memory embedding and persistence integrity
- Persisted embedding data is treated as untrusted durable state: malformed vector/provenance pairs are stripped on load and sanitized state is persisted.
- Persisted embeddings are rejected if empty, dimensionally inconsistent, missing provider/model identity, or containing NaN/Infinity; semantic comparison repeats finite/provenance checks as defense in depth.
- User-authored memory content/provenance survives semantic corruption and lexical/recency/importance retrieval remains available.
- `PersistedMemoryEmbeddingIntegrityTests` cover corrupt semantic state, valid preservation, durable sanitation, and lexical fallback.
- Migration independently treats malformed semantic state as stale, remains local-provider-only, preserves sensitivity opt-ins, revalidates candidates before embedding, validates returned vectors/provenance, and uses reference-identity checks so concurrent user edits are not overwritten.
- Adversarial migration coverage pins Sensitive/Restricted opt-ins, rejects non-finite provider vectors before batch persistence, and proves later-batch provider failure/cancellation preserves earlier committed work without partially applying the active batch.
- JSON memory persistence serializes access and uses same-directory write-through temp files before replacement. Tests pin replacement, malformed/truncated fail-closed reads, cancellation preservation and non-interleaved concurrent snapshots.
- Added a bounded `.bak` last-known-good generation. Before replacing a valid current primary, its exact persisted bytes are atomically copied to the backup; malformed current bytes can never displace an existing known-good generation. Normal reads never silently fall back. Recovery requires explicit `RecoverLastKnownGoodAsync` intent, validates/decrypts the backup with the same protection context before replacement, then restores the exact persisted generation.
- Recovery tests pin explicit-only fallback, previous-generation restoration, missing-backup failure without primary mutation, corrupt-primary backup preservation, and temp cleanup.
- Protected recovery coverage now pins the confidentiality and context-binding contract: both current and `.bak` snapshots retain the protected envelope and do not contain memory content as plaintext; a mismatched protection context fails backup validation before replacement and leaves both primary and backup byte-for-byte unchanged.

Files changed in latest run:
- `tests/Nvidea.Core.Tests/JsonFileMemoryStoreTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely and inspected `JsonFileMemoryStore`, `LocalStateEnvelope`, existing JSON-store tests, and the repository's established `ILocalStateProtector` test pattern before changing code.
- Added `ProtectedSnapshots_PrimaryAndBackupNeverPersistPlaintextContent`, which asserts both generations carry the protected-envelope header, neither leaks its generation's memory content in persisted UTF-8 bytes, and the current snapshot still round-trips through the configured protector.
- Added `RecoverLastKnownGoodAsync_ProtectionContextMismatchPreservesPrimary`, using a deterministic context-bound test protector that throws `CryptographicException` on context mismatch so the production envelope converts the mismatch into its fail-closed `InvalidDataException` boundary.
- The mismatch test captures both files before recovery and asserts the primary and backup are byte-for-byte unchanged after failure, with no recovery temp-file residue.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Browser validation remains fail closed and the release/judge trust chain remains unchanged.
- Memory persistence fails soft for corrupt semantic metadata while malformed/truncated primary JSON itself fails closed; recovery is an explicit operation rather than an implicit fallback.
- The backup contains the same at-rest representation as the primary. Protected-store regression coverage now explicitly guards against accidental plaintext backup regressions.
- Corrupt or protection-incompatible current state is never promoted to last-known-good. Invalid or wrong-context backup state is validated before replacement, preserving the primary on recovery failure.
- Migration remains local-only and does not introduce cloud disclosure of personal memory content. Sensitive/Restricted opt-ins remain explicit even when semantic metadata is corrupt.
- Migration batch validation and interruption semantics remain intact: already committed batches remain resumable durable progress, while provider failure/cancellation cannot partially apply an unvalidated batch.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release qualification wrapper, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- Persisted-memory, migration and JSON-store/recovery tests still need execution in a .NET 8 environment; compile/runtime success is not claimed from connector-only work.
- `File.Move(..., overwrite: true)` is relied upon as the final same-volume replacement step; crash/power-loss durability semantics still depend on the host filesystem/OS and need Windows validation.
- The recovery generation is intentionally one snapshot deep. It protects against a corrupt current destination but is not a journal, transactional database, or substitute for user backups. A filesystem failure affecting both sibling files remains unrecoverable here.
- Recovery is currently a core API; Windows UX should expose it only with clear corruption/provenance messaging and explicit confirmation rather than automatically invoking it.
- The deterministic test protector is only a contract fixture, not production cryptography; Windows DPAPI behavior still requires Windows execution coverage.
- SHA-256 browser receipts are integrity bindings, not digital signatures; freshness depends on the producer host clock.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Execute `JsonFileMemoryStoreTests`, `MemoryEmbeddingMigrationTests`, `PersistedMemoryEmbeddingIntegrityTests`, and the broader core test suite under .NET 8 as soon as an executable environment is available. If connector-only execution remains unavailable, audit the Windows memory-management UX and add an explicit recovery surface that clearly reports corruption, identifies that recovery rolls back one generation, requires deliberate confirmation, and never auto-recovers; keep recovery details payload-free in audit/UI telemetry. Keep the clean Windows checkout as the required path for browser release qualification and the mandatory judge-recording gate.
