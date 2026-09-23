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
Extended completed research receipts with SHA-256 commitments over the canonical full `ResearchReport` and payload-free `ResearchJudgeEvidence`; provider-free judge reads verify both commitments in fixed time and reject legacy/unbound or tampered checkpoints. Added remote-result AEAD adversarial qualification proving ciphertext mutation and ciphertext/tag substitution cannot alter a completed checkpoint while retaining authenticated provenance. Added a canonical RSA-PSS/SHA-256 worker-origin signature primitive covering every authoritative encrypted-envelope/provenance field, with provider-free adversarial qualification for forged signatures, key mismatch, cross-job replay, and ciphertext/report-receipt substitution.

## Latest run — worker-origin signature foundation
Files changed:
- `src/Nvidea.Core/Jobs/RemoteResearchWorkerSignature.cs`
- `tests/Nvidea.Core.Tests/RemoteResearchWorkerSignatureTests.cs`
- `progress.md`

Completed:
- Re-read the existing remote result protocol and live composition boundary before changing cryptographic authority.
- Added `RemoteResearchWorkerSignature`, a domain-separated RSA-PSS/SHA-256 signing/verification primitive over protocol version, opaque work-item id, remote-job id, wrapped data key, nonce, ciphertext, AEAD tag, completion timestamp, and expiry.
- Verification requires an explicitly supplied pinned worker public key; the verification key is not accepted from the result envelope, preventing attacker-selected identity from satisfying the trust check.
- Added adversarial regressions for forged signature bytes, wrong pinned worker identity, cross-job replay, and ciphertext/report-receipt substitution.
- During implementation review, deliberately reverted an initial direct v2 protocol rewrite because it would have changed constructor/call-site contracts before the live deployment/configuration boundary had been safely wired. The existing v1 transport therefore remains behaviorally unchanged and build-compatible while the independently qualified signature primitive lands first.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the signature commitment covers all result-envelope fields that currently carry encrypted payload or provenance authority.
- RSA keys below 2048 bits are rejected; signing requires private-key material; verification requires a separately supplied public key.
- Regression fixtures are provider-free and contain no credentials/live calls.
- Existing v1 result protocol and its call sites were preserved rather than leaving a partially wired breaking migration.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Signature commitment contains only already-encrypted/base64 envelope material and bounded provenance metadata; it does not add raw research payload to logs/judge evidence.
- Domain separation prevents the signature from being reused as authority for another protocol purpose.
- Existing AEAD still provides confidentiality/integrity to the client; the new primitive establishes the missing cryptographic mechanism for worker identity once wired to the result envelope and pinned deployment configuration.
- The primitive alone does NOT yet authenticate production remote results: v1 `ResearchResultProtector`, worker publication, client ingestion, live configuration and secret-reference topology have intentionally not been migrated in this run.
- Existing prompt-injection, consequential-action approval, cancellation, browser lifetime, provider routing and emergency-stop boundaries are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- New regression source is not executable in this connector environment; compile/runtime compatibility must be confirmed on a .NET 8 runner before release qualification.
- Existing completed checkpoints created before the bound-receipt version intentionally cannot claim judge-verified research provenance; rerunning research is required.
- Production remote-result protocol remains v1 and is not worker-signed yet. Do not claim worker-origin authentication until v2 wiring is complete and pinned verification is enforced before decryption/ingestion.

## Single Best Next Task
Safely wire `RemoteResearchWorkerSignature` into a v2 protected-result envelope end-to-end without breaking live composition: add the signature field/version, provision a distinct worker signing key through worker secret references, pin only its public verification key on the client via validated live configuration, sign after AEAD protection on the worker, verify before decryption/ingestion, update factory/composition/deployment preflight and all call sites, then qualify migration/fail-closed behavior plus forged signature, cross-job replay, key mismatch, and report/receipt substitution at the actual ingestor boundary.
