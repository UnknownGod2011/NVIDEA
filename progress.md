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

### 2026-09-19 to 2026-09-20 — memory embedding integrity
- Persisted embedding data is treated as untrusted durable state: malformed vector/provenance pairs are stripped on load and sanitized state is persisted.
- Persisted embeddings are rejected if empty, dimensionally inconsistent, missing provider/model identity, or containing NaN/Infinity; semantic comparison repeats finite/provenance checks as defense in depth.
- User-authored memory content/provenance survives semantic corruption and lexical/recency/importance retrieval remains available.
- `PersistedMemoryEmbeddingIntegrityTests` cover NaN, Infinity, dimension mismatch, missing vector/provenance, blank provider/model, empty vectors, valid preservation, durable sanitation, and lexical fallback.
- Migration preview/snapshot independently treats malformed semantic state as stale if provider/model identity is blank, dimensions disagree, or any component is non-finite, while initialization sanitation can reduce such records to missing-embedding candidates before preview.
- Migration remains local-provider-only, preserves sensitivity opt-ins, revalidates candidates before embedding, validates returned vectors/provenance, and uses reference-identity checks before applying results so concurrent user edits are not overwritten.
- Added adversarial migration coverage: corrupt Sensitive/Restricted records remain excluded unless their explicit opt-ins are enabled, and NaN/+Infinity/-Infinity returned by a migration provider invalidate the entire affected batch before any record from that batch is persisted.

Files changed in latest run:
- `tests/Nvidea.Core.Tests/MemoryEmbeddingMigrationTests.cs`
- `progress.md`

Validation/evidence:
- Re-read `progress.md` completely, inspected the current migration implementation and existing migration test fixture before changing code.
- Added a configurable test migration provider so adversarial vector output can be injected without weakening production interfaces.
- The fail-closed batch tests assert zero post-initialization writes and no embedding/provenance applied when any vector in a two-record batch is non-finite.
- Sensitivity coverage starts from corrupt persisted semantic state and verifies default exclusion, Sensitive-only opt-in, and explicit Restricted opt-in behavior.
- Repository metadata was explicitly reverified immediately before every GitHub mutation; writable target was exactly `UnknownGod2011/NVIDEA`. No other repository was mutated.
- Connector environment cannot execute .NET 8 or Windows/PowerShell/Chromium, so compile/test/runtime PASS is not claimed.
- No live/paid Nebius, Object Storage, Serverless, Tavily, authenticated browser, Ollama or inference operation was triggered.

## Security / privacy / failure review
- Browser transport, credential-bearing authority rejection, consequential-action approvals, sensitive autonomous-typing blocks, observation suppression, quarantine, prompt-injection boundaries and emergency cancellation remain intact.
- Browser validation remains fail closed and the release/judge trust chain remains unchanged.
- Memory persistence fails soft for corrupt semantic metadata: malformed embeddings are discarded while user-authored memory content/provenance survives and deterministic lexical retrieval remains usable.
- Migration repeats durable-state integrity assumptions rather than relying solely on initialization, reducing the chance that a future import/mutation path silently marks malformed semantic state as current.
- Migration remains local-only and does not introduce cloud disclosure of personal memory content. Adversarial tests now pin Sensitive/Restricted opt-ins even when semantic metadata is corrupt.
- Migration validates every returned vector in a batch before entering the apply/persist critical section; tests now pin the intended no-partial-apply behavior for non-finite provider output.

## Known blockers / risks
- Real-Chromium fixtures still need execution on Windows with .NET 8 and matching Playwright Chromium; static connector work is not an executable PASS.
- Verifier regression harness, release qualification wrapper, release gate, judge-recording wrapper and integrated readiness path each need local PowerShell 7 execution before PASS can be claimed.
- Persisted-memory and migration changes, including the new adversarial tests, still need execution in a .NET 8 environment; compile/runtime success is not claimed from connector-only work.
- SHA-256 browser receipts are integrity bindings, not digital signatures; freshness depends on the producer host clock.
- Blocking Service Workers can affect sites whose auth/product flows depend on workers; judge-path compatibility still needs validation without weakening transport policy.
- Live Nebius Serverless/Object Storage, Windows UX, authenticated Playwright, Tavily, semantic ranking and full readiness remain environment-validation items.

## Single Best Next Task
Execute `MemoryEmbeddingMigrationTests`, `PersistedMemoryEmbeddingIntegrityTests`, and the broader core test suite under .NET 8 as soon as an executable environment is available. If connector-only execution remains unavailable, audit migration cancellation/provider-exception semantics and add focused coverage proving a failure in a later batch cannot corrupt already committed earlier batches and that cancellation never applies an unvalidated batch. Keep the clean Windows checkout as the required path for browser release qualification and the mandatory judge-recording gate.
