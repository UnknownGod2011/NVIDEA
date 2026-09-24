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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, production desktop authenticated-result composition, and MysteryBox-only live signing-key deployment preflight.

## Latest run — wrong-worker production ingestion qualification
Files changed:
- `tests/Nvidea.Core.Tests/AuthenticatedRemoteResearchIngestionBoundaryTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md` completely and inspected the existing production-ingestor adversarial tests before changing code.
- Added a provider-free regression through the real `RemoteResearchResultIngestor` seam for a cryptographically valid result signed by an unpinned worker identity.
- The test pins a different worker public key and deliberately supplies the wrong client result-decryption private key as an ordering sentinel: worker identity verification must fail before unwrap/decrypt can exercise the client key.
- The regression asserts the durable job remains Running with remote provenance exactly Dispatched, `ResultAppliedAt` remains null, and no `research.remote_result_applied` audit authority is emitted.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before each mutation.

Validation/evidence:
- Static inspection plus the existing ingestion architecture show the authenticated result transport is invoked before `ResearchResultProtector.Unprotect`; the new wrong-client-key sentinel makes an accidental decrypt-before-auth reorder observable.
- Actual-ingestor coverage now includes unsigned result, wrong pinned worker identity, and signed remote-job replay; lower-level signature/transport suites continue to cover ciphertext mutation and substitution.
- No live provider, browser, workflow, API key, paid service, secret, or other repository was touched.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- A valid signature from the wrong worker is insufficient: client trust is explicitly anchored to the configured public pin.
- Authentication failure cannot advance durable research state or emit result-applied audit authority.
- The worker verification identity remains public-only client-side; the signing private key remains worker-only and MysteryBox-backed in live deployment.
- Verify-before-decrypt remains the production ordering; existing AEAD confidentiality, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The actual Nebius account still needs a real MysteryBox secret/version containing the worker result-signing private PEM plus a matching client public verification pin; no secret was created or read in this run.
- Actual-ingestor qualification still needs ciphertext/report-receipt substitution; those attacks are currently covered at lower transport/signature boundaries.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Complete ciphertext/report-receipt substitution qualification through the actual production ingestor boundary, then update the deployment/runbook docs with safe RSA signing-key generation, MysteryBox provisioning, rotation and public-pin distribution without ever copying private authority to the client.
