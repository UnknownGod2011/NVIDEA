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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, production desktop authenticated-result composition, and MysteryBox-only live signing-key deployment preflight. Actual-ingestor adversarial coverage includes unsigned results, wrong pinned worker identity, signed remote-job replay, and post-signature ciphertext/report-payload substitution. Added an operator-grade worker result-signing lifecycle runbook covering RSA-3072 generation, public fingerprinting, version-pinned MysteryBox provisioning, coordinated single-pin rotation, rollback, compromise revocation, and release checks. Added recording-day worker-signing readiness: cloud demo recording fails closed without version-pinned MysteryBox signing configuration and a valid public-only RSA client pin, while exposing only the canonical public SHA-256 fingerprint.

## Latest run — worker-signing readiness regression guard
Files changed:
- `scripts/tests/live-demo-worker-signing-readiness.contract.ps1`
- `progress.md`

Completed:
- Re-read `progress.md` completely, inspected the current live-demo readiness implementation, recent commits, and the repository's existing provider-free PowerShell contract-test conventions before changing anything.
- Added a network/provider-free regression contract for the recording-day worker-origin trust boundary.
- The contract locks in mandatory MysteryBox signing secret ID + immutable version-reference checks, explicit client public-pin loading, private-PEM rejection, RSA parsing, RSA >= 2048 enforcement, canonical SubjectPublicKeyInfo export, and SHA-256 public fingerprint derivation.
- The contract verifies `Test-WorkerResultSigningReadiness` remains inside the `RequireCloudResearch` fail-closed recording path rather than becoming an optional/dead helper.
- Added disclosure guards over readiness `Write-Host` output: source regressions that interpolate the PEM, signing secret ID, or signing secret version into operator output fail the contract; the only intended identity evidence is the derived public fingerprint.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before each mutation.

Validation/evidence:
- Static inspection confirms the new test follows the repository's existing network-free PowerShell contract-test style and does not invoke providers, dotnet, workflows, or paid services.
- The contract targets the exact production readiness source and asserts the security-critical fragments and cloud-path invocation order directly.
- No secret values are embedded in the test; it checks configuration variable names and output source only.
- Executable PASS is not claimed because this connector environment cannot execute PowerShell 7/.NET 8/Windows. The test must be run on the Windows qualification machine before recording.

## Security / privacy / failure review
- The recording machine is required to carry only the worker public verification identity; the worker signing private key remains represented by opaque MysteryBox deployment references.
- Public fingerprint output is intentionally non-secret and useful for out-of-band release/deployment comparison; the PEM itself is not logged by the readiness script.
- Missing secret/version references, missing public pin, private-key leakage into the public-pin variable, malformed RSA, and RSA < 2048 all block readiness; the new source contract guards these requirements against accidental removal.
- Existing AEAD confidentiality, verify-before-decrypt worker authentication, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The actual Nebius account still needs a real MysteryBox secret/version containing the worker result-signing private PEM plus a matching client public verification pin; no secret was created or read in this run.
- The readiness gate can prove that a version-pinned signing-secret reference and a strong client public pin are configured, but without resolving the private secret it cannot cryptographically prove those two identities match. The required end-to-end canary remains the authoritative match proof.
- Rotation currently requires a coordinated dispatch maintenance window because the verifier intentionally pins one worker identity; a multi-key grace ring is not implemented and must not be assumed.
- Exact current Nebius MysteryBox UI/CLI provisioning syntax still needs live official-account verification; documentation deliberately avoids invented commands.
- The new regression is static/provider-free by design; malformed/weak/private/valid RSA runtime behavior still needs executable PowerShell qualification on Windows.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Wire the canonical worker result-signing public fingerprint into the Windows judge-readiness/evidence surface using a public-only projection shared with the recording gate, with tests proving PEM/private material and opaque MysteryBox references can never enter durable judge evidence. Then use that identity in an end-to-end signed-result canary so recording readiness can prove the deployed worker signer actually matches the pinned Windows verifier rather than merely proving both are configured.
