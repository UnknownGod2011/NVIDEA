# NVIDEA Hackathon Progress

## Mission and hard boundary
Build a competition-grade open-source Personal AI operating layer for Windows for the Nebius x NVIDIA Global AI Hackathon, targeting Personal AI, Best Use of Tavily, and top-three/Grand Prize quality. WRITE ONLY `UnknownGod2011/NVIDEA`; `UnknownGod2011/keyboard.wtf` is READ-ONLY reference material; never mutate any other repository.

## Current architecture
- .NET 8 Core + WPF Windows host + deployable remote worker.
- NVIDIA Nemotron through Nebius Token Factory; layered privacy-aware memory; Tavily research; safe Playwright browser automation; capability permissions/audit.
- Encrypted Nebius remote research with atomic dispatch trust root, lifecycle/cancellation reconciliation, exact-once result ingestion and Object Storage/Serverless worker transport.
- Judge evidence projects real provider readiness, payload-free durable research lineage, authoritative research provenance, serialized browser-verification state, production-observed session milestones, and a fail-closed recording gate.

## Persistent history
### 2026-09-06 to 2026-09-21 — product foundation and hardening
Implemented Windows shell, Nebius/Nemotron inference, layered memory, Tavily research, permission/audit engine, durable jobs, Playwright browser execution, DPAPI state protection, encrypted remote execution, local voice, deployment/evaluator tooling, crash-consistency hardening, judge-visible runtime evidence, deterministic demo/runbook, submission validation, independent receipts and live-demo readiness tooling. Hardened browser transport, prompt-injection/consequential-action gates, emergency stop, protected browser receipts, durable research lineage and crash-ambiguous reconciliation.

### 2026-09-22 to 2026-09-23 — composition lifetime, browser qualification, citation authority
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, integrated `CompositionLifetimeGate`, and moved browser facades behind root-lifetime authority. Added exactly-one-lease browser goal semantics and qualified shutdown behavior. Added deterministic research citation verification, Verified/Partial/Unverified/NoSources authority, payload-safe `ResearchJudgeEvidence`, evaluator/Windows presentation, and provider-free durable evidence reads.

### 2026-09-24 — durable provenance and remote-result authentication migration
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, production desktop authenticated-result composition, and MysteryBox-only live signing-key deployment preflight. Actual-ingestor adversarial coverage includes unsigned results, wrong pinned worker identity, signed remote-job replay, and post-signature ciphertext/report-payload substitution. Added an operator-grade worker result-signing lifecycle runbook, recording-day signing readiness, and provider-free source-contract guards.

## Latest run — canonical public worker identity projection
Files changed:
- `src/Nvidea.Core/Jobs/WorkerResultVerificationPublicKeyTrust.cs`
- `tests/Nvidea.Core.Tests/WorkerResultVerificationFingerprintTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md` completely and inspected the current repository tree and worker verification trust boundary before implementation.
- Added a single canonical .NET fingerprint primitive for the pinned worker result-verification identity: SHA-256 over canonical SubjectPublicKeyInfo bytes, rendered as 64 uppercase hexadecimal characters.
- Added `LoadRequiredSha256Fingerprint` so Windows/judge composition can obtain only the non-secret identity projection from the existing fail-closed environment trust boundary.
- Fingerprinting first canonicalizes through `ValidateAndCanonicalize`; even if a caller accidentally supplies private PEM, the projected value is derived from public SPKI only and cannot retain signing authority.
- Added provider-free tests proving stable fingerprints across equivalent public/private representations, fingerprint changes across worker identities, fixed fingerprint shape, and absence of PEM/private/MysteryBox material in the projection.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the implementation uses platform `RSA`, `ExportSubjectPublicKeyInfo`, `SHA256.HashData`, and `Convert.ToHexString` only; no provider/network/API dependency was introduced.
- Tests are deterministic except for generating ephemeral RSA identities and contain no production key material or secret references.
- Executable .NET/Windows PASS is not claimed in this connector environment; the new tests require execution on the qualification machine.

## Security / privacy / failure review
- The new projection contains only a one-way public-key fingerprint; PEM, private-key bytes, MysteryBox secret ID/version, credentials, research payloads, and user data are not accepted as output fields.
- Existing RSA >= 2048 validation remains authoritative and runs before fingerprint derivation.
- Existing AEAD confidentiality, verify-before-decrypt worker authentication, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The actual Nebius account still needs a real MysteryBox secret/version containing the worker result-signing private PEM plus a matching client public verification pin.
- The readiness gate still cannot cryptographically prove the opaque deployed private signer matches the client public pin without an end-to-end signed-result canary.
- The new canonical .NET fingerprint is not yet rendered in the Windows Judge Evidence dialog or persisted in a dedicated public-only evidence model; this run intentionally established and tested the reusable trust primitive first.
- Rotation remains single-pin and therefore requires coordinated dispatch maintenance; no multi-key grace ring is assumed.

## Single Best Next Task
Add a dedicated public-only worker-authentication evidence record to the Windows Judge Evidence surface using `LoadRequiredSha256Fingerprint`, with serialization/privacy tests that reject or structurally exclude PEM and MysteryBox references. Then bind that fingerprint to an end-to-end signed-result canary so recording readiness can prove the deployed Nebius signer matches the pinned Windows verifier.
