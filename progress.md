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

## Latest run — verify-before-decrypt worker boundary
Files changed:
- `src/Nvidea.Core/Jobs/RemoteResearchWorkerSignature.cs`
- `tests/Nvidea.Core.Tests/RemoteResearchWorkerSignatureTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, current remote-result protocol, actual ingestion path, worker signature primitive, and recent commits before implementation.
- Added `AuthenticatedResearchResultProtector.Unprotect`, a narrow migration boundary that always verifies the RSA-PSS worker signature against a separately supplied pinned public key before invoking client-key decryption.
- The boundary deliberately keeps worker identity outside the remote envelope and therefore cannot accept attacker-selected verification keys.
- Added ordering regressions proving a forged worker signature is rejected before an intentionally wrong client private key can be exercised, and a wrong pinned worker identity is rejected before malformed ciphertext can reach base64/decryption parsing.
- Existing v1 worker publication and ingestion behavior remain unchanged; this run does not falsely claim production worker-origin authentication before signing-key provisioning and envelope migration are wired.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms authentication executes before `ResearchResultProtector.Unprotect` and therefore before RSA unwrap, AEAD decrypt, JSON deserialization, or result provenance parsing.
- Existing canonical signature commitment still covers protocol version, opaque work-item id, remote job id, wrapped data key, nonce, ciphertext, AEAD tag, completion timestamp, and expiry.
- New regressions are provider-free and use ephemeral RSA identities only; no credentials/live calls are present.
- Existing v1 result protocol/call sites remain build-compatible because the migration boundary is additive.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Worker verification is now structurally ordered before decryption in the reusable authenticated boundary, reducing exposure of unauthenticated remote bytes to expensive/private-key and parser surfaces once v2 is wired.
- Signature commitment contains only already-encrypted/base64 envelope material and bounded provenance metadata; it does not add raw research payload to logs/judge evidence.
- Domain separation prevents the signature from being reused as authority for another protocol purpose.
- Existing AEAD still provides confidentiality/integrity to the client; worker signatures provide origin authentication only after production worker signing and client pinning are wired.
- The new boundary alone does NOT authenticate current production remote results: v1 `ResearchResultProtector`, worker publication, `RemoteResearchResultIngestor`, live configuration and secret-reference topology have intentionally not yet migrated.
- Existing prompt-injection, consequential-action approval, cancellation, browser lifetime, provider routing and emergency-stop boundaries are unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- New regression source is not executable in this connector environment; compile/runtime compatibility must be confirmed on a .NET 8 runner before release qualification.
- Existing completed checkpoints created before the bound-receipt version intentionally cannot claim judge-verified research provenance; rerunning research is required.
- Production remote-result protocol remains v1 and is not worker-signed yet. Do not claim worker-origin authentication until v2 wiring is complete and pinned verification is enforced at the actual ingestor boundary.

## Single Best Next Task
Migrate the actual remote-result transport to v2 in one controlled slice: extend the protected result envelope with a required worker signature without weakening v1 compatibility assumptions, provision a distinct worker signing private key through worker secret references, pin only its public verification key in validated client configuration, sign after AEAD protection on the worker, make `RemoteResearchResultIngestor` call the verify-before-decrypt boundary, and qualify forged signature/cross-job replay/key mismatch/report-receipt substitution at the actual ingestor boundary. Then update deployment preflight and documentation without exposing either private key.
