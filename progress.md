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

### 2026-09-22 to 2026-09-23 — composition lifetime and browser-goal qualification
Serialized browser evidence transitions, bound demo validation to production session evidence, added a fail-closed recording gate, integrated `CompositionLifetimeGate` into `NvideaCompositionRoot`, and moved browser product/recovery/goal facades behind root-lifetime authority. Added `IBrowserGoalAgent`, exactly-one-lease transaction semantics, a private least-authority browser-host seam, and an assembly-internal deterministic root factory. Locked public API boundaries and qualified actual-root shutdown behavior for Run, Resume, Approve-and-Continue, and Cancel, including stale-facade fail-closed behavior and authority counters.

### 2026-09-23 — research citation integrity and evaluator authority
Added deterministic citation verification, fail-closed synthesis provenance, Verified/Partial/Unverified/NoSources report state, strict judging authority, payload-safe `ResearchJudgeEvidence`, serialization privacy coverage, production synthesis integration, deterministic evaluator integration, Windows judge presentation, and provider-free durable evidence reads. Mixed legitimate/fabricated markers are explicitly Partial and cannot turn evaluator or Windows research evidence green.

### 2026-09-24 — durable provenance, remote transport integrity, worker-auth foundation
Extended completed research receipts with SHA-256 commitments over the canonical full `ResearchReport` and payload-free `ResearchJudgeEvidence`; provider-free judge reads verify both commitments in fixed time and reject legacy/unbound or tampered checkpoints. Added remote-result AEAD adversarial qualification proving ciphertext mutation and ciphertext/tag substitution cannot alter a completed checkpoint while retaining authenticated provenance. Added a canonical RSA-PSS/SHA-256 worker-origin signature primitive covering every authoritative encrypted-envelope/provenance field, with provider-free adversarial qualification for forged signatures, key mismatch, cross-job replay, and ciphertext/report-receipt substitution. Added a narrow authenticated result boundary that verifies pinned worker identity before client-key decryption or JSON parsing.

## Latest run — signed result envelope transport migration
Files changed:
- `src/Nvidea.Core/Jobs/NebiusResearchResultProtocol.cs`
- `tests/Nvidea.Core.Tests/RemoteResearchWorkerSignatureTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, recent commits, the result protocol, actual `RemoteResearchResultIngestor`, and worker-signature boundary before changing code.
- Extended `ProtectedResearchResultEnvelope` with an optional `WorkerSignature` transport field. The default remains null so existing v1 producers/fixtures stay source-compatible while v2 wiring proceeds.
- Kept `WorkerSignature` outside the canonical signature commitment, avoiding a self-referential signature while still carrying the signature beside the exact encrypted envelope it authenticates.
- Added a provider-free JSON round-trip regression proving a signed envelope retains the signature through the same serialization shape used by directory/Object Storage transports and that the round-tripped encrypted envelope still verifies against the pinned worker identity.
- Deliberately did not switch the actual ingestor to require this field yet: current worker publication does not provision a distinct signing key, so enforcing it now would break live remote research rather than harden it.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the added positional-record parameter is optional and appended, preserving existing constructor call sites.
- The canonical RSA-PSS commitment remains over protocol version, opaque work-item id, remote job id, wrapped key, nonce, ciphertext, AEAD tag, completion time and expiry; signature metadata cannot change any of those fields without invalidating verification.
- New regression uses ephemeral RSA only and no provider/API credentials.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Result transport can now carry worker-origin proof without placing private research payload or signing private-key material in metadata.
- The signature is intentionally not accepted as authority merely because it exists on the envelope; client pinning and verify-before-decrypt enforcement remain mandatory.
- Existing unsigned v1 results remain representable during migration, preventing an accidental availability regression before signing-key provisioning is complete.
- Current production ingestion still calls the legacy decrypt path and therefore does NOT yet authenticate worker origin.
- Existing AEAD confidentiality/integrity, dispatch provenance checks, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- New regression source is not executable in this connector environment; compile/runtime compatibility must be confirmed on a .NET 8 runner before release qualification.
- Existing completed checkpoints created before the bound-receipt version intentionally cannot claim judge-verified research provenance; rerunning research is required.
- Production worker publication does not yet sign result envelopes and the actual ingestor does not yet require/verify `WorkerSignature`. Do not claim worker-origin authentication until both ends and pinned-key configuration are wired.

## Single Best Next Task
Provision a distinct worker result-signing private key through validated worker secret references and expose only its public verification key to client configuration. Then make `NebiusResearchWorker` sign the already-protected envelope into `WorkerSignature`, route `RemoteResearchResultIngestor` through `AuthenticatedResearchResultProtector` with the pinned public key before decryption, and qualify the actual ingestion boundary for missing/forged signatures, wrong pinned key, cross-job replay, and report/receipt ciphertext substitution. Keep unsigned v1 migration behavior explicit and fail closed whenever authenticated mode is configured.
