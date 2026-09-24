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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, production desktop authenticated-result composition, and MysteryBox-only live signing-key deployment preflight. Actual-ingestor adversarial coverage includes unsigned results, wrong pinned worker identity, signed remote-job replay, and post-signature ciphertext/report-payload substitution. Added an operator-grade worker result-signing lifecycle runbook covering RSA-3072 generation, public fingerprinting, version-pinned MysteryBox provisioning, coordinated single-pin rotation, rollback, compromise revocation, and release checks.

## Latest run — recording-day worker signing readiness
Files changed:
- `scripts/live-demo-readiness.ps1`
- `progress.md`

Completed:
- Re-read `progress.md` completely and inspected the existing recording gate, live-demo readiness script, worker deployment documentation, and result-signing lifecycle contract before changing anything.
- Added a first-class `Test-WorkerResultSigningReadiness` check to the live-demo readiness path used by the judge recording gate whenever cloud research is required.
- Recording readiness now fails closed unless both worker result-signing MysteryBox secret ID and immutable version-reference configuration are present.
- Recording readiness now fails closed unless `NVIDEA_WORKER_RESULT_VERIFICATION_PUBLIC_KEY_PEM` is present and imports as a valid RSA public identity of at least 2048 bits.
- Explicitly rejects PEM text containing private-key material on the recording/client side instead of allowing accidental private signing authority to enter demo configuration.
- Canonicalizes the imported public identity to SubjectPublicKeyInfo in memory and prints only its SHA-256 fingerprint as judge/operator-visible readiness evidence; it never prints the PEM, private material, secret ID, or secret version value.
- The existing `judge-recording-gate.ps1` already invokes `live-demo-readiness.ps1 -RequireCloudResearch`, so this security check becomes mandatory for a green recording gate without adding a bypass path.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the recording gate always passes `-RequireCloudResearch` to live-demo readiness for a non-diagnostic PASS, so worker signing readiness is now in the mandatory recording path.
- The readiness implementation uses .NET `RSA.ImportFromPem`, enforces RSA >= 2048, exports public-only SubjectPublicKeyInfo, hashes that public DER with SHA-256, and emits only the resulting fingerprint.
- Private-key PEM markers are rejected before import/fingerprinting; MysteryBox secret/version values are checked only for presence and never emitted.
- Existing diagnostic skip switches still cannot produce a recording PASS.
- No private key, API key, MysteryBox secret, provider account, browser, workflow, paid service, or other repository was accessed or mutated.
- Executable PASS is not claimed because this connector environment cannot run the PowerShell 7/.NET 8/Windows suite.

## Security / privacy / failure review
- The recording machine is required to carry only the worker public verification identity; the worker signing private key remains represented by opaque MysteryBox deployment references.
- Public fingerprint output is intentionally non-secret and useful for out-of-band release/deployment comparison; the PEM itself is not logged by the readiness script.
- Missing secret/version references, missing public pin, private-key leakage into the public-pin variable, malformed RSA, and RSA < 2048 all block readiness.
- Existing AEAD confidentiality, verify-before-decrypt worker authentication, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The actual Nebius account still needs a real MysteryBox secret/version containing the worker result-signing private PEM plus a matching client public verification pin; no secret was created or read in this run.
- The readiness gate can prove that a version-pinned signing-secret reference and a strong client public pin are configured, but without resolving the private secret it cannot cryptographically prove those two identities match. The required end-to-end canary remains the authoritative match proof.
- Rotation currently requires a coordinated dispatch maintenance window because the verifier intentionally pins one worker identity; a multi-key grace ring is not implemented and must not be assumed.
- Exact current Nebius MysteryBox UI/CLI provisioning syntax still needs live official-account verification; documentation deliberately avoids invented commands.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Add provider-free regression coverage for the new recording-day signing readiness gate: exercise missing secret/version references, missing public pin, malformed/weak RSA, accidental private PEM, and a valid public-only RSA identity, while asserting that output never contains PEM/private material or opaque secret-reference values. Then wire the public fingerprint into the Windows judge-readiness surface without exposing configuration secrets.
