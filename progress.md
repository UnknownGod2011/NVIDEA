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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, and fail-closed deployed worker signing composition.

## Latest run — pinned client worker-verification trust boundary
Files changed:
- `src/Nvidea.Core/Jobs/WorkerResultVerificationPublicKeyTrust.cs`
- `tests/Nvidea.Core.Tests/WorkerResultVerificationPublicKeyTrustTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, recent commits, actual `RemoteResearchResultIngestor`, worker signature primitive, worker signing-secret trust code, and runtime configuration before changing code.
- Added `WorkerResultVerificationPublicKeyTrust`, a dedicated client-side pin for `NVIDEA_WORKER_RESULT_VERIFICATION_PUBLIC_KEY_PEM`.
- The trust boundary fails closed when the pin is missing or malformed, requires RSA >= 2048 bits, and canonicalizes retained material to SubjectPublicKeyInfo public-key PEM.
- If private key material is accidentally supplied to the client trust boundary, it is deliberately stripped to public parameters before the value can be retained by composition. This prevents the desktop/client from inheriting worker signing authority through configuration error.
- Added provider-free regressions for dedicated setting isolation, missing pin, accidental private-material stripping, and undersized RSA rejection.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms `RemoteResearchWorkerSignature.Verify` already accepts a separately supplied pinned worker public key and `AuthenticatedResearchResultProtector.Unprotect` verifies that signature before invoking client-key decryption/parsing.
- Static inspection also confirms the production `RemoteResearchResultIngestor` still calls legacy `ResearchResultProtector.Unprotect`; this run therefore does not claim end-to-end worker-origin enforcement.
- New tests are provider-free and exercise only local RSA/configuration behavior; no live provider, browser, workflow, API key, or paid service was invoked.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Worker signing private authority and client verification authority now have explicit, separately named trust boundaries.
- The client boundary retains public material only, reducing blast radius if desktop configuration/state is disclosed.
- The verification key remains trusted local configuration and is never selected from the untrusted result envelope.
- Production client ingestion still calls legacy `ResearchResultProtector.Unprotect` and therefore does NOT yet enforce worker origin. End-to-end worker authentication must not be claimed yet.
- Existing AEAD confidentiality/integrity, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- Deployment manifests/secret provisioning must supply the worker signing private key and the matching client verification public key through their distinct configuration boundaries.
- `RemoteResearchResultIngestor` still uses the legacy decrypt path; the new client trust boundary is not yet wired into production ingestion.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Migrate `RemoteResearchResultIngestor` composition to require the canonicalized pinned worker verification public key and call `AuthenticatedResearchResultProtector.Unprotect(envelope, envelope.WorkerSignature, pinnedKey, clientPrivateKey, now)` before any decryption/parsing. Qualify the actual ingestor boundary for missing/forged signatures, wrong pinned key, cross-job replay, and ciphertext/report-receipt substitution. Keep any legacy unsigned ingestion behind an explicit migration mode and never allow it to produce authenticated/judge authority.
