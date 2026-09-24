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

## Latest run — public worker-authentication judge evidence
Files changed:
- `src/Nvidea.Core/Desktop/WorkerAuthenticationJudgeEvidence.cs`
- `tests/Nvidea.Core.Tests/WorkerAuthenticationJudgeEvidenceTests.cs`
- `progress.md`

Completed:
- Re-read progress completely and inspected the judge evidence, readiness, recording-gate, and worker-verification paths.
- Added a dedicated public-only `WorkerAuthenticationJudgeEvidence` record.
- It accepts only canonical uppercase SHA-256 identity fingerprints and fixes the advertised scheme to RSA-PSS/SHA-256.
- Added a pinned-environment factory that reuses the existing validated public identity projection.
- Added provider-free tests for canonical acceptance, malformed input rejection, serialization shape, and absence of sensitive deployment fields.
- Repository identity was reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the projection carries only a public fingerprint and signature-scheme label.
- Executable .NET/Windows PASS is not claimed in this connector environment.

## Security / privacy / failure review
- Invalid identity fingerprints fail closed.
- The evidence type contains no credentials, deployment references, provider identifiers, research payloads, URLs, source bodies, or user data.
- Existing verify-before-decrypt authentication, encryption, durable receipts, browser safety gates, cancellation, and emergency stop remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The public evidence record is not yet wired into `JudgeEvidenceDialog`.
- Configuration presence still cannot prove the deployed signer matches the pinned public identity without an end-to-end signed-result canary.
- Rotation remains single-pin and requires coordinated dispatch maintenance.

## Single Best Next Task
Wire `WorkerAuthenticationJudgeEvidence.FromPinnedEnvironment()` into the Windows Judge Evidence dialog as a fail-closed public-only panel, then bind it to an end-to-end signed-result canary before cloud recording readiness can report success.
