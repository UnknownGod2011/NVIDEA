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
Bound completed research reports and canonical judge evidence into durable SHA-256 receipts and reject tampered/legacy-unbound provenance. Qualified remote-result AEAD against mutation/substitution. Added RSA-PSS/SHA-256 worker-origin signatures over authoritative encrypted-envelope fields, adversarial forgery/key-mismatch/replay/substitution tests, verify-before-decrypt authentication, optional signed transport metadata, worker-side signing after AEAD protection, a dedicated worker-only signing-secret trust boundary, fail-closed deployed worker signing composition, a public-only pinned client worker-verification trust boundary, authenticated encrypted-result transport, and production desktop authenticated-result composition.

## Latest run — live worker result-signing deployment provisioning
Files changed:
- `src/Nvidea.Core/Jobs/NebiusResearchDeploymentPreflight.cs`
- `src/Nvidea.Core/Jobs/NebiusResearchLiveRuntimeFactory.cs`
- `tests/Nvidea.Core.Tests/WorkerResultSigningDeploymentPreflightTests.cs`
- `progress.md`

Completed:
- Re-read `progress.md`, recent commits, deployment preflight, live runtime factory, worker deployment documentation and existing deployment tests before changing code.
- Added the canonical deployment variable `NVIDEA_WORKER_RESULT_SIGNING_PRIVATE_KEY_PEM` to `NebiusResearchDeploymentPreflight`.
- Added a dedicated live-only provisioning gate that rejects plaintext result-signing private authority, requires a Nebius MysteryBox secret reference, and validates secret/version resource IDs without resolving secret contents.
- Wired that gate into `NebiusResearchLiveRuntimeFactory.Create`, so the production remote runtime cannot be constructed if the deployed worker would lack its result-signing identity.
- Kept the new gate separate from fixture-oriented generic topology validation so existing lower-level contract fixtures are not silently redefined as production deployment fixtures.
- Added provider-free regressions for missing signing secret, plaintext leakage, and explicitly version-pinned MysteryBox provisioning.
- Repository identity was explicitly reverified as exactly `UnknownGod2011/NVIDEA` before every mutation.

Validation/evidence:
- Static inspection confirms the live factory now runs both generic deployment preflight and the dedicated worker-result-signing provisioning gate before constructing any remote runtime.
- Static inspection confirms the gate validates references only and never loads/logs the worker private key.
- Existing worker bootstrap already fails closed when the actual signing private key value is absent/invalid; this run closes the earlier deployment-reference omission before Serverless submission.
- No live provider, browser, workflow, API key, paid service, or other repository was touched.
- Executable PASS is not claimed because this connector environment cannot run the .NET 8/Windows suite.

## Security / privacy / failure review
- Worker result-signing private authority is MysteryBox-backed and remains worker-only; client composition retains only the matching public verification identity.
- Plaintext deployment of the result-signing private key is rejected before live runtime construction.
- Remote result authentication remains verify-before-decrypt in desktop production composition; existing AEAD confidentiality, dispatch provenance, bound research receipts, prompt-injection gates, consequential-action approval, cancellation and emergency-stop boundaries remain unchanged.
- Version-pinned MysteryBox references are supported for deterministic demo deployment/rotation.

## Known blockers / risks
- Core/WPF code and accumulated suite still require executable .NET 8 + Windows validation.
- Live Nebius/Tavily/Serverless/authenticated-browser validation remains pending.
- The actual Nebius account still needs a real MysteryBox secret/version containing the worker result-signing private PEM plus a matching client public verification pin; no secret was created or read in this run.
- Production composition boundary still needs provider-free qualification proving missing/forged signatures, wrong pinned keys, cross-job replay and ciphertext/report-receipt substitution fail through the actual runtime/ingestor path, not only through transport primitives.
- Existing completed checkpoints created before bound receipts cannot claim judge-verified research provenance; rerunning research is required.

## Single Best Next Task
Add production-composition/ingestor regressions that exercise a signed remote result end-to-end through the real desktop/runtime seam and prove missing/forged signatures, wrong worker keys, cross-job replay and ciphertext/report-receipt substitution are rejected before result decryption. Then update the deployment/runbook docs to show generation, MysteryBox provisioning, rotation and public-pin distribution for the fourth RSA identity without ever copying private authority to the client.
